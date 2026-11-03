using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Visita
{
    [Key]
    public int Id { get; set; }

    [Required]
    public DateTime Fecha_Visita { get; set; }

    public string? Motivo { get; set; }

    public string? Observacion { get; set; }

    public string? Seguimiento { get; set; }

    //FORANEAS
    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;

    public int RealizadoPorId { get; set; }
    public Perfil RealizadoPor { get; set; } = null!;
}
