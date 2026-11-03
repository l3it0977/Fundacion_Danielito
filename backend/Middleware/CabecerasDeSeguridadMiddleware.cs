namespace SISTEMADANIELITO.Middleware;

/// <summary>
/// Agrega a cada respuesta de la API las cabeceras que le dicen al navegador qué NO debe hacer
/// con ella. La API solo entrega JSON, PDF y Excel, nunca páginas: por eso la política es la más
/// estricta posible (nada se puede cargar, ejecutar ni incrustar desde sus respuestas).
///
/// La interfaz web tiene sus propias cabeceras, con una política más amplia (necesita sus
/// scripts y el mapa), definidas en frontend/nginx.conf.template.
/// </summary>
public class CabecerasDeSeguridadMiddleware
{
    private readonly RequestDelegate _siguiente;

    public CabecerasDeSeguridadMiddleware(RequestDelegate siguiente)
    {
        _siguiente = siguiente;
    }

    public Task InvokeAsync(HttpContext contexto)
    {
        // Se agregan al empezar a responder: así también salen en los errores y en los 401.
        contexto.Response.OnStarting(() =>
        {
            var cabeceras = contexto.Response.Headers;

            // El navegador no debe "adivinar" que un JSON es un script.
            cabeceras["X-Content-Type-Options"] = "nosniff";

            // Nada de esto debe poder incrustarse en una página ajena.
            cabeceras["X-Frame-Options"] = "DENY";
            cabeceras["Content-Security-Policy"] = "default-src 'none'; frame-ancestors 'none'";

            // Que no se filtre la dirección desde la que se llamó a la API.
            cabeceras["Referrer-Policy"] = "no-referrer";

            // Las respuestas llevan datos personales de menores: que ningún intermediario las guarde.
            if (!cabeceras.ContainsKey("Cache-Control"))
            {
                cabeceras["Cache-Control"] = "no-store";
            }

            return Task.CompletedTask;
        });

        return _siguiente(contexto);
    }
}
