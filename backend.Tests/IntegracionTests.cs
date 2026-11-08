using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

/// <summary>
/// La aplicación completa, de punta a punta por HTTP: lo que vería el navegador.
/// Cada prueba levanta su propia aplicación y su propia base, así no se pisan.
/// </summary>
public class IntegracionTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new();

    public async Task InitializeAsync() => await _app.PrepararAsync();

    public Task DisposeAsync()
    {
        _app.Dispose();
        return Task.CompletedTask;
    }

    private static async Task<JsonElement> LeerJsonAsync(HttpResponseMessage respuesta)
    {
        return JsonDocument.Parse(await respuesta.Content.ReadAsStringAsync()).RootElement.Clone();
    }

    //CABECERAS DE SEGURIDAD

    [Fact]
    public async Task TodasLasRespuestas_LlevanLasCabecerasDeSeguridad_AunLosErrores()
    {
        var cliente = _app.CreateClient();

        var sinSesion = await cliente.GetAsync("/Nino");
        var loginMalo = await cliente.PostAsJsonAsync("/Auth/Login",
            new LoginDto { Email = "nadie@prueba.com", Password = "Incorrecta.1" });

        foreach (var respuesta in new[] { sinSesion, loginMalo })
        {
            Assert.Equal("nosniff", respuesta.Headers.GetValues("X-Content-Type-Options").Single());
            Assert.Equal("DENY", respuesta.Headers.GetValues("X-Frame-Options").Single());
            Assert.Equal("no-referrer", respuesta.Headers.GetValues("Referrer-Policy").Single());
            Assert.Contains("default-src 'none'", respuesta.Headers.GetValues("Content-Security-Policy").Single());
            Assert.Equal("no-store", respuesta.Headers.CacheControl?.ToString());
        }
    }

    //AUTENTICACIÓN

    [Fact]
    public async Task SinSesion_ElAccesoSeRechazaCon401()
    {
        var respuesta = await _app.CreateClient().GetAsync("/Nino/Buscar");

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
    }

    [Fact]
    public async Task LoginIncorrecto_Da401ConUnMensajeEnJson()
    {
        await _app.CrearUsuarioAsync("ana@prueba.com", "Maestro");

        var respuesta = await _app.CreateClient().PostAsJsonAsync("/Auth/Login",
            new LoginDto { Email = "ana@prueba.com", Password = "Otra.Clave9" });

        Assert.Equal(HttpStatusCode.Unauthorized, respuesta.StatusCode);
        Assert.Equal("Credenciales incorrectas.", (await LeerJsonAsync(respuesta)).GetProperty("mensaje").GetString());
    }

    [Fact]
    public async Task LoginCorrecto_PermiteUsarLaApi()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        var cliente = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        var respuesta = await cliente.GetAsync("/Nino/Buscar");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
    }

    [Fact]
    public async Task LaDesactivacionDeUnaCuenta_CortaElAccesoDeInmediato()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        int perfilDeAna = await _app.CrearUsuarioAsync("ana@prueba.com", "Maestro");

        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");
        var ana = await _app.ClienteAutenticadoAsync("ana@prueba.com");

        // Con sesión abierta, Ana puede trabajar (y el resultado queda guardado en la memoria del validador).
        Assert.Equal(HttpStatusCode.OK, (await ana.GetAsync("/Nino/Buscar")).StatusCode);

        var desactivar = await directora.PutAsJsonAsync("/Usuario/Estado",
            new CambiarEstadoUsuarioDTO { PerfilId = perfilDeAna, Activo = false });
        Assert.Equal(HttpStatusCode.OK, desactivar.StatusCode);

        // Su token sigue vigente, pero la cuenta ya no: la siguiente petición debe fallar YA.
        Assert.Equal(HttpStatusCode.Unauthorized, (await ana.GetAsync("/Nino/Buscar")).StatusCode);
    }

    //PERMISOS POR ROL

    [Fact]
    public async Task UnMaestro_NoEntraAReportesNiABitacora_PeroLaDireccionSi()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        await _app.CrearUsuarioAsync("ana@prueba.com", "Maestro");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");
        var maestra = await _app.ClienteAutenticadoAsync("ana@prueba.com");

        foreach (var ruta in new[] { "/Reporte/Asistencia", "/Reporte/AsistenciaExcel", "/Bitacora/Buscar", "/Usuario" })
        {
            Assert.Equal(HttpStatusCode.Forbidden, (await maestra.GetAsync(ruta)).StatusCode);
            Assert.Equal(HttpStatusCode.OK, (await directora.GetAsync(ruta)).StatusCode);
        }
    }

    [Fact]
    public async Task UnMaestro_SoloVeLosNinosDeSusGrupos()
    {
        int perfilDeAna = await _app.CrearUsuarioAsync("ana@prueba.com", "Maestro");
        int perfilDeBeto = await _app.CrearUsuarioAsync("beto@prueba.com", "Maestro");

        await _app.ConBaseAsync(async contexto =>
        {
            var grupoA = new Grupo { Nombre = "A", Grupo_Maestros = [new Grupo_Maestro { PerfilId = perfilDeAna }] };
            var grupoB = new Grupo { Nombre = "B", Grupo_Maestros = [new Grupo_Maestro { PerfilId = perfilDeBeto }] };
            contexto.Grupos.AddRange(grupoA, grupoB);
            await contexto.SaveChangesAsync();

            contexto.Ninos.AddRange(
                new Nino { Codigo = "A-1", Nombres = "Luis", Apellidos = "Gómez", GrupoId = grupoA.Id },
                new Nino { Codigo = "B-1", Nombres = "Carla", Apellidos = "Soto", GrupoId = grupoB.Id });
            await contexto.SaveChangesAsync();
        });

        var ana = await _app.ClienteAutenticadoAsync("ana@prueba.com");

        var resultado = await LeerJsonAsync(await ana.GetAsync("/Nino/Buscar"));
        Assert.Equal(1, resultado.GetProperty("total").GetInt32());
        Assert.Equal("A-1", resultado.GetProperty("items")[0].GetProperty("codigo").GetString());

        // Y tampoco puede abrir la ficha de un niño ajeno por su Id.
        int idDeCarla = await _app.ConBaseAsync(c => c.Ninos.Where(n => n.Codigo == "B-1").Select(n => n.Id).FirstAsync());
        Assert.Equal(HttpStatusCode.Forbidden, (await ana.GetAsync($"/Nino/{idDeCarla}")).StatusCode);
    }

    //ERRORES

    [Fact]
    public async Task EliminarUnNinoConHistorial_Da400ConElMotivo_YNoBorraNada()
    {
        int perfil = await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        int ninoId = await _app.ConBaseAsync(async contexto =>
        {
            var grupo = new Grupo { Nombre = "A" };
            contexto.Grupos.Add(grupo);
            await contexto.SaveChangesAsync();

            var nino = new Nino { Codigo = "N-1", Nombres = "Luis", Apellidos = "Gómez", GrupoId = grupo.Id };
            contexto.Ninos.Add(nino);
            await contexto.SaveChangesAsync();

            var actividad = new Actividad
            {
                Nombre = "Taller", Tipo = Actividad.TipoNormal, Fecha_Actividad = new DateOnly(2026, 10, 1),
                CreadoPorId = perfil, Actividad_Ninos = [new Actividad_Nino { NinoId = nino.Id }]
            };
            contexto.Actividades.Add(actividad);
            await contexto.SaveChangesAsync();

            return nino.Id;
        });

        var respuesta = await directora.DeleteAsync($"/Nino/{ninoId}");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
        Assert.Contains("Desactívalo", (await LeerJsonAsync(respuesta)).GetProperty("mensaje").GetString());
        Assert.True(await _app.ConBaseAsync(c => c.Ninos.AnyAsync(n => n.Id == ninoId)));
    }

    [Fact]
    public async Task UnRecursoInexistente_Da404ConMensajeEnJson()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        var respuesta = await directora.GetAsync("/Nino/99999");

        Assert.Equal(HttpStatusCode.NotFound, respuesta.StatusCode);
        Assert.Equal("Niño no encontrado", (await LeerJsonAsync(respuesta)).GetProperty("mensaje").GetString());
    }

    //REPORTES

    [Fact]
    public async Task LosReportes_SeDescarganConSuTipoYNombreDeArchivo()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        var pdf = await directora.GetAsync("/Reporte/Asistencia?anio=2026&mes=10");
        var excel = await directora.GetAsync("/Reporte/AsistenciaExcel?anio=2026");

        Assert.Equal("application/pdf", pdf.Content.Headers.ContentType?.MediaType);
        Assert.Equal("asistencia-todos-los-grupos-10-2026.pdf", pdf.Content.Headers.ContentDisposition?.FileName);
        Assert.Equal("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", excel.Content.Headers.ContentType?.MediaType);
        Assert.Equal("asistencia-todos-los-grupos-todo-2026.xlsx", excel.Content.Headers.ContentDisposition?.FileName);

        var inicio = await pdf.Content.ReadAsByteArrayAsync();
        Assert.Equal("%PDF", System.Text.Encoding.ASCII.GetString(inicio, 0, 4));
    }

    [Fact]
    public async Task UnPeriodoInvalido_Da400()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        var respuesta = await directora.GetAsync("/Reporte/Asistencia?anio=2026&mes=13");

        Assert.Equal(HttpStatusCode.BadRequest, respuesta.StatusCode);
    }

    //SALUD

    [Fact]
    public async Task Salud_RespondeOkSinPedirSesion()
    {
        var respuesta = await _app.CreateClient().GetAsync("/salud");

        Assert.Equal(HttpStatusCode.OK, respuesta.StatusCode);
        Assert.Equal("ok", (await LeerJsonAsync(respuesta)).GetProperty("estado").GetString());
    }

    //BITÁCORA DE PUNTA A PUNTA

    [Fact]
    public async Task UnCambioHechoPorLaApi_QuedaEnLaBitacoraANombreDeQuienLoHizo()
    {
        await _app.CrearUsuarioAsync("dunia@prueba.com", "Directora", "Dunia", "Nostas");
        var directora = await _app.ClienteAutenticadoAsync("dunia@prueba.com");

        int grupoId = await _app.ConBaseAsync(async contexto =>
        {
            var grupo = new Grupo { Nombre = "A" };
            contexto.Grupos.Add(grupo);
            await contexto.SaveChangesAsync();
            return grupo.Id;
        });

        var crear = await directora.PostAsJsonAsync("/Nino", new CreateNinoDTO
        {
            Codigo = "N-77", Nombres = "Ana", Apellidos = "Pérez", GrupoId = grupoId
        });
        Assert.Equal(HttpStatusCode.OK, crear.StatusCode);

        var bitacora = await LeerJsonAsync(await directora.GetAsync("/Bitacora/Buscar?texto=N-77"));

        var renglon = bitacora.GetProperty("items")[0];
        Assert.Equal("Dunia Nostas", renglon.GetProperty("usuario").GetString());
        Assert.Equal("Creado", renglon.GetProperty("accion").GetString());
        Assert.Equal("Nino", renglon.GetProperty("entidad").GetString());
    }
}

