namespace SISTEMADANIELITO.DTOS;

public class CambiarRolUsuarioDTO
{
    public int PerfilId { get; set; }

    /// <summary>Nombre del rol: Directora, Administrador o Maestro.</summary>
    public string Rol { get; set; } = string.Empty;
}
