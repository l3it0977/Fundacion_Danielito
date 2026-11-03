using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IActividadService
{
    public Task Guardar(CreateActividadDTO actividad, int creadoPorId);

    // maestroId no nulo limita la accion a las actividades que esa persona creo.
    public Task Modificar(UpdateActividadDTO actividad, int? maestroId);
    public Task Eliminar(int id, int? maestroId);
    public Task AsignarNinos(AsignarNinosDTO asignacion, int? maestroId);

    public Task<ActividadDTO> ObtenerPorId(int id, int? maestroId);
    public Task<List<ActividadDTO>> ObtenerTodos(int? maestroId);

    // Regla compartida con asistencia y reportes: lanza 404 si la actividad no
    // existe y 403 si un maestro intenta usar una actividad que no le corresponde.
    public Task ValidarAcceso(int actividadId, int? maestroId);
}
