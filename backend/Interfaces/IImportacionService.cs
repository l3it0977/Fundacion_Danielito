using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IImportacionService
{
    /// <summary>Importa niños desde un archivo Excel y devuelve el detalle de lo procesado.</summary>
    public Task<ResultadoImportacionDTO> ImportarNinos(Stream archivoExcel);

    /// <summary>Genera la plantilla de Excel con las columnas esperadas y una fila de ejemplo.</summary>
    public Task<byte[]> GenerarPlantilla();
}
