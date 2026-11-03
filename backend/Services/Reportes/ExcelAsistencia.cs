using ClosedXML.Excel;
using SISTEMADANIELITO.Models;
using static SISTEMADANIELITO.Services.Reportes.PlantillaFundacion;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>
/// Reporte de asistencia en Excel, con las mismas cifras que el PDF pero en celdas que se pueden
/// filtrar, ordenar y usar en tablas dinámicas. La hoja "Registros" trae un renglón por cada
/// asistencia del periodo.
/// </summary>
internal static class ExcelAsistencia
{
    private static readonly XLColor TealOscuro = XLColor.FromHtml(Marca.TealOscuro);
    private static readonly XLColor TealMedio = XLColor.FromHtml(Marca.TealMedio);
    private static readonly XLColor TealPalido = XLColor.FromHtml(Marca.TealPalido);

    public static byte[] Generar(DatosDeAsistencia datos)
    {
        using var libro = new XLWorkbook();

        HojaResumen(libro.AddWorksheet("Resumen"), datos);
        HojaPorNino(libro.AddWorksheet("Por niño"), datos);
        HojaRegistros(libro.AddWorksheet("Registros"), datos);

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);

        return memoria.ToArray();
    }

    private static void HojaResumen(IXLWorksheet hoja, DatosDeAsistencia datos)
    {
        hoja.Cell(1, 1).Value = "Fundación Danielito - Reporte de asistencia";
        hoja.Cell(1, 1).Style.Font.Bold = true;
        hoja.Cell(1, 1).Style.Font.FontSize = 14;
        hoja.Cell(1, 1).Style.Font.FontColor = TealOscuro;

        hoja.Cell(2, 1).Value = Periodo.TextoDelGrupo(datos.GrupoElegido?.Nombre);
        hoja.Cell(3, 1).Value = Periodo.Texto(datos.Mes, datos.Anio);
        hoja.Cell(4, 1).Value = $"Emitido el {DateTime.Now:dd/MM/yyyy HH:mm}";
        hoja.Range(2, 1, 4, 1).Style.Font.FontColor = XLColor.FromHtml(Marca.TextoMedio);

        // CIFRAS GENERALES
        int fila = 6;
        Totales total = datos.TotalGeneral;

        EscribirTitulo(hoja, fila++, "Cifras generales");
        EscribirDato(hoja, fila++, "Asistencia total", total.Fraccion, esPorcentaje: true);
        EscribirDato(hoja, fila++, "Presentes", total.Presentes);
        EscribirDato(hoja, fila++, "Ausentes", total.Ausentes);
        EscribirDato(hoja, fila++, "Justificados", total.Justificados);
        EscribirDato(hoja, fila++, "Registros con lista pasada", total.Registradas);
        EscribirDato(hoja, fila++, "Sin pasar lista", total.Pendientes);

        // POR GRUPO
        fila++;
        EscribirTitulo(hoja, fila++, "Asistencia por grupo");
        var porGrupo = datos.FilasPorGrupo();
        fila = TablaDeTotales(hoja, fila, "Grupo", porGrupo,
            porGrupo.Count > 1 ? total : null, datos.Ninos.Count, conNinos: true);

        // POR MES (solo cuando se piden todos los meses)
        if (!datos.Mes.HasValue)
        {
            fila++;
            EscribirTitulo(hoja, fila++, "Asistencia por mes");
            var porMes = datos.FilasPorMes();
            TablaDeTotales(hoja, fila, "Mes", porMes,
                porMes.Count > 1 ? total : null, 0, conNinos: false);
        }

        hoja.Column(1).Width = 30;
        hoja.Columns(2, 7).Width = 14;
    }

    private static void HojaPorNino(IXLWorksheet hoja, DatosDeAsistencia datos)
    {
        string[] titulos = ["Grupo", "Niño", "Presentes", "Ausentes", "Justificados", "Registros", "% asistencia"];
        EscribirEncabezados(hoja, 1, titulos);

        int fila = 2;
        foreach (Grupo grupo in datos.Grupos)
        {
            foreach (NinoDelReporte nino in datos.NinosDelGrupo(grupo.Id))
            {
                Totales t = datos.TotalesDelNino(nino.Id);

                hoja.Cell(fila, 1).Value = grupo.Nombre;
                hoja.Cell(fila, 2).Value = nino.Nombre_Completo;
                EscribirTotales(hoja, fila, 3, t);
                fila++;
            }
        }

        FiltrarYAjustar(hoja, titulos.Length, fila - 1);
    }

    private static void HojaRegistros(IXLWorksheet hoja, DatosDeAsistencia datos)
    {
        string[] titulos = ["Fecha", "Grupo", "Niño", "Estado"];
        EscribirEncabezados(hoja, 1, titulos);

        Dictionary<int, string> grupos = datos.Grupos.ToDictionary(x => x.Id, x => x.Nombre);
        Dictionary<int, string> ninos = datos.Ninos.ToDictionary(x => x.Id, x => x.Nombre_Completo);

        int fila = 2;
        foreach (RegistroAsistencia registro in datos.Registros.OrderBy(x => x.Fecha).ThenBy(x => x.GrupoId))
        {
            // Fecha real de Excel, no texto: se puede ordenar y agrupar por mes.
            hoja.Cell(fila, 1).Value = registro.Fecha.ToDateTime(TimeOnly.MinValue);
            hoja.Cell(fila, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            hoja.Cell(fila, 2).Value = grupos.GetValueOrDefault(registro.GrupoId, "");
            hoja.Cell(fila, 3).Value = ninos.GetValueOrDefault(registro.NinoId, "");
            hoja.Cell(fila, 4).Value = registro.Estado;
            fila++;
        }

        FiltrarYAjustar(hoja, titulos.Length, fila - 1);
    }

    //AYUDANTES

    private static int TablaDeTotales(IXLWorksheet hoja, int fila, string primeraColumna,
        List<FilaResumen> filas, Totales? total, int totalNinos, bool conNinos)
    {
        List<string> titulos = [primeraColumna];
        if (conNinos) titulos.Add("Niños");
        titulos.AddRange(["Presentes", "Ausentes", "Justificados", "Registros", "% asistencia"]);

        EscribirEncabezados(hoja, fila++, titulos.ToArray());

        foreach (FilaResumen datos in filas)
        {
            fila = EscribirFilaDeTotales(hoja, fila, datos.Etiqueta, datos.Ninos, datos.Datos, conNinos, esTotal: false);
        }

        if (total != null)
        {
            fila = EscribirFilaDeTotales(hoja, fila, "Total", totalNinos, total, conNinos, esTotal: true);
        }

        return fila;
    }

    private static int EscribirFilaDeTotales(IXLWorksheet hoja, int fila, string etiqueta,
        int? ninos, Totales datos, bool conNinos, bool esTotal)
    {
        int columna = 1;
        hoja.Cell(fila, columna++).Value = etiqueta;
        if (conNinos)
        {
            hoja.Cell(fila, columna++).Value = ninos ?? 0;
        }

        int primera = 1;
        EscribirTotales(hoja, fila, columna, datos);
        int ultima = columna + 4;

        if (esTotal)
        {
            var rango = hoja.Range(fila, primera, fila, ultima);
            rango.Style.Font.Bold = true;
            rango.Style.Fill.BackgroundColor = TealPalido;
        }

        return fila + 1;
    }

    /// <summary>Escribe presentes, ausentes, justificados, registros y porcentaje desde la columna dada.</summary>
    private static void EscribirTotales(IXLWorksheet hoja, int fila, int columna, Totales datos)
    {
        hoja.Cell(fila, columna).Value = datos.Presentes;
        hoja.Cell(fila, columna + 1).Value = datos.Ausentes;
        hoja.Cell(fila, columna + 2).Value = datos.Justificados;
        hoja.Cell(fila, columna + 3).Value = datos.Registradas;

        var celdaPorcentaje = hoja.Cell(fila, columna + 4);
        if (datos.Fraccion is double fraccion)
        {
            // Número real (0,75) con formato de porcentaje: se puede promediar o graficar.
            celdaPorcentaje.Value = fraccion;
            celdaPorcentaje.Style.NumberFormat.Format = "0%";
        }
        else
        {
            celdaPorcentaje.Value = "-";
        }
    }

    private static void EscribirTitulo(IXLWorksheet hoja, int fila, string texto)
    {
        hoja.Cell(fila, 1).Value = texto;
        hoja.Cell(fila, 1).Style.Font.Bold = true;
        hoja.Cell(fila, 1).Style.Font.FontSize = 12;
        hoja.Cell(fila, 1).Style.Font.FontColor = TealOscuro;
    }

    private static void EscribirDato(IXLWorksheet hoja, int fila, string etiqueta, double? valor, bool esPorcentaje)
    {
        hoja.Cell(fila, 1).Value = etiqueta;

        if (valor.HasValue)
        {
            hoja.Cell(fila, 2).Value = valor.Value;
            if (esPorcentaje) hoja.Cell(fila, 2).Style.NumberFormat.Format = "0%";
        }
        else
        {
            hoja.Cell(fila, 2).Value = "-";
        }

        hoja.Cell(fila, 2).Style.Alignment.Horizontal = XLAlignmentHorizontalValues.Right;
    }

    private static void EscribirDato(IXLWorksheet hoja, int fila, string etiqueta, int valor)
    {
        hoja.Cell(fila, 1).Value = etiqueta;
        hoja.Cell(fila, 2).Value = valor;
    }

    private static void EscribirEncabezados(IXLWorksheet hoja, int fila, string[] titulos)
    {
        for (int i = 0; i < titulos.Length; i++)
        {
            var celda = hoja.Cell(fila, i + 1);
            celda.Value = titulos[i];
            celda.Style.Font.Bold = true;
            celda.Style.Font.FontColor = XLColor.White;
            celda.Style.Fill.BackgroundColor = TealMedio;
        }
    }

    /// <summary>Fija la fila de títulos, activa el filtro y ajusta el ancho al contenido.</summary>
    private static void FiltrarYAjustar(IXLWorksheet hoja, int columnas, int ultimaFila)
    {
        hoja.SheetView.FreezeRows(1);

        if (ultimaFila > 1)
        {
            hoja.Range(1, 1, ultimaFila, columnas).SetAutoFilter();
        }

        hoja.Columns().AdjustToContents();
    }
}
