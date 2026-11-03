namespace SISTEMADANIELITO.DTOS;

public class AsignarNinosDTO
{
    public int ActividadId { get; set; }

    // Reemplaza por completo la lista de participantes de la actividad.
    public List<int> NinosId { get; set; } = [];
}
