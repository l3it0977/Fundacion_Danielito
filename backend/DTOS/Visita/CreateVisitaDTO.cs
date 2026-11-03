namespace SISTEMADANIELITO.DTOS;

public class CreateVisitaDTO
{
    public int NinoId { get; set; }
    public DateTime Fecha_Visita { get; set; }
    public string? Motivo { get; set; }
    public string? Observacion { get; set; }
    public string? Seguimiento { get; set; }

    // RealizadoPorId sale del token.
}
