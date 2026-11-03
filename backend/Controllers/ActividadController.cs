using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class ActividadController : ControllerBase
{
    private readonly IActividadService _actividadService;

    public ActividadController(IActividadService actividadService)
    {
        _actividadService = actividadService;
    }

    //CREAR
    // Los maestros también organizan actividades y eligen a sus participantes.
    [Authorize(Roles = "Directora,Administrador,Maestro")]
    [HttpPost]
    public string Post(CreateActividadDTO actividad)
    {
        _actividadService.Guardar(actividad, User.ObtenerPerfilId());
        return "Actividad creada correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public List<ActividadDTO> Get()
    {
        return _actividadService.ObtenerTodos(User.FiltroMaestro());
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public ActividadDTO GetById(int id)
    {
        return _actividadService.ObtenerPorId(id, User.FiltroMaestro());
    }

    //ACTUALIZAR
    // El filtro hace que un maestro solo pueda tocar las actividades que él creó.
    [Authorize(Roles = "Directora,Administrador,Maestro")]
    [HttpPut]
    public string Put(UpdateActividadDTO actividad)
    {
        _actividadService.Modificar(actividad, User.FiltroMaestro());
        return "Actividad modificada correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador,Maestro")]
    [HttpDelete("{id}")]
    public string Delete(int id)
    {
        _actividadService.Eliminar(id, User.FiltroMaestro());
        return "Actividad eliminada correctamente";
    }

    //ASIGNAR NIÑOS
    [Authorize(Roles = "Directora,Administrador,Maestro")]
    [HttpPost("AsignarNinos")]
    public string AsignarNinos(AsignarNinosDTO asignacion)
    {
        _actividadService.AsignarNinos(asignacion, User.FiltroMaestro());
        return "Niños asignados correctamente";
    }
}
