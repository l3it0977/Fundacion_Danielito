using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

public interface IDashboardService
{
    /// <summary>Panel de Directora y Administrador: cifras de todo el sistema.</summary>
    public Task<DashboardGeneralDTO> ObtenerGeneral();

    /// <summary>Panel de un Maestro: solo los niños de los grupos que tiene a cargo.</summary>
    public Task<DashboardMaestroDTO> ObtenerDelMaestro(int maestroId);
}
