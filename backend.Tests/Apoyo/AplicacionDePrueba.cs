using System.Net.Http.Headers;
using System.Net.Http.Json;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Tests.Apoyo;

/// <summary>
/// La aplicación completa (rutas, autenticación, roles, middleware, límite de intentos) corriendo
/// en memoria, con SQLite en lugar de PostgreSQL. Sirve para comprobar lo que ninguna prueba de
/// servicio ve: que las piezas, ya conectadas, se comportan como se espera de cara al navegador.
/// </summary>
public sealed class AplicacionDePrueba : WebApplicationFactory<Program>
{
    public const string ClaveDeLosUsuarios = "Clave.Segura1";

    private readonly SqliteConnection _conexionDominio = new("DataSource=:memory:");
    private readonly SqliteConnection _conexionIdentity = new("DataSource=:memory:");

    private readonly Dictionary<string, string?> _configuracionExtra;

    /// <param name="configuracionExtra">Valores de configuración que esta prueba necesita cambiar.</param>
    public AplicacionDePrueba(Dictionary<string, string?>? configuracionExtra = null)
    {
        _configuracionExtra = configuracionExtra ?? [];
        _conexionDominio.Open();
        _conexionIdentity.Open();
    }

    protected override void ConfigureWebHost(IWebHostBuilder constructor)
    {
        // Un entorno propio: en "Development" la aplicación leería los user-secrets de quien corre
        // las pruebas (contraseñas y claves reales), y los resultados cambiarían de una máquina a otra.
        constructor.UseEnvironment("Pruebas");

        var valores = new Dictionary<string, string?>
        {
            // Valores de relleno: la cadena nunca se usa porque abajo se reemplazan los contextos.
            ["ConnectionStrings:DefaultConnection"] = "Host=no-existe;Database=no-existe",
            ["Jwt:SecretKey"] = "una-clave-de-prueba-de-al-menos-32-caracteres!!",
            ["Seed:PasswordInicial"] = ClaveDeLosUsuarios,

            // Casi todas las pruebas inician sesión varias veces desde la misma IP.
            ["Seguridad:IntentosDeLoginPorMinuto"] = "1000"
        };

        foreach (var (clave, valor) in _configuracionExtra)
        {
            valores[clave] = valor;
        }

        // UseSetting y no ConfigureAppConfiguration: Program.cs lee algunos valores (la clave del
        // JWT) apenas arranca, antes de que ConfigureAppConfiguration llegue a aplicarse.
        foreach (var (clave, valor) in valores)
        {
            constructor.UseSetting(clave, valor);
        }

        constructor.ConfigureTestServices(servicios =>
        {
            Reemplazar<AppProyectoContext>(servicios, _conexionDominio);
            Reemplazar<AppIdentityContext>(servicios, _conexionIdentity);
        });
    }

    private static void Reemplazar<TContexto>(IServiceCollection servicios, SqliteConnection conexion)
        where TContexto : DbContext
    {
        servicios.RemoveAll<DbContextOptions<TContexto>>();
        servicios.RemoveAll<DbContextOptions>();
        servicios.RemoveAll<TContexto>();

        // Desde EF Core 9 cada AddDbContext deja registrada su acción de configuración (la que
        // llama a UseNpgsql) en un tipo interno. Si no se quita, convive con SQLite y EF se niega
        // a arrancar con dos proveedores a la vez.
        var configuraciones = servicios
            .Where(d => d.ServiceType.IsGenericType
                && d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")
                && d.ServiceType.GenericTypeArguments[0] == typeof(TContexto))
            .ToList();

        foreach (var configuracion in configuraciones)
        {
            servicios.Remove(configuracion);
        }

        servicios.AddDbContext<TContexto>((proveedor, opciones) =>
        {
            opciones.UseSqlite(conexion);

            // La auditoría solo afecta al contexto del dominio.
            if (typeof(TContexto) == typeof(AppProyectoContext))
            {
                opciones.AddInterceptors(proveedor.GetRequiredService<AuditoriaInterceptor>());
            }
        });
    }

    /// <summary>Crea las tablas de los dos contextos y los tres roles. Se llama una vez por prueba.</summary>
    public async Task PrepararAsync()
    {
        using var alcance = Services.CreateScope();
        var proveedor = alcance.ServiceProvider;

        proveedor.GetRequiredService<AppIdentityContext>().Database.EnsureCreated();
        proveedor.GetRequiredService<AppProyectoContext>().Database.EnsureCreated();

        var roles = proveedor.GetRequiredService<RoleManager<IdentityRole>>();
        foreach (var rol in new[] { "Directora", "Administrador", "Maestro" })
        {
            if (!await roles.RoleExistsAsync(rol))
            {
                await roles.CreateAsync(new IdentityRole(rol));
            }
        }
    }

    /// <summary>Crea un usuario con su perfil y devuelve el Id del perfil.</summary>
    public async Task<int> CrearUsuarioAsync(string correo, string rol, string nombre = "Nombre", string apellido = "Apellido")
    {
        using var alcance = Services.CreateScope();

        await alcance.ServiceProvider.GetRequiredService<IAuthService>().RegistrarUsuario(new RegistroUsuarioDto
        {
            Email = correo,
            Password = ClaveDeLosUsuarios,
            Nombre = nombre,
            Apellido = apellido,
            Rol = rol
        });

        var contexto = alcance.ServiceProvider.GetRequiredService<AppProyectoContext>();
        var usuarioId = (await alcance.ServiceProvider.GetRequiredService<UserManager<IdentityUser>>().FindByEmailAsync(correo))!.Id;

        return (await contexto.Perfiles.AsNoTracking().FirstAsync(x => x.UsuarioId == usuarioId)).Id;
    }

    /// <summary>Ejecuta algo directamente contra la base, para armar o revisar datos.</summary>
    public async Task<T> ConBaseAsync<T>(Func<AppProyectoContext, Task<T>> accion)
    {
        using var alcance = Services.CreateScope();

        return await accion(alcance.ServiceProvider.GetRequiredService<AppProyectoContext>());
    }

    public async Task ConBaseAsync(Func<AppProyectoContext, Task> accion)
    {
        await ConBaseAsync<object?>(async contexto =>
        {
            await accion(contexto);
            return null;
        });
    }

    /// <summary>Inicia sesión por HTTP, como lo haría el navegador, y devuelve un cliente ya autenticado.</summary>
    public async Task<HttpClient> ClienteAutenticadoAsync(string correo)
    {
        var cliente = CreateClient();

        var respuesta = await cliente.PostAsJsonAsync("/Auth/Login",
            new LoginDto { Email = correo, Password = ClaveDeLosUsuarios });
        respuesta.EnsureSuccessStatusCode();

        var token = (await respuesta.Content.ReadFromJsonAsync<RespuestaAutenticacionDto>())!.Token;
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        return cliente;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (disposing)
        {
            _conexionDominio.Dispose();
            _conexionIdentity.Dispose();
        }
    }
}
