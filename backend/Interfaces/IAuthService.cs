using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

/// <summary>
/// Inicio de sesión y alta de usuarios y roles. Es async porque trabaja con Identity.
/// Los fallos salen como excepciones de la aplicación (ver Excepciones) y el middleware
/// las traduce al código HTTP que corresponde.
/// </summary>
public interface IAuthService
{
    /// <summary>Crea el usuario de Identity junto con su perfil del dominio.</summary>
    public Task RegistrarUsuario(RegistroUsuarioDto usuario);

    public Task CrearRol(CrearRolDto rol);

    /// <summary>Valida las credenciales y entrega el JWT con los roles y el perfil del usuario.</summary>
    public Task<RespuestaAutenticacionDto> Login(LoginDto login);
}
