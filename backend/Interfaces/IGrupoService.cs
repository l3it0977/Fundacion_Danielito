using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IGrupoService
{
    public void Guardar(CreateGrupoDTO grupo);
    public void Modificar(UpdateGrupoDTO grupo);
    public void Eliminar(int id);
    public GrupoDTO ObtenerPorId(int id);
    public List<GrupoDTO> ObtenerTodos();
}
