namespace SISTEMADANIELITO.DTOS;

public class GrupoDTO
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
    public string? Descripcion { get; set; }
    public bool Activo { get; set; }
    public int Cantidad_Ninos { get; set; }

    // Un grupo puede tener varios maestros a cargo.
    public List<int> MaestrosId { get; set; } = [];
    public List<string> Nombres_Maestros { get; set; } = [];
}
