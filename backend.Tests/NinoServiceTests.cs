using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

public class NinoServiceTests : IDisposable
{
    private readonly BaseDeDatos _bd = new();
    private readonly NinoService _servicio;

    public NinoServiceTests()
    {
        _servicio = new NinoService(_bd.Context, BaseDeDatos.CrearMapper());
    }

    public void Dispose() => _bd.Dispose();

    [Fact]
    public void Eliminar_FichaSinHistorial_LaBorra()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        var nino = _bd.CrearNino(grupo, "N-1");

        _servicio.Eliminar(nino.Id);

        Assert.False(_bd.Context.Ninos.Any(x => x.Id == nino.Id));
    }

    [Fact]
    public void Eliminar_ConAsistencias_SeRechazaYConservaTodo()
    {
        var maestro = _bd.CrearPerfil("Maestro");
        var grupo = _bd.CrearGrupo("6 a 8 años", maestro);
        var nino = _bd.CrearNino(grupo, "N-1");
        var actividad = _bd.CrearActividad(maestro, new DateOnly(2026, 10, 1), nino);
        _bd.CrearAsistencia(actividad, nino, new DateOnly(2026, 10, 1), Asistencia.EstadoPresente, maestro);

        Assert.Throws<ReglaNegocioException>(() => _servicio.Eliminar(nino.Id));

        Assert.True(_bd.Context.Ninos.Any(x => x.Id == nino.Id));
        Assert.Equal(1, _bd.Context.Asistencias.Count());
    }

    [Fact]
    public void Eliminar_SoloAsignadoAUnaActividad_SeRechaza()
    {
        var maestro = _bd.CrearPerfil("Maestro");
        var grupo = _bd.CrearGrupo("6 a 8 años", maestro);
        var nino = _bd.CrearNino(grupo, "N-1");
        _bd.CrearActividad(maestro, new DateOnly(2026, 10, 1), nino);

        Assert.Throws<ReglaNegocioException>(() => _servicio.Eliminar(nino.Id));
    }

    [Fact]
    public void Eliminar_NinoInexistente_LanzaNoEncontrado()
    {
        Assert.Throws<RecursoNoEncontradoException>(() => _servicio.Eliminar(999));
    }

    [Fact]
    public void LaBase_ImpideBorrarUnNinoConHistorial_AunSinPasarPorElServicio()
    {
        var maestro = _bd.CrearPerfil("Maestro");
        var grupo = _bd.CrearGrupo("6 a 8 años", maestro);
        var nino = _bd.CrearNino(grupo, "N-1");
        var actividad = _bd.CrearActividad(maestro, new DateOnly(2026, 10, 1), nino);
        _bd.CrearAsistencia(actividad, nino, new DateOnly(2026, 10, 1), Asistencia.EstadoPresente, maestro);

        // ExecuteDelete va directo a la base, sin que EF intervenga: es la red de seguridad
        // (DeleteBehavior.Restrict). Sin ella el borrado arrastraría la asistencia.
        Assert.Throws<SqliteException>(() =>
            _bd.Context.Ninos.Where(x => x.Id == nino.Id).ExecuteDelete());

        Assert.Equal(1, _bd.Context.Asistencias.Count());
    }

    [Fact]
    public void CambiarEstado_Desactivar_ConservaElHistorialYLoQuitaDeLaSeleccion()
    {
        var maestro = _bd.CrearPerfil("Maestro");
        var grupo = _bd.CrearGrupo("6 a 8 años", maestro);
        var nino = _bd.CrearNino(grupo, "N-1");
        var otro = _bd.CrearNino(grupo, "N-2");
        var actividad = _bd.CrearActividad(maestro, new DateOnly(2026, 10, 1), nino);
        _bd.CrearAsistencia(actividad, nino, new DateOnly(2026, 10, 1), Asistencia.EstadoPresente, maestro);

        _servicio.CambiarEstado(new CambiarEstadoNinoDTO { NinoId = nino.Id, Activo = false });

        Assert.False(_bd.Context.Ninos.Single(x => x.Id == nino.Id).Activo);
        Assert.Equal(1, _bd.Context.Asistencias.Count());

        var seleccion = _servicio.ObtenerParaSeleccion();
        Assert.Single(seleccion);
        Assert.Equal(otro.Id, seleccion[0].Id);
    }

    [Fact]
    public void CambiarEstado_Reactivar_VuelveAAparecerEnLaSeleccion()
    {
        var grupo = _bd.CrearGrupo("6 a 8 años");
        var nino = _bd.CrearNino(grupo, "N-1", activo: false);

        _servicio.CambiarEstado(new CambiarEstadoNinoDTO { NinoId = nino.Id, Activo = true });

        Assert.Single(_servicio.ObtenerParaSeleccion());
    }

    [Fact]
    public void CambiarEstado_NinoInexistente_LanzaNoEncontrado()
    {
        Assert.Throws<RecursoNoEncontradoException>(() =>
            _servicio.CambiarEstado(new CambiarEstadoNinoDTO { NinoId = 999, Activo = false }));
    }
}
