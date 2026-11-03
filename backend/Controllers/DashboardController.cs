using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class DashboardController : ControllerBase
{
    private readonly IDashboardService _dashboardService;

    public DashboardController(IDashboardService dashboardService)
    {
        _dashboardService = dashboardService;
    }

    //PANEL GENERAL
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("General")]
    public DashboardGeneralDTO GetGeneral()
    {
        return _dashboardService.ObtenerGeneral();
    }

    //PANEL DEL MAESTRO
    // El maestro sale del token: así nadie puede pedir el panel de otra persona
    // cambiando un id en la URL.
    [Authorize(Roles = "Maestro")]
    [HttpGet("Maestro")]
    public DashboardMaestroDTO GetMaestro()
    {
        return _dashboardService.ObtenerDelMaestro(User.ObtenerPerfilId());
    }
}
