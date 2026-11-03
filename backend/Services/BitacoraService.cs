using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class BitacoraService : IBitacoraService
{
    private AppProyectoContext _context;

    public BitacoraService(AppProyectoContext context)
    {
        _context = context;
    }

    //BUSCAR (CON PAGINACIÓN)
    public async Task<ResultadoPaginadoDTO<BitacoraDTO>> Buscar(BuscarBitacoraDTO filtro)
    {
        int tamano = Math.Clamp(filtro.Tamano, 1, 100);

        IQueryable<Bitacora> consulta = _context.Bitacoras;

        if (!string.IsNullOrWhiteSpace(filtro.Entidad))
        {
            consulta = consulta.Where(x => x.Entidad == filtro.Entidad);
        }

        if (!string.IsNullOrWhiteSpace(filtro.Accion))
        {
            consulta = consulta.Where(x => x.Accion == filtro.Accion);
        }

        if (filtro.PerfilId != null)
        {
            consulta = consulta.Where(x => x.PerfilId == filtro.PerfilId);
        }

        string? texto = filtro.Texto?.Trim().ToLower();
        if (!string.IsNullOrEmpty(texto))
        {
            consulta = consulta.Where(x => x.Descripcion.ToLower().Contains(texto));
        }

        // Según cómo llegue la fecha puede venir como hora local; ToUniversalTime la lleva a UTC.
        if (filtro.Desde != null)
        {
            DateTime desde = filtro.Desde.Value.ToUniversalTime();
            consulta = consulta.Where(x => x.Fecha >= desde);
        }

        if (filtro.Hasta != null)
        {
            DateTime hasta = filtro.Hasta.Value.ToUniversalTime();
            consulta = consulta.Where(x => x.Fecha <= hasta);
        }

        int total = await consulta.CountAsync();

        int totalPaginas = Math.Max(1, (int)Math.Ceiling(total / (double)tamano));
        int pagina = Math.Clamp(filtro.Pagina, 1, totalPaginas);

        List<Bitacora> renglones = await consulta
            .OrderByDescending(x => x.Fecha)
            .ThenByDescending(x => x.Id)
            .Skip((pagina - 1) * tamano)
            .Take(tamano)
            .ToListAsync();

        return new ResultadoPaginadoDTO<BitacoraDTO>
        {
            Items = renglones.Select(ConvertirADto).ToList(),
            Total = total,
            Pagina = pagina,
            Tamano = tamano
        };
    }

    //REGISTRAR UN HECHO A MANO
    public async Task Registrar(int? perfilId, string entidad, int? entidadId, string accion, string descripcion)
    {
        string usuario = "Sistema";

        if (perfilId != null)
        {
            Perfil? bdPerfil = await _context.Perfiles.FirstOrDefaultAsync(x => x.Id == perfilId);

            if (bdPerfil != null)
            {
                usuario = $"{bdPerfil.Nombre} {bdPerfil.Apellido}";
            }
        }

        _context.Bitacoras.Add(new Bitacora
        {
            Fecha = DateTime.UtcNow,
            PerfilId = perfilId,
            Usuario = usuario,
            Entidad = entidad,
            EntidadId = entidadId,
            Accion = accion,
            Descripcion = descripcion
        });

        await _context.SaveChangesAsync();
    }

    /// <summary>Convierte el JSON guardado en una lista de campos con su antes y después.</summary>
    private static BitacoraDTO ConvertirADto(Bitacora renglon)
    {
        return new BitacoraDTO
        {
            Id = renglon.Id,
            Fecha = DateTime.SpecifyKind(renglon.Fecha, DateTimeKind.Utc),
            PerfilId = renglon.PerfilId,
            Usuario = renglon.Usuario,
            Entidad = renglon.Entidad,
            EntidadId = renglon.EntidadId,
            Accion = renglon.Accion,
            Descripcion = renglon.Descripcion,
            Cambios = LeerCambios(renglon.Cambios)
        };
    }

    private static List<CambioBitacoraDTO> LeerCambios(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return [];
        }

        try
        {
            using JsonDocument documento = JsonDocument.Parse(json);

            return documento.RootElement.EnumerateObject()
                .Select(campo => new CambioBitacoraDTO
                {
                    Campo = campo.Name,
                    Antes = Texto(campo.Value, "antes"),
                    Despues = Texto(campo.Value, "despues")
                })
                .ToList();
        }
        catch (JsonException)
        {
            // Un renglón con el JSON dañado no debe tumbar toda la pantalla.
            return [];
        }
    }

    private static string? Texto(JsonElement cambio, string nombre)
    {
        if (!cambio.TryGetProperty(nombre, out JsonElement valor) || valor.ValueKind == JsonValueKind.Null)
        {
            return null;
        }

        return valor.ValueKind == JsonValueKind.String ? valor.GetString() : valor.ToString();
    }
}
