using AutoMapper;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class ActividadService : IActividadService
{
    private AppProyectoContext _context;
    private readonly IMapper _mapper;

    public ActividadService(AppProyectoContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    //CREAR
    public void Guardar(CreateActividadDTO actividad, int creadoPorId)
    {
        ValidarTipo(actividad.Tipo);
        List<int> ninosElegidos = ValidarNinos(actividad.NinosId);

        Actividad actividadModel = _mapper.Map<Actividad>(actividad);
        actividadModel.CreadoPorId = creadoPorId;
        actividadModel.Activa = true;
        actividadModel.Fecha_Actividad = actividad.Fecha_Actividad.Date;

        // Los participantes se eligen uno a uno, sin importar a qué grupo pertenezcan.
        actividadModel.Actividad_Ninos = ninosElegidos
            .Select(ninoId => new Actividad_Nino { NinoId = ninoId })
            .ToList();

        _context.Actividades.Add(actividadModel);
        _context.SaveChanges();

        // Se guarda primero para que la actividad tenga Id, que la asistencia necesita.
        GenerarAsistenciaPendiente(actividadModel, ninosElegidos, creadoPorId);
        _context.SaveChanges();
    }

    //ACTUALIZAR
    public void Modificar(UpdateActividadDTO actividad, int? maestroId)
    {
        Actividad? bdActividad = _context.Actividades
            .Include(x => x.Actividad_Ninos)
            .FirstOrDefault(x => x.Id.Equals(actividad.Id));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(bdActividad, maestroId);
        ValidarTipo(actividad.Tipo);
        List<int> ninosElegidos = ValidarNinos(actividad.NinosId);

        bdActividad.Nombre = actividad.Nombre;
        bdActividad.Descripcion = actividad.Descripcion;
        bdActividad.Fecha_Actividad = actividad.Fecha_Actividad.Date;
        bdActividad.Tipo = actividad.Tipo;
        bdActividad.Activa = actividad.Activa;

        SincronizarParticipantes(bdActividad, ninosElegidos, bdActividad.CreadoPorId);

        _context.Actividades.Update(bdActividad);
        _context.SaveChanges();
    }

    //ELIMINAR
    public void Eliminar(int id, int? maestroId)
    {
        Actividad? actividad = _context.Actividades.FirstOrDefault(x => x.Id.Equals(id));
        if (actividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(actividad, maestroId);

        _context.Actividades.Remove(actividad);
        _context.SaveChanges();
    }

    //OBTENER POR ID
    public ActividadDTO ObtenerPorId(int id, int? maestroId)
    {
        Actividad? actividad = ConsultaConRelaciones()
            .FirstOrDefault(x => x.Id.Equals(id));

        if (actividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        if (maestroId != null && !EsVisiblePara(actividad, maestroId.Value))
        {
            throw (new UnauthorizedAccessException("No tiene acceso a esta actividad"));
        }

        var dto = _mapper.Map<ActividadDTO>(actividad);
        return dto;
    }

    //OBTENER TODOS
    public List<ActividadDTO> ObtenerTodos(int? maestroId)
    {
        IQueryable<Actividad> consulta = ConsultaConRelaciones();

        // El maestro ve lo que él organizó y lo que involucra a alguno de sus niños.
        if (maestroId != null)
        {
            consulta = consulta.Where(x =>
                x.CreadoPorId == maestroId ||
                x.Actividad_Ninos.Any(an =>
                    an.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId)));
        }

        List<Actividad> lstActividades = consulta
            .OrderByDescending(x => x.Fecha_Actividad)
            .ToList();

        var dto = _mapper.Map<List<ActividadDTO>>(lstActividades);
        return dto;
    }

    //ASIGNAR NIÑOS
    public void AsignarNinos(AsignarNinosDTO asignacion, int? maestroId)
    {
        Actividad? bdActividad = _context.Actividades
            .Include(x => x.Actividad_Ninos)
            .FirstOrDefault(x => x.Id.Equals(asignacion.ActividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(bdActividad, maestroId);

        // Se admite cualquier niño: la actividad ya no está limitada a ciertos grupos.
        List<int> ninosElegidos = ValidarNinos(asignacion.NinosId);

        SincronizarParticipantes(bdActividad, ninosElegidos, bdActividad.CreadoPorId);

        _context.SaveChanges();
    }

    //VALIDAR ACCESO
    /// <summary>
    /// Punto único que decide si alguien puede trabajar con una actividad.
    /// Directora y Administrador (maestroId nulo) acceden a todas. Un maestro accede
    /// a las que creó y a las que incluyen a algún niño de sus grupos. Asistencia y
    /// reportes lo usan para no repetir la regla en cada servicio.
    /// </summary>
    public void ValidarAcceso(int actividadId, int? maestroId)
    {
        Actividad? bdActividad = _context.Actividades.FirstOrDefault(x => x.Id.Equals(actividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        if (maestroId != null && !EsVisiblePara(bdActividad, maestroId.Value))
        {
            throw (new UnauthorizedAccessException("No tiene acceso a esta actividad"));
        }
    }

    //AYUDANTES

    private IQueryable<Actividad> ConsultaConRelaciones()
    {
        // Los grupos del DTO salen del grupo de cada niño, por eso se incluyen.
        return _context.Actividades
            .Include(x => x.CreadoPor)
            .Include(x => x.Actividad_Ninos)
                .ThenInclude(x => x.Nino)
                    .ThenInclude(x => x.Grupo);
    }

    /// <summary>
    /// Deja la lista de participantes igual a la recibida y mantiene la asistencia
    /// al día: quita la de los niños retirados y crea la de los nuevos.
    /// </summary>
    private void SincronizarParticipantes(Actividad bdActividad, List<int> ninosElegidos, int registradoPorId)
    {
        List<int> ninosRetirados = bdActividad.Actividad_Ninos
            .Where(x => !ninosElegidos.Contains(x.NinoId))
            .Select(x => x.NinoId)
            .ToList();

        _context.Actividad_Ninos.RemoveRange(
            bdActividad.Actividad_Ninos.Where(x => ninosRetirados.Contains(x.NinoId)));

        // Si a alguien se le quita de la actividad, su asistencia sin pasar deja de
        // tener sentido. La ya registrada se conserva: es historial de lo ocurrido.
        if (ninosRetirados.Count > 0)
        {
            List<Asistencia> pendientesARetirar = _context.Asistencias
                .Where(x => x.ActividadId == bdActividad.Id
                    && ninosRetirados.Contains(x.NinoId)
                    && x.Estado == Asistencia.EstadoPendiente)
                .ToList();

            _context.Asistencias.RemoveRange(pendientesARetirar);
        }

        List<int> ninosActuales = bdActividad.Actividad_Ninos.Select(x => x.NinoId).ToList();
        List<int> ninosNuevos = ninosElegidos.Where(x => !ninosActuales.Contains(x)).ToList();

        foreach (int ninoId in ninosNuevos)
        {
            _context.Actividad_Ninos.Add(new Actividad_Nino
            {
                ActividadId = bdActividad.Id,
                NinoId = ninoId
            });
        }

        GenerarAsistenciaPendiente(bdActividad, ninosNuevos, registradoPorId);
    }

    /// <summary>
    /// Crea la lista de asistencia de la actividad: un registro por niño, en estado
    /// pendiente y con la fecha de la actividad. Así el maestro encuentra la lista
    /// ya armada al pasar asistencia, en vez de tener que crearla a mano.
    /// </summary>
    private void GenerarAsistenciaPendiente(Actividad actividad, List<int> ninosId, int registradoPorId)
    {
        if (ninosId.Count == 0)
        {
            return;
        }

        // La fecha se normaliza sin hora: el índice único la incluye, y sin esto
        // se crearían registros que la base considera distintos pero son del mismo día.
        DateTime fechaDeLaLista = actividad.Fecha_Actividad.Date;

        // Puede haber asistencia previa si al niño se le quitó y se le volvió a agregar.
        List<int> yaTienenRegistro = _context.Asistencias
            .Where(x => x.ActividadId == actividad.Id && x.Fecha == fechaDeLaLista)
            .Select(x => x.NinoId)
            .ToList();

        foreach (int ninoId in ninosId.Where(x => !yaTienenRegistro.Contains(x)))
        {
            _context.Asistencias.Add(new Asistencia
            {
                ActividadId = actividad.Id,
                NinoId = ninoId,
                Fecha = fechaDeLaLista,
                Estado = Asistencia.EstadoPendiente,
                RegistradoPorId = registradoPorId
            });
        }
    }

    /// <summary>Un maestro solo ve lo que organizó o lo que involucra a sus niños.</summary>
    private bool EsVisiblePara(Actividad actividad, int maestroId)
    {
        if (actividad.CreadoPorId == maestroId)
        {
            return true;
        }

        return _context.Actividad_Ninos.Any(x =>
            x.ActividadId == actividad.Id &&
            x.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId));
    }

    /// <summary>
    /// Un maestro solo puede editar o borrar las actividades que él creó.
    /// Con maestroId nulo quien actúa es Directora o Administrador, sin restricción.
    /// </summary>
    private void ValidarQuePuedaEditarla(Actividad actividad, int? maestroId)
    {
        if (maestroId != null && actividad.CreadoPorId != maestroId)
        {
            throw (new UnauthorizedAccessException(
                "Solo puedes modificar las actividades que tú creaste"));
        }
    }

    private void ValidarTipo(string tipo)
    {
        if (!Actividad.TiposValidos.Contains(tipo))
        {
            throw (new ReglaNegocioException(
                $"Tipo inválido. Valores permitidos: {string.Join(", ", Actividad.TiposValidos)}"));
        }
    }

    /// <summary>Comprueba que los niños existan. Ya no importa de qué grupo sean.</summary>
    private List<int> ValidarNinos(List<int> ninosId)
    {
        List<int> sinRepetidos = ninosId.Distinct().ToList();

        if (sinRepetidos.Count == 0)
        {
            throw (new ReglaNegocioException("Debe elegir al menos un niño participante"));
        }

        int encontrados = _context.Ninos.Count(x => sinRepetidos.Contains(x.Id));

        if (encontrados != sinRepetidos.Count)
        {
            throw (new ReglaNegocioException("Uno o más niños indicados no existen"));
        }

        return sinRepetidos;
    }
}
