namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Resumen de lo ocurrido al importar el Excel.
/// Informa fila por fila: sin ese detalle, quien sube el archivo no sabe qué corregir.
/// </summary>
public class ResultadoImportacionDTO
{
    public int Filas_Leidas { get; set; }
    public int Importados { get; set; }
    public int Rechazados { get; set; }

    /// <summary>Un mensaje por fila rechazada, indicando el número de fila y el motivo.</summary>
    public List<string> Errores { get; set; } = [];
}
