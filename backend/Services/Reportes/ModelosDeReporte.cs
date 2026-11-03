using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services.Reportes;

internal sealed record RegistroAsistencia(int NinoId, int GrupoId, DateOnly Fecha, string Estado);

internal sealed record NinoDelReporte(int Id, int GrupoId, string Nombre_Completo, bool Activo);

internal sealed record FilaResumen(string Etiqueta, int? Ninos, Totales Datos);

/// <summary>Conteo por estado. Solo las sesiones con lista pasada entran en el porcentaje.</summary>
internal sealed record Totales(int Presentes, int Ausentes, int Justificados, int Pendientes)
{
    public int Registradas => Presentes + Ausentes + Justificados;

    /// <summary>Presentes sobre registros con lista pasada; null si todavía no hay ninguno.</summary>
    public double? Fraccion => Registradas == 0 ? null : (double)Presentes / Registradas;

    // Sin registros se muestra un guion en lugar de 100%, que daría a entender asistencia perfecta.
    public string Porcentaje => Fraccion == null
        ? "-"
        : Math.Round(Fraccion.Value * 100) + "%";

    public static Totales De(IEnumerable<string> estados)
    {
        int presentes = 0, ausentes = 0, justificados = 0, pendientes = 0;

        foreach (string estado in estados)
        {
            switch (estado)
            {
                case Asistencia.EstadoPresente: presentes++; break;
                case Asistencia.EstadoAusente: ausentes++; break;
                case Asistencia.EstadoJustificado: justificados++; break;
                default: pendientes++; break;
            }
        }

        return new Totales(presentes, ausentes, justificados, pendientes);
    }
}

/// <summary>
/// Todo lo que necesita el reporte de asistencia, ya cargado de la base. Los renderizadores
/// (PDF y Excel) lo reciben listo y no consultan nada: así ambos muestran las mismas cifras.
/// </summary>
internal sealed class DatosDeAsistencia
{
    public Grupo? GrupoElegido { get; }
    public int? Mes { get; }
    public int Anio { get; }

    public List<Grupo> Grupos { get; }
    public List<NinoDelReporte> Ninos { get; }
    public List<RegistroAsistencia> Registros { get; }

    public Totales TotalGeneral { get; }

    private readonly ILookup<int, string> _estadosPorGrupo;
    private readonly ILookup<int, string> _estadosPorNino;

    public DatosDeAsistencia(Grupo? grupoElegido, int? mes, int anio,
        List<Grupo> grupos, List<NinoDelReporte> ninos, List<RegistroAsistencia> registros)
    {
        GrupoElegido = grupoElegido;
        Mes = mes;
        Anio = anio;
        Grupos = grupos;
        Ninos = ninos;
        Registros = registros;

        TotalGeneral = Totales.De(registros.Select(x => x.Estado));
        _estadosPorGrupo = registros.ToLookup(x => x.GrupoId, x => x.Estado);
        _estadosPorNino = registros.ToLookup(x => x.NinoId, x => x.Estado);
    }

    /// <summary>Un solo grupo y un solo mes: se muestra la matriz día por día.</summary>
    public bool ConMatriz => GrupoElegido != null && Mes.HasValue;

    public List<FilaResumen> FilasPorGrupo()
    {
        return Grupos
            .Select(x => new FilaResumen(
                x.Nombre,
                Ninos.Count(n => n.GrupoId == x.Id),
                Totales.De(_estadosPorGrupo[x.Id])))
            .ToList();
    }

    public List<FilaResumen> FilasPorMes()
    {
        return Registros
            .GroupBy(x => x.Fecha.Month)
            .OrderBy(x => x.Key)
            .Select(x => new FilaResumen(
                Periodo.NombreDelMes(x.Key), null, Totales.De(x.Select(r => r.Estado))))
            .ToList();
    }

    public List<NinoDelReporte> NinosDelGrupo(int grupoId)
    {
        return Ninos.Where(x => x.GrupoId == grupoId).ToList();
    }

    public Totales TotalesDelNino(int ninoId)
    {
        return Totales.De(_estadosPorNino[ninoId]);
    }

    public string MaestrosDelGrupo(Grupo grupo)
    {
        return string.Join(", ", grupo.Grupo_Maestros
            .Select(gm => gm.Perfil.Nombre + " " + gm.Perfil.Apellido));
    }
}

internal sealed record ActividadDelReporte(
    DateOnly Fecha, string Nombre, string Tipo, List<string> Grupos, int Participantes);

internal sealed class DatosDeActividades
{
    public Grupo? GrupoElegido { get; init; }
    public int? Mes { get; init; }
    public int Anio { get; init; }
    public List<ActividadDelReporte> Actividades { get; init; } = [];

    public int TotalParticipaciones => Actividades.Sum(x => x.Participantes);
}

internal sealed record RegistroDeSesion(string Nombre_Completo, string Estado, string? Motivo_Justificacion);

internal sealed record SesionDelReporte(DateOnly Fecha, List<RegistroDeSesion> Registros);

internal sealed class DatosPorActividad
{
    public required Actividad Actividad { get; init; }
    public List<SesionDelReporte> Sesiones { get; init; } = [];
}
