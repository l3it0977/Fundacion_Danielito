using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Perfil
{
    [Key]
    public int Id { get; set; }

    // Id de AspNetUsers. Sin FK: Identity y el dominio son contextos distintos.
    [Required]
    [StringLength(450)]
    public string UsuarioId { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(255, ErrorMessage = "El nombre no puede superar los {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(255, ErrorMessage = "El apellido no puede superar los {1} caracteres")]
    public string Apellido { get; set; } = string.Empty;

    public bool Activo { get; set; } = true;

    // Grupos que tiene a cargo, a través de la tabla intermedia.
    public List<Grupo_Maestro> Grupo_Maestros { get; set; } = [];
    public List<Actividad> Actividades { get; set; } = [];
    public List<Asistencia> Asistencias_Registradas { get; set; } = [];
    public List<Visita> Visitas_Realizadas { get; set; } = [];
}