/// <summary>El límite de intentos de login va aparte: necesita su propia aplicación con un tope bajo.</summary>
public class LimiteDeLoginTests : IAsyncLifetime
{
    private readonly AplicacionDePrueba _app = new(new Dictionary<string, string?>
    {
        ["Seguridad:IntentosDeLoginPorMinuto"] = "3"
    });

    public async Task InitializeAsync() => await _app.PrepararAsync();

    public Task DisposeAsync()
    {
        _app.Dispose();
        return Task.CompletedTask;
    }

    [Fact]
    public async Task TrasElTopeDeIntentos_Responde429ConRetryAfterYMensaje()
    {
        var cliente = _app.CreateClient();
        var intento = new LoginDto { Email = "nadie@prueba.com", Password = "Incorrecta.1" };

        for (int i = 0; i < 3; i++)
        {
            Assert.Equal(HttpStatusCode.Unauthorized, (await cliente.PostAsJsonAsync("/Auth/Login", intento)).StatusCode);
        }

        var bloqueado = await cliente.PostAsJsonAsync("/Auth/Login", intento);

        Assert.Equal(HttpStatusCode.TooManyRequests, bloqueado.StatusCode);
        Assert.Equal("60", bloqueado.Headers.GetValues("Retry-After").Single());
        Assert.Contains("Demasiados intentos", await bloqueado.Content.ReadAsStringAsync());
    }

    [Fact]
    public async Task ElLimiteSoloAfectaAlLogin()
    {
        var cliente = _app.CreateClient();

        for (int i = 0; i < 6; i++)
        {
            Assert.Equal(HttpStatusCode.OK, (await cliente.GetAsync("/salud")).StatusCode);
        }
    }
}
