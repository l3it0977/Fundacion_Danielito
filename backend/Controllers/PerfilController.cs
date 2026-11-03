using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Controllers;

/// <summary>
/// Datos y contraseña del usuario que está usando el sistema.
/// Todo se resuelve con el perfil que viene en el token, así nadie puede modificar
/// la cuenta de otra persona pasando un id distinto.
///
/// Es async porque trabaja con Identity, igual que AuthController.
/// </summary>
[ApiController]
[Route("[controller]")]
public class PerfilController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly AppProyectoContext _context;

    public PerfilController(UserManager<IdentityUser> userManager, AppProyectoContext context)
    {
        _userManager = userManager;
        _context = context;
    }

    //OBTENER MI PERFIL
    [Authorize]
    [HttpGet]
    public async Task<PerfilDTO> Get()
    {
        Perfil bdPerfil = await ObtenerMiPerfil();
        IdentityUser bdUsuario = await ObtenerMiUsuario(bdPerfil.UsuarioId);

        var roles = await _userManager.GetRolesAsync(bdUsuario);

        // Solo se consultan los grupos si es Maestro; para los demás la lista no aplica.
        List<string> misGrupos = [];
        if (roles.Contains("Maestro"))
        {
            misGrupos = await _context.Grupos
                .Where(x => x.Grupo_Maestros.Any(gm => gm.PerfilId == bdPerfil.Id))
                .Select(x => x.Nombre)
                .ToListAsync();
        }

        return new PerfilDTO
        {
            Id = bdPerfil.Id,
            Nombre = bdPerfil.Nombre,
            Apellido = bdPerfil.Apellido,
            Email = bdUsuario.Email ?? string.Empty,
            Activo = bdPerfil.Activo,
            Rol = roles.FirstOrDefault() ?? string.Empty,
            Mis_Grupos = misGrupos
        };
    }

    //ACTUALIZAR MIS DATOS
    [Authorize]
    [HttpPut]
    public async Task<string> Put(ActualizarPerfilDTO datos)
    {
        if (!ModelState.IsValid)
        {
            throw (new ReglaNegocioException("Los datos enviados no son válidos."));
        }

        Perfil bdPerfil = await ObtenerMiPerfil();
        IdentityUser bdUsuario = await ObtenerMiUsuario(bdPerfil.UsuarioId);

        // Si cambia el correo hay que comprobar que no lo tenga otro usuario.
        if (!string.Equals(bdUsuario.Email, datos.Email, StringComparison.OrdinalIgnoreCase))
        {
            var usuarioConEseEmail = await _userManager.FindByEmailAsync(datos.Email);
            if (usuarioConEseEmail != null)
            {
                throw (new ReglaNegocioException("Ese correo ya está registrado por otro usuario."));
            }

            // El nombre de usuario se mantiene igual al correo, como en el registro.
            await _userManager.SetEmailAsync(bdUsuario, datos.Email);
            await _userManager.SetUserNameAsync(bdUsuario, datos.Email);
        }

        bdPerfil.Nombre = datos.Nombre;
        bdPerfil.Apellido = datos.Apellido;

        _context.Perfiles.Update(bdPerfil);
        await _context.SaveChangesAsync();

        return "Perfil actualizado correctamente";
    }

    //CAMBIAR MI CONTRASEÑA
    [Authorize]
    [HttpPut("Password")]
    public async Task<string> PutPassword(CambiarPasswordDTO datos)
    {
        if (!ModelState.IsValid)
        {
            throw (new ReglaNegocioException("Los datos enviados no son válidos."));
        }

        Perfil bdPerfil = await ObtenerMiPerfil();
        IdentityUser bdUsuario = await ObtenerMiUsuario(bdPerfil.UsuarioId);

        var resultado = await _userManager.ChangePasswordAsync(
            bdUsuario, datos.Password_Actual, datos.Password_Nueva);

        if (!resultado.Succeeded)
        {
            string detalle = string.Join("; ", resultado.Errors.Select(error => error.Description));
            throw (new ReglaNegocioException(detalle));
        }

        return "Contraseña actualizada correctamente";
    }

    //AYUDANTES

    private async Task<Perfil> ObtenerMiPerfil()
    {
        int miPerfilId = User.ObtenerPerfilId();

        Perfil? bdPerfil = await _context.Perfiles
            .FirstOrDefaultAsync(x => x.Id.Equals(miPerfilId));

        if (bdPerfil == null)
        {
            throw (new RecursoNoEncontradoException("Perfil no encontrado"));
        }

        return bdPerfil;
    }

    private async Task<IdentityUser> ObtenerMiUsuario(string usuarioId)
    {
        IdentityUser? bdUsuario = await _userManager.FindByIdAsync(usuarioId);

        if (bdUsuario == null)
        {
            throw (new RecursoNoEncontradoException("Usuario no encontrado"));
        }

        return bdUsuario;
    }
}
