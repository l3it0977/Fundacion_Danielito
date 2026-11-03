namespace SISTEMADANIELITO.DTOS;

public class ActividadDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public DateTime Fecha_Actividad { get; set; }
    public string Tipo { get; set; } = string.Empty;
    public bool Activa { get; set; }

    public int CreadoPorId { get; set; }
    public string Nombre_Creador { get; set; } = string.Empty;

    public List<int> Ninos { get; set; } = [];

    // Se deducen de los niños participantes, no se asignan a mano.
    public List<string> Grupos { get; set; } = [];
}
