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
    public string Post(CreateVisitaDTO visita)
    {
        _visitaService.Guardar(visita, User.ObtenerPerfilId());
        return "Visita creada correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public List<VisitaDTO> Get()
    {
        return _visitaService.ObtenerTodos(User.FiltroMaestro());
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public VisitaDTO GetById(int id)
    {
        return _visitaService.ObtenerPorId(id);
    }

    //ACTUALIZAR
    [Authorize]
    [HttpPut]
    public string Put(UpdateVisitaDTO visita)
    {
        _visitaService.Modificar(visita);
        return "Visita modificada correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public string Delete(int id)
    {
        _visitaService.Eliminar(id);
        return "Visita eliminada correctamente";
    }
}
