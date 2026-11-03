namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Un usuario visto desde la administración: combina el perfil (nombre, apellido, estado)
/// con los datos que viven en Identity (correo y rol).
/// </summary>
public class UsuarioDTO
{
    /// <summary>Id del perfil. Es el que se usa en el resto de los endpoints.</summary>
    public int Id { get; set; }

    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Rol { get; set; } = string.Empty;
    public bool Activo { get; set; }

    /// <summary>Grupos a cargo. Solo tiene contenido cuando el usuario es Maestro.</summary>
    public List<string> Mis_Grupos { get; set; } = [];
}
