using QuestPDF.Fluent;
using QuestPDF.Helpers;
using static SISTEMADANIELITO.Services.Reportes.PlantillaFundacion;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>Listado de las actividades realizadas, con la plantilla de la fundación.</summary>
internal static class PdfActividades
{
    public static byte[] Generar(DatosDeActividades datos)
    {
        var actividades = datos.Actividades;

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                ConfigurarPagina(pagina, PageSizes.Letter);

                pagina.Header().Element(encabezado =>
                    Encabezado(encabezado, "Reporte de actividades",
                        Periodo.TextoDelGrupo(datos.GrupoElegido?.Nombre), Periodo.Texto(datos.Mes, datos.Anio)));

                pagina.Content().PaddingVertical(10).Column(cuerpo =>
                {
                    cuerpo.Spacing(12);

                    cuerpo.Item().Row(fila =>
                    {
                        fila.Spacing(8);
                        CajaDato(fila, actividades.Count.ToString(), "Actividades", Marca.TealMedio);
                        CajaDato(fila, datos.TotalParticipaciones.ToString(), "Participaciones", Marca.Naranja);
                    });

                    if (actividades.Count == 0)
                    {
                        cuerpo.Item().Text("No hay actividades en el periodo seleccionado.").Italic();
                        return;
                    }

                    cuerpo.Item().Table(tabla =>
                    {
                        tabla.ColumnsDefinition(columnas =>
                        {
                            columnas.ConstantColumn(62);    // Fecha
                            columnas.RelativeColumn(2);     // Nombre
                            columnas.ConstantColumn(70);    // Tipo
                            columnas.RelativeColumn(2);     // Grupos
                            columnas.ConstantColumn(75);    // Participantes
                        });

                        tabla.Header(filaEncabezado =>
                        {
                            filaEncabezado.Cell().Element(CeldaEncabezado).Text("Fecha");
                            filaEncabezado.Cell().Element(CeldaEncabezado).Text("Actividad");
                            filaEncabezado.Cell().Element(CeldaEncabezado).Text("Tipo");
                            filaEncabezado.Cell().Element(CeldaEncabezado).Text("Grupos");
                            filaEncabezado.Cell().Element(CeldaEncabezado)
                                .AlignCenter().Text("Participantes");
                        });

                        for (int i = 0; i < actividades.Count; i++)
                        {
                            var actividad = actividades[i];
                            var estilo = CeldaDeFila(i);

                            tabla.Cell().Element(estilo).Text(actividad.Fecha.ToString("dd/MM/yyyy"));
                            tabla.Cell().Element(estilo).Text(actividad.Nombre);
                            tabla.Cell().Element(estilo).Text(actividad.Tipo);
                            tabla.Cell().Element(estilo).Text(string.Join(", ", actividad.Grupos));
                            tabla.Cell().Element(estilo).AlignCenter().Text(actividad.Participantes.ToString());
                        }
                    });
                });

                pagina.Footer().Element(Pie);
            });
        });

        return documento.GeneratePdf();
    }
}
