namespace SISTEMADANIELITO.DTOS;

public class BitacoraDTO
{
    public int Id { get; set; }

    /// <summary>Instante del cambio, en UTC.</summary>
    public DateTime Fecha { get; set; }

    public int? PerfilId { get; set; }
    public string Usuario { get; set; } = string.Empty;
    public string Entidad { get; set; } = string.Empty;
    public int? EntidadId { get; set; }
    public string Accion { get; set; } = string.Empty;
    public string Descripcion { get; set; } = string.Empty;

    /// <summary>Solo en las modificaciones: qué campos cambiaron, con su valor anterior y el nuevo.</summary>
    public List<CambioBitacoraDTO> Cambios { get; set; } = [];
}

public class CambioBitacoraDTO
{
    public string Campo { get; set; } = string.Empty;
    public string? Antes { get; set; }
    public string? Despues { get; set; }
}
