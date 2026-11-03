using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

/// <summary>
/// Un maestro solo debe ver y tocar lo suyo. Si esta regla falla, un maestro podría leer
/// o modificar datos de niños que no tiene a cargo.
/// </summary>
public class PermisosDeMaestroTests : IDisposable
{
    private static readonly DateOnly Fecha = new(2026, 10, 5);

    private readonly BaseDeDatos _bd = new();
    private readonly ActividadService _actividades;
    private readonly AsistenciaService _asistencias;
    private readonly NinoService _ninos;

    private readonly Perfil _ana;      // maestra del grupo A
    private readonly Perfil _beto;     // maestro del grupo B
    private readonly Nino _ninoDeA;
    private readonly Nino _ninoDeB;
    private readonly Actividad _actividadDeA;

    public PermisosDeMaestroTests()
    {
        var mapper = BaseDeDatos.CrearMapper();
        _actividades = new ActividadService(_bd.Context, mapper);
        _asistencias = new AsistenciaService(_bd.Context, mapper, _actividades);
        _ninos = new NinoService(_bd.Context, mapper);

        _ana = _bd.CrearPerfil("Ana");
        _beto = _bd.CrearPerfil("Beto");
        var grupoA = _bd.CrearGrupo("Grupo A", _ana);
        var grupoB = _bd.CrearGrupo("Grupo B", _beto);
        _ninoDeA = _bd.CrearNino(grupoA, "A-1");
        _ninoDeB = _bd.CrearNino(grupoB, "B-1");

        _actividadDeA = _bd.CrearActividad(_ana, Fecha, _ninoDeA);
    }

    public void Dispose() => _bd.Dispose();

    //ACTIVIDADES

    [Fact]
    public async Task ValidarAcceso_QuienLaCreo_Puede()
    {
        await _actividades.ValidarAcceso(_actividadDeA.Id, _ana.Id);
    }

    [Fact]
    public async Task ValidarAcceso_MaestroConNinosDeSuGrupoEnLaActividad_Puede()
    {
        // Beto no la creó, pero la actividad incluye a un niño de su grupo.
        var actividadMixta = _bd.CrearActividad(_ana, Fecha, _ninoDeA, _ninoDeB);

        await _actividades.ValidarAcceso(actividadMixta.Id, _beto.Id);
    }

    [Fact]
    public async Task ValidarAcceso_MaestroAjeno_LanzaNoAutorizado()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _actividades.ValidarAcceso(_actividadDeA.Id, _beto.Id));
    }

    [Fact]
    public async Task ValidarAcceso_SinFiltroDeMaestro_PuedeLaDireccion()
    {
        await _actividades.ValidarAcceso(_actividadDeA.Id, null);
    }

    [Fact]
    public async Task ValidarAcceso_ActividadInexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _actividades.ValidarAcceso(999, _ana.Id));
    }

    //NIÑOS

    [Fact]
    public async Task ObtenerTodos_UnMaestroSoloVeLosNinosDeSusGrupos()
    {
        var visibles = await _ninos.ObtenerTodos(_ana.Id);

        Assert.Single(visibles);
        Assert.Equal(_ninoDeA.Id, visibles[0].Id);
    }

    [Fact]
    public async Task ObtenerTodos_LaDireccionVeATodos()
    {
        Assert.Equal(2, (await _ninos.ObtenerTodos(null)).Count);
    }

    [Fact]
    public async Task ObtenerPorId_NinoDeOtroGrupo_LanzaNoAutorizado()
    {
        await Assert.ThrowsAsync<UnauthorizedAccessException>(() => _ninos.ObtenerPorId(_ninoDeB.Id, _ana.Id));
    }

    //ASISTENCIA

    [Fact]
    public async Task RegistrarPorActividad_MaestroAjeno_LanzaNoAutorizadoYNoGuardaNada()
    {
        var registro = NuevoRegistro(_actividadDeA, Fecha, (_ninoDeA, Asistencia.EstadoPresente));

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _asistencias.RegistrarPorActividad(registro, _beto.Id, _beto.Id));

        Assert.Equal(0, _bd.Context.Asistencias.Count());
    }

    [Fact]
    public async Task RegistrarPorActividad_NinoQueNoParticipa_SeRechaza()
    {
        var registro = NuevoRegistro(_actividadDeA, Fecha, (_ninoDeB, Asistencia.EstadoPresente));

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _asistencias.RegistrarPorActividad(registro, _ana.Id, _ana.Id));
    }

    [Fact]
    public async Task RegistrarPorActividad_EstadoInvalido_SeRechaza()
    {
        var registro = NuevoRegistro(_actividadDeA, Fecha, (_ninoDeA, "quizas"));

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _asistencias.RegistrarPorActividad(registro, _ana.Id, _ana.Id));
    }

    [Fact]
    public async Task RegistrarPorActividad_FechaAnteriorALaActividad_SeRechaza()
    {
        var registro = NuevoRegistro(_actividadDeA, Fecha.AddDays(-1), (_ninoDeA, Asistencia.EstadoPresente));

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _asistencias.RegistrarPorActividad(registro, _ana.Id, _ana.Id));
    }

    [Fact]
    public async Task RegistrarPorActividad_EnviarDosVecesLaMismaLista_ActualizaSinDuplicar()
    {
        await _asistencias.RegistrarPorActividad(
            NuevoRegistro(_actividadDeA, Fecha, (_ninoDeA, Asistencia.EstadoAusente)), _ana.Id, _ana.Id);

        await _asistencias.RegistrarPorActividad(
            NuevoRegistro(_actividadDeA, Fecha, (_ninoDeA, Asistencia.EstadoPresente)), _ana.Id, _ana.Id);

        var guardadas = _bd.Context.Asistencias.ToList();
        Assert.Single(guardadas);
        Assert.Equal(Asistencia.EstadoPresente, guardadas[0].Estado);
    }

    [Fact]
    public async Task RegistrarPorActividad_ListaVacia_SeRechaza()
    {
        var registro = NuevoRegistro(_actividadDeA, Fecha);

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _asistencias.RegistrarPorActividad(registro, _ana.Id, _ana.Id));
    }

    private static RegistrarAsistenciasDTO NuevoRegistro(
        Actividad actividad, DateOnly fecha, params (Nino Nino, string Estado)[] items)
    {
        return new RegistrarAsistenciasDTO
        {
            ActividadId = actividad.Id,
            Fecha = fecha,
            Asistencias = items
                .Select(i => new AsistenciaItemDTO { NinoId = i.Nino.Id, Estado = i.Estado })
                .ToList()
        };
    }
}
