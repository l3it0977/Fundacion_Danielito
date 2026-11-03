using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SISTEMADANIELITO.Data;

namespace SISTEMADANIELITO.Middleware;

/// <summary>
/// Comprueba en cada petición que la cuenta del token siga activa.
///
/// El token dura 2 horas y por sí solo no sabe nada de la base: sin esta revisión, un
/// usuario desactivado seguiría entrando hasta que el token expirara. El resultado se
/// guarda un minuto en memoria para no consultar la base en cada petición, y
/// UsuarioService lo borra al activar o desactivar para que el cambio sea inmediato.
/// </summary>
public static class ValidadorDeCuenta
{
    private static readonly TimeSpan DuracionEnCache = TimeSpan.FromMinutes(1);

    public static string Clave(int perfilId) => $"perfil-activo-{perfilId}";

    public static async Task Validar(TokenValidatedContext contexto)
    {
        string? valor = contexto.Principal?.FindFirst("PerfilId")?.Value;

        if (!int.TryParse(valor, out int perfilId))
        {
            contexto.Fail("El token no contiene un perfil válido.");
            return;
        }

        var servicios = contexto.HttpContext.RequestServices;
        var cache = servicios.GetRequiredService<IMemoryCache>();

        if (!cache.TryGetValue(Clave(perfilId), out bool activo))
        {
            var context = servicios.GetRequiredService<AppProyectoContext>();

            // Un perfil que ya no existe cuenta como inactivo.
            activo = await context.Perfiles
                .AsNoTracking()
                .AnyAsync(x => x.Id == perfilId && x.Activo);

            cache.Set(Clave(perfilId), activo, DuracionEnCache);
        }

        if (!activo)
        {
            contexto.Fail("La cuenta está inactiva.");
        }
    }
}
