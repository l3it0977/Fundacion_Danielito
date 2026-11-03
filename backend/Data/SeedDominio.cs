using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Data;

public static class SeedDominio
{
    /// <summary>Nombre y apellido de un maestro, para buscarlo entre los perfiles.</summary>
    private record MaestroDelGrupo(string Nombre, string Apellido);

    // Grupos por edad con los maestros a cargo. Un grupo puede tener más de uno:
    // "15 a 18 años" arranca con dos para que el caso quede cubierto desde el inicio.
    // La relación vive en la base: reasignar es cambiar registros, no tocar el código.
    private static readonly (string Grupo, MaestroDelGrupo[] Maestros)[] GruposIniciales =
    [
        ("6 a 8 años",   [new("Ivi", "Condori")]),
        ("9 a 11 años",  [new("Danna", "Garcia")]),
        ("12 a 14 años", [new("Diego", "Chore")]),
        ("15 a 18 años", [new("Juan Carlos", "Contreras"), new("Diego", "Chore")]),
        ("18+",          [new("Juan Carlos", "Contreras")])
    ];

    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var proveedor = scope.ServiceProvider;

        var logger = proveedor.GetRequiredService<ILogger<Program>>();
        var context = proveedor.GetRequiredService<AppProyectoContext>();

        foreach (var (nombreGrupo, maestrosDelGrupo) in GruposIniciales)
        {
            if (await context.Grupos.AnyAsync(x => x.Nombre.Equals(nombreGrupo)))
            {
                continue;
            }

            List<Perfil> perfilesEncontrados = [];

            foreach (MaestroDelGrupo maestro in maestrosDelGrupo)
            {
                Perfil? bdPerfil = await context.Perfiles.FirstOrDefaultAsync(x =>
                    x.Nombre.Equals(maestro.Nombre) && x.Apellido.Equals(maestro.Apellido));

                if (bdPerfil == null)
                {
                    logger.LogWarning(
                        "Falta el maestro {Nombre} {Apellido} para el grupo '{Grupo}'.",
                        maestro.Nombre, maestro.Apellido, nombreGrupo);
                    continue;
                }

                perfilesEncontrados.Add(bdPerfil);
            }

            // Un grupo sin ningún maestro no sirve: mejor no crearlo y avisar.
            if (perfilesEncontrados.Count == 0)
            {
                logger.LogWarning(
                    "No se creó el grupo '{Grupo}': ninguno de sus maestros existe.",
                    nombreGrupo);
                continue;
            }

            context.Grupos.Add(new Grupo
            {
                Nombre = nombreGrupo,
                Activo = true,
                Grupo_Maestros = perfilesEncontrados
                    .Select(perfil => new Grupo_Maestro { PerfilId = perfil.Id })
                    .ToList()
            });

            logger.LogInformation(
                "Grupo '{Grupo}' creado con {Cantidad} maestro(s).",
                nombreGrupo, perfilesEncontrados.Count);
        }

        await context.SaveChangesAsync();
    }
}
