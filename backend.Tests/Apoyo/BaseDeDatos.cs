using System.Security.Claims;
using AutoMapper;
using Microsoft.AspNetCore.Http;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.Mapper;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Tests.Apoyo;

/// <summary>
/// Base de datos SQLite en memoria con el modelo real del sistema, y atajos para armar
/// los datos de cada prueba. Cada instancia es una base nueva y vacía.
/// </summary>
public sealed class BaseDeDatos : IDisposable
{
    private readonly SqliteConnection _conexion;
    private int _siguienteUsuario;

    public AppProyectoContext Context { get; }

    /// <summary>
    /// conAuditoria activa el interceptor de la bitácora, como en la aplicación real.
    /// usuario es quien "hace la petición"; null simula un proceso sin sesión (la siembra inicial).
    /// </summary>
    public BaseDeDatos(bool conAuditoria = false, ClaimsPrincipal? usuario = null)
    {
        _conexion = new SqliteConnection("DataSource=:memory:");
        _conexion.Open();

        var constructor = new DbContextOptionsBuilder<AppProyectoContext>().UseSqlite(_conexion);

        if (conAuditoria)
        {
            var accesor = new HttpContextAccessor { HttpContext = new DefaultHttpContext { User = usuario ?? new ClaimsPrincipal() } };
            constructor.AddInterceptors(new AuditoriaInterceptor(accesor, NullLogger<AuditoriaInterceptor>.Instance));
        }

        Context = new AppProyectoContext(constructor.Options);
        Context.Database.EnsureCreated();
    }

    public static IMapper CrearMapper()
    {
        var configuracion = new MapperConfiguration(
            cfg => cfg.AddProfile<MappingProfile>(), NullLoggerFactory.Instance);

        return configuracion.CreateMapper();
    }

    /// <summary>Una persona con sesión iniciada, con los mismos claims que lleva el JWT real.</summary>
    public static ClaimsPrincipal UsuarioAutenticado(int perfilId, string nombre, string apellido)
    {
        var identidad = new ClaimsIdentity(
            [new Claim("PerfilId", perfilId.ToString()), new Claim("Nombre", nombre), new Claim("Apellido", apellido)],
            authenticationType: "prueba");

        return new ClaimsPrincipal(identidad);
    }

    public Perfil CrearPerfil(string nombre, bool activo = true)
    {
        var perfil = new Perfil
        {
            UsuarioId = $"usuario-{++_siguienteUsuario}",
            Nombre = nombre,
            Apellido = "Prueba",
            Activo = activo
        };

        Context.Perfiles.Add(perfil);
        Context.SaveChanges();
        return perfil;
    }

    public Grupo CrearGrupo(string nombre, params Perfil[] maestros)
    {
        var grupo = new Grupo { Nombre = nombre };
        grupo.Grupo_Maestros.AddRange(maestros.Select(m => new Grupo_Maestro { Perfil = m }));

        Context.Grupos.Add(grupo);
        Context.SaveChanges();
        return grupo;
    }

    public Nino CrearNino(Grupo grupo, string codigo, bool activo = true)
    {
        var nino = new Nino
        {
            Codigo = codigo,
            Nombres = "Nombre" + codigo,
            Apellidos = "Apellido" + codigo,
            GrupoId = grupo.Id,
            Activo = activo
        };

        Context.Ninos.Add(nino);
        Context.SaveChanges();
        return nino;
    }

    public Actividad CrearActividad(Perfil creador, DateOnly fecha, params Nino[] participantes)
    {
        var actividad = new Actividad
        {
            Nombre = "Actividad de prueba",
            Tipo = Actividad.TipoNormal,
            Fecha_Actividad = fecha,
            CreadoPorId = creador.Id
        };
        actividad.Actividad_Ninos.AddRange(participantes.Select(n => new Actividad_Nino { NinoId = n.Id }));

        Context.Actividades.Add(actividad);
        Context.SaveChanges();
        return actividad;
    }

    public Asistencia CrearAsistencia(Actividad actividad, Nino nino, DateOnly fecha, string estado, Perfil registradoPor)
    {
        var asistencia = new Asistencia
        {
            ActividadId = actividad.Id,
            NinoId = nino.Id,
            Fecha = fecha,
            Estado = estado,
            RegistradoPorId = registradoPor.Id
        };

        Context.Asistencias.Add(asistencia);
        Context.SaveChanges();
        return asistencia;
    }

    /// <summary>Vacía la bitácora, para que la prueba cuente solo lo que ocurre después del armado.</summary>
    public void LimpiarBitacora()
    {
        Context.Bitacoras.ExecuteDelete();
        Context.ChangeTracker.Clear();
    }

    public void Dispose()
    {
        Context.Dispose();
        _conexion.Dispose();
    }
}
