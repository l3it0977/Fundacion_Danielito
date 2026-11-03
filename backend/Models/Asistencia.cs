using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Asistencia
{
    // Estado con el que nacen los registros al crear la actividad: distingue
    // "todavía nadie pasó lista" de "el niño faltó".
    public const string EstadoPendiente = "pendiente";
    public const string EstadoPresente = "presente";
    public const string EstadoAusente = "ausente";
    public const string EstadoJustificado = "justificado";

    public static readonly string[] EstadosValidos =
        [EstadoPendiente, EstadoPresente, EstadoAusente, EstadoJustificado];

    /// <summary>Estados que cuentan como lista ya pasada, para calcular porcentajes.</summary>
    public static readonly string[] EstadosRegistrados =
        [EstadoPresente, EstadoAusente, EstadoJustificado];

    [Key]
    public int Id { get; set; }

    // Una actividad puede tener varias sesiones, por eso la fecha es parte de la identidad
    // del registro junto con la actividad y el niño.
    [Required]
    public DateTime Fecha { get; set; }

    [Required(ErrorMessage = "El estado es obligatorio")]
    [StringLength(20)]
    public string Estado { get; set; } = string.Empty;

    public string? Motivo_Justificacion { get; set; }

    public string? Observacion { get; set; }

    //FORANEAS
    public int ActividadId { get; set; }
    public Actividad Actividad { get; set; } = null!;

    public int NinoId { get; set; }
    public Nino Nino { get; set; } = null!;

    public int RegistradoPorId { get; set; }
    public Perfil RegistradoPor { get; set; } = null!;
}
