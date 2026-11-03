using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services.Reportes;

namespace SISTEMADANIELITO.Services;

/// <summary>
/// Carga los datos de cada reporte y se los entrega al renderizador que corresponde
/// (Services/Reportes). Aquí no hay nada de diseño: la plantilla de la fundación vive en
/// PlantillaFundacion y cada formato en su propia clase.
/// </summary>
public class ReporteService : IReporteService
{
    private AppProyectoContext _context;

    // Decide si un maestro puede descargar el reporte de una actividad.
    private readonly IActividadService _actividadService;

    public ReporteService(AppProyectoContext context, IActividadService actividadService)
    {
        _context = context;
        _actividadService = actividadService;
    }

    //REPORTE 1: ASISTENCIA TOTAL Y POR GRUPO
    public async Task<byte[]> GenerarAsistencia(int? grupoId, int? mes, int anio)
    {
        DatosDeAsistencia datos = await CargarAsistencia(grupoId, mes, anio);

        return PdfAsistencia.Generar(datos);
    }

    public async Task<byte[]> GenerarAsistenciaExcel(int? grupoId, int? mes, int anio)
    {
        DatosDeAsistencia datos = await CargarAsistencia(grupoId, mes, anio);

        return ExcelAsistencia.Generar(datos);
    }

    private async Task<DatosDeAsistencia> CargarAsistencia(int? grupoId, int? mes, int anio)
    {
        Periodo.Validar(mes, anio);

        List<Grupo> todosLosGrupos = await _context.Grupos
            .Include(x => x.Grupo_Maestros)
                .ThenInclude(x => x.Perfil)
            .OrderBy(x => x.Id)
            .ToListAsync();

        Grupo? bdGrupo = null;
        if (grupoId.HasValue)
        {
            bdGrupo = todosLosGrupos.FirstOrDefault(x => x.Id.Equals(grupoId.Value));

            if (bdGrupo == null)
            {
                throw (new RecursoNoEncontradoException("Grupo no encontrado"));
            }
        }

        // Asistencias del periodo: alimentan los totales y las celdas de la matriz.
        // Rango de fechas y no Year/Month: así PostgreSQL puede usar el índice de Fecha.
        var (inicio, fin) = Periodo.Rango(mes, anio);
        var consulta = _context.Asistencias.Where(x => x.Fecha >= inicio && x.Fecha < fin);

        if (grupoId.HasValue)
        {
            consulta = consulta.Where(x => x.Nino.GrupoId == grupoId.Value);
        }

        var filas = await consulta
            .Select(x => new { x.NinoId, x.Nino.GrupoId, x.Fecha, x.Estado })
            .ToListAsync();

        List<RegistroAsistencia> registros = filas
            .Select(x => new RegistroAsistencia(x.NinoId, x.GrupoId, x.Fecha, x.Estado))
            .ToList();

        // Con todos los grupos se omiten los desactivados que no tuvieron movimiento.
        HashSet<int> gruposConRegistros = registros.Select(x => x.GrupoId).ToHashSet();
        List<Grupo> grupos = todosLosGrupos
            .Where(x => grupoId.HasValue
                ? x.Id == grupoId.Value
                : x.Activo || gruposConRegistros.Contains(x.Id))
            .ToList();

        HashSet<int> ninosConRegistros = registros.Select(x => x.NinoId).ToHashSet();
        List<int> idsDeGrupos = grupos.Select(x => x.Id).ToList();

        var ninosDeLaBase = await _context.Ninos
            .Where(x => idsDeGrupos.Contains(x.GrupoId))
            .OrderBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .Select(x => new NinoDelReporte(x.Id, x.GrupoId, x.Apellidos + ", " + x.Nombres, x.Activo))
            .ToListAsync();

        // Un niño desactivado solo aparece si tuvo asistencias en el periodo.
        List<NinoDelReporte> ninos = ninosDeLaBase
            .Where(x => x.Activo || ninosConRegistros.Contains(x.Id))
            .ToList();

        return new DatosDeAsistencia(bdGrupo, mes, anio, grupos, ninos, registros);
    }

    //REPORTE 2: ACTIVIDADES
    public async Task<byte[]> GenerarActividades(int? grupoId, int? mes, int anio)
    {
        Periodo.Validar(mes, anio);

        Grupo? bdGrupo = null;
        if (grupoId.HasValue)
        {
            bdGrupo = await _context.Grupos.FirstOrDefaultAsync(x => x.Id.Equals(grupoId.Value));

            if (bdGrupo == null)
            {
                throw (new RecursoNoEncontradoException("Grupo no encontrado"));
            }
        }

        var (inicio, fin) = Periodo.Rango(mes, anio);
        var consulta = _context.Actividades
            .Where(x => x.Fecha_Actividad >= inicio && x.Fecha_Actividad < fin);

        bool filtrarGrupo = grupoId.HasValue;
        int idGrupo = grupoId ?? 0;

        if (filtrarGrupo)
        {
            consulta = consulta.Where(x => x.Actividad_Ninos.Any(an => an.Nino.GrupoId == idGrupo));
        }

        // Con un grupo elegido, solo cuentan los niños de ese grupo. Los grupos y el conteo
        // se arman en memoria: son pocas actividades y así la consulta no depende de
        // subconsultas correlacionadas que no todos los proveedores de base traducen.
        List<Actividad> bdActividades = await consulta
            .Include(x => x.Actividad_Ninos)
                .ThenInclude(x => x.Nino)
                    .ThenInclude(x => x.Grupo)
            .OrderBy(x => x.Fecha_Actividad)
            .ToListAsync();

        List<ActividadDelReporte> actividades = bdActividades
            .Select(x =>
            {
                var participantes = x.Actividad_Ninos
                    .Where(an => !filtrarGrupo || an.Nino.GrupoId == idGrupo)
                    .ToList();

                return new ActividadDelReporte(
                    x.Fecha_Actividad,
                    x.Nombre,
                    x.Tipo,
                    participantes.Select(an => an.Nino.Grupo.Nombre).Distinct().ToList(),
                    participantes.Count);
            })
            .ToList();

        return PdfActividades.Generar(new DatosDeActividades
        {
            GrupoElegido = bdGrupo,
            Mes = mes,
            Anio = anio,
            Actividades = actividades
        });
    }

    //REPORTE 3: ASISTENCIA POR ACTIVIDAD
    public async Task<byte[]> GenerarAsistenciaPorActividad(int actividadId, int? maestroId)
    {
        await _actividadService.ValidarAcceso(actividadId, maestroId);

        Actividad? bdActividad = await _context.Actividades
            .FirstOrDefaultAsync(x => x.Id.Equals(actividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        var registros = await _context.Asistencias
            .Where(x => x.ActividadId == actividadId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Nino.Apellidos)
            .Select(x => new
            {
                x.Fecha,
                Nombre_Completo = x.Nino.Apellidos + ", " + x.Nino.Nombres,
                x.Estado,
                x.Motivo_Justificacion
            })
            .ToListAsync();

        // Cada fecha distinta es una sesión de la actividad.
        List<SesionDelReporte> sesiones = registros
            .GroupBy(x => x.Fecha)
            .OrderBy(sesion => sesion.Key)
            .Select(sesion => new SesionDelReporte(
                sesion.Key,
                sesion.Select(x => new RegistroDeSesion(x.Nombre_Completo, x.Estado, x.Motivo_Justificacion)).ToList()))
            .ToList();

        return PdfAsistenciaPorActividad.Generar(new DatosPorActividad
        {
            Actividad = bdActividad,
            Sesiones = sesiones
        });
    }
}
