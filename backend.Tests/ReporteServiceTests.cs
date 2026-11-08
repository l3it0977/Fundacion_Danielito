using System.Text;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Services.Reportes;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

public class ReporteServiceTests : IDisposable
{
    private readonly BaseDeDatos _bd = new();
    private readonly ReporteService _servicio;

    private readonly Perfil _maestro;
    private readonly Grupo _grupo;

    public ReporteServiceTests()
    {
        var actividades = new ActividadService(_bd.Context, BaseDeDatos.CrearMapper());
        _servicio = new ReporteService(_bd.Context, actividades);

        _maestro = _bd.CrearPerfil("Maestro");
        _grupo = _bd.CrearGrupo("6 a 8 años", _maestro);
    }

    public void Dispose() => _bd.Dispose();

    //PORCENTAJES

    [Fact]
    public void Totales_SoloCuentanLasSesionesConListaPasada()
    {
        var totales = Totales.De(
            ["presente", "presente", "ausente", "justificado", "pendiente", "pendiente"]);

        Assert.Equal(2, totales.Presentes);
        Assert.Equal(1, totales.Ausentes);
        Assert.Equal(1, totales.Justificados);
        Assert.Equal(2, totales.Pendientes);
        Assert.Equal(4, totales.Registradas);
        Assert.Equal("50%", totales.Porcentaje);
    }

    [Fact]
    public void Totales_SinListaPasada_MuestraGuionYNoUn100PorCiento()
    {
        Assert.Equal("-", Totales.De([]).Porcentaje);
        Assert.Equal("-", Totales.De(["pendiente"]).Porcentaje);
    }

    [Fact]
    public void Totales_RedondeaAlEnteroMasCercano()
    {
        // 2 de 3 = 66,67 %
        Assert.Equal("67%", Totales.De(["presente", "presente", "ausente"]).Porcentaje);
    }

    //GENERACIÓN DE PDF

    [Theory]
    [InlineData(null, null)]    // todos los grupos, todos los meses
    [InlineData(1, null)]       // un grupo, todo el año
    [InlineData(null, 10)]      // todos los grupos, un mes
    [InlineData(1, 10)]         // un grupo y un mes: con matriz diaria
    public async Task GenerarAsistencia_ConCualquierCombinacionDeFiltros_DevuelveUnPdf(int? grupoId, int? mes)
    {
        var ninoUno = _bd.CrearNino(_grupo, "N-1");
        var ninoDos = _bd.CrearNino(_grupo, "N-2");
        var actividad = _bd.CrearActividad(_maestro, new DateOnly(2026, 10, 1), ninoUno, ninoDos);
        _bd.CrearAsistencia(actividad, ninoUno, new DateOnly(2026, 10, 1), Asistencia.EstadoPresente, _maestro);
        _bd.CrearAsistencia(actividad, ninoDos, new DateOnly(2026, 10, 1), Asistencia.EstadoAusente, _maestro);

        byte[] pdf = await _servicio.GenerarAsistencia(grupoId, mes, 2026);

        AssertEsPdf(pdf);
    }

    [Fact]
    public async Task GenerarAsistencia_SinNingunDato_AunAsiDevuelveUnPdf()
    {
        AssertEsPdf(await _servicio.GenerarAsistencia(null, null, 2026));
    }

    [Fact]
    public async Task GenerarAsistencia_GrupoInexistente_LanzaNoEncontrado()
    {
        await Assert.ThrowsAsync<RecursoNoEncontradoException>(() => _servicio.GenerarAsistencia(999, null, 2026));
    }

    [Theory]
    [InlineData(0, 2026)]
    [InlineData(13, 2026)]
    [InlineData(5, 1999)]
    [InlineData(5, 2101)]
    public async Task GenerarAsistencia_PeriodoInvalido_LanzaReglaDeNegocio(int mes, int anio)
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() => _servicio.GenerarAsistencia(null, mes, anio));
    }

    [Fact]
    public async Task GenerarAsistenciaPorActividad_MaestroAjeno_LanzaNoAutorizado()
    {
        var otro = _bd.CrearPerfil("Otro");
        var nino = _bd.CrearNino(_grupo, "N-1");
        var actividad = _bd.CrearActividad(_maestro, new DateOnly(2026, 10, 1), nino);

        await Assert.ThrowsAsync<UnauthorizedAccessException>(() =>
            _servicio.GenerarAsistenciaPorActividad(actividad.Id, otro.Id));
    }

    private static void AssertEsPdf(byte[] contenido)
    {
        Assert.True(contenido.Length > 1000, "El PDF está vacío o es demasiado pequeño.");
        Assert.Equal("%PDF", Encoding.ASCII.GetString(contenido, 0, 4));
    }
}
