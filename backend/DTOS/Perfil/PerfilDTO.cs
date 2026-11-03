namespace SISTEMADANIELITO.DTOS;

/// <summary>Datos del usuario que está usando el sistema.</summary>
public class PerfilDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string Apellido { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public bool Activo { get; set; }
    public string Rol { get; set; } = string.Empty;

    /// <summary>Grupos a cargo. Solo tiene contenido cuando el usuario es Maestro.</summary>
    public List<string> Mis_Grupos { get; set; } = [];
}
