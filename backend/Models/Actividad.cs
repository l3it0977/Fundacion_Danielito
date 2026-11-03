using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Actividad
{
    public const string TipoNormal = "normal";
    public const string TipoIntervencion = "intervencion";

    public static readonly string[] TiposValidos = [TipoNormal, TipoIntervencion];

    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre de la actividad es obligatorio")]
    [StringLength(255, ErrorMessage = "El nombre no puede superar los {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    // Obligatoria: al crear la actividad se genera su lista de asistencia,
    // y esa lista necesita un día concreto al que corresponder.
    [Required(ErrorMessage = "La fecha de la actividad es obligatoria")]
    public DateTime Fecha_Actividad { get; set; }

    [Required(ErrorMessage = "El tipo es obligatorio")]
    [StringLength(50)]
    public string Tipo { get; set; } = string.Empty;

    public bool Activa { get; set; } = true;

    // Los participantes se eligen uno a uno, de cualquier grupo. Los grupos que
    // aparecen en paneles y reportes se deducen de estos niños.
    public List<Actividad_Nino> Actividad_Ninos { get; set; } = [];
    public List<Asistencia> Asistencias { get; set; } = [];

    //FORANEAS
    public int CreadoPorId { get; set; }
    public Perfil CreadoPor { get; set; } = null!;
}
