using System.Runtime.CompilerServices;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Diagnostics;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Data;

/// <summary>
/// Deja constancia en la bitácora de cada alta, cambio y baja de niños, grupos, actividades,
/// visitas y cuentas. Al engancharse a EF Core ningún servicio tiene que acordarse de llamarlo.
///
/// Funciona en dos tiempos: antes de guardar anota qué cambió (los Id de los registros nuevos
/// todavía no existen) y después de guardar escribe la bitácora con los Id ya asignados.
/// La asistencia no se audita aquí: ya guarda quién la registró, y pasar lista genera un
/// registro por niño, lo que ahogaría la bitácora.
/// </summary>
public class AuditoriaInterceptor : SaveChangesInterceptor
{
    private sealed record Pendiente(
        EntityEntry Entrada, string Entidad, string Accion, string Descripcion, string? Cambios, int? Id, int? IdDelPadre);

    // Lo anotado en "antes de guardar" espera aquí, por contexto, hasta "después de guardar".
    private static readonly ConditionalWeakTable<DbContext, List<Pendiente>> Anotados = new();

    private readonly IHttpContextAccessor _accesor;
    private readonly ILogger<AuditoriaInterceptor> _registro;

    public AuditoriaInterceptor(IHttpContextAccessor accesor, ILogger<AuditoriaInterceptor> registro)
    {
        _accesor = accesor;
        _registro = registro;
    }

    //ANTES DE GUARDAR

    public override InterceptionResult<int> SavingChanges(DbContextEventData eventData, InterceptionResult<int> result)
    {
        Anotar(eventData.Context);
        return result;
    }

    public override ValueTask<InterceptionResult<int>> SavingChangesAsync(
        DbContextEventData eventData, InterceptionResult<int> result, CancellationToken cancellationToken = default)
    {
        Anotar(eventData.Context);
        return ValueTask.FromResult(result);
    }

    //DESPUÉS DE GUARDAR

    public override int SavedChanges(SaveChangesCompletedEventData eventData, int result)
    {
        var filas = PrepararFilas(eventData.Context);

        if (filas.Count > 0)
        {
            try
            {
                eventData.Context!.Set<Bitacora>().AddRange(filas);
                eventData.Context.SaveChanges();
            }
            catch (Exception excepcion)
            {
                Descartar(eventData.Context!, filas, excepcion);
            }
        }

        return result;
    }

    public override async ValueTask<int> SavedChangesAsync(
        SaveChangesCompletedEventData eventData, int result, CancellationToken cancellationToken = default)
    {
        var filas = PrepararFilas(eventData.Context);

        if (filas.Count > 0)
        {
            try
            {
                eventData.Context!.Set<Bitacora>().AddRange(filas);
                await eventData.Context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception excepcion)
            {
                Descartar(eventData.Context!, filas, excepcion);
            }
        }

        return result;
    }

    public override void SaveChangesFailed(DbContextErrorEventData eventData)
    {
        if (eventData.Context != null) Anotados.Remove(eventData.Context);
    }

    public override Task SaveChangesFailedAsync(DbContextErrorEventData eventData, CancellationToken cancellationToken = default)
    {
        if (eventData.Context != null) Anotados.Remove(eventData.Context);
        return Task.CompletedTask;
    }

    //LÓGICA

    private void Anotar(DbContext? contexto)
    {
        if (contexto == null) return;

        contexto.ChangeTracker.DetectChanges();

        var lista = new List<Pendiente>();

        foreach (EntityEntry entrada in contexto.ChangeTracker.Entries())
        {
            if (entrada.State is not (EntityState.Added or EntityState.Modified or EntityState.Deleted)) continue;

            var descripcion = Describir(entrada.Entity);
            if (descripcion == null) continue;

            var (entidad, texto, idDelPadre) = descripcion.Value;

            string accion;
            string? cambios = null;

            switch (entrada.State)
            {
                case EntityState.Added:
                    accion = Bitacora.AccionCreado;
                    break;
                case EntityState.Deleted:
                    accion = Bitacora.AccionEliminado;
                    break;
                default:
                    accion = Bitacora.AccionModificado;
                    cambios = CalcularCambios(entrada);

                    // Un guardado que no cambió ningún valor no merece un renglón.
                    if (cambios == null) continue;
                    break;
            }

            // Los registros nuevos aún no tienen Id definitivo: se lee después de guardar.
            int? id = entrada.State == EntityState.Added ? null : LeerId(entrada);

            lista.Add(new Pendiente(entrada, entidad, accion, texto, cambios, id, idDelPadre));
        }

        if (lista.Count > 0)
        {
            Anotados.AddOrUpdate(contexto, lista);
        }
        else
        {
            Anotados.Remove(contexto);
        }
    }

