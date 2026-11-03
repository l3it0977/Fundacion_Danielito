using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class AuthController : ControllerBase
{
    private readonly UserManager<IdentityUser> _userManager;
    private readonly SignInManager<IdentityUser> _signInManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly AppProyectoContext _context;
    private readonly IConfiguration _configuration;

    public AuthController(
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
    [HttpPost("RegistrarUsuario")]
    [Authorize(Roles = "Directora,Administrador")]
    public async Task<IActionResult> Registrar([FromBody] RegistroUsuarioDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var usuarioExiste = await _userManager.FindByEmailAsync(model.Email);
        if (usuarioExiste != null)
        {
            return BadRequest(new { mensaje = "El correo electrónico ya está registrado." });
        }

        var elRolExiste = await _roleManager.RoleExistsAsync(model.Rol);
        if (!elRolExiste)
        {
            return BadRequest(new { mensaje = $"El rol '{model.Rol}' no existe." });
        }

        var nuevoUsuario = new IdentityUser { UserName = model.Email, Email = model.Email };

        var resultado = await _userManager.CreateAsync(nuevoUsuario, model.Password);
        if (!resultado.Succeeded)
        {
            return BadRequest(resultado.Errors);
        }

        // El perfil vive en el contexto del dominio, así que no comparte transacción con
        // Identity: si algo falla se borra el usuario para no dejarlo huérfano y sin nombre.
        try
        {
            await _userManager.AddToRoleAsync(nuevoUsuario, model.Rol);

            Perfil perfil = new Perfil
            {
                UsuarioId = nuevoUsuario.Id,
                Nombre = model.Nombre,
                Apellido = model.Apellido,
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

        return Ok(new { mensaje = "Usuario creado correctamente." });
    }

    //CREAR ROL
    [HttpPost("CrearRol")]
    [Authorize(Roles = "Directora,Administrador")]
    public async Task<IActionResult> CrearRol([FromBody] CrearRolDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        var elRolExiste = await _roleManager.RoleExistsAsync(model.NombreRol);
        if (elRolExiste)
        {
            return BadRequest(new { mensaje = $"El rol '{model.NombreRol}' ya existe." });
        }

        var resultado = await _roleManager.CreateAsync(new IdentityRole(model.NombreRol));
        if (resultado.Succeeded)
        {
            return Ok(new { mensaje = $"Rol '{model.NombreRol}' creado correctamente." });
        }

        return BadRequest(resultado.Errors);
    }

    //LOGIN
    [HttpPost("Login")]
    [AllowAnonymous]
    [EnableRateLimiting("PoliticaLogin")]
    public async Task<IActionResult> Login([FromBody] LoginDto model)
    {
        if (!ModelState.IsValid) return BadRequest(ModelState);

        // Mensaje único en todos los fallos: distinguirlos permite averiguar qué correos existen.
        var credencialesInvalidas = Unauthorized(new { mensaje = "Credenciales incorrectas." });

        var usuario = await _userManager.FindByEmailAsync(model.Email);
        if (usuario == null)
        {
            return credencialesInvalidas;
        }

        var resultado = await _signInManager.CheckPasswordSignInAsync(usuario, model.Password, lockoutOnFailure: true);

        if (resultado.IsLockedOut)
        {
            return Unauthorized(new { mensaje = "La cuenta está bloqueada temporalmente por intentos fallidos." });
        }

        if (!resultado.Succeeded)
        {
            return credencialesInvalidas;
        }

        Perfil? bdPerfil = await _context.Perfiles.FirstOrDefaultAsync(x => x.UsuarioId.Equals(usuario.Id));
        if (bdPerfil == null)
        {
            return credencialesInvalidas;
        }

        if (!bdPerfil.Activo)
        {
            return Unauthorized(new { mensaje = "La cuenta está inactiva. Contacta al administrador." });
        }

        var tokenGenerado = await GenerarJwtToken(usuario, bdPerfil);
        return Ok(tokenGenerado);
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

        // 2 horas: al desactivar a un usuario su token sigue siendo válido hasta que expire,
        // porque el JWT no se comprueba contra la base en cada petición. Acortar la vigencia
        // reduce esa ventana sin penalizar el rendimiento.
        var tiempoExpiracion = DateTime.UtcNow.AddHours(2);

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
        var tokenString = tokenHandler.WriteToken(token);

        return new RespuestaAutenticacionDto
        {
            Token = tokenString,
            Expiracion = tiempoExpiracion
        };
    }
}
