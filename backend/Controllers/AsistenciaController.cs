using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class AsistenciaController : ControllerBase
{
    private readonly IAsistenciaService _asistenciaService;

    public AsistenciaController(IAsistenciaService asistenciaService)
    {
        _asistenciaService = asistenciaService;
    }

    //CREAR
    // FiltroMaestro es nulo para Directora y Administrador; para un maestro, limita
    // la acción a las actividades a las que tiene acceso.
    [Authorize]
    [HttpPost]
    public string Post(CreateAsistenciaDTO asistencia)
    {
        _asistenciaService.Guardar(asistencia, User.ObtenerPerfilId(), User.FiltroMaestro());
        return "Asistencia registrada correctamente";
    }

    //REGISTRAR POR ACTIVIDAD
    [Authorize]
    [HttpPost("RegistrarPorActividad")]
    public string RegistrarPorActividad(RegistrarAsistenciasDTO registro)
    {
        _asistenciaService.RegistrarPorActividad(registro, User.ObtenerPerfilId(), User.FiltroMaestro());
        return "Asistencias registradas correctamente";
    }

    //OBTENER TODOS
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet]
    public List<AsistenciaDTO> Get()
    {
        return _asistenciaService.ObtenerTodos();
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public AsistenciaDTO GetById(int id)
    {
        return _asistenciaService.ObtenerPorId(id, User.FiltroMaestro());
    }

    //OBTENER POR ACTIVIDAD
    [Authorize]
    [HttpGet("Actividad/{actividadId}")]
    public List<AsistenciaDTO> GetPorActividad(int actividadId)
    {
        return _asistenciaService.ObtenerPorActividad(actividadId, User.FiltroMaestro());
    }

    //ACTUALIZAR
    [Authorize]
    [HttpPut]
    public string Put(UpdateAsistenciaDTO asistencia)
    {
        _asistenciaService.Modificar(asistencia, User.FiltroMaestro());
        return "Asistencia modificada correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public string Delete(int id)
    {
        _asistenciaService.Eliminar(id);
        return "Asistencia eliminada correctamente";
    }
}
