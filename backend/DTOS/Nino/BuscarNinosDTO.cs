namespace SISTEMADANIELITO.DTOS;

/// <summary>Filtros y paginación para listar niños. Todo es opcional.</summary>
public class BuscarNinosDTO
{
    /// <summary>Se busca dentro de nombres, apellidos y código, sin distinguir mayúsculas.</summary>
    public string? Texto { get; set; }

    public int? GrupoId { get; set; }

    /// <summary>Por defecto solo se listan los activos.</summary>
    public bool IncluirInactivos { get; set; }

    public int Pagina { get; set; } = 1;

    /// <summary>Se limita a entre 1 y 100 resultados por página.</summary>
    public int Tamano { get; set; } = 25;
}
