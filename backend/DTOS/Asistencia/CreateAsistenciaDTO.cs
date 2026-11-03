namespace SISTEMADANIELITO.DTOS;

public class CreateAsistenciaDTO
{
    public int ActividadId { get; set; }
    public int NinoId { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Motivo_Justificacion { get; set; }
    public string? Observacion { get; set; }

    // RegistradoPorId no se recibe: sale del token, para que nadie registre a nombre de otro.
}
