using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Middleware;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

/// <summary>
/// Administración de usuarios: listar, activar, desactivar y cambiar de rol.
///
/// Las validaciones impiden que alguien se deje a sí mismo fuera del sistema o que
/// la organización quede sin ninguna cuenta capaz de administrarla.
/// </summary>
public class UsuarioService : IUsuarioService
{
    /// <summary>Roles que pueden administrar el sistema. Siempre debe quedar al menos uno activo.</summary>
    private static readonly string[] RolesAdministradores = ["Directora", "Administrador"];

    private AppProyectoContext _context;
    private readonly AppIdentityContext _contextIdentity;
    private readonly UserManager<IdentityUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly IMemoryCache _cache;

    public UsuarioService(
        AppProyectoContext context,
        AppIdentityContext contextIdentity,
        UserManager<IdentityUser> userManager,
        RoleManager<IdentityRole> roleManager,
        IMemoryCache cache)
    {
        _context = context;
        _contextIdentity = contextIdentity;
        _userManager = userManager;
        _roleManager = roleManager;
        _cache = cache;
    }

    //OBTENER TODOS
    public async Task<List<UsuarioDTO>> ObtenerTodos()
    {
        List<Perfil> lstPerfiles = await _context.Perfiles
            .OrderBy(x => x.Apellido)
            .ThenBy(x => x.Nombre)
            .ToListAsync();

        // El correo y el rol se traen de una sola vez para no consultar Identity por
        // cada usuario, que sería una consulta por fila.
        Dictionary<string, DatosDeIdentity> datosPorUsuario = await ObtenerDatosDeIdentity();

        // Lo mismo con los grupos a cargo.
        Dictionary<int, List<string>> gruposPorMaestro = await ObtenerGruposPorMaestro();

        return lstPerfiles
            .Select(perfil => ArmarDTO(perfil, datosPorUsuario, gruposPorMaestro))
            .ToList();
    }

    //OBTENER POR ID
    public async Task<UsuarioDTO> ObtenerPorId(int perfilId)
    {
        Perfil bdPerfil = await BuscarPerfil(perfilId);

        Dictionary<string, DatosDeIdentity> datosPorUsuario = await ObtenerDatosDeIdentity();
        Dictionary<int, List<string>> gruposPorMaestro = await ObtenerGruposPorMaestro();

        return ArmarDTO(bdPerfil, datosPorUsuario, gruposPorMaestro);
    }

    //ACTIVAR O DESACTIVAR
    public async Task CambiarEstado(CambiarEstadoUsuarioDTO cambio, int perfilQueEjecuta)
    {
        Perfil bdPerfil = await BuscarPerfil(cambio.PerfilId);

        // Sin esta validación, alguien podría desactivarse y quedar fuera de su propia cuenta.
        if (bdPerfil.Id == perfilQueEjecuta)
        {
            throw (new ReglaNegocioException("No puedes cambiar el estado de tu propia cuenta."));
        }

        // Al desactivar hay que asegurarse de no dejar el sistema sin nadie que pueda administrarlo.
        if (!cambio.Activo && bdPerfil.Activo)
        {
            await ValidarQueQuedeAlgunAdministrador(bdPerfil);
        }

        bdPerfil.Activo = cambio.Activo;

        _context.Perfiles.Update(bdPerfil);
        await _context.SaveChangesAsync();

        // Sin esto, el estado anterior seguiría valiendo hasta un minuto en el validador del token.
        _cache.Remove(ValidadorDeCuenta.Clave(bdPerfil.Id));
    }

    //CAMBIAR ROL
    public async Task CambiarRol(CambiarRolUsuarioDTO cambio, int perfilQueEjecuta)
    {
        Perfil bdPerfil = await BuscarPerfil(cambio.PerfilId);

        // Cambiarse el propio rol es una forma silenciosa de perder los permisos:
        // una Directora que se pasa a Maestro ya no puede volver atrás.
        if (bdPerfil.Id == perfilQueEjecuta)
        {
            throw (new ReglaNegocioException("No puedes cambiar tu propio rol."));
        }

        if (!await _roleManager.RoleExistsAsync(cambio.Rol))
        {
            throw (new ReglaNegocioException($"El rol '{cambio.Rol}' no existe."));
        }

        IdentityUser bdUsuario = await BuscarUsuario(bdPerfil.UsuarioId);
        var rolesActuales = await _userManager.GetRolesAsync(bdUsuario);

        if (rolesActuales.Contains(cambio.Rol))
        {
            throw (new ReglaNegocioException($"El usuario ya tiene el rol '{cambio.Rol}'."));
        }

        // Si deja de ser administrador, se comprueba que no sea el último que queda.
        if (rolesActuales.Any(RolesAdministradores.Contains) &&
            !RolesAdministradores.Contains(cambio.Rol))
        {
            await ValidarQueQuedeAlgunAdministrador(bdPerfil);
        }

        // Si pasa a Maestro conservando grupos, el sistema queda coherente; pero si deja
        // de ser Maestro y tiene grupos a cargo, esos grupos se quedarían sin responsable.
        if (rolesActuales.Contains("Maestro") && cambio.Rol != "Maestro")
        {
            // Un grupo puede tener varios maestros, así que solo molesta si este
            // es el único que le queda: ese grupo se quedaría sin nadie a cargo.
            List<string> gruposQueQuedarianSolos = await _context.Grupos
                .Where(x => x.Grupo_Maestros.Any(gm => gm.PerfilId == bdPerfil.Id)
                    && x.Grupo_Maestros.Count == 1)
                .Select(x => x.Nombre)
                .ToListAsync();

            if (gruposQueQuedarianSolos.Count > 0)
            {
                throw (new ReglaNegocioException(
                    "No se puede cambiar el rol: es el único maestro de " +
                    $"{string.Join(", ", gruposQueQuedarianSolos)}. " +
                    "Primero asigna otro maestro a esos grupos."));
            }
        }

        await _userManager.RemoveFromRolesAsync(bdUsuario, rolesActuales);
        await _userManager.AddToRoleAsync(bdUsuario, cambio.Rol);
    }

