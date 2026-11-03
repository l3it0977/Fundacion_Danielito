namespace SISTEMADANIELITO.Interfaces;

/// <summary>
/// Genera los reportes en PDF con la plantilla de la fundación. Cada método devuelve
/// los bytes del archivo ya listo para descargar.
/// </summary>
public interface IReporteService
{
    /// <summary>
    /// Asistencia total y por grupo. grupoId nulo = todos los grupos de edad;
    /// mes nulo = todos los meses del año.
    /// </summary>
    public byte[] GenerarAsistencia(int? grupoId, int? mes, int anio);

    /// <summary>
    /// Listado de actividades realizadas. grupoId nulo = todos los grupos de edad;
    /// mes nulo = todos los meses del año.
    /// </summary>
    public byte[] GenerarActividades(int? grupoId, int? mes, int anio);

    /// <summary>Asistencia de una actividad, agrupada por cada sesión (fecha) registrada.</summary>
    // maestroId no nulo: solo si la actividad le corresponde a ese maestro.
    public byte[] GenerarAsistenciaPorActividad(int actividadId, int? maestroId);
}
