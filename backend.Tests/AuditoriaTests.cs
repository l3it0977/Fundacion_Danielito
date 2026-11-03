using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

/// <summary>La bitácora debe registrar lo importante, quién lo hizo, y nada más.</summary>
public class AuditoriaTests : IDisposable
{
    private readonly BaseDeDatos _bd = new(
        conAuditoria: true,
        usuario: BaseDeDatos.UsuarioAutenticado(7, "Dunia", "Nostas"));

    public void Dispose() => _bd.Dispose();

    private List<Bitacora> Renglones() => _bd.Context.Bitacoras.AsNoTracking().OrderBy(x => x.Id).ToList();

    private Nino NinoExistente()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        var nino = _bd.CrearNino(grupo, "N-1");
        _bd.LimpiarBitacora();

        return _bd.Context.Ninos.First(x => x.Id == nino.Id);
    }

    [Fact]
    public async Task CrearUnNino_RegistraAltaConSuIdYQuienLoHizo()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        _bd.LimpiarBitacora();

        var nino = new Nino { Codigo = "N-9", Nombres = "Ana", Apellidos = "Pérez", GrupoId = grupo.Id };
        _bd.Context.Ninos.Add(nino);
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Equal("Nino", renglon.Entidad);
        Assert.Equal(Bitacora.AccionCreado, renglon.Accion);
        Assert.Equal(nino.Id, renglon.EntidadId);
        Assert.NotEqual(0, renglon.EntidadId);
        Assert.Equal("Ana Pérez (N-9)", renglon.Descripcion);
        Assert.Equal("Dunia Nostas", renglon.Usuario);
        Assert.Equal(7, renglon.PerfilId);
        Assert.Equal(DateTimeKind.Utc, DateTime.SpecifyKind(renglon.Fecha, DateTimeKind.Utc).Kind);
        Assert.True((DateTime.UtcNow - renglon.Fecha).TotalMinutes < 1);
    }

    [Fact]
    public async Task ModificarUnNino_GuardaSoloLosCamposQueCambiaron()
    {
        var nino = NinoExistente();

        nino.Nombres = "Anita";
        nino.Contacto = "70000000";
        // Igual que hacen los servicios: Update() marca todas las propiedades como modificadas.
        _bd.Context.Ninos.Update(nino);
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Equal(Bitacora.AccionModificado, renglon.Accion);

        using var json = JsonDocument.Parse(renglon.Cambios!);
        var campos = json.RootElement.EnumerateObject().Select(x => x.Name).OrderBy(x => x).ToArray();

        Assert.Equal(["Contacto", "Nombres"], campos);
        Assert.Equal("NombreN-1", json.RootElement.GetProperty("Nombres").GetProperty("antes").GetString());
        Assert.Equal("Anita", json.RootElement.GetProperty("Nombres").GetProperty("despues").GetString());
        Assert.Equal(JsonValueKind.Null, json.RootElement.GetProperty("Contacto").GetProperty("antes").ValueKind);
    }

    [Fact]
    public async Task GuardarSinCambiarNingunValor_NoDejaRenglon()
    {
        var nino = NinoExistente();

        _bd.Context.Ninos.Update(nino);
        await _bd.Context.SaveChangesAsync();

        Assert.Empty(Renglones());
    }

    [Fact]
    public async Task EliminarUnNino_RegistraLaBajaConSuId()
    {
        var nino = NinoExistente();
        int id = nino.Id;

        _bd.Context.Ninos.Remove(nino);
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Equal(Bitacora.AccionEliminado, renglon.Accion);
        Assert.Equal(id, renglon.EntidadId);
    }

    [Fact]
    public async Task DesactivarUnNino_PorElServicio_QuedaRegistrado()
    {
        var nino = NinoExistente();
        var servicio = new NinoService(_bd.Context, BaseDeDatos.CrearMapper());

        await servicio.CambiarEstado(new CambiarEstadoNinoDTO { NinoId = nino.Id, Activo = false });

        var renglon = Assert.Single(Renglones());
        using var json = JsonDocument.Parse(renglon.Cambios!);
        Assert.Equal("Activo", Assert.Single(json.RootElement.EnumerateObject()).Name);
    }

    [Fact]
    public async Task LaAsistenciaNoSeAudita()
    {
        var maestro = _bd.CrearPerfil("Maestro");
        var grupo = _bd.CrearGrupo("6 a 8 años", maestro);
        var nino = _bd.CrearNino(grupo, "N-1");
        var actividad = _bd.CrearActividad(maestro, new DateOnly(2026, 10, 1), nino);
        _bd.LimpiarBitacora();

        _bd.Context.Asistencias.Add(new Asistencia
        {
            ActividadId = actividad.Id,
            NinoId = nino.Id,
            Fecha = new DateOnly(2026, 10, 1),
            Estado = Asistencia.EstadoPresente,
            RegistradoPorId = maestro.Id
        });
        await _bd.Context.SaveChangesAsync();

        Assert.Empty(Renglones());
    }

    [Fact]
    public async Task CambiarElEstadoDeUnaCuenta_SeRegistraComoCambioDeUsuario()
    {
        var perfil = _bd.CrearPerfil("Ana");
        _bd.LimpiarBitacora();

        var bdPerfil = _bd.Context.Perfiles.First(x => x.Id == perfil.Id);
        bdPerfil.Activo = false;
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Equal("Usuario", renglon.Entidad);
        Assert.Equal(perfil.Id, renglon.EntidadId);
        Assert.Equal("Ana Prueba", renglon.Descripcion);
    }

    [Fact]
    public async Task AsignarUnMaestroAUnGrupo_SeRegistraComoCambioDelGrupo()
    {
        var maestro = _bd.CrearPerfil("Ana");
        var grupo = _bd.CrearGrupo("6 a 8 años");
        _bd.LimpiarBitacora();

        _bd.Context.Grupo_Maestros.Add(new Grupo_Maestro { GrupoId = grupo.Id, PerfilId = maestro.Id });
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Equal("Grupo", renglon.Entidad);
        Assert.Equal(grupo.Id, renglon.EntidadId);
        Assert.Equal(Bitacora.AccionCreado, renglon.Accion);
    }

    [Fact]
    public async Task VariosCambiosEnUnMismoGuardado_DejanUnRenglonCadaUno_SinRepetirse()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        _bd.LimpiarBitacora();

        _bd.Context.Ninos.AddRange(
            new Nino { Codigo = "A", Nombres = "Uno", Apellidos = "X", GrupoId = grupo.Id },
            new Nino { Codigo = "B", Nombres = "Dos", Apellidos = "X", GrupoId = grupo.Id },
            new Nino { Codigo = "C", Nombres = "Tres", Apellidos = "X", GrupoId = grupo.Id });
        await _bd.Context.SaveChangesAsync();

        var renglones = Renglones();
        Assert.Equal(3, renglones.Count);
        Assert.Equal(3, renglones.Select(x => x.EntidadId).Distinct().Count());
        Assert.All(renglones, x => Assert.NotEqual(0, x.EntidadId));
    }

    [Fact]
    public async Task SiElGuardadoFalla_NoQuedaNingunRenglonPendiente()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        _bd.CrearNino(grupo, "REPETIDO");
        _bd.LimpiarBitacora();

        // El código es único: este guardado falla.
        _bd.Context.Ninos.Add(new Nino { Codigo = "REPETIDO", Nombres = "X", Apellidos = "Y", GrupoId = grupo.Id });
        await Assert.ThrowsAsync<DbUpdateException>(() => _bd.Context.SaveChangesAsync());
        Assert.Empty(Renglones());

        // Ya sin el registro roto, el siguiente guardado deja solo su propio renglón.
        _bd.Context.ChangeTracker.Clear();
        _bd.Context.Ninos.Add(new Nino { Codigo = "NUEVO", Nombres = "X", Apellidos = "Y", GrupoId = grupo.Id });
        await _bd.Context.SaveChangesAsync();

        var renglon = Assert.Single(Renglones());
        Assert.Contains("NUEVO", renglon.Descripcion);
    }

    [Fact]
    public async Task SinSesion_ElUsuarioEsSistema()
    {
        using var sinSesion = new BaseDeDatos(conAuditoria: true, usuario: null);
        var grupo = new Grupo { Nombre = "Sembrado" };
        sinSesion.Context.Grupos.Add(grupo);
        await sinSesion.Context.SaveChangesAsync();

        var renglon = Assert.Single(sinSesion.Context.Bitacoras.AsNoTracking());
        Assert.Equal("Sistema", renglon.Usuario);
        Assert.Null(renglon.PerfilId);
    }

    //CONSULTA

    [Fact]
    public async Task Buscar_FiltraPorEntidadAccionYTexto_ConLosCambiosYaLeidos()
    {
        var nino = NinoExistente();
        nino.Nombres = "Anita";
        await _bd.Context.SaveChangesAsync();

        _bd.Context.Grupos.Add(new Grupo { Nombre = "Nuevo grupo" });
        await _bd.Context.SaveChangesAsync();

        var servicio = new BitacoraService(_bd.Context);

        var soloNinos = await servicio.Buscar(new BuscarBitacoraDTO { Entidad = "Nino" });
        var creados = await servicio.Buscar(new BuscarBitacoraDTO { Accion = Bitacora.AccionCreado });
        var porTexto = await servicio.Buscar(new BuscarBitacoraDTO { Texto = "ANITA" });

        Assert.Single(soloNinos.Items);
        Assert.Equal("Grupo", Assert.Single(creados.Items).Entidad);
        Assert.Single(porTexto.Items);

        var cambio = Assert.Single(soloNinos.Items[0].Cambios);
        Assert.Equal("Nombres", cambio.Campo);
        Assert.Equal("NombreN-1", cambio.Antes);
        Assert.Equal("Anita", cambio.Despues);
    }

    [Fact]
    public async Task Buscar_OrdenaDeLoMasRecienteAlMasAntiguoYPagina()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        _bd.LimpiarBitacora();

        for (int i = 1; i <= 5; i++)
        {
            _bd.Context.Ninos.Add(new Nino { Codigo = $"N{i}", Nombres = $"Nombre{i}", Apellidos = "X", GrupoId = grupo.Id });
            await _bd.Context.SaveChangesAsync();
        }

        var servicio = new BitacoraService(_bd.Context);
        var pagina = await servicio.Buscar(new BuscarBitacoraDTO { Tamano = 2, Pagina = 1 });

        Assert.Equal(5, pagina.Total);
        Assert.Equal(3, pagina.TotalPaginas);
        Assert.Contains("N5", pagina.Items[0].Descripcion);
        Assert.Contains("N4", pagina.Items[1].Descripcion);
    }

    [Fact]
    public async Task Buscar_FiltraPorRangoDeFechas()
    {
        _bd.Context.Bitacoras.AddRange(
            new Bitacora { Fecha = new DateTime(2026, 10, 1, 12, 0, 0, DateTimeKind.Utc), Entidad = "Nino", Accion = "Creado", Descripcion = "viejo" },
            new Bitacora { Fecha = new DateTime(2026, 11, 1, 12, 0, 0, DateTimeKind.Utc), Entidad = "Nino", Accion = "Creado", Descripcion = "nuevo" });
        await _bd.Context.SaveChangesAsync();

        var servicio = new BitacoraService(_bd.Context);
        var resultado = await servicio.Buscar(new BuscarBitacoraDTO
        {
            Desde = new DateTime(2026, 10, 15, 0, 0, 0, DateTimeKind.Utc)
        });

        Assert.Equal("nuevo", Assert.Single(resultado.Items).Descripcion);
    }

    [Fact]
    public async Task Registrar_AnotaUnHechoAMano_ConElNombreDeQuienLoHizo()
    {
        var perfil = _bd.CrearPerfil("Ana");
        _bd.LimpiarBitacora();
        var servicio = new BitacoraService(_bd.Context);

        await servicio.Registrar(perfil.Id, "Usuario", 99, Bitacora.AccionModificado, "cambio de rol");

        var renglon = Assert.Single(Renglones());
        Assert.Equal("Ana Prueba", renglon.Usuario);
        Assert.Equal(99, renglon.EntidadId);
    }
}
