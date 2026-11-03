using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.DTOS;

public class ActualizarPerfilDTO
{
    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(255, ErrorMessage = "El nombre no puede superar los {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(255, ErrorMessage = "El apellido no puede superar los {1} caracteres")]
    public string Apellido { get; set; } = string.Empty;

    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;
}
