using System.Security.Claims;

namespace SISTEMADANIELITO.Controllers;

public static class ClaimsPrincipalExtensions
{
    public static int ObtenerPerfilId(this ClaimsPrincipal usuario)
    {
        string? valor = usuario.FindFirst("PerfilId")?.Value;

        if (!int.TryParse(valor, out int perfilId))
        {
            throw (new UnauthorizedAccessException("El token no contiene un perfil válido"));
        }

        return perfilId;
    }

    // Los servicios reciben null cuando no hay que restringir nada: solo el maestro ve
    // un subconjunto, la directora y el administrador ven todo.
    public static int? FiltroMaestro(this ClaimsPrincipal usuario)
    {
        if (!usuario.IsInRole("Maestro"))
        {
            return null;
        }

        return usuario.ObtenerPerfilId();
    }
}
