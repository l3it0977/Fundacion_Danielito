namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Activa o desactiva a un niño. Se usa en lugar de borrarlo, para no perder su
/// historial de asistencias, visitas y actividades.
/// </summary>
public class CambiarEstadoNinoDTO
{
    public int NinoId { get; set; }
    public bool Activo { get; set; }
}
