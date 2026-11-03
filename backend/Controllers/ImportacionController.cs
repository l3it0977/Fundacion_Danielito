using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class ImportacionController : ControllerBase
{
    private readonly IImportacionService _importacionService;

    /// <summary>Tope de tamaño del archivo, para que nadie suba algo enorme y agote la memoria.</summary>
    private const long TamanoMaximoBytes = 5 * 1024 * 1024;

    public ImportacionController(IImportacionService importacionService)
    {
        _importacionService = importacionService;
    }

    //IMPORTAR NIÑOS
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPost("Ninos")]
    public ResultadoImportacionDTO PostNinos(IFormFile archivo)
    {
        if (archivo == null || archivo.Length == 0)
        {
            throw (new ReglaNegocioException("Debe adjuntar un archivo."));
        }

        if (archivo.Length > TamanoMaximoBytes)
        {
            throw (new ReglaNegocioException("El archivo supera el tamaño máximo de 5 MB."));
        }

        string extension = Path.GetExtension(archivo.FileName).ToLower();
        if (extension != ".xlsx")
        {
            throw (new ReglaNegocioException("El archivo debe tener extensión .xlsx."));
        }

        // Se lee directamente del flujo de la petición: el archivo no se guarda en disco.
        using Stream contenido = archivo.OpenReadStream();

        return _importacionService.ImportarNinos(contenido);
    }

    //DESCARGAR PLANTILLA
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("Plantilla")]
    public IActionResult GetPlantilla()
    {
        byte[] archivo = _importacionService.GenerarPlantilla();

        return File(archivo,
            "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
            "plantilla-ninos.xlsx");
    }
}
