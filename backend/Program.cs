using System.Text;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Mapper;
using SISTEMADANIELITO.Middleware;
using SISTEMADANIELITO.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// Guarda por un minuto si cada cuenta sigue activa (ver ValidadorDeCuenta).
builder.Services.AddMemoryCache();
builder.Services.AddOpenApi();

// CORS: el navegador bloquea las peticiones del frontend si el backend no autoriza su origen.
// Los orígenes permitidos salen de configuración, para no dejar direcciones fijas en el código.
const string PoliticaFrontend = "PoliticaFrontend";
string[] origenesPermitidos = builder.Configuration
    .GetSection("Cors:OrigenesPermitidos")
    .Get<string[]>() ?? [];

builder.Services.AddCors(opciones =>
{
    opciones.AddPolicy(PoliticaFrontend, politica =>
    {
        politica.WithOrigins(origenesPermitidos)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

// Detrás de un proxy inverso (nginx, Caddy...) la API solo ve la dirección del proxy: el límite
// de intentos de login compartiría un único cupo para todos y la bitácora no sabría de dónde
// vienen las peticiones. Con esto se confía en las cabeceras X-Forwarded-*. Actívalo SOLO si
// el puerto de la API no es accesible desde fuera: si lo es, cualquiera podría falsificarlas.
bool detrasDeProxy = builder.Configuration.GetValue<bool>("ForwardedHeaders:Habilitado");

if (detrasDeProxy)
{
    builder.Services.Configure<ForwardedHeadersOptions>(opciones =>
    {
        opciones.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
        opciones.KnownIPNetworks.Clear();
        opciones.KnownProxies.Clear();
    });
}

// Límite de intentos de login por dirección IP. El bloqueo por cuenta (5 fallos) no frena a
// quien prueba contraseñas contra muchos correos distintos; este límite sí.
// Detrás de un proxy hay que activar ForwardedHeaders (ver más abajo), o todas las peticiones
// compartirían el cupo de la IP del proxy.
const string PoliticaLogin = "PoliticaLogin";

builder.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    opciones.AddPolicy(PoliticaLogin, contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            contexto.Connection.RemoteIpAddress?.ToString() ?? "desconocida",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = builder.Configuration.GetValue("Seguridad:IntentosDeLoginPorMinuto", 10),
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));

    opciones.OnRejected = async (contexto, cancelacion) =>
    {
        contexto.HttpContext.Response.Headers.RetryAfter = "60";
        contexto.HttpContext.Response.ContentType = "application/json";

        await contexto.HttpContext.Response.WriteAsync(
            "{\"mensaje\":\"Demasiados intentos. Espera un minuto antes de volver a intentarlo.\"}",
            cancelacion);
    };
});

// QuestPDF reemplaza a DomPDF en los reportes. La licencia Community cubre a organizaciones
// caritativas y a empresas con menos de USD 1.000.000 de ingresos anuales.
QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configuración de Entity Framework Core con PostgreSQL
string cadenaConexion = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException(
        "Falta la cadena de conexión 'DefaultConnection'. Configúrala con: dotnet user-secrets set \"ConnectionStrings:DefaultConnection\" \"...\"");

// La auditoría necesita saber quién hace la petición para anotarlo en la bitácora.
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<AuditoriaInterceptor>();

builder.Services.AddDbContext<AppProyectoContext>((servicios, options) => options
    .UseNpgsql(cadenaConexion)
    .AddInterceptors(servicios.GetRequiredService<AuditoriaInterceptor>()));

// Configuración de Identity con Entity Framework Core y PostgreSQL
// Historial propio: si ambos contextos comparten __EFMigrationsHistory, cada uno ve las
// migraciones del otro como suyas y un rollback puede revertir las que no le corresponden.
builder.Services.AddDbContext<AppIdentityContext>(options =>
    options.UseNpgsql(cadenaConexion, npgsql =>
        npgsql.MigrationsHistoryTable("__EFMigrationsHistory_Identity")));
builder.Services.AddIdentity<IdentityUser, IdentityRole>(options =>
{
    // Bloqueo temporal tras varios intentos fallidos, contra ataques de fuerza bruta.
    options.Lockout.MaxFailedAccessAttempts = 5;
    options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
    options.User.RequireUniqueEmail = true;
    options.Password.RequiredLength = 8;
})
    .AddEntityFrameworkStores<AppIdentityContext>();

// Configuración de servicios para inyección de dependencias
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IBitacoraService, BitacoraService>();
builder.Services.AddScoped<IGrupoService, GrupoService>();
builder.Services.AddScoped<INinoService, NinoService>();
builder.Services.AddScoped<IActividadService, ActividadService>();
builder.Services.AddScoped<IAsistenciaService, AsistenciaService>();
builder.Services.AddScoped<IVisitaService, VisitaService>();
builder.Services.AddScoped<IDashboardService, DashboardService>();
builder.Services.AddScoped<IReporteService, ReporteService>();
builder.Services.AddScoped<IImportacionService, ImportacionService>();
builder.Services.AddScoped<IUsuarioService, UsuarioService>();

// Configuración de AutoMapper
builder.Services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

//------------------------------------------Configuración de JWT------------------------------------------
var llaveSecreta = builder.Configuration["Jwt:SecretKey"]
    ?? throw new InvalidOperationException(
        "Falta 'Jwt:SecretKey'. Configúrala con: dotnet user-secrets set \"Jwt:SecretKey\" \"...\"");
var key = Encoding.UTF8.GetBytes(llaveSecreta);

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(key),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"],
        ValidateLifetime = true,
        ClockSkew = TimeSpan.FromMinutes(1)
    };

    // Un token válido no basta: la cuenta debe seguir activa. Así desactivar a alguien
    // le corta el acceso enseguida, sin esperar a que el token expire.
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = ValidadorDeCuenta.Validar
    };
});
//-------------------------------------------------------------------------------------------------------

