namespace SISTEMADANIELITO.DTOS;

public class UpdateNinoDTO
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateTime? Fecha_Nacimiento { get; set; }
    public string? Contacto { get; set; }
    public string? Curso { get; set; }
    public string? Colegio { get; set; }
    public bool Vulnerable { get; set; }
    public string? Motivo_Vulnerabilidad { get; set; }
    public string? Observaciones { get; set; }
    public bool Fue_Al_Encuentro { get; set; }
    public bool Bautizado { get; set; }
    public bool Asiste_Iglesia { get; set; }
    public string? Nombre_Iglesia { get; set; }
    public string? Nombre_Celula { get; set; }
    public bool Activo { get; set; }
    public decimal? Latitud { get; set; }
    public decimal? Longitud { get; set; }

    // Editable: al cambiar de grupo cambia también el maestro a cargo, que ya no se guarda aparte.
    public int GrupoId { get; set; }
}
