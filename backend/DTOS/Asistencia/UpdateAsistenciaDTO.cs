namespace SISTEMADANIELITO.DTOS;

public class UpdateAsistenciaDTO
{
    public int Id { get; set; }
    public DateOnly Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Motivo_Justificacion { get; set; }
    public string? Observacion { get; set; }
}
