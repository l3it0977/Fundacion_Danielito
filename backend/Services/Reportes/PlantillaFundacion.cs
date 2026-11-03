using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>
/// Identidad visual de la Fundación Danielito para los reportes en PDF: colores, logo,
/// encabezado, pie y estilos de tabla. Cualquier reporte nuevo debe armarse con estas piezas
/// para que todos se vean iguales.
/// </summary>
internal static class PlantillaFundacion
{
    /// <summary>Colores institucionales, los mismos del sistema web (tokens.css).</summary>
    public static class Marca
    {
        public const string TealOscuro = "#1d4a4b";
        public const string TealMedio = "#31706e";
        public const string TealPalido = "#a3d3ce";
        public const string Naranja = "#ea8028";
        public const string Fondo = "#f4f8f8";
        public const string Texto = "#1a2e2e";
        public const string TextoMedio = "#4a6363";
        public const string Borde = "#e5eeee";
    }

    /// <summary>Se lee una sola vez. Si falta el archivo el reporte sale igual, solo sin logo.</summary>
    private static readonly byte[]? LogoFundacion = CargarLogo();

    private static byte[]? CargarLogo()
    {
        string ruta = Path.Combine(AppContext.BaseDirectory, "Recursos", "logo-danielito.png");

        return File.Exists(ruta) ? File.ReadAllBytes(ruta) : null;
    }

    /// <summary>Página con márgenes, tipografía y colores de texto estándar.</summary>
    public static void ConfigurarPagina(PageDescriptor pagina, PageSize tamano)
    {
        pagina.Size(tamano);
        pagina.Margin(1.5f, Unit.Centimetre);
        pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Marca.Texto));
    }

    /// <summary>Logo y nombre de la fundación, y una franja con el título y los filtros aplicados.</summary>
    public static void Encabezado(IContainer contenedor, string titulo, params string[] detalles)
    {
        contenedor.Column(columna =>
        {
            columna.Item().Row(fila =>
            {
                fila.Spacing(10);

                if (LogoFundacion != null)
                {
                    fila.ConstantItem(44).Image(LogoFundacion);
                }

                fila.RelativeItem().AlignMiddle().Column(nombre =>
                {
                    nombre.Item().Text("Fundación Danielito")
                        .FontSize(16).Bold().FontColor(Marca.TealOscuro);
                    nombre.Item().Text("Warnes, Santa Cruz - Bolivia")
                        .FontSize(8).FontColor(Marca.TextoMedio);
                });

                fila.AutoItem().AlignMiddle().AlignRight().Column(emision =>
                {
                    emision.Item().AlignRight().Text("Emitido el").FontSize(7).FontColor(Marca.TextoMedio);
                    emision.Item().AlignRight().Text(DateTime.Now.ToString("dd/MM/yyyy HH:mm"))
                        .FontSize(9).Bold();
                });
            });

            columna.Item().PaddingTop(6).LineHorizontal(2).LineColor(Marca.Naranja);

            columna.Item().Background(Marca.TealOscuro).PaddingVertical(6).PaddingHorizontal(10)
                .Column(franja =>
                {
                    franja.Item().Text(titulo).FontSize(13).Bold().FontColor(Colors.White);
                    franja.Item().Text(string.Join("   ·   ", detalles))
                        .FontSize(9).FontColor(Marca.TealPalido);
                });
        });
    }

    public static void Pie(IContainer contenedor)
    {
        contenedor.BorderTop(1).BorderColor(Marca.Borde).PaddingTop(4).Row(fila =>
        {
            fila.RelativeItem().Text("Fundación Danielito  ·  Sistema de seguimiento de niños")
                .FontSize(8).FontColor(Marca.TextoMedio);

            fila.AutoItem().Text(texto =>
            {
                texto.DefaultTextStyle(estilo => estilo.FontSize(8).FontColor(Marca.TextoMedio));
                texto.Span("Página ");
                texto.CurrentPageNumber();
                texto.Span(" de ");
                texto.TotalPages();
            });
        });
    }

    public static void TituloSeccion(IContainer contenedor, string titulo)
    {
        contenedor.BorderBottom(1.5f).BorderColor(Marca.Naranja).PaddingBottom(2)
            .Text(titulo).FontSize(11).Bold().FontColor(Marca.TealOscuro);
    }

    /// <summary>Recuadro con una cifra grande y su etiqueta.</summary>
    public static void CajaDato(RowDescriptor fila, string valor, string etiqueta, string color)
    {
        fila.RelativeItem().Background(Marca.Fondo).BorderLeft(3).BorderColor(color).Padding(8)
            .Column(caja =>
            {
                caja.Item().Text(valor).FontSize(18).Bold().FontColor(color);
                caja.Item().Text(etiqueta).FontSize(8).FontColor(Marca.TextoMedio);
            });
    }

    //ESTILOS DE TABLA

    /// <summary>Celda de encabezado: fondo verde institucional y texto blanco en negrita.</summary>
    public static IContainer CeldaEncabezado(IContainer celda)
    {
        return celda
            .Background(Marca.TealMedio)
            .Padding(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Colors.White));
    }

    /// <summary>Celda de datos; las filas pares llevan fondo suave para seguir la línea con la vista.</summary>
    public static Func<IContainer, IContainer> CeldaDeFila(int indice)
    {
        return celda => celda
            .Background(indice % 2 == 0 ? Colors.White : Marca.Fondo)
            .BorderBottom(1)
            .BorderColor(Marca.Borde)
            .Padding(4);
    }

    // Las columnas de días son angostas: sin relleno lateral caben los números de dos cifras.
    public static IContainer CeldaEncabezadoDia(IContainer celda)
    {
        return celda
            .Background(Marca.TealMedio)
            .PaddingVertical(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Colors.White));
    }

    public static Func<IContainer, IContainer> CeldaDeDia(int indice)
    {
        return celda => celda
            .Background(indice % 2 == 0 ? Colors.White : Marca.Fondo)
            .BorderBottom(1)
            .BorderColor(Marca.Borde)
            .PaddingVertical(4);
    }

    public static IContainer CeldaDeTotal(IContainer celda)
    {
        return celda
            .Background(Marca.TealPalido)
            .Padding(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Marca.TealOscuro));
    }

    /// <summary>Una sola letra por celda, para que la matriz mensual quepa a lo ancho.</summary>
    public static string AbreviaturaDelEstado(string? estado)
    {
        return estado switch
        {
            Asistencia.EstadoPendiente => "·",
            Asistencia.EstadoPresente => "P",
            Asistencia.EstadoAusente => "A",
            Asistencia.EstadoJustificado => "J",
            _ => "-"
        };
    }
}
