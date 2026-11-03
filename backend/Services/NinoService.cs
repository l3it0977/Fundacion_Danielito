using AutoMapper;
using SISTEMADANIELITO.Excepciones;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class NinoService : INinoService
{
    private AppProyectoContext _context;
    private readonly IMapper _mapper;

    public NinoService(AppProyectoContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    //CREAR
    public async Task Guardar(CreateNinoDTO nino)
    {
        await ValidarGrupo(nino.GrupoId);
        await ValidarCodigoUnico(nino.Codigo, null);
        ValidarCoordenadas(nino.Latitud, nino.Longitud);

        Nino ninoModel = _mapper.Map<Nino>(nino);
        _context.Ninos.Add(ninoModel);
        await _context.SaveChangesAsync();
    }

    //ACTUALIZAR
    public async Task Modificar(UpdateNinoDTO nino)
    {
        Nino? bdNino = await _context.Ninos.FirstOrDefaultAsync(x => x.Id.Equals(nino.Id));
        if (bdNino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        await ValidarGrupo(nino.GrupoId);
        await ValidarCodigoUnico(nino.Codigo, nino.Id);
        ValidarCoordenadas(nino.Latitud, nino.Longitud);

        bdNino.Codigo = nino.Codigo;
        bdNino.Nombres = nino.Nombres;
        bdNino.Apellidos = nino.Apellidos;
        bdNino.Fecha_Nacimiento = nino.Fecha_Nacimiento;
        bdNino.Contacto = nino.Contacto;
        bdNino.Curso = nino.Curso;
        bdNino.Colegio = nino.Colegio;
        bdNino.Vulnerable = nino.Vulnerable;
        bdNino.Motivo_Vulnerabilidad = nino.Motivo_Vulnerabilidad;
        bdNino.Observaciones = nino.Observaciones;
        bdNino.Fue_Al_Encuentro = nino.Fue_Al_Encuentro;
        bdNino.Bautizado = nino.Bautizado;
        bdNino.Asiste_Iglesia = nino.Asiste_Iglesia;
        bdNino.Nombre_Iglesia = nino.Nombre_Iglesia;
        bdNino.Nombre_Celula = nino.Nombre_Celula;
        bdNino.Activo = nino.Activo;
        bdNino.Latitud = nino.Latitud;
        bdNino.Longitud = nino.Longitud;
        bdNino.GrupoId = nino.GrupoId;

        _context.Ninos.Update(bdNino);
        await _context.SaveChangesAsync();
    }

    //ELIMINAR
    public async Task Eliminar(int id)
    {
        Nino? nino = await _context.Ninos.FirstOrDefaultAsync(x => x.Id.Equals(id));
        if (nino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        // Borrar a un niño con historial arrastraría sus asistencias y cambiaría los
        // reportes ya entregados. La base también lo impide (DeleteBehavior.Restrict),
        // pero aquí se avisa con un mensaje claro.
        bool tieneHistorial = await _context.Asistencias.AnyAsync(x => x.NinoId == id)
            || await _context.Visitas.AnyAsync(x => x.NinoId == id)
            || await _context.Actividad_Ninos.AnyAsync(x => x.NinoId == id);

        if (tieneHistorial)
        {
            throw (new ReglaNegocioException(
                $"{nino.Nombres} {nino.Apellidos} ya tiene asistencias, visitas o actividades. " +
                "Desactívalo para conservar su historial."));
        }

        _context.Ninos.Remove(nino);
        await _context.SaveChangesAsync();
    }

    //ACTIVAR O DESACTIVAR
    public async Task CambiarEstado(CambiarEstadoNinoDTO cambio)
    {
        Nino? bdNino = await _context.Ninos.FirstOrDefaultAsync(x => x.Id.Equals(cambio.NinoId));
        if (bdNino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        bdNino.Activo = cambio.Activo;

        _context.Ninos.Update(bdNino);
        await _context.SaveChangesAsync();
    }

    //OBTENER POR ID
    public async Task<NinoDTO> ObtenerPorId(int id, int? maestroId)
    {
        Nino? nino = await _context.Ninos
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Grupo_Maestros)
                    .ThenInclude(x => x.Perfil)
            .FirstOrDefaultAsync(x => x.Id.Equals(id));

        if (nino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        // Un maestro solo accede a los niños de los grupos que tiene a cargo.
        if (maestroId != null && !nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId))
        {
            throw (new UnauthorizedAccessException("No tiene acceso a este niño"));
        }

        var dto = _mapper.Map<NinoDTO>(nino);
        return dto;
    }

    //OBTENER TODOS
    public async Task<List<NinoDTO>> ObtenerTodos(int? maestroId)
    {
        IQueryable<Nino> consulta = _context.Ninos
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Grupo_Maestros)
                    .ThenInclude(x => x.Perfil);

        if (maestroId != null)
        {
            consulta = consulta.Where(x =>
                x.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId));
        }

        List<Nino> lstNinos = await consulta
            .OrderBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .ToListAsync();

        var dto = _mapper.Map<List<NinoDTO>>(lstNinos);
        return dto;
    }

    //BUSCAR (CON PAGINACIÓN)
    public async Task<ResultadoPaginadoDTO<NinoDTO>> Buscar(BuscarNinosDTO filtro, int? maestroId)
    {
        int tamano = Math.Clamp(filtro.Tamano, 1, 100);

        IQueryable<Nino> consulta = _context.Ninos;

        if (maestroId != null)
        {
            consulta = consulta.Where(x =>
                x.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId));
        }

        if (!filtro.IncluirInactivos)
        {
            consulta = consulta.Where(x => x.Activo);
        }

        if (filtro.GrupoId != null)
        {
            consulta = consulta.Where(x => x.GrupoId == filtro.GrupoId);
        }

        string? texto = filtro.Texto?.Trim().ToLower();
        if (!string.IsNullOrEmpty(texto))
        {
            consulta = consulta.Where(x =>
                (x.Nombres + " " + x.Apellidos).ToLower().Contains(texto) ||
                x.Codigo.ToLower().Contains(texto));
        }

        int total = await consulta.CountAsync();

        // Si piden una página que no existe (por ejemplo tras desactivar al último de la
        // última página), se entrega la última en lugar de una lista vacía.
        int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamano));
        int pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);

        List<Nino> lstNinos = await consulta
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Grupo_Maestros)
                    .ThenInclude(x => x.Perfil)
            .OrderBy(x => x.GrupoId)
            .ThenBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return new ResultadoPaginadoDTO<NinoDTO>
        {
            Items = _mapper.Map<List<NinoDTO>>(lstNinos),
            Total = total,
            Pagina = pagina,
            Tamano = tamano
        };
    }

    //OBTENER PARA SELECCION
    public async Task<List<NinoSeleccionDTO>> ObtenerParaSeleccion()
    {
        // Sin filtro por maestro: al armar una actividad se puede elegir a cualquier
        // niño. Devuelve solo nombre, código y grupo, no la ficha completa.
        List<Nino> lstNinos = await _context.Ninos
            .Include(x => x.Grupo)
            .Where(x => x.Activo)
            .OrderBy(x => x.Grupo.Nombre)
            .ThenBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .ToListAsync();

        var dto = _mapper.Map<List<NinoSeleccionDTO>>(lstNinos);
        return dto;
    }

    /// <summary>
    /// Comprueba que las coordenadas estén dentro del rango real del planeta.
    /// Sin esto, pegar un valor equivocado produce un error de desbordamiento de
    /// PostgreSQL, que llega al usuario como un fallo inesperado y no como un aviso.
    /// </summary>
    private void ValidarCoordenadas(decimal? latitud, decimal? longitud)
    {
        if (latitud != null && (latitud < -90 || latitud > 90))
        {
            throw (new ReglaNegocioException("La latitud debe estar entre -90 y 90"));
        }

        if (longitud != null && (longitud < -180 || longitud > 180))
        {
            throw (new ReglaNegocioException("La longitud debe estar entre -180 y 180"));
        }

        // Una sola coordenada no ubica nada: o van las dos o ninguna.
        if ((latitud == null) != (longitud == null))
        {
            throw (new ReglaNegocioException("Debe indicar latitud y longitud juntas"));
        }
    }

    private async Task ValidarGrupo(int grupoId)
    {
        Grupo? bdGrupo = await _context.Grupos.FirstOrDefaultAsync(x => x.Id.Equals(grupoId));
        if (bdGrupo == null)
        {
            throw (new ReglaNegocioException("El grupo indicado no existe"));
        }
    }

    private async Task ValidarCodigoUnico(string codigo, int? idActual)
    {
        bool existe = await _context.Ninos.AnyAsync(x => x.Codigo.Equals(codigo) && x.Id != idActual);
        if (existe)
        {
            throw (new ReglaNegocioException($"Ya existe un niño con el código '{codigo}'"));
        }
    }
}
