using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class AuthService : IAuthService
{
    /// <summary>Vigencia del token. Cada petición además comprueba que la cuenta siga activa.</summary>
    private static readonly TimeSpan DuracionDelToken = TimeSpan.FromHours(2);

    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppProyectoContext _context;
    private readonly IConfiguration _configuration;

    public AuthService(
        UserManager<IdentityUser> userManager,
        SignInManager<IdentityUser> signInManager,
        RoleManager<IdentityRole> roleManager,
        AppProyectoContext context,
        IConfiguration configuration)
    {
        _userManager = userManager;
        _signInManager = signInManager;
        _roleManager = roleManager;
        _context = context;
        _configuration = configuration;
    }

    //CREAR USUARIO
    public async Task RegistrarUsuario(RegistroUsuarioDto usuario)
    {
        var usuarioExiste = await _userManager.FindByEmailAsync(usuario.Email);
        if (usuarioExiste != null)
        {
            throw (new ReglaNegocioException("El correo electrónico ya está registrado."));
        }

        var elRolExiste = await _roleManager.RoleExistsAsync(usuario.Rol);
        if (!elRolExiste)
        {
            throw (new ReglaNegocioException($"El rol '{usuario.Rol}' no existe."));
        }

        var nuevoUsuario = new IdentityUser { UserName = usuario.Email, Email = usuario.Email };
        var resultado = await _userManager.CreateAsync(nuevoUsuario, usuario.Password);

        if (!resultado.Succeeded)
        {
            // Identity explica qué le falta a la contraseña ("necesita un número", etc.).
            throw (new ReglaNegocioException(
                string.Join(" ", resultado.Errors.Select(x => x.Description))));
        }

        // El perfil vive en el contexto del dominio, así que no comparte transacción con
        // Identity: si algo falla se borra el usuario para no dejarlo huérfano y sin nombre.
        try
        {
            await _userManager.AddToRoleAsync(nuevoUsuario, usuario.Rol);

            Perfil perfil = new Perfil
            {
                UsuarioId = nuevoUsuario.Id,
                Nombre = usuario.Nombre,
                Apellido = usuario.Apellido,
                Activo = true
            };

            _context.Perfiles.Add(perfil);
            await _context.SaveChangesAsync();
        }
        catch (Exception)
        {
            await _userManager.DeleteAsync(nuevoUsuario);
            throw;
        }
    }

    //CREAR ROL
    public async Task CrearRol(CrearRolDto rol)
    {
        var elRolExiste = await _roleManager.RoleExistsAsync(rol.NombreRol);
        if (elRolExiste)
        {
            throw (new ReglaNegocioException($"El rol '{rol.NombreRol}' ya existe."));
        }

        var resultado = await _roleManager.CreateAsync(new IdentityRole(rol.NombreRol));
        if (!resultado.Succeeded)
        {
            throw (new ReglaNegocioException(
                string.Join(" ", resultado.Errors.Select(x => x.Description))));
        }
    }

    //LOGIN
    public async Task<RespuestaAutenticacionDto> Login(LoginDto login)
    {
        // Mensaje único en todos los fallos de credenciales: distinguirlos permite averiguar
        // qué correos existen.
        const string credencialesInvalidas = "Credenciales incorrectas.";

        var usuario = await _userManager.FindByEmailAsync(login.Email);
        if (usuario == null)
        {
            throw (new AutenticacionFallidaException(credencialesInvalidas));
        }

        var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, login.Password, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            throw (new AutenticacionFallidaException(
                "La cuenta está bloqueada temporalmente por intentos fallidos."));
        }

        if (!resultado.Succeeded)
        {
            throw (new AutenticacionFallidaException(credencialesInvalidas));
        }

        Perfil? bdPerfil = await _context.Perfiles.FirstOrDefaultAsync(x => x.UsuarioId.Equals(usuario.Id));
        if (bdPerfil == null)
        {
            throw (new AutenticacionFallidaException(credencialesInvalidas));
        }

        if (!bdPerfil.Activo)
        {
            throw (new AutenticacionFallidaException("La cuenta está inactiva. Contacta al administrador."));
        }

        return await GenerarJwtToken(usuario, bdPerfil);
    }

    private async Task<RespuestaAutenticacionDto> GenerarJwtToken(IdentityUser usuario, Perfil perfil)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var llaveSecreta = _configuration["Jwt:SecretKey"]!;
        var key = Encoding.UTF8.GetBytes(llaveSecreta);

        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, usuario.Id),
            new Claim(ClaimTypes.Email, usuario.Email ?? string.Empty),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),

            // Lo consumen los servicios del dominio para filtrar lo que ve un Maestro.
            new Claim("PerfilId", perfil.Id.ToString()),
            new Claim("Nombre", perfil.Nombre),
            new Claim("Apellido", perfil.Apellido)
        };

        // Los roles viajan como claims -> habilitan [Authorize(Roles = "...")]
        var roles = await _userManager.GetRolesAsync(usuario);
        foreach (var rol in roles)
        {
            claims.Add(new Claim(ClaimTypes.Role, rol));
        }

        var tiempoExpiracion = DateTime.UtcNow.Add(DuracionDelToken);

        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = tiempoExpiracion,
            SigningCredentials = new SigningCredentials(
                new SymmetricSecurityKey(key),
                SecurityAlgorithms.HmacSha256Signature),
            Issuer = _configuration["Jwt:Issuer"],
            Audience = _configuration["Jwt:Audience"]
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);

        return new RespuestaAutenticacionDto
        {
            Token = tokenHandler.WriteToken(token),
            Expiracion = tiempoExpiracion
        };
    }
}
