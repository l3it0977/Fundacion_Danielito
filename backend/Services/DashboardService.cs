using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

/// <summary>
/// Arma las cifras de los paneles.
/// Todas las consultas están en LINQ y no usan SQL escrito a mano, así que el código
/// no queda atado a la sintaxis de un motor de base de datos en particular.
/// </summary>
public class DashboardService : IDashboardService
{
    /// <summary>Resultado de agrupar las asistencias por estado.</summary>
    private class ConteoPorEstado
    {
        public string Estado { get; set; } = string.Empty;
        public int Cantidad { get; set; }
    }

    private AppProyectoContext _context;

    public DashboardService(AppProyectoContext context)
    {
        _context = context;
    }

    //PANEL GENERAL (DIRECTORA Y ADMINISTRADOR)
    public DashboardGeneralDTO ObtenerGeneral()
    {
        // Un solo recorrido de la tabla de asistencias para los tres estados.
        List<ConteoPorEstado> conteoEstados = _context.Asistencias
            .GroupBy(x => x.Estado)
            .Select(grupo => new ConteoPorEstado { Estado = grupo.Key, Cantidad = grupo.Count() })
            .ToList();

        int presentes = BuscarCantidad(conteoEstados, Asistencia.EstadoPresente);
        int ausentes = BuscarCantidad(conteoEstados, Asistencia.EstadoAusente);
        int justificados = BuscarCantidad(conteoEstados, Asistencia.EstadoJustificado);

        // Los pendientes quedan fuera del total: son listas que todavía nadie pasó.
        // Contarlos hundiría el porcentaje apenas se crea una actividad.
        int totalAsistencias = presentes + ausentes + justificados;

        // Equivale al SUM(CASE WHEN estado = 'presente'...) agrupado por grupo.
        List<AsistenciaPorGrupoDTO> asistenciaPorGrupo = _context.Asistencias
            .GroupBy(x => x.Nino.Grupo.Nombre)
            .Select(grupo => new AsistenciaPorGrupoDTO
            {
                Grupo = grupo.Key,
                Total = grupo.Count(x => x.Estado != Asistencia.EstadoPendiente),
                Presentes = grupo.Count(x => x.Estado == Asistencia.EstadoPresente),
                Ausentes = grupo.Count(x => x.Estado == Asistencia.EstadoAusente),
                Justificados = grupo.Count(x => x.Estado == Asistencia.EstadoJustificado)
            })
            .OrderBy(x => x.Grupo)
            .ToList();

        List<ActividadResumenDTO> ultimasActividades = _context.Actividades
            .OrderByDescending(x => x.Fecha_Actividad)
            .Take(5)
            .Select(x => new ActividadResumenDTO
            {
                Id = x.Id,
                Nombre = x.Nombre,
                Fecha_Actividad = x.Fecha_Actividad,
                Tipo = x.Tipo,
                Grupos = x.Actividad_Ninos.Select(an => an.Nino.Grupo.Nombre).Distinct().ToList()
            })
            .ToList();

        List<NinoResumenDTO> listaVulnerables = _context.Ninos
            .Where(x => x.Vulnerable)
            .OrderBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .Select(x => new NinoResumenDTO
            {
                Id = x.Id,
                Codigo = x.Codigo,
                Nombre_Completo = x.Nombres + " " + x.Apellidos,
                Grupo = x.Grupo.Nombre,
                Motivo_Vulnerabilidad = x.Motivo_Vulnerabilidad
            })
            .ToList();

        return new DashboardGeneralDTO
        {
            Total_Ninos = _context.Ninos.Count(),
            Total_Grupos = _context.Grupos.Count(),
            Total_Actividades = _context.Actividades.Count(),
            Total_Asistencias = totalAsistencias,
            Ninos_Vulnerables = listaVulnerables.Count,
            Presentes = presentes,
            Ausentes = ausentes,
            Justificados = justificados,
            Porcentaje_Asistencia = CalcularPorcentaje(presentes, totalAsistencias),
            Asistencia_Por_Grupo = asistenciaPorGrupo,
            Ultimas_Actividades = ultimasActividades,
            Lista_Vulnerables = listaVulnerables
        };
    }

    //PANEL DEL MAESTRO
    public DashboardMaestroDTO ObtenerDelMaestro(int maestroId)
    {
        // Todo el panel se limita a los niños de los grupos de este maestro.
        IQueryable<Nino> misNinos = _context.Ninos
            .Where(x => x.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId));

        // Rango de fechas y no Month/Year: así PostgreSQL puede usar el índice de Fecha.
        DateTime inicioDelMes = new(DateTime.Today.Year, DateTime.Today.Month, 1);
        DateTime inicioDelMesSiguiente = inicioDelMes.AddMonths(1);