    //AYUDANTES

    /// <summary>Correo y rol de cada usuario de Identity, indexados por su id.</summary>
    private async Task<Dictionary<string, DatosDeIdentity>> ObtenerDatosDeIdentity()
    {
        // Un join entre usuarios, la tabla intermedia de roles y los roles, en una sola consulta.
        var consulta =
            from usuario in _contextIdentity.Users
            join usuarioRol in _contextIdentity.UserRoles
                on usuario.Id equals usuarioRol.UserId into rolesDelUsuario
            from usuarioRol in rolesDelUsuario.DefaultIfEmpty()
            join rol in _contextIdentity.Roles
                on usuarioRol.RoleId equals rol.Id into rolesEncontrados
            from rol in rolesEncontrados.DefaultIfEmpty()
            select new
            {
                usuario.Id,
                usuario.Email,
                NombreRol = rol != null ? rol.Name : null
            };

        var filas = await consulta.ToListAsync();

        return filas.ToDictionary(
            fila => fila.Id,
            fila => new DatosDeIdentity
            {
                Email = fila.Email ?? string.Empty,
                Rol = fila.NombreRol ?? string.Empty
            });
    }

    /// <summary>Nombres de los grupos que tiene a cargo cada maestro, indexados por perfil.</summary>
    private async Task<Dictionary<int, List<string>>> ObtenerGruposPorMaestro()
    {
        var filas = await _context.Grupo_Maestros
            .Select(x => new { x.PerfilId, Nombre_Grupo = x.Grupo.Nombre })
            .ToListAsync();

        return filas
            .GroupBy(x => x.PerfilId)
            .ToDictionary(
                maestro => maestro.Key,
                maestro => maestro.Select(x => x.Nombre_Grupo).ToList());
    }

    private static UsuarioDTO ArmarDTO(
        Perfil perfil,
        Dictionary<string, DatosDeIdentity> datosPorUsuario,
        Dictionary<int, List<string>> gruposPorMaestro)
    {
        datosPorUsuario.TryGetValue(perfil.UsuarioId, out DatosDeIdentity? datos);
        gruposPorMaestro.TryGetValue(perfil.Id, out List<string>? grupos);

        return new UsuarioDTO
        {
            Id = perfil.Id,
            Nombre = perfil.Nombre,
            Apellido = perfil.Apellido,
            Email = datos?.Email ?? string.Empty,
            Rol = datos?.Rol ?? string.Empty,
            Activo = perfil.Activo,
            Mis_Grupos = grupos ?? []
        };
    }

    /// <summary>
    /// Comprueba que, dejando fuera al usuario indicado, siga habiendo al menos una
    /// Directora o Administrador activo. Sin esto es posible dejar el sistema sin
    /// nadie capaz de crear usuarios ni reactivar cuentas.
    /// </summary>
    private async Task ValidarQueQuedeAlgunAdministrador(Perfil perfilAfectado)
    {
        Dictionary<string, DatosDeIdentity> datosPorUsuario = await ObtenerDatosDeIdentity();

        List<Perfil> perfilesActivos = await _context.Perfiles
            .Where(x => x.Activo && x.Id != perfilAfectado.Id)
            .ToListAsync();

        bool quedaAlguno = perfilesActivos.Any(perfil =>
            datosPorUsuario.TryGetValue(perfil.UsuarioId, out DatosDeIdentity? datos) &&
            RolesAdministradores.Contains(datos.Rol));

        if (!quedaAlguno)
        {
            throw (new ReglaNegocioException(
                "Es el último usuario con permisos de administración activo. " +
                "Asigna a otra persona antes de quitarle el acceso."));
        }
    }

    private async Task<Perfil> BuscarPerfil(int perfilId)
    {
        Perfil? bdPerfil = await _context.Perfiles.FirstOrDefaultAsync(x => x.Id.Equals(perfilId));

        if (bdPerfil == null)
        {
            throw (new RecursoNoEncontradoException("Usuario no encontrado"));
        }

        return bdPerfil;
    }

    private async Task<IdentityUser> BuscarUsuario(string usuarioId)
    {
        IdentityUser? bdUsuario = await _userManager.FindByIdAsync(usuarioId);

        if (bdUsuario == null)
        {
            throw (new RecursoNoEncontradoException("Usuario no encontrado en el módulo de seguridad"));
        }

        return bdUsuario;
    }

    /// <summary>Correo y rol que corresponden a un usuario de Identity.</summary>
    private class DatosDeIdentity
    {
        public string Email { get; set; } = string.Empty;
        public string Rol { get; set; } = string.Empty;
    }
}
