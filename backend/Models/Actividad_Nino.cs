namespace SISTEMADANIELITO.Models;

public class Actividad_Nino
{
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;
}
