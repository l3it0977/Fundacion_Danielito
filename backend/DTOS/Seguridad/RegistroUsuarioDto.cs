using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.DTOS;

public class RegistroUsuarioDto
{
    [Required(ErrorMessage = "El email es obligatorio")]
    [EmailAddress(ErrorMessage = "El formato del email no es válido")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "La contraseña es obligatoria")]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 8)]
    public string Password { get; set; } = string.Empty;

    [Required(ErrorMessage = "El nombre es obligatorio")]
    [StringLength(255, ErrorMessage = "El nombre no puede superar los {1} caracteres")]
    public string Nombre { get; set; } = string.Empty;

    [Required(ErrorMessage = "El apellido es obligatorio")]
    [StringLength(255, ErrorMessage = "El apellido no puede superar los {1} caracteres")]
    public string Apellido { get; set; } = string.Empty;

    // Obligatorio: en este sistema todo usuario pertenece a un rol.
    [Required(ErrorMessage = "El rol es obligatorio")]
    public string Rol { get; set; } = string.Empty;
}
