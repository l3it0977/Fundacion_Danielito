using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

/// <summary>
/// Administración de usuarios.
///
/// No hay endpoint para eliminar: los usuarios se desactivan, porque borrarlos haría
/// perder el rastro de quién registró cada asistencia y cada actividad.
/// </summary>
[ApiController]
[Route("[controller]")]
public class UsuarioController : ControllerBase
{
    private readonly IUsuarioService _usuarioService;

    public UsuarioController(IUsuarioService usuarioService)
    {
        _usuarioService = usuarioService;
    }

    //OBTENER TODOS
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet]
    public async Task<List<UsuarioDTO>> Get()
    {
        return await _usuarioService.ObtenerTodos();
    }

    //OBTENER POR ID
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("{Id}")]
    public async Task<UsuarioDTO> GetById(int id)
    {
        return await _usuarioService.ObtenerPorId(id);
    }

    //ACTIVAR O DESACTIVAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut("Estado")]
    public async Task<string> PutEstado(CambiarEstadoUsuarioDTO cambio)
    {
        // Se pasa quién ejecuta la acción para impedir que se desactive a sí mismo.
        await _usuarioService.CambiarEstado(cambio, User.ObtenerPerfilId());

        return cambio.Activo
            ? "Usuario activado correctamente"
            : "Usuario desactivado correctamente";
    }

    //CAMBIAR ROL
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut("Rol")]
    public async Task<string> PutRol(CambiarRolUsuarioDTO cambio)
    {
        await _usuarioService.CambiarRol(cambio, User.ObtenerPerfilId());

        return "Rol actualizado correctamente";
    }
}
