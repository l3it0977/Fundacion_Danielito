using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IAsistenciaService
{
    // maestroId no nulo limita la acción a las actividades a las que ese maestro
    // tiene acceso: las que creó y las que incluyen a niños de sus grupos.
    public void Guardar(CreateAsistenciaDTO asistencia, int registradoPorId, int? maestroId);
    public void Modificar(UpdateAsistenciaDTO asistencia, int? maestroId);
    public void Eliminar(int id);
    public AsistenciaDTO ObtenerPorId(int id, int? maestroId);
    public List<AsistenciaDTO> ObtenerTodos();
    public List<AsistenciaDTO> ObtenerPorActividad(int actividadId, int? maestroId);
    public void RegistrarPorActividad(RegistrarAsistenciasDTO registro, int registradoPorId, int? maestroId);
}
