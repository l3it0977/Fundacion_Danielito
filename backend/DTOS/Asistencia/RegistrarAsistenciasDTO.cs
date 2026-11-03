namespace SISTEMADANIELITO.DTOS;

// Toma de asistencia de una sesión completa: todos los niños en un solo envío.
public class RegistrarAsistenciasDTO
{
    public int ActividadId { get; set; }
    public DateTime Fecha { get; set; }
    public List<AsistenciaItemDTO> Asistencias { get; set; } = [];
}

public class AsistenciaItemDTO
{
    public int NinoId { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Motivo_Justificacion { get; set; }
    public string? Observacion { get; set; }
}
