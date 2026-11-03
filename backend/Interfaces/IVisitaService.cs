using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IVisitaService
{
    public Task Guardar(CreateVisitaDTO visita, int realizadoPorId);
    public Task Modificar(UpdateVisitaDTO visita);
    public Task Eliminar(int id);
    public Task<VisitaDTO> ObtenerPorId(int id);
    public Task<List<VisitaDTO>> ObtenerTodos(int? maestroId);
}
