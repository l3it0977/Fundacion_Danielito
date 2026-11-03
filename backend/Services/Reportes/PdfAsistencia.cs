using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SISTEMADANIELITO.Models;
using static SISTEMADANIELITO.Services.Reportes.PlantillaFundacion;

namespace SISTEMADANIELITO.Services.Reportes;

/// <summary>Reporte de asistencia total y por grupo, con la plantilla de la fundación.</summary>
internal static class PdfAsistencia
{
    public static byte[] Generar(DatosDeAsistencia datos)
    {
        List<FilaResumen> filasPorGrupo = datos.FilasPorGrupo();
        bool conMatriz = datos.ConMatriz;

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                // La matriz puede tener hasta 31 columnas de días, así que va en horizontal.
                ConfigurarPagina(pagina, conMatriz ? PageSizes.Letter.Landscape() : PageSizes.Letter);

                pagina.Header().Element(encabezado =>
                    Encabezado(encabezado, "Reporte de asistencia",
                        Periodo.TextoDelGrupo(datos.GrupoElegido?.Nombre), Periodo.Texto(datos.Mes, datos.Anio)));

                pagina.Content().PaddingVertical(10).Column(cuerpo =>
                {
                    cuerpo.Spacing(12);

                    cuerpo.Item().Element(c => ResumenGeneral(c, datos.TotalGeneral));

                    if (datos.TotalGeneral.Registradas == 0)
                    {
                        cuerpo.Item().Text(
                            "No hay asistencias registradas en el periodo seleccionado.")
                            .Italic();
                    }

                    // ASISTENCIA POR GRUPO
                    cuerpo.Item().EnsureSpace(90).Element(c => TituloSeccion(c, "Asistencia por grupo"));
                    cuerpo.Item().Element(c => TablaResumen(c, "Grupo", filasPorGrupo,
                        filasPorGrupo.Count > 1 ? datos.TotalGeneral : null,
                        datos.Ninos.Count, conNinos: true));

                    // ASISTENCIA POR MES (solo cuando se piden todos los meses)
                    if (!datos.Mes.HasValue)
                    {
                        List<FilaResumen> filasPorMes = datos.FilasPorMes();

                        cuerpo.Item().EnsureSpace(90).Element(c => TituloSeccion(c, "Asistencia por mes"));
                        cuerpo.Item().Element(c => TablaResumen(c, "Mes", filasPorMes,
                            filasPorMes.Count > 1 ? datos.TotalGeneral : null, 0, conNinos: false));
                    }

                    // DETALLE POR GRUPO
                    foreach (Grupo grupo in datos.Grupos)
                    {
                        List<NinoDelReporte> ninosDelGrupo = datos.NinosDelGrupo(grupo.Id);
                        string maestros = datos.MaestrosDelGrupo(grupo);

                        cuerpo.Item().EnsureSpace(110).Element(c => TituloSeccion(c,
                            conMatriz
                                ? $"Detalle diario - {grupo.Nombre}"
                                : $"Detalle por niño - {grupo.Nombre}"));

                        cuerpo.Item().Text($"Maestros: {(maestros == "" ? "sin asignar" : maestros)}")
                            .FontColor(Marca.TextoMedio);

                        if (ninosDelGrupo.Count == 0)
                        {
                            cuerpo.Item().Text("Este grupo no tiene niños registrados.").Italic();
                            continue;
                        }

                        if (conMatriz)
                        {
                            cuerpo.Item().Element(c => TablaMatriz(c, ninosDelGrupo, datos.Registros));
                        }
                        else
                        {
                            List<FilaResumen> filasPorNino = ninosDelGrupo
                                .Select(x => new FilaResumen(x.Nombre_Completo, null, datos.TotalesDelNino(x.Id)))
                                .ToList();

                            cuerpo.Item().Element(c => TablaResumen(c, "Niño", filasPorNino,
                                null, 0, conNinos: false));
                        }
                    }

                    cuerpo.Item().PaddingTop(4).Text(
                        "Porcentaje = presentes / registros con lista pasada (se excluyen los pendientes). " +
                        (conMatriz ? "P = presente, A = ausente, J = justificado, · = sin pasar lista." : ""))
                        .FontSize(8).FontColor(Marca.TextoMedio);
                });

                pagina.Footer().Element(Pie);
            });
        });

        return documento.GeneratePdf();
    }

    /// <summary>Recuadros con el porcentaje total y el desglose por estado.</summary>
    private static void ResumenGeneral(IContainer contenedor, Totales total)
    {
        contenedor.Column(columna =>
        {
            columna.Spacing(4);

            columna.Item().Row(fila =>
            {
                fila.Spacing(8);
                CajaDato(fila, total.Porcentaje, "Asistencia total", Marca.TealOscuro);
                CajaDato(fila, total.Presentes.ToString(), "Presentes", Marca.TealMedio);
                CajaDato(fila, total.Ausentes.ToString(), "Ausentes", Marca.Naranja);
                CajaDato(fila, total.Justificados.ToString(), "Justificados", Marca.TextoMedio);
            });

            columna.Item().Text(
                $"Registros con lista pasada: {total.Registradas}   ·   Sin pasar lista: {total.Pendientes}")
                .FontSize(8).FontColor(Marca.TextoMedio);
        });
    }

    /// <summary>
    /// Tabla de totales por estado. Sirve para grupos, meses y niños: solo cambia
    /// la primera columna. totalNinos solo se usa si conNinos es true y hay fila de total.
    /// </summary>
    private static void TablaResumen(IContainer contenedor, string tituloPrimeraColumna,
        List<FilaResumen> filas, Totales? total, int totalNinos, bool conNinos)
    {
        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn(3);
                if (conNinos)
                {
                    columnas.ConstantColumn(45);
                }
                columnas.ConstantColumn(58);
                columnas.ConstantColumn(58);
                columnas.ConstantColumn(72);
                columnas.ConstantColumn(60);
                columnas.ConstantColumn(45);
            });

            tabla.Header(encabezado =>
            {
                encabezado.Cell().Element(CeldaEncabezado).Text(tituloPrimeraColumna);
                if (conNinos)
                {
                    encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("Niños");
                }
                encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("Presentes");
                encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("Ausentes");
                encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("Justificados");
                encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("Registros");
                encabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("%");
            });

            for (int i = 0; i < filas.Count; i++)
            {
                EscribirFila(tabla, CeldaDeFila(i), filas[i].Etiqueta, filas[i].Ninos, filas[i].Datos, conNinos);
            }

            if (total != null)
            {
                EscribirFila(tabla, CeldaDeTotal, "Total", totalNinos, total, conNinos);
            }
        });
    }

    private static void EscribirFila(TableDescriptor tabla, Func<IContainer, IContainer> estilo,
        string etiqueta, int? ninos, Totales datos, bool conNinos)
    {
        tabla.Cell().Element(estilo).Text(etiqueta);
        if (conNinos)
        {
            tabla.Cell().Element(estilo).AlignCenter().Text((ninos ?? 0).ToString());
        }
        tabla.Cell().Element(estilo).AlignCenter().Text(datos.Presentes.ToString());
        tabla.Cell().Element(estilo).AlignCenter().Text(datos.Ausentes.ToString());
        tabla.Cell().Element(estilo).AlignCenter().Text(datos.Justificados.ToString());
        tabla.Cell().Element(estilo).AlignCenter().Text(datos.Registradas.ToString());
        tabla.Cell().Element(estilo).AlignCenter().Text(datos.Porcentaje).Bold();
    }

    /// <summary>Una fila por niño y una columna por día del mes en que hubo registro.</summary>
    private static void TablaMatriz(IContainer contenedor, List<NinoDelReporte> ninos,
        List<RegistroAsistencia> registros)
    {
        // Solo se muestran los días con registro, para no ocupar 31 columnas vacías.
        List<int> diasConRegistro = registros
            .Select(x => x.Fecha.Day)
            .Distinct()
            .OrderBy(dia => dia)
            .ToList();

        ILookup<int, RegistroAsistencia> registrosPorNino = registros.ToLookup(x => x.NinoId);

        contenedor.Table(tabla =>
        {
            tabla.ColumnsDefinition(columnas =>
            {
                columnas.RelativeColumn();                     // Nombre del niño
                foreach (int _ in diasConRegistro)
                {
                    columnas.ConstantColumn(17);               // Una por día
                }
                columnas.ConstantColumn(42);                   // Porcentaje
            });

            tabla.Header(filaEncabezado =>
            {
                filaEncabezado.Cell().Element(CeldaEncabezado).Text("Niño");
                foreach (int dia in diasConRegistro)
                {
                    filaEncabezado.Cell().Element(CeldaEncabezadoDia)
                        .AlignCenter().Text(dia.ToString()).FontSize(8);
                }
                filaEncabezado.Cell().Element(CeldaEncabezado).AlignCenter().Text("%");
            });

            for (int i = 0; i < ninos.Count; i++)
            {
                var estilo = CeldaDeFila(i);
                var estiloDia = CeldaDeDia(i);
                List<RegistroAsistencia> delNino = registrosPorNino[ninos[i].Id].ToList();

                tabla.Cell().Element(estilo).Text(ninos[i].Nombre_Completo);

                foreach (int dia in diasConRegistro)
                {
                    RegistroAsistencia? registro = delNino.FirstOrDefault(x => x.Fecha.Day == dia);

                    tabla.Cell().Element(estiloDia).AlignCenter()
                        .Text(AbreviaturaDelEstado(registro?.Estado));
                }

                tabla.Cell().Element(estilo).AlignCenter()
                    .Text(Totales.De(delNino.Select(x => x.Estado)).Porcentaje).Bold();
            }
        });
    }
}
