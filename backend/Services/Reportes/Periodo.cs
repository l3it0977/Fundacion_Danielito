using System.Globalization;
using SISTEMADANIELITO.Excepciones;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>Ayudantes del periodo (mes y año) que se elige al pedir un reporte.</summary>
internal static class Periodo
{
    /// <summary>Para que los nombres de los meses salgan en español, como en los reportes originales.</summary>
    private static readonly CultureInfo CulturaEspanol = new("es-ES");

    public static void Validar(int? mes, int anio)
    {
        if (mes.HasValue && (mes < 1 || mes > 12))
        {
            throw (new ReglaNegocioException("El mes debe estar entre 1 y 12"));
        }

        if (anio < 2000 || anio > 2100)
        {
            throw (new ReglaNegocioException("El año indicado no es válido"));
        }
    }

    /// <summary>Desde el primer día del periodo (incluido) hasta el siguiente al último (excluido).</summary>
    public static (DateOnly Inicio, DateOnly Fin) Rango(int? mes, int anio)
    {
        DateOnly inicio = new(anio, mes ?? 1, 1);
        DateOnly fin = mes.HasValue ? inicio.AddMonths(1) : inicio.AddYears(1);

        return (inicio, fin);
    }

    public static string NombreDelMes(int mes)
    {
        string nombre = CulturaEspanol.DateTimeFormat.GetMonthName(mes);

        // GetMonthName devuelve el nombre en minúscula.
        return char.ToUpper(nombre[0]) + nombre[1..];
    }

    public static string Texto(int? mes, int anio)
    {
        return mes.HasValue
            ? $"Periodo: {NombreDelMes(mes.Value)} de {anio}"
            : $"Periodo: todos los meses de {anio}";
    }

    public static string TextoDelGrupo(string? nombreDelGrupo)
    {
        return nombreDelGrupo != null
            ? $"Grupo: {nombreDelGrupo}"
            : "Grupo: todos los grupos de edad";
    }
}
