using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.Models;

public class Grupo
{
    [Key]
    public int Id { get; set; }

    [Required(ErrorMessage = "El nombre del grupo es obligatorio")]
    [StringLength(255, ErrorMessage = "El nombre no puede superar los {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    public string? Descripcion { get; set; }

    public bool Activo { get; set; } = true;

    public List<Nino> Ninos { get; set; } = [];

    // Los maestros a cargo. Un grupo puede tener más de uno.
    public List<Grupo_Maestro> Grupo_Maestros { get; set; } = [];
}
