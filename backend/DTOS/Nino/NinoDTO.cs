namespace SISTEMADANIELITO.DTOS;

public class NinoDTO
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombres { get; set; } = string.Empty;
    public string Apellidos { get; set; } = string.Empty;
    public DateOnly? Fecha_Nacimiento { get; set; }
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

    public int GrupoId { get; set; }
    public string Nombre_Grupo { get; set; } = string.Empty;

    // Vienen del grupo, que puede tener varios maestros a cargo.
    public List<int> MaestrosId { get; set; } = [];
    public List<string> Nombres_Maestros { get; set; } = [];

    // Calculada, no almacenada: guardarla la dejaba desactualizada con el paso del tiempo.
    public int? Edad
    {
        get
        {
            if (Fecha_Nacimiento == null)
            {
                return null;
            }

            DateOnly hoy = DateOnly.FromDateTime(DateTime.Today);
            DateOnly nacimiento = Fecha_Nacimiento.Value;

            int edad = hoy.Year - nacimiento.Year;
            if (nacimiento > hoy.AddYears(-edad))
            {
                edad--;
            }

            return edad;
        }
    }
}
