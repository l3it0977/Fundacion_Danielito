using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IVisitaService
{
    public void Guardar(CreateVisitaDTO visita, int realizadoPorId);
    public void Modificar(UpdateVisitaDTO visita);
    public void Eliminar(int id);
    public VisitaDTO ObtenerPorId(int id);
    public List<VisitaDTO> ObtenerTodos(int? maestroId);
}
