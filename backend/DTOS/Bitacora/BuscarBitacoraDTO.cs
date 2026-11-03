namespace SISTEMADANIELITO.DTOS;

/// <summary>Filtros y paginación de la bitácora. Todo es opcional.</summary>
public class BuscarBitacoraDTO
{
    /// <summary>Nino, Grupo, Actividad, Visita o Usuario.</summary>
    public string? Entidad { get; set; }

    /// <summary>Creado, Modificado o Eliminado.</summary>
    public string? Accion { get; set; }

    public int? PerfilId { get; set; }

    /// <summary>Se busca dentro de la descripción, sin distinguir mayúsculas.</summary>
    public string? Texto { get; set; }

    /// <summary>Instantes en UTC; el frontend convierte los días locales que elige el usuario.</summary>
    public DateTime? Desde { get; set; }
    public DateTime? Hasta { get; set; }

    public int Pagina { get; set; } = 1;
    public int Tamano { get; set; } = 25;
}
