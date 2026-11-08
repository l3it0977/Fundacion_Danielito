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
    public Task<byte[]> GenerarAsistencia(int? grupoId, int? mes, int anio);

    /// <summary>
    /// Los mismos datos del reporte de asistencia, en Excel: hojas Resumen, Por niño y Registros,
    /// con filtros activados y fechas reales para poder analizarlos.
    /// </summary>
    public Task<byte[]> GenerarAsistenciaExcel(int? grupoId, int? mes, int anio);

    /// <summary>Asistencia de una actividad, agrupada por cada sesión (fecha) registrada.</summary>
    // maestroId no nulo: solo si la actividad le corresponde a ese maestro.
    public Task<byte[]> GenerarAsistenciaPorActividad(int actividadId, int? maestroId);
}
