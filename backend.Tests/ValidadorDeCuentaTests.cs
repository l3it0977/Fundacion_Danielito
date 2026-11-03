using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.Middleware;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

/// <summary>Un token válido no basta: la cuenta tiene que seguir activa.</summary>
public class ValidadorDeCuentaTests : IDisposable
{
    private readonly BaseDeDatos _bd = new();
    private readonly ServiceProvider _servicios;
    private readonly IMemoryCache _cache;

    public ValidadorDeCuentaTests()
    {
        var coleccion = new ServiceCollection();
        coleccion.AddMemoryCache();
        coleccion.AddSingleton<AppProyectoContext>(_bd.Context);

        _servicios = coleccion.BuildServiceProvider();
        _cache = _servicios.GetRequiredService<IMemoryCache>();
    }

    public void Dispose()
    {
        _servicios.Dispose();
        _bd.Dispose();
    }

    [Fact]
    public async Task CuentaActiva_PasaLaValidacion()
    {
        var perfil = _bd.CrearPerfil("Ana");

        var contexto = await Validar(perfil.Id.ToString());

        Assert.Null(contexto.Result?.Failure);
    }

    [Fact]
    public async Task CuentaDesactivada_SeRechaza()
    {
        var perfil = _bd.CrearPerfil("Ana", activo: false);

        var contexto = await Validar(perfil.Id.ToString());

        Assert.NotNull(contexto.Result?.Failure);
    }

    [Fact]
    public async Task PerfilQueYaNoExiste_SeRechaza()
    {
        var contexto = await Validar("999");

        Assert.NotNull(contexto.Result?.Failure);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("no-es-un-numero")]
    public async Task TokenSinPerfilValido_SeRechaza(string? perfilId)
    {
        var contexto = await Validar(perfilId);

        Assert.NotNull(contexto.Result?.Failure);
    }

    [Fact]
    public async Task Desactivar_TieneEfectoInmediatoSiSeLimpiaLaCache()
    {
        var perfil = _bd.CrearPerfil("Ana");
        Assert.Null((await Validar(perfil.Id.ToString())).Result?.Failure);

        // Lo que hace UsuarioService.CambiarEstado: guardar el cambio y borrar la cache.
        perfil.Activo = false;
        _bd.Context.SaveChanges();
        _cache.Remove(ValidadorDeCuenta.Clave(perfil.Id));

        Assert.NotNull((await Validar(perfil.Id.ToString())).Result?.Failure);
    }

    [Fact]
    public async Task Cache_EvitaConsultarLaBaseEnCadaPeticion()
    {
        var perfil = _bd.CrearPerfil("Ana");
        await Validar(perfil.Id.ToString());

        // Un cambio hecho por fuera de la aplicación no se ve hasta que expire la cache.
        perfil.Activo = false;
        _bd.Context.SaveChanges();

        Assert.Null((await Validar(perfil.Id.ToString())).Result?.Failure);
    }

    private async Task<TokenValidatedContext> Validar(string? perfilId)
    {
        var identidad = new ClaimsIdentity("prueba");
        if (perfilId != null)
        {
            identidad.AddClaim(new Claim("PerfilId", perfilId));
        }

        var contexto = new TokenValidatedContext(
            new DefaultHttpContext { RequestServices = _servicios },
            new AuthenticationScheme("Bearer", null, typeof(JwtBearerHandler)),
            new JwtBearerOptions())
        {
            Principal = new ClaimsPrincipal(identidad)
        };

        await ValidadorDeCuenta.Validar(contexto);
        return contexto;
    }
}
