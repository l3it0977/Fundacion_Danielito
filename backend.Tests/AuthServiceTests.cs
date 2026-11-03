using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Identity;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

/// <summary>Inicio de sesión y alta de usuarios con Identity real sobre SQLite en memoria.</summary>
public class AuthServiceTests : IDisposable
{
    private const string Password = "Clave.Segura1";

    private readonly BaseDeDatos _bd = new();
    private readonly SqliteConnection _conexionIdentity = new("DataSource=:memory:");
    private readonly ServiceProvider _servicios;
    private readonly IServiceScope _alcance;
    private readonly AuthService _servicio;

    public AuthServiceTests()
    {
        _conexionIdentity.Open();

        var configuracion = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["Jwt:SecretKey"] = "una-clave-de-prueba-de-al-menos-32-caracteres!!",
                ["Jwt:Issuer"] = "emisor-de-prueba",
                ["Jwt:Audience"] = "audiencia-de-prueba"
            })
            .Build();

        var coleccion = new ServiceCollection();
        coleccion.AddLogging();
        coleccion.AddSingleton<IConfiguration>(configuracion);
        coleccion.AddSingleton(_bd.Context);
        coleccion.AddDbContext<AppIdentityContext>(o => o.UseSqlite(_conexionIdentity));
        coleccion.AddIdentity<IdentityUser, IdentityRole>(o =>
            {
                o.Lockout.MaxFailedAccessAttempts = 3;
                o.User.RequireUniqueEmail = true;
                o.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<AppIdentityContext>();
        coleccion.AddScoped<IAuthService, AuthService>();

        _servicios = coleccion.BuildServiceProvider();
        _alcance = _servicios.CreateScope();

        _alcance.ServiceProvider.GetRequiredService<AppIdentityContext>().Database.EnsureCreated();
        _servicio = (AuthService)_alcance.ServiceProvider.GetRequiredService<IAuthService>();

        var roles = _alcance.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { "Directora", "Maestro" })
        {
            roles.CreateAsync(new IdentityRole(rol)).GetAwaiter().GetResult();
        }
    }

    public void Dispose()
    {
        _alcance.Dispose();
        _servicios.Dispose();
        _conexionIdentity.Dispose();
        _bd.Dispose();
    }

    //LOGIN

    [Fact]
    public async Task Login_ConCredencialesCorrectas_EntregaUnTokenConRolesYPerfil()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro"));

        var respuesta = await _servicio.Login(new LoginDto { Email = "ana@prueba.com", Password = Password });

        var token = new JwtSecurityTokenHandler().ReadJwtToken(respuesta.Token);
        var perfil = _bd.Context.Perfiles.Single();

        Assert.Equal(perfil.Id.ToString(), token.Claims.First(c => c.Type == "PerfilId").Value);
        Assert.Contains(token.Claims, c => (c.Type == "role" || c.Type.EndsWith("/role")) && c.Value == "Maestro");
        Assert.Equal("emisor-de-prueba", token.Issuer);
        Assert.True(respuesta.Expiracion > DateTime.UtcNow.AddMinutes(100));
    }

    [Fact]
    public async Task Login_ContrasenaIncorrectaYCorreoInexistente_DanElMismoMensaje()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro"));

        var contrasenaMala = await Assert.ThrowsAsync<AutenticacionFallidaException>(() =>
            _servicio.Login(new LoginDto { Email = "ana@prueba.com", Password = "Otra.Clave9" }));
        var correoInexistente = await Assert.ThrowsAsync<AutenticacionFallidaException>(() =>
            _servicio.Login(new LoginDto { Email = "nadie@prueba.com", Password = Password }));

        // Distinguirlos permitiría averiguar qué correos existen.
        Assert.Equal(contrasenaMala.Message, correoInexistente.Message);
    }

    [Fact]
    public async Task Login_CuentaInactiva_SeRechaza()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro"));
        _bd.Context.Perfiles.Single().Activo = false;
        _bd.Context.SaveChanges();

        var fallo = await Assert.ThrowsAsync<AutenticacionFallidaException>(() =>
            _servicio.Login(new LoginDto { Email = "ana@prueba.com", Password = Password }));

        Assert.Contains("inactiva", fallo.Message);
    }

    [Fact]
    public async Task Login_TrasVariosFallos_BloqueaLaCuentaAunConLaClaveCorrecta()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro"));

        for (int i = 0; i < 3; i++)
        {
            await Assert.ThrowsAsync<AutenticacionFallidaException>(() =>
                _servicio.Login(new LoginDto { Email = "ana@prueba.com", Password = "Mala.Clave9" }));
        }

        var fallo = await Assert.ThrowsAsync<AutenticacionFallidaException>(() =>
            _servicio.Login(new LoginDto { Email = "ana@prueba.com", Password = Password }));

        Assert.Contains("bloqueada", fallo.Message);
    }

    //REGISTRO

    [Fact]
    public async Task RegistrarUsuario_CreaElUsuarioConSuPerfilActivo()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Directora"));

        var perfil = _bd.Context.Perfiles.Single();
        Assert.Equal("Ana", perfil.Nombre);
        Assert.True(perfil.Activo);
    }

    [Fact]
    public async Task RegistrarUsuario_CorreoRepetido_SeRechaza()
    {
        await _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro"));

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Maestro")));

        Assert.Equal(1, _bd.Context.Perfiles.Count());
    }

    [Fact]
    public async Task RegistrarUsuario_RolInexistente_SeRechazaYNoCreaNada()
    {
        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _servicio.RegistrarUsuario(NuevoUsuario("ana@prueba.com", "Inventado")));

        Assert.Equal(0, _bd.Context.Perfiles.Count());
    }

    [Fact]
    public async Task RegistrarUsuario_ContrasenaDebil_ExplicaQueLeFalta()
    {
        var usuario = NuevoUsuario("ana@prueba.com", "Maestro");
        usuario.Password = "corta";

        var fallo = await Assert.ThrowsAsync<ReglaNegocioException>(() => _servicio.RegistrarUsuario(usuario));

        Assert.False(string.IsNullOrWhiteSpace(fallo.Message));
        Assert.Equal(0, _bd.Context.Perfiles.Count());
    }

    [Fact]
    public async Task CrearRol_Repetido_SeRechaza()
    {
        await _servicio.CrearRol(new CrearRolDto { NombreRol = "Auxiliar" });

        await Assert.ThrowsAsync<ReglaNegocioException>(() =>
            _servicio.CrearRol(new CrearRolDto { NombreRol = "Auxiliar" }));
    }

    private static RegistroUsuarioDto NuevoUsuario(string correo, string rol)
    {
        return new RegistroUsuarioDto
        {
            Email = correo,
            Password = Password,
            Nombre = "Ana",
            Apellido = "Prueba",
            Rol = rol
        };
    }
}
