using ClosedXML.Excel;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

/// <summary>
/// Importación de niños desde Excel.
///
/// Ninguna fila incompleta se completa con valores inventados: se rechaza y se informa
/// el motivo, indicando el número de fila. Rellenar los huecos con la fecha de hoy o con
/// un grupo cualquiera produce registros que parecen válidos y nadie vuelve a revisar.
/// </summary>
public class ImportacionService : IImportacionService
{
    private AppProyectoContext _context;

    /// <summary>Encabezados que debe tener la primera fila del archivo.</summary>
    private const string ColumnaCodigo = "codigo";
    private const string ColumnaNombres = "nombres";
    private const string ColumnaApellidos = "apellidos";
    private const string ColumnaFechaNacimiento = "fecha_nacimiento";
    private const string ColumnaGrupo = "grupo";

    public ImportacionService(AppProyectoContext context)
    {
        _context = context;
    }

    //IMPORTAR
    public ResultadoImportacionDTO ImportarNinos(Stream archivoExcel)
    {
        ResultadoImportacionDTO resultado = new();

        using var libro = AbrirLibro(archivoExcel);
        var hoja = libro.Worksheets.First();

        // Se mapean los encabezados a su número de columna, así el orden del archivo no importa.
        Dictionary<string, int> columnas = LeerEncabezados(hoja);
        ValidarColumnasObligatorias(columnas);

        // Se cargan una sola vez para no consultar la base en cada fila.
        Dictionary<string, int> gruposPorNombre = _context.Grupos
            .ToDictionary(grupo => grupo.Nombre.Trim().ToLower(), grupo => grupo.Id);

        HashSet<string> codigosExistentes = _context.Ninos
            .Select(nino => nino.Codigo)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

        List<Nino> ninosParaGuardar = [];

        // Se empieza en la fila 2 porque la 1 son los encabezados.
        foreach (var fila in hoja.RowsUsed().Skip(1))
        {
            int numeroFila = fila.RowNumber();
            resultado.Filas_Leidas++;

            string codigo = LeerTexto(fila, columnas, ColumnaCodigo);
            string nombres = LeerTexto(fila, columnas, ColumnaNombres);
            string apellidos = LeerTexto(fila, columnas, ColumnaApellidos);
            string nombreGrupo = LeerTexto(fila, columnas, ColumnaGrupo);

            // Fila totalmente vacía: se ignora en silencio, no cuenta como error.
            if (string.IsNullOrWhiteSpace(codigo) && string.IsNullOrWhiteSpace(nombres) &&
                string.IsNullOrWhiteSpace(apellidos) && string.IsNullOrWhiteSpace(nombreGrupo))
            {
                resultado.Filas_Leidas--;
                continue;
            }

            if (string.IsNullOrWhiteSpace(nombres) || string.IsNullOrWhiteSpace(apellidos))
            {
                RechazarFila(resultado, numeroFila, "faltan los nombres o los apellidos");
                continue;
            }

            if (string.IsNullOrWhiteSpace(codigo))
            {
                RechazarFila(resultado, numeroFila, "falta el código");
                continue;
            }

            if (codigosExistentes.Contains(codigo))
            {
                RechazarFila(resultado, numeroFila, $"el código '{codigo}' ya existe o está repetido en el archivo");
                continue;
            }

            if (string.IsNullOrWhiteSpace(nombreGrupo))
            {
                RechazarFila(resultado, numeroFila, "falta el grupo");
                continue;
            }

            if (!gruposPorNombre.TryGetValue(nombreGrupo.Trim().ToLower(), out int grupoId))
            {
                RechazarFila(resultado, numeroFila, $"el grupo '{nombreGrupo}' no existe");
                continue;
            }

            DateOnly? fechaNacimiento = LeerFecha(fila, columnas, ColumnaFechaNacimiento);

            ninosParaGuardar.Add(new Nino
            {
                Codigo = codigo,
                Nombres = nombres,
                Apellidos = apellidos,
                Fecha_Nacimiento = fechaNacimiento,
                GrupoId = grupoId,
                Activo = true
                // El maestro no se asigna aquí: se obtiene del grupo al consultar el niño.
            });

            // Se agrega al conjunto para detectar códigos repetidos dentro del mismo archivo.
            codigosExistentes.Add(codigo);
            resultado.Importados++;
        }

        // Se guarda todo junto: o entran todas las filas válidas, o no entra ninguna.
        if (ninosParaGuardar.Count > 0)
        {
            _context.Ninos.AddRange(ninosParaGuardar);
            _context.SaveChanges();
        }

        return resultado;
    }

