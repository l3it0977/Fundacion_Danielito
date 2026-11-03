namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Activa o desactiva a un usuario. Se usa en lugar de borrarlo, para no perder
/// el historial de asistencias y actividades que esa persona registró.
/// </summary>
public class CambiarEstadoUsuarioDTO
{
    public int PerfilId { get; set; }
    public bool Activo { get; set; }
}
