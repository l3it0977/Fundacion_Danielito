using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface INinoService
{
    public void Guardar(CreateNinoDTO nino);
    public void Modificar(UpdateNinoDTO nino);
    public void Eliminar(int id);

    // maestroId no nulo restringe el resultado a los niños de sus grupos.
    public NinoDTO ObtenerPorId(int id, int? maestroId);
    public List<NinoDTO> ObtenerTodos(int? maestroId);

    /// <summary>
    /// Lista reducida de todos los niños, para elegir participantes al armar una
    /// actividad. No filtra por maestro y solo expone nombre, código y grupo.
    /// </summary>
    public List<NinoSeleccionDTO> ObtenerParaSeleccion();
}
