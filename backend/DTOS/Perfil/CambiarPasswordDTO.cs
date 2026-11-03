using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.DTOS;

public class CambiarPasswordDTO
{
    /// <summary>Se exige la actual para que un token robado no alcance para secuestrar la cuenta.</summary>
    [Required(ErrorMessage = "La contraseña actual es obligatoria")]
    public string Password_Actual { get; set; } = string.Empty;

    [Required(ErrorMessage = "La nueva contraseña es obligatoria")]
    [StringLength(100, ErrorMessage = "La {0} debe tener al menos {2} caracteres.", MinimumLength = 8)]
    public string Password_Nueva { get; set; } = string.Empty;
}
