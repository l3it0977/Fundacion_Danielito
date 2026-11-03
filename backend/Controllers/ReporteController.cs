using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

/// <summary>
/// Descarga de los reportes en PDF.
/// Los tres exigen sesión: contienen datos personales de los niños, así que un enlace
/// suelto no debe bastar para descargarlos.
/// </summary>
[ApiController]
[Route("[controller]")]
public class ReporteController : ControllerBase
{
    private readonly IReporteService _reporteService;

    public ReporteController(IReporteService reporteService)
    {
        _reporteService = reporteService;
    }

    //ASISTENCIA TOTAL Y POR GRUPO
    // grupoId omitido = todos los grupos de edad; mes omitido = todos los meses del año.
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("Asistencia")]
    public IActionResult GetAsistencia(int? grupoId, int? mes, int? anio)
    {
        int anioElegido = anio ?? DateTime.Today.Year;

        byte[] archivo = _reporteService.GenerarAsistencia(grupoId, mes, anioElegido);

        return File(archivo, "application/pdf",
            $"asistencia-{NombreDelPeriodo(grupoId, mes, anioElegido)}.pdf");
    }

    //ACTIVIDADES
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("Actividades")]
    public IActionResult GetActividades(int? grupoId, int? mes, int? anio)
    {
        int anioElegido = anio ?? DateTime.Today.Year;

        byte[] archivo = _reporteService.GenerarActividades(grupoId, mes, anioElegido);

        return File(archivo, "application/pdf",
            $"actividades-{NombreDelPeriodo(grupoId, mes, anioElegido)}.pdf");
    }

    //ASISTENCIA POR ACTIVIDAD
    // El maestro también descarga el PDF de sus actividades; el servicio comprueba
    // que la actividad le corresponda. Los otros reportes abarcan grupos o meses
    // completos y siguen reservados a la dirección.
    [Authorize(Roles = "Directora,Administrador,Maestro")]
    [HttpGet("AsistenciaPorActividad/{actividadId}")]
    public IActionResult GetAsistenciaPorActividad(int actividadId)
    {
        byte[] archivo = _reporteService.GenerarAsistenciaPorActividad(actividadId, User.FiltroMaestro());

        return File(archivo, "application/pdf", $"asistencia-actividad-{actividadId}.pdf");
    }

    /// <summary>Ej.: "grupo-2-10-2026", "todos-los-grupos-todo-2026".</summary>
    private static string NombreDelPeriodo(int? grupoId, int? mes, int anio)
    {
        string grupo = grupoId.HasValue ? $"grupo-{grupoId}" : "todos-los-grupos";
        string periodo = mes.HasValue ? $"{mes:00}-{anio}" : $"todo-{anio}";

        return $"{grupo}-{periodo}";
    }
}
