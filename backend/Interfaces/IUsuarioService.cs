using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

/// <summary>
/// Administración de usuarios. Es async porque trabaja con Identity, igual que el módulo
/// de seguridad. Todos los métodos que modifican reciben el perfil de quien ejecuta la
/// acción, para impedir que alguien se deje a sí mismo fuera del sistema.
/// </summary>
public interface IUsuarioService
{
    public Task<List<UsuarioDTO>> ObtenerTodos();
    public Task<UsuarioDTO> ObtenerPorId(int perfilId);
    public Task CambiarEstado(CambiarEstadoUsuarioDTO cambio, int perfilQueEjecuta);
    public Task CambiarRol(CambiarRolUsuarioDTO cambio, int perfilQueEjecuta);
}