var app = builder.Build();

// Crea o actualiza las tablas al arrancar. Está apagado por omisión: sirve para el despliegue con
// Docker, donde la base nace vacía. Con varias copias de la API a la vez habría que migrar aparte.
if (builder.Configuration.GetValue<bool>("BaseDeDatos:MigrarAlIniciar"))
{
    using var alcance = app.Services.CreateScope();

    await alcance.ServiceProvider.GetRequiredService<AppIdentityContext>().Database.MigrateAsync();
    await alcance.ServiceProvider.GetRequiredService<AppProyectoContext>().Database.MigrateAsync();
    app.Logger.LogInformation("Migraciones aplicadas.");
}

// Siembra roles y usuarios iniciales. Requiere la base ya migrada; si falla, la API arranca igual.
try
{
    await SeedSeguridad.InicializarAsync(app.Services);
    await SeedDominio.InicializarAsync(app.Services);
}
catch (Exception ex)
{
    app.Logger.LogError(ex, "Falló la siembra inicial de datos.");
}

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Primero de todo, para que la dirección real de quien llama ya esté lista para el resto.
if (detrasDeProxy)
{
    app.UseForwardedHeaders();
}

app.UseMiddleware<CabecerasDeSeguridadMiddleware>();

// Va antes del resto para que también capture los errores del pipeline.
app.UseMiddleware<ManejadorErroresMiddleware>();

// La redirección a HTTPS solo tiene sentido si es la propia API la que atiende HTTPS. Detrás de
// un proxy que ya lo hace, redirigir aquí provocaría un bucle; por eso es opcional.
if (builder.Configuration.GetValue<bool>("Seguridad:RedirigirAHttps"))
{
    app.UseHttpsRedirection();

    if (!app.Environment.IsDevelopment())
    {
        app.UseHsts();
    }
}

// CORS va antes de la autenticación: si no, un 401 llega al navegador sin las cabeceras
// de CORS y el error que se ve es confuso.
app.UseCors(PoliticaFrontend);

app.UseRateLimiter();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Para que el orquestador (Docker, un monitor) sepa si la API está viva y alcanza la base.
// Es anónimo y no devuelve ningún dato.
app.MapGet("/salud", async (AppProyectoContext contexto) =>
    await contexto.Database.CanConnectAsync()
        ? Results.Ok(new { estado = "ok" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable))
    .AllowAnonymous();

app.Run();

// Lo necesitan las pruebas de integración para levantar la aplicación completa en memoria.
public partial class Program;