    private List<Bitacora> PrepararFilas(DbContext? contexto)
    {
        if (contexto == null || !Anotados.TryGetValue(contexto, out var lista))
        {
            return [];
        }

        // Se retira antes de guardar la bitácora: ese guardado vuelve a pasar por el interceptor.
        Anotados.Remove(contexto);

        var (perfilId, usuario) = QuienActua();
        DateTime ahora = DateTime.UtcNow;

        return lista.Select(p => new Bitacora
        {
            Fecha = ahora,
            PerfilId = perfilId,
            Usuario = usuario,
            Entidad = p.Entidad,
            EntidadId = p.IdDelPadre ?? p.Id ?? LeerId(p.Entrada),
            Accion = p.Accion,
            Descripcion = Recortar(p.Descripcion, 500),
            Cambios = p.Cambios
        }).ToList();
    }

    private void Descartar(DbContext contexto, List<Bitacora> filas, Exception excepcion)
    {
        // El cambio de verdad ya se guardó: una falla de la bitácora no debe deshacerlo ni
        // dejar sus renglones pendientes para el siguiente guardado.
        foreach (Bitacora fila in filas)
        {
            contexto.Entry(fila).State = EntityState.Detached;
        }

        _registro.LogError(excepcion, "No se pudo escribir la bitácora de auditoría.");
    }

    private (int? PerfilId, string Usuario) QuienActua()
    {
        var usuario = _accesor.HttpContext?.User;

        if (usuario?.Identity?.IsAuthenticated != true)
        {
            return (null, "Sistema");
        }

        int? perfilId = int.TryParse(usuario.FindFirst("PerfilId")?.Value, out int id) ? id : null;
        string nombre = $"{usuario.FindFirst("Nombre")?.Value} {usuario.FindFirst("Apellido")?.Value}".Trim();

        return (perfilId, nombre == "" ? "Sistema" : nombre);
    }

    /// <summary>Qué entidad es, cómo reconocerla y, si es una pieza de otra, el Id de esa otra.</summary>
    private static (string Entidad, string Descripcion, int? IdDelPadre)? Describir(object entidad)
    {
        return entidad switch
        {
            Nino n => ("Nino", $"{n.Nombres} {n.Apellidos} ({n.Codigo})", null),
            Grupo g => ("Grupo", g.Nombre, null),
            Actividad a => ("Actividad", a.Nombre, null),
            Visita v => ("Visita", $"Visita del {v.Fecha_Visita:dd/MM/yyyy}", null),
            Perfil p => ("Usuario", $"{p.Nombre} {p.Apellido}", null),

            // Quién está a cargo de un grupo se registra como un cambio del propio grupo.
            Grupo_Maestro gm => ("Grupo", $"Maestro a cargo (perfil {gm.PerfilId})", gm.GrupoId),
            _ => null
        };
    }

    private static string? CalcularCambios(EntityEntry entrada)
    {
        var cambios = new Dictionary<string, object?>();

        foreach (PropertyEntry propiedad in entrada.Properties)
        {
            if (!propiedad.IsModified || propiedad.Metadata.IsPrimaryKey()) continue;

            // Update() marca todo como modificado: solo cuenta lo que de verdad cambió de valor.
            if (Equals(propiedad.OriginalValue, propiedad.CurrentValue)) continue;

            cambios[propiedad.Metadata.Name] = new { antes = propiedad.OriginalValue, despues = propiedad.CurrentValue };
        }

        return cambios.Count == 0 ? null : JsonSerializer.Serialize(cambios);
    }

    private static int? LeerId(EntityEntry entrada)
    {
        var propiedad = entrada.Metadata.FindProperty("Id");

        return propiedad == null ? null : entrada.Property("Id").CurrentValue as int?;
    }

    private static string Recortar(string texto, int maximo)
    {
        return texto.Length <= maximo ? texto : texto[..maximo];
    }
}
