using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

/// <summary>Registro de auditoría. Solo lectura, y solo para la dirección.</summary>
[ApiController]
[Route("[controller]")]
public class BitacoraController : ControllerBase
{
    private readonly IBitacoraService _bitacoraService;

    public BitacoraController(IBitacoraService bitacoraService)
    {
        _bitacoraService = bitacoraService;
    }

    //BUSCAR (CON PAGINACIÓN)
    [Authorize(Roles = "Directora,Administrador")]
    [HttpGet("Buscar")]
    public async Task<ResultadoPaginadoDTO<BitacoraDTO>> GetBuscar([FromQuery] BuscarBitacoraDTO filtro)
    {
        return await _bitacoraService.Buscar(filtro);
    }
}
