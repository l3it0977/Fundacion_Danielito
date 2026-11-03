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
    public void Guardar(CreateNinoDTO nino)
    {
        ValidarGrupo(nino.GrupoId);
        ValidarCodigoUnico(nino.Codigo, null);
        ValidarCoordenadas(nino.Latitud, nino.Longitud);

        Nino ninoModel = _mapper.Map<Nino>(nino);
        _context.Ninos.Add(ninoModel);
        _context.SaveChanges();
    }

    //ACTUALIZAR
    public void Modificar(UpdateNinoDTO nino)
    {
        Nino? bdNino = _context.Ninos.FirstOrDefault(x => x.Id.Equals(nino.Id));
        if (bdNino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        ValidarGrupo(nino.GrupoId);
        ValidarCodigoUnico(nino.Codigo, nino.Id);
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
        _context.SaveChanges();
    }

    //ELIMINAR
    public void Eliminar(int id)
    {
        Nino? nino = _context.Ninos.FirstOrDefault(x => x.Id.Equals(id));
        if (nino == null)
        {
            throw (new RecursoNoEncontradoException("Niño no encontrado"));
        }

        _context.Ninos.Remove(nino);
        _context.SaveChanges();
    }

    //OBTENER POR ID
    public NinoDTO ObtenerPorId(int id, int? maestroId)
    {
        Nino? nino = _context.Ninos
            .Include(x => x.Grupo)
                .ThenInclude(x => x.Grupo_Maestros)
                    .ThenInclude(x => x.Perfil)
            .FirstOrDefault(x => x.Id.Equals(id));

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
    public List<NinoDTO> ObtenerTodos(int? maestroId)
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

        List<Nino> lstNinos = consulta
            .OrderBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .ToList();

        var dto = _mapper.Map<List<NinoDTO>>(lstNinos);
        return dto;
    }

    //OBTENER PARA SELECCION
    public List<NinoSeleccionDTO> ObtenerParaSeleccion()
    {
        // Sin filtro por maestro: al armar una actividad se puede elegir a cualquier
        // niño. Devuelve solo nombre, código y grupo, no la ficha completa.
        List<Nino> lstNinos = _context.Ninos
            .Include(x => x.Grupo)
            .Where(x => x.Activo)
            .OrderBy(x => x.Grupo.Nombre)
            .ThenBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .ToList();

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

    private void ValidarGrupo(int grupoId)
    {
        Grupo? bdGrupo = _context.Grupos.FirstOrDefault(x => x.Id.Equals(grupoId));
        if (bdGrupo == null)
        {
            throw (new ReglaNegocioException("El grupo indicado no existe"));
        }
    }

    private void ValidarCodigoUnico(string codigo, int? idActual)
    {
        bool existe = _context.Ninos.Any(x => x.Codigo.Equals(codigo) && x.Id != idActual);
        if (existe)
        {
            throw (new ReglaNegocioException($"Ya existe un niño con el código '{codigo}'"));
        }
    }
}
