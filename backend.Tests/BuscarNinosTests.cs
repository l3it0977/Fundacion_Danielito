using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Models;
using SISTEMADANIELITO.Services;
using SISTEMADANIELITO.Tests.Apoyo;

namespace SISTEMADANIELITO.Tests;

public class BuscarNinosTests : IDisposable
{
    private readonly BaseDeDatos _bd = new();
    private readonly NinoService _servicio;

    private readonly Perfil _ana;
    private readonly Perfil _beto;
    private readonly Grupo _grupoDeAna;
    private readonly Grupo _grupoDeBeto;

    public BuscarNinosTests()
    {
        _servicio = new NinoService(_bd.Context, BaseDeDatos.CrearMapper());

        _ana = _bd.CrearPerfil("Ana");
        _beto = _bd.CrearPerfil("Beto");
        _grupoDeAna = _bd.CrearGrupo("Grupo A", _ana);
        _grupoDeBeto = _bd.CrearGrupo("Grupo B", _beto);
    }

    public void Dispose() => _bd.Dispose();

    private Nino Crear(Grupo grupo, string codigo, string nombres, string apellidos, bool activo = true)
    {
        var nino = new Nino { Codigo = codigo, Nombres = nombres, Apellidos = apellidos, GrupoId = grupo.Id, Activo = activo };
        _bd.Context.Ninos.Add(nino);
        _bd.Context.SaveChanges();
        return nino;
    }

    [Fact]
    public async Task PorDefecto_NoIncluyeALosInactivos()
    {
        Crear(_grupoDeAna, "A-1", "Luis", "Gómez");
        Crear(_grupoDeAna, "A-2", "Marta", "Pérez", activo: false);

        var resultado = await _servicio.Buscar(new BuscarNinosDTO(), null);

        Assert.Single(resultado.Items);
        Assert.Equal(1, resultado.Total);
    }

    [Fact]
    public async Task ConIncluirInactivos_LosMuestra()
    {
        Crear(_grupoDeAna, "A-1", "Luis", "Gómez");
        Crear(_grupoDeAna, "A-2", "Marta", "Pérez", activo: false);

        var resultado = await _servicio.Buscar(new BuscarNinosDTO { IncluirInactivos = true }, null);

        Assert.Equal(2, resultado.Total);
    }

    [Theory]
    [InlineData("luis")]            // en minúsculas, aunque se guardó "Luis"
    [InlineData("GÓMEZ")]           // el apellido, en mayúsculas
    [InlineData("luis gómez")]      // nombre y apellido juntos
    [InlineData("a-1")]             // el código
    [InlineData("  luis  ")]        // con espacios alrededor
    public async Task Texto_BuscaEnNombreApellidoYCodigoSinDistinguirMayusculas(string texto)
    {
        var luis = Crear(_grupoDeAna, "A-1", "Luis", "Gómez");
        Crear(_grupoDeAna, "A-2", "Marta", "Pérez");

        var resultado = await _servicio.Buscar(new BuscarNinosDTO { Texto = texto }, null);

        Assert.Single(resultado.Items);
        Assert.Equal(luis.Id, resultado.Items[0].Id);
    }

    [Fact]
    public async Task UnMaestroSoloVeLosNinosDeSusGrupos_AunConFiltrosAbiertos()
    {
        var deAna = Crear(_grupoDeAna, "A-1", "Luis", "Gómez");
        Crear(_grupoDeBeto, "B-1", "Carla", "Soto");

        var resultado = await _servicio.Buscar(new BuscarNinosDTO { IncluirInactivos = true }, _ana.Id);

        Assert.Single(resultado.Items);
        Assert.Equal(deAna.Id, resultado.Items[0].Id);
        Assert.Equal(1, resultado.Total);
    }

    [Fact]
    public async Task FiltraPorGrupo()
    {
        Crear(_grupoDeAna, "A-1", "Luis", "Gómez");
        var deBeto = Crear(_grupoDeBeto, "B-1", "Carla", "Soto");

        var resultado = await _servicio.Buscar(new BuscarNinosDTO { GrupoId = _grupoDeBeto.Id }, null);

        Assert.Equal(deBeto.Id, Assert.Single(resultado.Items).Id);
    }

    [Fact]
    public async Task Ordena_PorGrupoLuegoPorApellidoYNombre()
    {
        Crear(_grupoDeBeto, "B-1", "Carla", "Soto");
        Crear(_grupoDeAna, "A-2", "Zoe", "Alvarez");
        Crear(_grupoDeAna, "A-1", "Ana", "Alvarez");
        Crear(_grupoDeAna, "A-3", "Mia", "Zapata");

        var resultado = await _servicio.Buscar(new BuscarNinosDTO(), null);

        Assert.Equal(["A-1", "A-2", "A-3", "B-1"], resultado.Items.Select(x => x.Codigo).ToArray());
    }

    [Fact]
    public async Task Pagina_ReparteLosResultadosYCuentaElTotal()
    {
        for (int i = 1; i <= 7; i++)
        {
            Crear(_grupoDeAna, $"A-{i}", $"Nombre{i}", "Apellido");
        }

        var primera = await _servicio.Buscar(new BuscarNinosDTO { Pagina = 1, Tamano = 3 }, null);
        var ultima = await _servicio.Buscar(new BuscarNinosDTO { Pagina = 3, Tamano = 3 }, null);

        Assert.Equal(3, primera.Items.Count);
        Assert.Equal(7, primera.Total);
        Assert.Equal(3, primera.TotalPaginas);
        Assert.Single(ultima.Items);
        Assert.Equal("A-7", ultima.Items[0].Codigo);
    }

    [Fact]
    public async Task UnaPaginaQueNoExiste_DevuelveLaUltima()
    {
        for (int i = 1; i <= 5; i++)
        {
            Crear(_grupoDeAna, $"A-{i}", $"Nombre{i}", "Apellido");
        }

        var resultado = await _servicio.Buscar(new BuscarNinosDTO { Pagina = 99, Tamano = 2 }, null);

        Assert.Equal(3, resultado.Pagina);
        Assert.Single(resultado.Items);
    }

    [Theory]
    [InlineData(0, 1)]
    [InlineData(-5, 1)]
    [InlineData(1000, 100)]
    public async Task ElTamanoSeLimitaAUnRangoSensato(int pedido, int esperado)
    {
        var resultado = await _servicio.Buscar(new BuscarNinosDTO { Tamano = pedido }, null);

        Assert.Equal(esperado, resultado.Tamano);
    }

    [Fact]
    public async Task SinResultados_DevuelveUnaListaVaciaConLaPaginaUno()
    {
        var resultado = await _servicio.Buscar(new BuscarNinosDTO { Texto = "nadie" }, null);

        Assert.Empty(resultado.Items);
        Assert.Equal(0, resultado.Total);
        Assert.Equal(1, resultado.Pagina);
    }
}
