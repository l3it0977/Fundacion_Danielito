using System.ComponentModel.DataAnnotations;

namespace SISTEMADANIELITO.DTOS;

public class CrearRolDto
{
    [Required(ErrorMessage = "El nombre del rol es obligatorio")]
    public string NombreRol { get; set; } = string.Empty;
}
