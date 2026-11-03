using QuestPDF.Fluent;
using QuestPDF.Helpers;
using static SISTEMADANIELITO.Services.Reportes.PlantillaFundacion;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>Asistencia de una actividad, una tabla por cada sesión (fecha) registrada.</summary>
internal static class PdfAsistenciaPorActividad
{
    public static byte[] Generar(DatosPorActividad datos)
    {
        var sesiones = datos.Sesiones;

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                ConfigurarPagina(pagina, PageSizes.Letter);

                pagina.Header().Element(encabezado =>
                    Encabezado(encabezado, "Asistencia por actividad",
                        datos.Actividad.Nombre,
                        $"Tipo: {datos.Actividad.Tipo}  ·  Sesiones registradas: {sesiones.Count}"));

                pagina.Content().PaddingVertical(10).Column(cuerpo =>
                {
                    cuerpo.Spacing(10);

                    if (sesiones.Count == 0)
                    {
                        cuerpo.Item().Text("No hay asistencias registradas para esta actividad.").Italic();
                        return;
                    }

                    foreach (var sesion in sesiones)
                    {
                        Totales totalesSesion = Totales.De(sesion.Registros.Select(x => x.Estado));

                        cuerpo.Item().EnsureSpace(90).Element(c => TituloSeccion(c,
                            $"Sesión del {sesion.Fecha:dd/MM/yyyy}  ·  " +
                            $"{totalesSesion.Presentes} de {totalesSesion.Registradas} presentes " +
                            $"({totalesSesion.Porcentaje})"));

                        cuerpo.Item().Table(tabla =>
                        {
                            tabla.ColumnsDefinition(columnas =>
                            {
                                columnas.RelativeColumn(2);     // Niño
                                columnas.ConstantColumn(90);    // Estado
                                columnas.RelativeColumn(2);     // Justificación
                            });

                            tabla.Header(filaEncabezado =>
                            {
                                filaEncabezado.Cell().Element(CeldaEncabezado).Text("Niño");
                                filaEncabezado.Cell().Element(CeldaEncabezado).Text("Estado");
                                filaEncabezado.Cell().Element(CeldaEncabezado).Text("Justificación");
                            });

                            int indice = 0;
                            foreach (var registro in sesion.Registros)
                            {
                                var estilo = CeldaDeFila(indice++);

                                tabla.Cell().Element(estilo).Text(registro.Nombre_Completo);
                                tabla.Cell().Element(estilo).Text(registro.Estado);
                                tabla.Cell().Element(estilo).Text(registro.Motivo_Justificacion ?? "");
                            }
                        });
                    }
                });

                pagina.Footer().Element(Pie);
            });
        });

        return documento.GeneratePdf();
    }
}
