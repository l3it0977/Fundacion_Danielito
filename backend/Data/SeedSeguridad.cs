using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Data;

public static class SeedSeguridad
{
    private static readonly string[] Roles = ["Directora", "Administrador", "Maestro"];

    private static readonly (string Nombre, string Apellido, string Email, string Rol)[] UsuariosIniciales =
    [
        ("Dunia", "Nostas", "directora@test.com", "Directora"),
        ("Vania", "Colque", "admin1@test.com", "Administrador"),
        ("Adela", "Colque", "admin2@test.com", "Administrador"),
        ("Danna", "Garcia", "mestro1@test.com", "Maestro"),
        ("Diego", "Chore", "mestro2@test.com", "Maestro"),
        ("Ivi", "Condori", "mestro3@test.com", "Maestro"),
        ("Juan Carlos", "Contreras", "mestro4@test.com", "Maestro")
    ];

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var proveedor = scope.ServiceProvider;

        var logger = proveedor.GetRequiredService<ILogger<Program>>();
        var roleManager = proveedor.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = proveedor.GetRequiredService<UserManager<IdentityUser>>();
        var context = proveedor.GetRequiredService<AppProyectoContext>();
        var configuration = proveedor.GetRequiredService<IConfiguration>();

        foreach (var rol in Roles)
        {
            if (!await roleManager.RoleExistsAsync(rol))
            {
                await roleManager.CreateAsync(new IdentityRole(rol));
                logger.LogInformation("Rol '{Rol}' creado.", rol);
            }
        }

        var passwordInicial = configuration["Seed:PasswordInicial"];
        if (string.IsNullOrWhiteSpace(passwordInicial))
        {
            logger.LogWarning(
                "No se sembraron usuarios: falta 'Seed:PasswordInicial'. " +
                "Configúrala con: dotnet user-secrets set \"Seed:PasswordInicial\" \"...\"");
            return;
        }

        foreach (var (nombre, apellido, email, rol) in UsuariosIniciales)
        {
            if (await userManager.FindByEmailAsync(email) != null)
            {
                continue;
            }

            var nuevoUsuario = new IdentityUser { UserName = email, Email = email };

            var resultado = await userManager.CreateAsync(nuevoUsuario, passwordInicial);
            if (!resultado.Succeeded)
            {
                logger.LogError(
                    "No se pudo crear '{Email}': {Errores}",
                    email,
                    string.Join("; ", resultado.Errors.Select(x => x.Description)));
                continue;
            }

            await userManager.AddToRoleAsync(nuevoUsuario, rol);

            context.Perfiles.Add(new Perfil
            {
                UsuarioId = nuevoUsuario.Id,
                Nombre = nombre,
                Apellido = apellido,
                Activo = true
            });

            logger.LogInformation("Usuario '{Email}' creado con rol '{Rol}'.", email, rol);
        }

        await context.SaveChangesAsync();
    }
}
