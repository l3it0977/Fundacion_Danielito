namespace SISTEMADANIELITO.DTOS;

public class CreateGrupoDTO
{
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; } = true;

    // Ids de los perfiles que quedan a cargo del grupo. Debe haber al menos uno.
    public List<int> MaestrosId { get; set; } = [];
}
