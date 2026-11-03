using AutoMapper;
using SISTEMADANIELITO.Excepciones;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class AsistenciaService : IAsistenciaService
{
    private AppProyectoContext _context;
    private readonly IMapper _mapper;

    // Se reutiliza su regla de acceso para que maestros y dirección vean lo mismo
    // en Actividades y en Asistencia.
    private readonly IActividadService _actividadService;

    public AsistenciaService(AppProyectoContext context, IMapper mapper, IActividadService actividadService)
    {
        _context = context;
        _mapper = mapper;
        _actividadService = actividadService;
    }

    //CREAR
    public void Guardar(CreateAsistenciaDTO asistencia, int registradoPorId, int? maestroId)
    {
        _actividadService.ValidarAcceso(asistencia.ActividadId, maestroId);
        ValidarEstado(asistencia.Estado);
        ValidarFecha(asistencia.ActividadId, asistencia.Fecha);
        ValidarParticipacion(asistencia.ActividadId, asistencia.NinoId);

        bool duplicada = _context.Asistencias.Any(x =>
            x.ActividadId == asistencia.ActividadId &&
            x.NinoId == asistencia.NinoId &&
            x.Fecha == asistencia.Fecha);

        if (duplicada)
        {
            throw (new ReglaNegocioException("Ya existe una asistencia registrada para ese niño en esa fecha"));
        }

        Asistencia asistenciaModel = _mapper.Map<Asistencia>(asistencia);
        asistenciaModel.RegistradoPorId = registradoPorId;

        _context.Asistencias.Add(asistenciaModel);
        _context.SaveChanges();
    }

    //ACTUALIZAR
    public void Modificar(UpdateAsistenciaDTO asistencia, int? maestroId)
    {
        Asistencia? bdAsistencia = _context.Asistencias.FirstOrDefault(x => x.Id.Equals(asistencia.Id));
        if (bdAsistencia == null)
        {
            throw (new RecursoNoEncontradoException("Asistencia no encontrada"));
        }

        // Antes cualquier sesión iniciada podía corregir cualquier registro por su Id.
        _actividadService.ValidarAcceso(bdAsistencia.ActividadId, maestroId);
        ValidarEstado(asistencia.Estado);
        ValidarFecha(bdAsistencia.ActividadId, asistencia.Fecha);

        bdAsistencia.Fecha = asistencia.Fecha;
        bdAsistencia.Estado = asistencia.Estado;
        bdAsistencia.Motivo_Justificacion = asistencia.Motivo_Justificacion;
        bdAsistencia.Observacion = asistencia.Observacion;

        _context.Asistencias.Update(bdAsistencia);
        _context.SaveChanges();
    }

    //ELIMINAR
    public void Eliminar(int id)
    {
        Asistencia? asistencia = _context.Asistencias.FirstOrDefault(x => x.Id.Equals(id));
        if (asistencia == null)
        {
            throw (new RecursoNoEncontradoException("Asistencia no encontrada"));
        }

        _context.Asistencias.Remove(asistencia);
        _context.SaveChanges();
    }

    //OBTENER POR ID
    public AsistenciaDTO ObtenerPorId(int id, int? maestroId)
    {
        Asistencia? asistencia = _context.Asistencias
            .Include(x => x.Actividad)
            .Include(x => x.Nino)
            .Include(x => x.RegistradoPor)
            .FirstOrDefault(x => x.Id.Equals(id));

        if (asistencia == null)
        {
            throw (new RecursoNoEncontradoException("Asistencia no encontrada"));
        }

        _actividadService.ValidarAcceso(asistencia.ActividadId, maestroId);

        var dto = _mapper.Map<AsistenciaDTO>(asistencia);
        return dto;
    }

    //OBTENER TODOS
    public List<AsistenciaDTO> ObtenerTodos()
    {
        List<Asistencia> lstAsistencias = _context.Asistencias
            .Include(x => x.Actividad)
            .Include(x => x.Nino)
            .Include(x => x.RegistradoPor)
            .OrderByDescending(x => x.Fecha)
            .ToList();

        var dto = _mapper.Map<List<AsistenciaDTO>>(lstAsistencias);
        return dto;
    }

    //OBTENER POR ACTIVIDAD
    public List<AsistenciaDTO> ObtenerPorActividad(int actividadId, int? maestroId)
    {
        // Se valida el acceso a la actividad, no niño por niño: quien puede usarla
        // ve su lista completa, igual que la Directora. Antes un maestro solo veía a
        // los niños de sus grupos, y si armaba una actividad con niños de otros
        // grupos, esos niños no le aparecían para pasar lista.
        _actividadService.ValidarAcceso(actividadId, maestroId);

        List<Asistencia> lstAsistencias = _context.Asistencias
            .Include(x => x.Actividad)
            .Include(x => x.Nino)
            .Include(x => x.RegistradoPor)
            .Where(x => x.ActividadId == actividadId)
            .OrderByDescending(x => x.Fecha)
            .ThenBy(x => x.Nino.Apellidos)
            .ToList();

        var dto = _mapper.Map<List<AsistenciaDTO>>(lstAsistencias);
        return dto;
    }

    //REGISTRAR POR ACTIVIDAD
    public void RegistrarPorActividad(RegistrarAsistenciasDTO registro, int registradoPorId, int? maestroId)
    {
        // Antes no se comprobaba: cualquier maestro podía pasar lista en cualquier actividad.
        _actividadService.ValidarAcceso(registro.ActividadId, maestroId);

        Actividad? bdActividad = _context.Actividades
            .Include(x => x.Actividad_Ninos)
            .FirstOrDefault(x => x.Id.Equals(registro.ActividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarFecha(registro.ActividadId, registro.Fecha);

        if (registro.Asistencias.Count == 0)
        {
            throw (new ReglaNegocioException("Debe registrar al menos una asistencia"));
        }

        List<int> participantes = bdActividad.Actividad_Ninos.Select(x => x.NinoId).ToList();

        foreach (AsistenciaItemDTO item in registro.Asistencias)
        {
            ValidarEstado(item.Estado);

            if (!participantes.Contains(item.NinoId))
            {
                throw (new ReglaNegocioException($"El niño {item.NinoId} no está asignado a esta actividad"));
            }
        }

        List<Asistencia> existentes = _context.Asistencias
            .Where(x => x.ActividadId == registro.ActividadId && x.Fecha == registro.Fecha)
            .ToList();

        foreach (AsistenciaItemDTO item in registro.Asistencias)
        {
            Asistencia? bdAsistencia = existentes.FirstOrDefault(x => x.NinoId.Equals(item.NinoId));

            if (bdAsistencia == null)
            {
                _context.Asistencias.Add(new Asistencia
                {
                    ActividadId = registro.ActividadId,
                    NinoId = item.NinoId,
                    Fecha = registro.Fecha,
                    Estado = item.Estado,
                    Motivo_Justificacion = item.Motivo_Justificacion,
                    Observacion = item.Observacion,
                    RegistradoPorId = registradoPorId
                });
            }
            else
            {
                bdAsistencia.Estado = item.Estado;
                bdAsistencia.Motivo_Justificacion = item.Motivo_Justificacion;
                bdAsistencia.Observacion = item.Observacion;
                bdAsistencia.RegistradoPorId = registradoPorId;
                _context.Asistencias.Update(bdAsistencia);
            }
        }

        _context.SaveChanges();
    }

    private void ValidarEstado(string estado)
    {
        if (!Asistencia.EstadosValidos.Contains(estado))
        {
            throw (new ReglaNegocioException(
                $"Estado inválido. Valores permitidos: {string.Join(", ", Asistencia.EstadosValidos)}"));
        }
    }

    /// <summary>Una sesión no puede registrarse antes de la fecha de su actividad.</summary>
    private void ValidarFecha(int actividadId, DateOnly fecha)
    {
        Actividad? bdActividad = _context.Actividades.FirstOrDefault(x => x.Id.Equals(actividadId));
        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        if (fecha < bdActividad.Fecha_Actividad)
        {
            throw (new ReglaNegocioException("La fecha no puede ser anterior a la fecha de la actividad"));
        }
    }

    private void ValidarParticipacion(int actividadId, int ninoId)
    {
        bool participa = _context.Actividad_Ninos
            .Any(x => x.ActividadId == actividadId && x.NinoId == ninoId);

        if (!participa)
        {
            throw (new ReglaNegocioException("El niño no está asignado a esta actividad"));
        }
    }
}
