using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IAsistenciaService
{
    // maestroId no nulo limita la acción a las actividades a las que ese maestro
    // tiene acceso: las que creó y las que incluyen a niños de sus grupos.
    public Task Guardar(CreateAsistenciaDTO asistencia, int registradoPorId, int? maestroId);
    public Task Modificar(UpdateAsistenciaDTO asistencia, int? maestroId);
    public Task Eliminar(int id);
    public Task<AsistenciaDTO> ObtenerPorId(int id, int? maestroId);
    public Task<List<AsistenciaDTO>> ObtenerTodos();
    public Task<List<AsistenciaDTO>> ObtenerPorActividad(int actividadId, int? maestroId);
    public Task RegistrarPorActividad(RegistrarAsistenciasDTO registro, int registradoPorId, int? maestroId);
}
