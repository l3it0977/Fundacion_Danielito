using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Nino
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El código es obligatorio")]
    [StringLength(255, ErrorMessage = "El código no puede superar los {1} caracteres")]
    public string Codigo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los nombres son obligatorios")]
    [StringLength(255, ErrorMessage = "Los nombres no pueden superar los {1} caracteres")]
    public string Nombres { get; set; } = string.Empty;

    [Required(ErrorMessage = "Los apellidos son obligatorios")]
    [StringLength(255, ErrorMessage = "Los apellidos no pueden superar los {1} caracteres")]
    public string Apellidos { get; set; } = string.Empty;

    // La edad no se guarda: se calcula desde esta fecha al exponer el DTO.
    public DateTime? Fecha_Nacimiento { get; set; }

    [StringLength(255)]
    public string? Contacto { get; set; }

    [StringLength(255)]
    public string? Curso { get; set; }

    [StringLength(255)]
    public string? Colegio { get; set; }

    public bool Vulnerable { get; set; }

    public string? Motivo_Vulnerabilidad { get; set; }

    public string? Observaciones { get; set; }

    public bool Fue_Al_Encuentro { get; set; }

    public bool Bautizado { get; set; }

    public bool Asiste_Iglesia { get; set; }

    [StringLength(255)]
    public string? Nombre_Iglesia { get; set; }

    [StringLength(255)]
    public string? Nombre_Celula { get; set; }

    public bool Activo { get; set; } = true;

    public decimal? Latitud { get; set; }

    public decimal? Longitud { get; set; }

    public List<Asistencia> Asistencias { get; set; } = [];
    public List<Visita> Visitas { get; set; } = [];
    public List<Actividad_Nino> Actividad_Ninos { get; set; } = [];

    //FORANEAS
    // El maestro no se guarda aquí: se obtiene por Grupo.Maestro, así no pueden contradecirse.
    public int GrupoId { get; set; }
    public Grupo Grupo { get; set; } = null!;
}
