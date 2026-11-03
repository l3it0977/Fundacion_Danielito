using AutoMapper;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class GrupoService : IGrupoService
{
    private AppProyectoContext _context;
    private readonly IMapper _mapper;
    private readonly UserManager<IdentityUser> _userManager;

    public GrupoService(
        AppProyectoContext context,
        IMapper mapper,
        UserManager<IdentityUser> userManager)
    {
        _context = context;
        _mapper = mapper;
        _userManager = userManager;
    }

    //CREAR
    public async Task Guardar(CreateGrupoDTO grupo)
    {
        List<int> maestrosElegidos = await ValidarMaestros(grupo.MaestrosId);

        Grupo grupoModel = _mapper.Map<Grupo>(grupo);

        // La colección intermedia se arma a mano: el mapper la ignora a propósito.
        grupoModel.Grupo_Maestros = maestrosElegidos
            .Select(perfilId => new Grupo_Maestro { PerfilId = perfilId })
            .ToList();

        _context.Grupos.Add(grupoModel);
        await _context.SaveChangesAsync();
    }

    //ACTUALIZAR
    public async Task Modificar(UpdateGrupoDTO grupo)
    {
        Grupo? bdGrupo = await _context.Grupos
            .Include(x => x.Grupo_Maestros)
            .FirstOrDefaultAsync(x => x.Id.Equals(grupo.Id));

        if (bdGrupo == null)
        {
            throw (new RecursoNoEncontradoException("Grupo no encontrado"));
        }

        List<int> maestrosElegidos = await ValidarMaestros(grupo.MaestrosId);

        bdGrupo.Nombre = grupo.Nombre;
        bdGrupo.Descripcion = grupo.Descripcion;
        bdGrupo.Activo = grupo.Activo;

        SincronizarMaestros(bdGrupo, maestrosElegidos);

        _context.Grupos.Update(bdGrupo);
        await _context.SaveChangesAsync();
    }

    //ELIMINAR
    public async Task Eliminar(int id)
    {
        Grupo? grupo = await _context.Grupos
            .Include(x => x.Ninos)
            .FirstOrDefaultAsync(x => x.Id.Equals(id));

        if (grupo == null)
        {
            throw (new RecursoNoEncontradoException("Grupo no encontrado"));
        }

        if (grupo.Ninos.Count > 0)
        {
            throw (new ReglaNegocioException("No se puede eliminar un grupo con niños asignados"));
        }

        _context.Grupos.Remove(grupo);
        await _context.SaveChangesAsync();
    }

    //OBTENER POR ID
    public async Task<GrupoDTO> ObtenerPorId(int id)
    {
        Grupo? grupo = await _context.Grupos
            .Include(x => x.Grupo_Maestros)
                .ThenInclude(x => x.Perfil)
            .Include(x => x.Ninos)
            .FirstOrDefaultAsync(x => x.Id.Equals(id));

        if (grupo == null)
        {
            throw (new RecursoNoEncontradoException("Grupo no encontrado"));
        }

        var dto = _mapper.Map<GrupoDTO>(grupo);
        return dto;
    }

    //OBTENER TODOS
    public async Task<List<GrupoDTO>> ObtenerTodos()
    {
        List<Grupo> lstGrupos = await _context.Grupos
            .Include(x => x.Grupo_Maestros)
                .ThenInclude(x => x.Perfil)
            .Include(x => x.Ninos)
            .OrderBy(x => x.Nombre)
            .ToListAsync();

        var dto = _mapper.Map<List<GrupoDTO>>(lstGrupos);
        return dto;
    }

    //AYUDANTES

    /// <summary>
    /// Comprueba que los maestros indicados existan y tengan rol Maestro.
    /// Devuelve la lista sin repetidos, lista para armar la tabla intermedia.
    /// </summary>
    private async Task<List<int>> ValidarMaestros(List<int> maestrosId)
    {
        List<int> sinRepetidos = maestrosId.Distinct().ToList();

        if (sinRepetidos.Count == 0)
        {
            throw (new ReglaNegocioException("Debe asignar al menos un maestro al grupo"));
        }

        // Una sola consulta en lugar de una por maestro.
        List<Perfil> lstPerfiles = await _context.Perfiles
            .Where(x => sinRepetidos.Contains(x.Id))
            .ToListAsync();

        if (lstPerfiles.Count != sinRepetidos.Count)
        {
            throw (new ReglaNegocioException("Uno o más maestros indicados no existen"));
        }

        // Sin esta comprobación se podía dejar a cargo de un grupo a alguien que no
        // es maestro, y después los filtros por rol no le mostraban nada.
        foreach (Perfil perfil in lstPerfiles)
        {
            if (!await TieneRolMaestro(perfil.UsuarioId))
            {
                throw (new ReglaNegocioException(
                    $"{perfil.Nombre} {perfil.Apellido} no tiene el rol Maestro"));
            }
        }

        return sinRepetidos;
    }

    private async Task<bool> TieneRolMaestro(string usuarioId)
    {
        IdentityUser? bdUsuario = await _userManager.FindByIdAsync(usuarioId);

        if (bdUsuario == null)
        {
            return false;
        }

        return await _userManager.IsInRoleAsync(bdUsuario, "Maestro");
    }

    /// <summary>Deja la lista de maestros del grupo igual a la recibida.</summary>
    private void SincronizarMaestros(Grupo bdGrupo, List<int> maestrosElegidos)
    {
        _context.Grupo_Maestros.RemoveRange(
            bdGrupo.Grupo_Maestros.Where(x => !maestrosElegidos.Contains(x.PerfilId)));

        List<int> maestrosActuales = bdGrupo.Grupo_Maestros.Select(x => x.PerfilId).ToList();

        foreach (int perfilId in maestrosElegidos.Where(x => !maestrosActuales.Contains(x)))
        {
            _context.Grupo_Maestros.Add(new Grupo_Maestro
            {
                GrupoId = bdGrupo.Id,
                PerfilId = perfilId
            });
        }
    }
}
