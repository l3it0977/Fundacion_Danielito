namespace SISTEMADANIELITO.DTOS;

public class UpdateGrupoDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }

    // Reemplaza por completo la lista de maestros a cargo del grupo.
    public List<int> MaestrosId { get; set; } = [];
}
