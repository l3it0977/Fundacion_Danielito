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
    public async Task Guardar(CreateActividadDTO actividad, int creadoPorId)
    {
        ValidarTipo(actividad.Tipo);
        List<int> ninosElegidos = await ValidarNinos(actividad.NinosId);

        Actividad actividadModel = _mapper.Map<Actividad>(actividad);
        actividadModel.CreadoPorId = creadoPorId;
        actividadModel.Activa = true;
        actividadModel.Fecha_Actividad = actividad.Fecha_Actividad;

        // Los participantes se eligen uno a uno, sin importar a qué grupo pertenezcan.
        actividadModel.Actividad_Ninos = ninosElegidos
            .Select(ninoId => new Actividad_Nino { NinoId = ninoId })
            .ToList();

        _context.Actividades.Add(actividadModel);
        await _context.SaveChangesAsync();

        // Se guarda primero para que la actividad tenga Id, que la asistencia necesita.
        await GenerarAsistenciaPendiente(actividadModel, ninosElegidos, creadoPorId);
        await _context.SaveChangesAsync();
    }

    //ACTUALIZAR
    public async Task Modificar(UpdateActividadDTO actividad, int? maestroId)
    {
        Actividad? bdActividad = await _context.Actividades
            .Include(x => x.Actividad_Ninos)
            .FirstOrDefaultAsync(x => x.Id.Equals(actividad.Id));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(bdActividad, maestroId);
        ValidarTipo(actividad.Tipo);
        List<int> ninosElegidos = await ValidarNinos(actividad.NinosId);

        bdActividad.Nombre = actividad.Nombre;
        bdActividad.Descripcion = actividad.Descripcion;
        bdActividad.Fecha_Actividad = actividad.Fecha_Actividad;
        bdActividad.Tipo = actividad.Tipo;
        bdActividad.Activa = actividad.Activa;

        await SincronizarParticipantes(bdActividad, ninosElegidos, bdActividad.CreadoPorId);

        _context.Actividades.Update(bdActividad);
        await _context.SaveChangesAsync();
    }

    //ELIMINAR
    public async Task Eliminar(int id, int? maestroId)
    {
        Actividad? actividad = await _context.Actividades.FirstOrDefaultAsync(x => x.Id.Equals(id));
        if (actividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(actividad, maestroId);

        _context.Actividades.Remove(actividad);
        await _context.SaveChangesAsync();
    }

    //OBTENER POR ID
    public async Task<ActividadDTO> ObtenerPorId(int id, int? maestroId)
    {
        Actividad? actividad = await ConsultaConRelaciones()
            .FirstOrDefaultAsync(x => x.Id.Equals(id));

        if (actividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        if (maestroId != null && !await EsVisiblePara(actividad, maestroId.Value))
        {
            throw (new UnauthorizedAccessException("No tiene acceso a esta actividad"));
        }

        var dto = _mapper.Map<ActividadDTO>(actividad);
        return dto;
    }

    //OBTENER TODOS
    public async Task<List<ActividadDTO>> ObtenerTodos(int? maestroId)
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

        List<Actividad> lstActividades = await consulta
            .OrderByDescending(x => x.Fecha_Actividad)
            .ToListAsync();

        var dto = _mapper.Map<List<ActividadDTO>>(lstActividades);
        return dto;
    }

    //ASIGNAR NIÑOS
    public async Task AsignarNinos(AsignarNinosDTO asignacion, int? maestroId)
    {
        Actividad? bdActividad = await _context.Actividades
            .Include(x => x.Actividad_Ninos)
            .FirstOrDefaultAsync(x => x.Id.Equals(asignacion.ActividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        ValidarQuePuedaEditarla(bdActividad, maestroId);

        // Se admite cualquier niño: la actividad ya no está limitada a ciertos grupos.
        List<int> ninosElegidos = await ValidarNinos(asignacion.NinosId);

        await SincronizarParticipantes(bdActividad, ninosElegidos, bdActividad.CreadoPorId);

        await _context.SaveChangesAsync();
    }

    //VALIDAR ACCESO
    /// <summary>
    /// Punto único que decide si alguien puede trabajar con una actividad.
    /// Directora y Administrador (maestroId nulo) acceden a todas. Un maestro accede
    /// a las que creó y a las que incluyen a algún niño de sus grupos. Asistencia y
    /// reportes lo usan para no repetir la regla en cada servicio.
    /// </summary>
    public async Task ValidarAcceso(int actividadId, int? maestroId)
    {
        Actividad? bdActividad = await _context.Actividades.FirstOrDefaultAsync(x => x.Id.Equals(actividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        if (maestroId != null && !await EsVisiblePara(bdActividad, maestroId.Value))
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
    private async Task SincronizarParticipantes(Actividad bdActividad, List<int> ninosElegidos, int registradoPorId)
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
            List<Asistencia> pendientesARetirar = await _context.Asistencias
                .Where(x => x.ActividadId == bdActividad.Id
                    && ninosRetirados.Contains(x.NinoId)
                    && x.Estado == Asistencia.EstadoPendiente)
                .ToListAsync();

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

        await GenerarAsistenciaPendiente(bdActividad, ninosNuevos, registradoPorId);
    }

    /// <summary>
    /// Crea la lista de asistencia de la actividad: un registro por niño, en estado
    /// pendiente y con la fecha de la actividad. Así el maestro encuentra la lista
    /// ya armada al pasar asistencia, en vez de tener que crearla a mano.
    /// </summary>
    private async Task GenerarAsistenciaPendiente(Actividad actividad, List<int> ninosId, int registradoPorId)
    {
        if (ninosId.Count == 0)
        {
            return;
        }

        DateOnly fechaDeLaLista = actividad.Fecha_Actividad;

        // Puede haber asistencia previa si al niño se le quitó y se le volvió a agregar.
        List<int> yaTienenRegistro = await _context.Asistencias
            .Where(x => x.ActividadId == actividad.Id && x.Fecha == fechaDeLaLista)
            .Select(x => x.NinoId)
            .ToListAsync();

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
    private async Task<bool> EsVisiblePara(Actividad actividad, int maestroId)
    {
        if (actividad.CreadoPorId == maestroId)
        {
            return true;
        }

        return await _context.Actividad_Ninos.AnyAsync(x =>
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
    private async Task<List<int>> ValidarNinos(List<int> ninosId)
    {
        List<int> sinRepetidos = ninosId.Distinct().ToList();

        if (sinRepetidos.Count == 0)
        {
            throw (new ReglaNegocioException("Debe elegir al menos un niño participante"));
        }

        int encontrados = await _context.Ninos.CountAsync(x => sinRepetidos.Contains(x.Id));

        if (encontrados != sinRepetidos.Count)
        {
            throw (new ReglaNegocioException("Uno o más niños indicados no existen"));
        }

        return sinRepetidos;
    }
}
