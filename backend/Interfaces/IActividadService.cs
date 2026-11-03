using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IActividadService
{
    public void Guardar(CreateActividadDTO actividad, int creadoPorId);

    // maestroId no nulo limita la accion a las actividades que esa persona creo.
    public void Modificar(UpdateActividadDTO actividad, int? maestroId);
    public void Eliminar(int id, int? maestroId);
    public void AsignarNinos(AsignarNinosDTO asignacion, int? maestroId);

    public ActividadDTO ObtenerPorId(int id, int? maestroId);
    public List<ActividadDTO> ObtenerTodos(int? maestroId);

    // Regla compartida con asistencia y reportes: lanza 404 si la actividad no
    // existe y 403 si un maestro intenta usar una actividad que no le corresponde.
    public void ValidarAcceso(int actividadId, int? maestroId);
}
