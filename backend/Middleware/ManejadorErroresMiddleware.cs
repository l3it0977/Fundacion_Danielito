using System.Text.Json;
using Npgsql;
using SISTEMADANIELITO.Excepciones;

namespace SISTEMADANIELITO.Middleware;

/// <summary>
/// Intercepta cualquier excepción que suba desde los servicios y la convierte en una
/// respuesta JSON con el código HTTP correcto.
///
/// Antes de esto, todo error de negocio salía como un HTTP 500 con el stack trace completo:
/// un código de niño duplicado y una caída real de la base se veían exactamente igual,
/// y además la respuesta exponía las rutas internas del servidor.
/// </summary>
public class ManejadorErroresMiddleware
{
    private readonly RequestDelegate _siguiente;
    private readonly ILogger<ManejadorErroresMiddleware> _registro;

    public ManejadorErroresMiddleware(RequestDelegate siguiente, ILogger<ManejadorErroresMiddleware> registro)
    {
        _siguiente = siguiente;
        _registro = registro;
    }

    public async Task InvokeAsync(HttpContext contexto)
    {
        try
        {
            // Deja pasar la petición al resto del pipeline y espera el resultado.
            await _siguiente(contexto);
        }
        catch (Exception excepcion)
        {
            await ResponderConError(contexto, excepcion);
        }
    }

    private async Task ResponderConError(HttpContext contexto, Exception excepcion)
    {
        // Si ya se empezó a enviar la respuesta no se puede cambiar el código de estado.
        if (contexto.Response.HasStarted)
        {
            _registro.LogError(excepcion, "Error después de iniciar la respuesta.");
            return;
        }

        int codigoHttp;
        string mensajeParaElCliente;

        switch (excepcion)
        {
            case RecursoNoEncontradoException:
                codigoHttp = StatusCodes.Status404NotFound;
                mensajeParaElCliente = excepcion.Message;
                break;

            case ReglaNegocioException:
                codigoHttp = StatusCodes.Status400BadRequest;
                mensajeParaElCliente = excepcion.Message;
                break;

            case UnauthorizedAccessException:
                codigoHttp = StatusCodes.Status403Forbidden;
                mensajeParaElCliente = excepcion.Message;
                break;

            // Red de seguridad: si una restricción de la base salta antes que nuestra
            // validación (por ejemplo dos peticiones simultáneas con el mismo código),
            // se responde 409 en lugar de un 500 confuso.
            case Exception when EsViolacionDeUnicidad(excepcion):
                codigoHttp = StatusCodes.Status409Conflict;
                mensajeParaElCliente = "El registro ya existe o duplica un valor único.";
                break;

            // Lo mismo con las claves foráneas: la base rechaza borrar algo que tiene
            // registros relacionados (23503), y eso no es un fallo del servidor.
            case Exception when EsViolacionDeClaveForanea(excepcion):
                codigoHttp = StatusCodes.Status409Conflict;
                mensajeParaElCliente = "No se puede eliminar porque tiene registros relacionados.";
                break;

            default:
                // Error inesperado: el detalle va al log del servidor, nunca al cliente.
                codigoHttp = StatusCodes.Status500InternalServerError;
                mensajeParaElCliente = "Ocurrió un error inesperado. Intenta nuevamente.";
                break;
        }

        if (codigoHttp == StatusCodes.Status500InternalServerError)
        {
            _registro.LogError(excepcion, "Error no controlado en {Ruta}.", contexto.Request.Path);
        }
        else
        {
            _registro.LogWarning("{Codigo} en {Ruta}: {Mensaje}",
                codigoHttp, contexto.Request.Path, excepcion.Message);
        }

        contexto.Response.Clear();
        contexto.Response.StatusCode = codigoHttp;
        contexto.Response.ContentType = "application/json";

        string cuerpo = JsonSerializer.Serialize(new { mensaje = mensajeParaElCliente });
        await contexto.Response.WriteAsync(cuerpo);
    }

    /// <summary>El código 23503 de PostgreSQL corresponde a "foreign_key_violation".</summary>
    private static bool EsViolacionDeClaveForanea(Exception excepcion)
    {
        return excepcion is PostgresException { SqlState: "23503" }
            || excepcion.InnerException is PostgresException { SqlState: "23503" };
    }

    /// <summary>
    /// El código 23505 de PostgreSQL corresponde a "unique_violation".
    /// Puede venir envuelto en una DbUpdateException, así que se revisa la excepción interna.
    /// </summary>
    private static bool EsViolacionDeUnicidad(Exception excepcion)
    {
        return excepcion is PostgresException { SqlState: "23505" }
            || excepcion.InnerException is PostgresException { SqlState: "23505" };
    }
}
