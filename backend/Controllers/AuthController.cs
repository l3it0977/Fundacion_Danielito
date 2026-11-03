using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly IAuthService _authService;

    public AuthController(IAuthService authService)
    {
        _authService = authService;
    }

    //CREAR USUARIO
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPost("RegistrarUsuario")]
    public async Task<MensajeDto> Registrar(RegistroUsuarioDto usuario)
    {
        await _authService.RegistrarUsuario(usuario);
        return new MensajeDto("Usuario creado correctamente.");
    }

    //CREAR ROL
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPost("CrearRol")]
    public async Task<MensajeDto> CrearRol(CrearRolDto rol)
    {
        await _authService.CrearRol(rol);
        return new MensajeDto($"Rol '{rol.NombreRol}' creado correctamente.");
    }

    //LOGIN
    [AllowAnonymous]
    [EnableRateLimiting("PoliticaLogin")]
    [HttpPost("Login")]
    public async Task<RespuestaAutenticacionDto> Login(LoginDto login)
    {
        return await _authService.Login(login);
    }
}
