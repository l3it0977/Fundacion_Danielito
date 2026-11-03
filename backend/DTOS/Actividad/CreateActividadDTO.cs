namespace SISTEMADANIELITO.DTOS;

public class CreateActividadDTO
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }

    // Obligatoria: con ella se genera la lista de asistencia de la actividad.
    public DateTime Fecha_Actividad { get; set; }

    public string Tipo { get; set; } = string.Empty;

    // Participantes elegidos uno a uno, de cualquier grupo.
    public List<int> NinosId { get; set; } = [];
}
