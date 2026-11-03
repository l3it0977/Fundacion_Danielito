using AutoMapper;
using SISTEMADANIELITO.Excepciones;
using Microsoft.EntityFrameworkCore;
using SISTEMADANIELITO.Data;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Services;

public class VisitaService : IVisitaService
{
    private AppProyectoContext _context;
    private readonly IMapper _mapper;

    public VisitaService(AppProyectoContext context, IMapper mapper)
    {
        _context = context;
        _mapper = mapper;
    }

    //CREAR
    public void Guardar(CreateVisitaDTO visita, int realizadoPorId)
    {
        ValidarNino(visita.NinoId);

        Visita visitaModel = _mapper.Map<Visita>(visita);
        visitaModel.RealizadoPorId = realizadoPorId;

        _context.Visitas.Add(visitaModel);
        _context.SaveChanges();
    }

    //ACTUALIZAR
    public void Modificar(UpdateVisitaDTO visita)
    {
        Visita? bdVisita = _context.Visitas.FirstOrDefault(x => x.Id.Equals(visita.Id));
        if (bdVisita == null)
        {
            throw (new RecursoNoEncontradoException("Visita no encontrada"));
        }

        bdVisita.Fecha_Visita = visita.Fecha_Visita;
        bdVisita.Motivo = visita.Motivo;
        bdVisita.Observacion = visita.Observacion;
        bdVisita.Seguimiento = visita.Seguimiento;

        _context.Visitas.Update(bdVisita);
        _context.SaveChanges();
    }

    //ELIMINAR
    public void Eliminar(int id)
    {
        Visita? visita = _context.Visitas.FirstOrDefault(x => x.Id.Equals(id));
        if (visita == null)
        {
            throw (new RecursoNoEncontradoException("Visita no encontrada"));
        }

        _context.Visitas.Remove(visita);
        _context.SaveChanges();
    }

    //OBTENER POR ID
    public VisitaDTO ObtenerPorId(int id)
    {
        Visita? visita = _context.Visitas
            .Include(x => x.Nino)
            .Include(x => x.RealizadoPor)
            .FirstOrDefault(x => x.Id.Equals(id));

        if (visita == null)
        {
            throw (new RecursoNoEncontradoException("Visita no encontrada"));
        }

        var dto = _mapper.Map<VisitaDTO>(visita);
        return dto;
    }

    //OBTENER TODOS
    public List<VisitaDTO> ObtenerTodos(int? maestroId)
    {
        IQueryable<Visita> consulta = _context.Visitas
            .Include(x => x.Nino)
            .Include(x => x.RealizadoPor);

        if (maestroId != null)
        {
            consulta = consulta.Where(x =>
                x.Nino.Grupo.Grupo_Maestros.Any(gm => gm.PerfilId == maestroId));
        }

        List<Visita> lstVisitas = consulta
            .OrderByDescending(x => x.Fecha_Visita)
            .ToList();

        var dto = _mapper.Map<List<VisitaDTO>>(lstVisitas);
        return dto;
    }

    private void ValidarNino(int ninoId)
    {
        Nino? bdNino = _context.Ninos.FirstOrDefault(x => x.Id.Equals(ninoId));
        if (bdNino == null)
        {
            throw (new ReglaNegocioException("El niño indicado no existe"));
        }
    }
}
