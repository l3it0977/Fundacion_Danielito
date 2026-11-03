using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class VisitaController : ControllerBase
{
    private readonly IVisitaService _visitaService;

    public VisitaController(IVisitaService visitaService)
    {
        _visitaService = visitaService;
    }

    //CREAR
    [Authorize]
    [HttpPost]
    public async Task<string> Post(CreateVisitaDTO visita)
    {
        await _visitaService.Guardar(visita, User.ObtenerPerfilId());
        return "Visita creada correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public async Task<List<VisitaDTO>> Get()
    {
        return await _visitaService.ObtenerTodos(User.FiltroMaestro());
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public async Task<VisitaDTO> GetById(int id)
    {
        return await _visitaService.ObtenerPorId(id);
    }

    //ACTUALIZAR
    [Authorize]
    [HttpPut]
    public async Task<string> Put(UpdateVisitaDTO visita)
    {
        await _visitaService.Modificar(visita);
        return "Visita modificada correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        await _visitaService.Eliminar(id);
        return "Visita eliminada correctamente";
    }
}