    //PLANTILLA
    public byte[] GenerarPlantilla()
    {
        using var libro = new XLWorkbook();
        var hoja = libro.Worksheets.Add("Niños");

        string[] encabezados =
        [
            ColumnaCodigo, ColumnaNombres, ColumnaApellidos, ColumnaFechaNacimiento, ColumnaGrupo
        ];

        for (int indice = 0; indice < encabezados.Length; indice++)
        {
            var celda = hoja.Cell(1, indice + 1);
            celda.Value = encabezados[indice];
            celda.Style.Font.Bold = true;
            celda.Style.Fill.BackgroundColor = XLColor.LightGray;
        }

        // Fila de ejemplo para que se vea el formato esperado de cada dato.
        hoja.Cell(2, 1).Value = "N001";
        hoja.Cell(2, 2).Value = "Ana María";
        hoja.Cell(2, 3).Value = "Pérez López";
        hoja.Cell(2, 4).Value = new DateTime(2018, 5, 20);
        hoja.Cell(2, 4).Style.DateFormat.Format = "dd/MM/yyyy";

        // El grupo se escribe por nombre, no por id, para que sea legible al llenar el archivo.
        string primerGrupo = _context.Grupos
            .OrderBy(grupo => grupo.Id)
            .Select(grupo => grupo.Nombre)
            .FirstOrDefault() ?? "6 a 8 años";

        hoja.Cell(2, 5).Value = primerGrupo;

        hoja.Columns().AdjustToContents();

        using var memoria = new MemoryStream();
        libro.SaveAs(memoria);

        return memoria.ToArray();
    }

    //AYUDANTES

    private static XLWorkbook AbrirLibro(Stream archivoExcel)
    {
        try
        {
            return new XLWorkbook(archivoExcel);
        }
        catch (Exception)
        {
            throw (new ReglaNegocioException(
                "No se pudo leer el archivo. Debe ser un Excel válido (.xlsx)."));
        }
    }

    /// <summary>
    /// Lee la primera fila y devuelve cada encabezado con su número de columna.
    /// Los nombres se normalizan a minúsculas y con guion bajo, para que
    /// "Fecha Nacimiento" y "fecha_nacimiento" se entiendan como lo mismo.
    /// </summary>
    private static Dictionary<string, int> LeerEncabezados(IXLWorksheet hoja)
    {
        Dictionary<string, int> columnas = [];

        var filaEncabezados = hoja.FirstRowUsed();
        if (filaEncabezados == null)
        {
            throw (new ReglaNegocioException("El archivo está vacío."));
        }

        foreach (var celda in filaEncabezados.CellsUsed())
        {
            string nombre = celda.GetString()
                .Trim()
                .ToLower()
                .Replace(" ", "_");

            if (!string.IsNullOrWhiteSpace(nombre))
            {
                columnas[nombre] = celda.Address.ColumnNumber;
            }
        }

        return columnas;
    }

    private static void ValidarColumnasObligatorias(Dictionary<string, int> columnas)
    {
        string[] obligatorias = [ColumnaCodigo, ColumnaNombres, ColumnaApellidos, ColumnaGrupo];

        List<string> faltantes = obligatorias
            .Where(nombre => !columnas.ContainsKey(nombre))
            .ToList();

        if (faltantes.Count > 0)
        {
            throw (new ReglaNegocioException(
                $"Al archivo le faltan estas columnas: {string.Join(", ", faltantes)}"));
        }
    }

    private static string LeerTexto(IXLRow fila, Dictionary<string, int> columnas, string nombreColumna)
    {
        if (!columnas.TryGetValue(nombreColumna, out int numeroColumna))
        {
            return string.Empty;
        }

        return fila.Cell(numeroColumna).GetString().Trim();
    }

    /// <summary>
    /// Lee una fecha que puede venir como fecha real de Excel o como texto.
    /// Si no se puede interpretar devuelve null, en lugar de inventar la fecha de hoy.
    /// </summary>
    private static DateOnly? LeerFecha(IXLRow fila, Dictionary<string, int> columnas, string nombreColumna)
    {
        if (!columnas.TryGetValue(nombreColumna, out int numeroColumna))
        {
            return null;
        }

        var celda = fila.Cell(numeroColumna);

        if (celda.IsEmpty())
        {
            return null;
        }

        if (celda.DataType == XLDataType.DateTime && celda.TryGetValue(out DateTime fechaExcel))
        {
            return DateOnly.FromDateTime(fechaExcel);
        }

        if (DateTime.TryParse(celda.GetString(), out DateTime fechaTexto))
        {
            return DateOnly.FromDateTime(fechaTexto);
        }

        return null;
    }

    private static void RechazarFila(ResultadoImportacionDTO resultado, int numeroFila, string motivo)
    {
        resultado.Rechazados++;
        resultado.Errores.Add($"Fila {numeroFila}: {motivo}.");
    }
}
