namespace SISTEMADANIELITO.DTOS;

/// <summary>
/// Versión mínima de un niño, para elegir participantes al armar una actividad.
/// Lleva solo lo necesario para identificarlo: así un maestro puede elegir niños
/// de otros grupos sin acceder a su contacto, observaciones ni domicilio.
/// </summary>
public class NinoSeleccionDTO
{
    public int Id { get; set; }
    public string Codigo { get; set; } = string.Empty;
    public string Nombre_Completo { get; set; } = string.Empty;
    public int GrupoId { get; set; }
    public string Nombre_Grupo { get; set; } = string.Empty;
}
