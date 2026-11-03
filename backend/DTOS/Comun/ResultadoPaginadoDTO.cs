namespace SISTEMADANIELITO.DTOS;

/// <summary>Una página de resultados junto con lo necesario para armar la paginación.</summary>
public class ResultadoPaginadoDTO<T>
{
    public List<T> Items { get; set; } = [];

    /// <summary>Cantidad de resultados en total, sin contar la paginación.</summary>
    public int Total { get; set; }

    /// <summary>Página realmente devuelta: si se pidió una que no existe, es la última.</summary>
    public int Pagina { get; set; }

    public int Tamano { get; set; }

    public int TotalPaginas => Tamano <= 0 ? 0 : (int)Math.Ceiling(Total / (double)Tamano);
}
