using ClosedXML.Excel;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

public class ReporteExcelTests : IDisposable
{
    private readonly BaseDeDatos _bd = new();
    private readonly ReporteService _servicio;

    private readonly Perfil _maestro;
    private readonly Grupo _grupo;
    private readonly Grupo _otroGrupo;

    public ReporteExcelTests()
    {
        _servicio = new ReporteService(_bd.Context, new ActividadService(_bd.Context, BaseDeDatos.CrearMapper()));

        _maestro = _bd.CrearPerfil("Maestro");
        _grupo = _bd.CrearGrupo("6 a 8 años", _maestro);
        _otroGrupo = _bd.CrearGrupo("9 a 11 años", _maestro);

        var ana = _bd.CrearNino(_grupo, "N-1");
        var beto = _bd.CrearNino(_grupo, "N-2");
        var carla = _bd.CrearNino(_otroGrupo, "N-3");
        var actividad = _bd.CrearActividad(_maestro, new DateOnly(2026, 10, 1), ana, beto, carla);

        _bd.CrearAsistencia(actividad, ana, new DateOnly(2026, 10, 1), Asistencia.EstadoPresente, _maestro);
        _bd.CrearAsistencia(actividad, beto, new DateOnly(2026, 10, 1), Asistencia.EstadoAusente, _maestro);
        _bd.CrearAsistencia(actividad, carla, new DateOnly(2026, 11, 5), Asistencia.EstadoPresente, _maestro);
    }

    public void Dispose() => _bd.Dispose();

    private static XLWorkbook Abrir(byte[] contenido) => new(new MemoryStream(contenido));

    [Fact]
    public async Task Excel_TieneLasTresHojasYSeAbreCorrectamente()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(null, null, 2026));

        Assert.Equal(["Resumen", "Por niño", "Registros"], libro.Worksheets.Select(h => h.Name).ToArray());
    }

    [Fact]
    public async Task Excel_ElResumenCoincideConLosDatos()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(null, null, 2026));
        var hoja = libro.Worksheet("Resumen");

        // Filas de "Cifras generales": porcentaje, presentes, ausentes, justificados, registros, pendientes.
        Assert.Equal("Asistencia total", hoja.Cell(7, 1).GetString());
        Assert.Equal(2d / 3d, hoja.Cell(7, 2).GetDouble(), precision: 6);
        Assert.Equal(2, hoja.Cell(8, 2).GetValue<int>());
        Assert.Equal(1, hoja.Cell(9, 2).GetValue<int>());
        Assert.Equal(3, hoja.Cell(11, 2).GetValue<int>());
    }

    [Fact]
    public async Task Excel_ElPorcentajeEsUnNumeroRealConFormatoDePorcentaje()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(null, null, 2026));
        var celda = libro.Worksheet("Resumen").Cell(7, 2);

        Assert.Equal(XLDataType.Number, celda.DataType);
        Assert.Equal("0%", celda.Style.NumberFormat.Format);
    }

    [Fact]
    public async Task Excel_RegistrosTraeUnRenglonPorAsistenciaConFechaReal()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(null, null, 2026));
        var hoja = libro.Worksheet("Registros");

        Assert.Equal("Fecha", hoja.Cell(1, 1).GetString());
        Assert.Equal(3, hoja.LastRowUsed()!.RowNumber() - 1);

        var primera = hoja.Cell(2, 1);
        Assert.Equal(XLDataType.DateTime, primera.DataType);
        Assert.Equal(new DateTime(2026, 10, 1), primera.GetDateTime());
        Assert.Equal("6 a 8 años", hoja.Cell(2, 2).GetString());
    }

    [Fact]
    public async Task Excel_ConUnGrupoYUnMes_SoloIncluyeEsosRegistros()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(_grupo.Id, 10, 2026));
        var hoja = libro.Worksheet("Registros");

        Assert.Equal(2, hoja.LastRowUsed()!.RowNumber() - 1);
        Assert.All(Enumerable.Range(2, 2), fila => Assert.Equal("6 a 8 años", hoja.Cell(fila, 2).GetString()));
    }

    [Fact]
    public async Task Excel_PorNino_TieneUnRenglonPorNinoConSuPorcentaje()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(_grupo.Id, null, 2026));
        var hoja = libro.Worksheet("Por niño");

        Assert.Equal(2, hoja.LastRowUsed()!.RowNumber() - 1);

        // Ana asistió a su única sesión y Beto faltó.
        var porcentajes = new[] { hoja.Cell(2, 7).GetDouble(), hoja.Cell(3, 7).GetDouble() }.OrderBy(x => x);
        Assert.Equal([0d, 1d], porcentajes);
    }

    [Fact]
    public async Task Excel_SinDatos_SeGeneraIgualConGuionEnElPorcentaje()
    {
        using var libro = Abrir(await _servicio.GenerarAsistenciaExcel(null, 3, 2026));

        Assert.Equal("-", libro.Worksheet("Resumen").Cell(7, 2).GetString());
    }
}
