using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IDashboardService
{
    /// <summary>Panel de Directora y Administrador: cifras de todo el sistema.</summary>
    public DashboardGeneralDTO ObtenerGeneral();

    /// <summary>Panel de un Maestro: solo los niños de los grupos que tiene a cargo.</summary>
    public DashboardMaestroDTO ObtenerDelMaestro(int maestroId);
}
