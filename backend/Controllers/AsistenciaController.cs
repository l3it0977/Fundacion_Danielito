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
    public async Task<string> Post(CreateAsistenciaDTO asistencia)
    {
        await _asistenciaService.Guardar(asistencia, User.ObtenerPerfilId(), User.FiltroMaestro());
        return "Asistencia registrada correctamente";
    }

    //REGISTRAR POR ACTIVIDAD
    [Authorize]
    [HttpPost("RegistrarPorActividad")]
    public async Task<string> RegistrarPorActividad(RegistrarAsistenciasDTO registro)
    {
        await _asistenciaService.RegistrarPorActividad(registro, User.ObtenerPerfilId(), User.FiltroMaestro());
        return "Asistencias registradas correctamente";
    }

    //OBTENER TODOS
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet]
    public async Task<List<AsistenciaDTO>> Get()
    {
        return await _asistenciaService.ObtenerTodos();
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public async Task<AsistenciaDTO> GetById(int id)
    {
        return await _asistenciaService.ObtenerPorId(id, User.FiltroMaestro());
    }

    //OBTENER POR ACTIVIDAD
    [Authorize]
    [HttpGet("Actividad/{actividadId}")]
    public async Task<List<AsistenciaDTO>> GetPorActividad(int actividadId)
    {
        return await _asistenciaService.ObtenerPorActividad(actividadId, User.FiltroMaestro());
    }

    //ACTUALIZAR
    [Authorize]
    [HttpPut]
    public async Task<string> Put(UpdateAsistenciaDTO asistencia)
    {
        await _asistenciaService.Modificar(asistencia, User.FiltroMaestro());
        return "Asistencia modificada correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        await _asistenciaService.Eliminar(id);
        return "Asistencia eliminada correctamente";
    }
}
