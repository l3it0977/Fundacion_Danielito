namespace SISTEMADANIELITO.DTOS;

public class AsistenciaDTO
{
    public int Id { get; set; }
    public DateTime Fecha { get; set; }
    public string Estado { get; set; } = string.Empty;
    public string? Motivo_Justificacion { get; set; }
    public string? Observacion { get; set; }

    public int ActividadId { get; set; }
    public string Nombre_Actividad { get; set; } = string.Empty;

    public int NinoId { get; set; }
    public string Nombre_Nino { get; set; } = string.Empty;

    public int RegistradoPorId { get; set; }
    public string Nombre_Registrador { get; set; } = string.Empty;
}
