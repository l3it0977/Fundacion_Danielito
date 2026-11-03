using System.Globalization;
using Microsoft.EntityFrameworkCore;
using QuestPDF.Fluent;
using QuestPDF.Helpers;
using QuestPDF.Infrastructure;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.Excepciones;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class ReporteService : IReporteService
{
    private AppProyectoContext _context;

    /// <summary>Para que los nombres de los meses salgan en español, como en los reportes originales.</summary>
    private static readonly CultureInfo CulturaEspanol = new("es-ES");

    // Decide si un maestro puede descargar el reporte de una actividad.
    private readonly IActividadService _actividadService;

    public ReporteService(AppProyectoContext context, IActividadService actividadService)
    {
        _context = context;
        _actividadService = actividadService;
    }

    //REPORTE 1: ASISTENCIA TOTAL Y POR GRUPO
    public byte[] GenerarAsistencia(int? grupoId, int? mes, int anio)
    {
        ValidarPeriodo(mes, anio);

        List<Grupo> todosLosGrupos = _context.Grupos
            .Include(x => x.Grupo_Maestros)
                .ThenInclude(x => x.Perfil)
            .OrderBy(x => x.Id)
            .ToList();

        Grupo? bdGrupo = null;
        if (grupoId.HasValue)
        {
            bdGrupo = todosLosGrupos.FirstOrDefault(x => x.Id.Equals(grupoId.Value));

            if (bdGrupo == null)
            {
                throw (new RecursoNoEncontradoException("Grupo no encontrado"));
            }
        }

        // Asistencias del periodo: alimentan los totales y las celdas de la matriz.
        // Rango de fechas y no Year/Month: así PostgreSQL puede usar el índice de Fecha.
        var (inicio, fin) = RangoDelPeriodo(mes, anio);
        var consulta = _context.Asistencias.Where(x => x.Fecha >= inicio && x.Fecha < fin);

        if (grupoId.HasValue)
        {
            consulta = consulta.Where(x => x.Nino.GrupoId == grupoId.Value);
        }

        List<RegistroAsistencia> registros = consulta
            .Select(x => new { x.NinoId, x.Nino.GrupoId, x.Fecha, x.Estado })
            .ToList()
            .Select(x => new RegistroAsistencia(x.NinoId, x.GrupoId, x.Fecha, x.Estado))
            .ToList();

        // Con todos los grupos se omiten los desactivados que no tuvieron movimiento.
        HashSet<int> gruposConRegistros = registros.Select(x => x.GrupoId).ToHashSet();
        List<Grupo> grupos = todosLosGrupos
            .Where(x => grupoId.HasValue
                ? x.Id == grupoId.Value
                : x.Activo || gruposConRegistros.Contains(x.Id))
            .ToList();

        HashSet<int> ninosConRegistros = registros.Select(x => x.NinoId).ToHashSet();
        List<int> idsDeGrupos = grupos.Select(x => x.Id).ToList();

        List<NinoDelReporte> ninos = _context.Ninos
            .Where(x => idsDeGrupos.Contains(x.GrupoId))
            .OrderBy(x => x.Apellidos)
            .ThenBy(x => x.Nombres)
            .Select(x => new NinoDelReporte(x.Id, x.GrupoId, x.Apellidos + ", " + x.Nombres, x.Activo))
            .ToList()
            .Where(x => x.Activo || ninosConRegistros.Contains(x.Id))
            .ToList();

        ILookup<int, string> estadosPorGrupo = registros.ToLookup(x => x.GrupoId, x => x.Estado);
        ILookup<int, string> estadosPorNino = registros.ToLookup(x => x.NinoId, x => x.Estado);

        Totales totalGeneral = Totales.De(registros.Select(x => x.Estado));

        List<FilaResumen> filasPorGrupo = grupos
            .Select(x => new FilaResumen(
                x.Nombre,
                ninos.Count(n => n.GrupoId == x.Id),
                Totales.De(estadosPorGrupo[x.Id])))
            .ToList();

        // Un solo grupo y un solo mes: se mantiene la matriz día por día.
        bool conMatriz = bdGrupo != null && mes.HasValue;

        string textoGrupo = bdGrupo != null
            ? $"Grupo: {bdGrupo.Nombre}"
            : "Grupo: todos los grupos de edad";

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                // La matriz puede tener hasta 31 columnas de días, así que va en horizontal.
                pagina.Size(conMatriz ? PageSizes.Letter.Landscape() : PageSizes.Letter);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Marca.Texto));

                pagina.Header().Element(encabezado =>
                    ComponerEncabezado(encabezado, "Reporte de asistencia",
                        textoGrupo, TextoDelPeriodo(mes, anio)));

                pagina.Content().PaddingVertical(10).Column(cuerpo =>
                {
                    cuerpo.Spacing(12);

                    cuerpo.Item().Element(c => ComponerResumenGeneral(c, totalGeneral));

                    if (totalGeneral.Registradas == 0)
                    {
                        cuerpo.Item().Text(
                            "No hay asistencias registradas en el periodo seleccionado.")
                            .Italic();
                    }

                    // ASISTENCIA POR GRUPO
                    cuerpo.Item().EnsureSpace(90).Element(c => ComponerTituloSeccion(c, "Asistencia por grupo"));
                    cuerpo.Item().Element(c => TablaResumen(c, "Grupo", filasPorGrupo,
                        filasPorGrupo.Count > 1 ? totalGeneral : null,
                        ninos.Count, conNinos: true));

                    // ASISTENCIA POR MES (solo cuando se piden todos los meses)
                    if (!mes.HasValue)
                    {
                        List<FilaResumen> filasPorMes = registros
                            .GroupBy(x => x.Fecha.Month)
                            .OrderBy(x => x.Key)
                            .Select(x => new FilaResumen(
                                ObtenerNombreDelMes(x.Key), null, Totales.De(x.Select(r => r.Estado))))
                            .ToList();

                        cuerpo.Item().EnsureSpace(90).Element(c => ComponerTituloSeccion(c, "Asistencia por mes"));
                        cuerpo.Item().Element(c => TablaResumen(c, "Mes", filasPorMes,
                            filasPorMes.Count > 1 ? totalGeneral : null, 0, conNinos: false));
                    }

                    // DETALLE POR GRUPO
                    foreach (Grupo grupo in grupos)
                    {
                        List<NinoDelReporte> ninosDelGrupo = ninos.Where(x => x.GrupoId == grupo.Id).ToList();

                        string maestros = string.Join(", ", grupo.Grupo_Maestros
                            .Select(gm => gm.Perfil.Nombre + " " + gm.Perfil.Apellido));

                        cuerpo.Item().EnsureSpace(110).Element(c => ComponerTituloSeccion(c,
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
                            cuerpo.Item().Element(c => TablaMatriz(c, ninosDelGrupo, registros));
                        }
                        else
                        {
                            List<FilaResumen> filasPorNino = ninosDelGrupo
                                .Select(x => new FilaResumen(x.Nombre_Completo, null, Totales.De(estadosPorNino[x.Id])))
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

                pagina.Footer().Element(ComponerPie);
            });
        });

        return documento.GeneratePdf();
    }

    //REPORTE 2: ACTIVIDADES
    public byte[] GenerarActividades(int? grupoId, int? mes, int anio)
    {
        ValidarPeriodo(mes, anio);

        Grupo? bdGrupo = null;
        if (grupoId.HasValue)
        {
            bdGrupo = _context.Grupos.FirstOrDefault(x => x.Id.Equals(grupoId.Value));

            if (bdGrupo == null)
            {
                throw (new RecursoNoEncontradoException("Grupo no encontrado"));
            }
        }

        var (inicio, fin) = RangoDelPeriodo(mes, anio);
        var consulta = _context.Actividades
            .Where(x => x.Fecha_Actividad >= inicio && x.Fecha_Actividad < fin);

        bool filtrarGrupo = grupoId.HasValue;
        int idGrupo = grupoId ?? 0;

        if (filtrarGrupo)
        {
            consulta = consulta.Where(x => x.Actividad_Ninos.Any(an => an.Nino.GrupoId == idGrupo));
        }

        // Con un grupo elegido, solo cuentan los niños de ese grupo.
        var actividades = consulta
            .OrderBy(x => x.Fecha_Actividad)
            .Select(x => new
            {
                x.Nombre,
                x.Tipo,
                Fecha = x.Fecha_Actividad,
                Grupos = x.Actividad_Ninos
                    .Where(an => !filtrarGrupo || an.Nino.GrupoId == idGrupo)
                    .Select(an => an.Nino.Grupo.Nombre)
                    .Distinct()
                    .ToList(),
                Participantes = x.Actividad_Ninos
                    .Count(an => !filtrarGrupo || an.Nino.GrupoId == idGrupo)
            })
            .ToList();

        int totalParticipaciones = actividades.Sum(x => x.Participantes);

        string textoGrupo = bdGrupo != null
            ? $"Grupo: {bdGrupo.Nombre}"
            : "Grupo: todos los grupos de edad";

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Marca.Texto));

                pagina.Header().Element(encabezado =>
                    ComponerEncabezado(encabezado, "Reporte de actividades",
                        textoGrupo, TextoDelPeriodo(mes, anio)));

                pagina.Content().PaddingVertical(10).Column(cuerpo =>
                {
                    cuerpo.Spacing(12);

                    cuerpo.Item().Row(fila =>
                    {
                        fila.Spacing(8);
                        ComponerCajaDato(fila, actividades.Count.ToString(), "Actividades", Marca.TealMedio);
                        ComponerCajaDato(fila, totalParticipaciones.ToString(), "Participaciones", Marca.Naranja);
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
                            filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Fecha");
                            filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Actividad");
                            filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Tipo");
                            filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Grupos");
                            filaEncabezado.Cell().Element(EstiloCeldaEncabezado)
                                .AlignCenter().Text("Participantes");
                        });

                        for (int i = 0; i < actividades.Count; i++)
                        {
                            var actividad = actividades[i];
                            var estilo = EstiloFila(i);

                            tabla.Cell().Element(estilo).Text(actividad.Fecha.ToString("dd/MM/yyyy"));
                            tabla.Cell().Element(estilo).Text(actividad.Nombre);
                            tabla.Cell().Element(estilo).Text(actividad.Tipo);
                            tabla.Cell().Element(estilo).Text(string.Join(", ", actividad.Grupos));
                            tabla.Cell().Element(estilo).AlignCenter().Text(actividad.Participantes.ToString());
                        }
                    });
                });

                pagina.Footer().Element(ComponerPie);
            });
        });

        return documento.GeneratePdf();
    }

    //REPORTE 3: ASISTENCIA POR ACTIVIDAD
    public byte[] GenerarAsistenciaPorActividad(int actividadId, int? maestroId)
    {
        _actividadService.ValidarAcceso(actividadId, maestroId);

        Actividad? bdActividad = _context.Actividades
            .FirstOrDefault(x => x.Id.Equals(actividadId));

        if (bdActividad == null)
        {
            throw (new RecursoNoEncontradoException("Actividad no encontrada"));
        }

        var registros = _context.Asistencias
            .Where(x => x.ActividadId == actividadId)
            .OrderBy(x => x.Fecha)
            .ThenBy(x => x.Nino.Apellidos)
            .Select(x => new
            {
                x.Fecha,
                Nombre_Completo = x.Nino.Apellidos + ", " + x.Nino.Nombres,
                x.Estado,
                x.Motivo_Justificacion
            })
            .ToList();

        // Cada fecha distinta es una sesión de la actividad.
        var sesiones = registros
            .GroupBy(x => x.Fecha.Date)
            .OrderBy(sesion => sesion.Key)
            .ToList();

        var documento = Document.Create(contenedor =>
        {
            contenedor.Page(pagina =>
            {
                pagina.Size(PageSizes.Letter);
                pagina.Margin(1.5f, Unit.Centimetre);
                pagina.DefaultTextStyle(estilo => estilo.FontSize(9).FontColor(Marca.Texto));

                pagina.Header().Element(encabezado =>
                    ComponerEncabezado(encabezado, "Asistencia por actividad",
                        bdActividad.Nombre, $"Tipo: {bdActividad.Tipo}  ·  Sesiones registradas: {sesiones.Count}"));

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
                        Totales totalesSesion = Totales.De(sesion.Select(x => x.Estado));

                        cuerpo.Item().EnsureSpace(90).Element(c => ComponerTituloSeccion(c,
                            $"Sesión del {sesion.Key:dd/MM/yyyy}  ·  " +
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
                                filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Niño");
                                filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Estado");
                                filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Justificación");
                            });

                            int indice = 0;
                            foreach (var registro in sesion)
                            {
                                var estilo = EstiloFila(indice++);

                                tabla.Cell().Element(estilo).Text(registro.Nombre_Completo);
                                tabla.Cell().Element(estilo).Text(registro.Estado);
                                tabla.Cell().Element(estilo).Text(registro.Motivo_Justificacion ?? "");
                            }
                        });
                    }
                });

                pagina.Footer().Element(ComponerPie);
            });
        });

        return documento.GeneratePdf();
    }

    //PLANTILLA DE LA FUNDACIÓN

    /// <summary>Colores institucionales, los mismos del sistema web (tokens.css).</summary>
    private static class Marca
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

    /// <summary>Logo y nombre de la fundación, y una franja con el título y los filtros aplicados.</summary>
    private static void ComponerEncabezado(IContainer contenedor, string titulo, params string[] detalles)
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

    private static void ComponerPie(IContainer contenedor)
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

    private static void ComponerTituloSeccion(IContainer contenedor, string titulo)
    {
        contenedor.BorderBottom(1.5f).BorderColor(Marca.Naranja).PaddingBottom(2)
            .Text(titulo).FontSize(11).Bold().FontColor(Marca.TealOscuro);
    }

    /// <summary>Recuadros con el porcentaje total y el desglose por estado.</summary>
    private static void ComponerResumenGeneral(IContainer contenedor, Totales total)
    {
        contenedor.Column(columna =>
        {
            columna.Spacing(4);

            columna.Item().Row(fila =>
            {
                fila.Spacing(8);
                ComponerCajaDato(fila, total.Porcentaje, "Asistencia total", Marca.TealOscuro);
                ComponerCajaDato(fila, total.Presentes.ToString(), "Presentes", Marca.TealMedio);
                ComponerCajaDato(fila, total.Ausentes.ToString(), "Ausentes", Marca.Naranja);
                ComponerCajaDato(fila, total.Justificados.ToString(), "Justificados", Marca.TextoMedio);
            });

            columna.Item().Text(
                $"Registros con lista pasada: {total.Registradas}   ·   Sin pasar lista: {total.Pendientes}")
                .FontSize(8).FontColor(Marca.TextoMedio);
        });
    }

    private static void ComponerCajaDato(RowDescriptor fila, string valor, string etiqueta, string color)
    {
        fila.RelativeItem().Background(Marca.Fondo).BorderLeft(3).BorderColor(color).Padding(8)
            .Column(caja =>
            {
                caja.Item().Text(valor).FontSize(18).Bold().FontColor(color);
                caja.Item().Text(etiqueta).FontSize(8).FontColor(Marca.TextoMedio);
            });
    }

    //TABLAS

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
                encabezado.Cell().Element(EstiloCeldaEncabezado).Text(tituloPrimeraColumna);
                if (conNinos)
                {
                    encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("Niños");
                }
                encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("Presentes");
                encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("Ausentes");
                encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("Justificados");
                encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("Registros");
                encabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("%");
            });

            for (int i = 0; i < filas.Count; i++)
            {
                EscribirFila(tabla, EstiloFila(i), filas[i].Etiqueta, filas[i].Ninos, filas[i].Datos, conNinos);
            }

            if (total != null)
            {
                EscribirFila(tabla, EstiloCeldaTotal, "Total", totalNinos, total, conNinos);
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
                filaEncabezado.Cell().Element(EstiloCeldaEncabezado).Text("Niño");
                foreach (int dia in diasConRegistro)
                {
                    filaEncabezado.Cell().Element(EstiloCeldaEncabezadoDia)
                        .AlignCenter().Text(dia.ToString()).FontSize(8);
                }
                filaEncabezado.Cell().Element(EstiloCeldaEncabezado).AlignCenter().Text("%");
            });

            for (int i = 0; i < ninos.Count; i++)
            {
                var estilo = EstiloFila(i);
                var estiloDia = EstiloFilaDia(i);
                List<RegistroAsistencia> delNino = registrosPorNino[ninos[i].Id].ToList();

                tabla.Cell().Element(estilo).Text(ninos[i].Nombre_Completo);

                foreach (int dia in diasConRegistro)
                {
                    RegistroAsistencia? registro = delNino.FirstOrDefault(x => x.Fecha.Day == dia);

                    tabla.Cell().Element(estiloDia).AlignCenter()
                        .Text(ObtenerAbreviaturaEstado(registro?.Estado));
                }

                tabla.Cell().Element(estilo).AlignCenter()
                    .Text(Totales.De(delNino.Select(x => x.Estado)).Porcentaje).Bold();
            }
        });
    }

    //ESTILOS Y AYUDANTES

    /// <summary>Celda de encabezado: fondo verde institucional y texto blanco en negrita.</summary>
    private static IContainer EstiloCeldaEncabezado(IContainer celda)
    {
        return celda
            .Background(Marca.TealMedio)
            .Padding(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Colors.White));
    }

    /// <summary>Celda de datos; las filas pares llevan fondo suave para seguir la línea con la vista.</summary>
    private static Func<IContainer, IContainer> EstiloFila(int indice)
    {
        return celda => celda
            .Background(indice % 2 == 0 ? Colors.White : Marca.Fondo)
            .BorderBottom(1)
            .BorderColor(Marca.Borde)
            .Padding(4);
    }

    // Las columnas de días son angostas: sin relleno lateral caben los números de dos cifras.
    private static IContainer EstiloCeldaEncabezadoDia(IContainer celda)
    {
        return celda
            .Background(Marca.TealMedio)
            .PaddingVertical(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Colors.White));
    }

    private static Func<IContainer, IContainer> EstiloFilaDia(int indice)
    {
        return celda => celda
            .Background(indice % 2 == 0 ? Colors.White : Marca.Fondo)
            .BorderBottom(1)
            .BorderColor(Marca.Borde)
            .PaddingVertical(4);
    }

    private static IContainer EstiloCeldaTotal(IContainer celda)
    {
        return celda
            .Background(Marca.TealPalido)
            .Padding(4)
            .DefaultTextStyle(estilo => estilo.Bold().FontColor(Marca.TealOscuro));
    }

    /// <summary>Una sola letra por celda, para que la matriz mensual quepa a lo ancho.</summary>
    private static string ObtenerAbreviaturaEstado(string? estado)
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

    private static string TextoDelPeriodo(int? mes, int anio)
    {
        return mes.HasValue
            ? $"Periodo: {ObtenerNombreDelMes(mes.Value)} de {anio}"
            : $"Periodo: todos los meses de {anio}";
    }

    private static string ObtenerNombreDelMes(int mes)
    {
        string nombre = CulturaEspanol.DateTimeFormat.GetMonthName(mes);

        // GetMonthName devuelve el nombre en minúscula.
        return char.ToUpper(nombre[0]) + nombre[1..];
    }

    /// <summary>Desde el primer día del periodo (incluido) hasta el siguiente al último (excluido).</summary>
    private static (DateTime Inicio, DateTime Fin) RangoDelPeriodo(int? mes, int anio)
    {
        DateTime inicio = new(anio, mes ?? 1, 1);
        DateTime fin = mes.HasValue ? inicio.AddMonths(1) : inicio.AddYears(1);

        return (inicio, fin);
    }

    private static void ValidarPeriodo(int? mes, int anio)
    {
        if (mes.HasValue && (mes < 1 || mes > 12))
        {
            throw (new ReglaNegocioException("El mes debe estar entre 1 y 12"));
        }

        if (anio < 2000 || anio > 2100)
        {
            throw (new ReglaNegocioException("El año indicado no es válido"));
        }
    }

    //TIPOS INTERNOS

    private sealed record RegistroAsistencia(int NinoId, int GrupoId, DateTime Fecha, string Estado);

    private sealed record NinoDelReporte(int Id, int GrupoId, string Nombre_Completo, bool Activo);

    private sealed record FilaResumen(string Etiqueta, int? Ninos, Totales Datos);

    /// <summary>Conteo por estado. Solo las sesiones con lista pasada entran en el porcentaje.</summary>
    private sealed record Totales(int Presentes, int Ausentes, int Justificados, int Pendientes)
    {
        public int Registradas => Presentes + Ausentes + Justificados;

        // Sin registros se muestra un guion en lugar de 100%, que daría a entender asistencia perfecta.
        public string Porcentaje => Registradas == 0
            ? "-"
            : Math.Round((double)Presentes / Registradas * 100) + "%";

        public static Totales De(IEnumerable<string> estados)
        {
            int presentes = 0, ausentes = 0, justificados = 0, pendientes = 0;

            foreach (string estado in estados)
            {
                switch (estado)
                {
                    case Asistencia.EstadoPresente: presentes++; break;
                    case Asistencia.EstadoAusente: ausentes++; break;
                    case Asistencia.EstadoJustificado: justificados++; break;
                    default: pendientes++; break;
                }
            }

            return new Totales(presentes, ausentes, justificados, pendientes);
        }
    }
}
