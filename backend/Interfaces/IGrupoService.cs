using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IGrupoService
{
    public Task Guardar(CreateGrupoDTO grupo);
    public Task Modificar(UpdateGrupoDTO grupo);
    public Task Eliminar(int id);
    public Task<GrupoDTO> ObtenerPorId(int id);
    public Task<List<GrupoDTO>> ObtenerTodos();
}