        List<ConteoPorEstado> conteoDelMes = _context.Asistencias
            .Where(x => x.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId))
            .Where(x => x.Fecha >= inicioDelMes && x.Fecha < inicioDelMesSiguiente)
            .GroupBy(x => x.Estado)
            .Select(grupo => new ConteoPorEstado { Estado = grupo.Key, Cantidad = grupo.Count() })
            .ToList();

        int presentesDelMes = BuscarCantidad(conteoDelMes, Asistencia.EstadoPresente);
        int ausentesDelMes = BuscarCantidad(conteoDelMes, Asistencia.EstadoAusente);
        int justificadosDelMes = BuscarCantidad(conteoDelMes, Asistencia.EstadoJustificado);

        // Igual que en el panel general: los pendientes no cuentan como lista pasada.
        int totalDelMes = presentesDelMes + ausentesDelMes + justificadosDelMes;

        List<TopAsistenteDTO> masAsistentes = _context.Asistencias
            .Where(x => x.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId))
            .Where(x => x.Estado == Asistencia.EstadoPresente)
            .GroupBy(x => new { x.NinoId, x.Nino.Nombres, x.Nino.Apellidos })
            .Select(grupo => new TopAsistenteDTO
            {
                NinoId = grupo.Key.NinoId,
                Nombre_Completo = grupo.Key.Nombres + " " + grupo.Key.Apellidos,
                Total_Asistencias = grupo.Count()
            })
            .OrderByDescending(x => x.Total_Asistencias)
            .Take(5)
            .ToList();

        return new DashboardMaestroDTO
        {
            Total_Mis_Ninos = misNinos.Count(),
            Total_Mis_Actividades = _context.Actividades
                .Count(x => x.CreadoPorId == maestroId ||
                    x.Actividad_Ninos.Any(an =>
                        an.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId))),
            Total_Mis_Asistencias = _context.Asistencias
                .Count(x => x.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId)),
            Cumpleanos_Cercanos = ObtenerCumpleanosCercanos(maestroId),
            Mis_Mas_Asistentes = masAsistentes,
            Asistencia_Del_Mes = new ResumenAsistenciaDTO
            {
                Total = totalDelMes,
                Presentes = presentesDelMes,
                Ausentes = ausentesDelMes,
                Justificados = justificadosDelMes,
                Porcentaje_Asistencia = CalcularPorcentaje(presentesDelMes, totalDelMes)
            }
        };
    }

    /// <summary>
    /// Niños que cumplen años dentro de los próximos 30 días.
    /// El cálculo se hace en memoria porque implica mover el cumpleaños al año actual
    /// (y al siguiente si ya pasó), algo que no se puede traducir a SQL directamente.
    /// </summary>
    private List<CumpleanosDTO> ObtenerCumpleanosCercanos(int maestroId)
    {
        DateTime hoy = DateTime.Today;

        var ninosConFecha = _context.Ninos
            .Where(x => x.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId)
                && x.Fecha_Nacimiento != null)
            .Select(x => new
            {
                x.Id,
                x.Nombres,
                x.Apellidos,
                Fecha_Nacimiento = x.Fecha_Nacimiento!.Value
            })
            .ToList();

        List<CumpleanosDTO> proximos = [];

        foreach (var nino in ninosConFecha)
        {
            DateTime nacimiento = nino.Fecha_Nacimiento.Date;

            // Se ubica el cumpleaños en el año actual; si ya pasó, se pasa al año siguiente.
            DateTime proximoCumple = ConstruirCumple(nacimiento, hoy.Year);
            if (proximoCumple < hoy)
            {
                proximoCumple = ConstruirCumple(nacimiento, hoy.Year + 1);
            }

            int diasRestantes = (proximoCumple - hoy).Days;
            if (diasRestantes > 30)
            {
                continue;
            }

            proximos.Add(new CumpleanosDTO
            {
                NinoId = nino.Id,
                Nombre_Completo = nino.Nombres + " " + nino.Apellidos,
                Fecha_Nacimiento = nacimiento,
                Edad_Que_Cumple = proximoCumple.Year - nacimiento.Year,
                Dias_Restantes = diasRestantes
            });
        }

        return proximos
            .OrderBy(x => x.Dias_Restantes)
            .Take(5)
            .ToList();
    }

    /// <summary>
    /// Arma la fecha del cumpleaños en el año indicado. Contempla a los nacidos un 29 de
    /// febrero: en años no bisiestos su cumpleaños se toma como el 28.
    /// </summary>
    private static DateTime ConstruirCumple(DateTime nacimiento, int anio)
    {
        int diasDelMes = DateTime.DaysInMonth(anio, nacimiento.Month);
        int dia = Math.Min(nacimiento.Day, diasDelMes);

        return new DateTime(anio, nacimiento.Month, dia);
    }

    /// <summary>Busca cuántas asistencias hay de un estado; 0 si ese estado no aparece.</summary>
    private static int BuscarCantidad(List<ConteoPorEstado> conteos, string estado)
    {
        return conteos.FirstOrDefault(x => x.Estado == estado)?.Cantidad ?? 0;
    }

    /// <summary>Porcentaje con un decimal; devuelve 0 cuando no hay registros.</summary>
    private static double CalcularPorcentaje(int parte, int total)
    {
        if (total == 0)
        {
            return 0;
        }

        return Math.Round((double)parte / total * 100, 1);
    }
}
