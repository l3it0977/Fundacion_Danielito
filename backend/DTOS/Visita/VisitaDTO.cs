namespace SISTEMADANIELITO.DTOS;

public class VisitaDTO
{
    public int Id { get; set; }
    public DateTime Fecha_Visita { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string? Seguimiento { get; set; }

    public int NinoId { get; set; }
    public string Nombre_Nino { get; set; } = string.Empty;

    public int RealizadoPorId { get; set; }
    public string Nombre_Realizador { get; set; } = string.Empty;
}
