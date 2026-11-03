using SISTEMADANIELITO.DTOS;

namespace SISTEMADANIELITO.Interfaces;

/// <summary>
/// Consulta de la bitácora de auditoría. Los renglones los escribe AuditoriaInterceptor al
/// guardar; Registrar existe solo para los hechos que no pasan por la base del dominio,
/// como el cambio de rol de un usuario (vive en Identity).
/// </summary>
public interface IBitacoraService
{
    public Task<ResultadoPaginadoDTO<BitacoraDTO>> Buscar(BuscarBitacoraDTO filtro);

    public Task Registrar(int? perfilId, string entidad, int? entidadId, string accion, string descripcion);
}
