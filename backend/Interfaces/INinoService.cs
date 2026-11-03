using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface INinoService
{
    public Task Guardar(CreateNinoDTO nino);
    public Task Modificar(UpdateNinoDTO nino);

    /// <summary>
    /// Borrado real, solo para fichas creadas por error: si el niño ya tiene asistencias,
    /// visitas o actividades se rechaza y hay que desactivarlo.
    /// </summary>
    public Task Eliminar(int id);

    /// <summary>Activa o desactiva al niño sin tocar su historial.</summary>
    public Task CambiarEstado(CambiarEstadoNinoDTO cambio);

    // maestroId no nulo restringe el resultado a los niños de sus grupos.
    public Task<NinoDTO> ObtenerPorId(int id, int? maestroId);
    public Task<List<NinoDTO>> ObtenerTodos(int? maestroId);

    /// <summary>
    /// Lista reducida de todos los niños, para elegir participantes al armar una
    /// actividad. No filtra por maestro y solo expone nombre, código y grupo.
    /// </summary>
    public Task<List<NinoSeleccionDTO>> ObtenerParaSeleccion();
}
