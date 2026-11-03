namespace SISTEMADANIELITO.DTOS;

public class UpdateActividadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateOnly Fecha_Actividad { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public bool Activa { get; set; }
    public List<int> NinosId { get; set; } = [];
}
