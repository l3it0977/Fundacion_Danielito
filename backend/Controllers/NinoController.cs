using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class NinoController : ControllerBase
{
    private readonly INinoService _ninoService;

    public NinoController(INinoService ninoService)
    {
        _ninoService = ninoService;
    }

    //CREAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPost]
    public async Task<string> Post(CreateNinoDTO nino)
    {
        await _ninoService.Guardar(nino);
        return "Niño creado correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public async Task<List<NinoDTO>> Get()
    {
        return await _ninoService.ObtenerTodos(User.FiltroMaestro());
    }

    //BUSCAR (CON PAGINACIÓN)
    // Para la pantalla de niños: el filtrado y la paginación se hacen en el servidor,
    // así la lista sigue siendo ágil aunque haya miles de fichas.
    [Authorize]
    [HttpGet("Buscar")]
    public async Task<ResultadoPaginadoDTO<NinoDTO>> GetBuscar([FromQuery] BuscarNinosDTO filtro)
    {
        return await _ninoService.Buscar(filtro, User.FiltroMaestro());
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public async Task<NinoDTO> GetById(int id)
    {
        return await _ninoService.ObtenerPorId(id, User.FiltroMaestro());
    }

    //OBTENER PARA SELECCION
    // Lista completa y reducida, para elegir participantes al armar una actividad.
    // Un maestro puede ver aquí niños de otros grupos, pero solo su nombre y grupo:
    // la ficha con contacto, observaciones y domicilio sigue restringida.
    [Authorize]
    [HttpGet("ParaSeleccion")]
    public async Task<List<NinoSeleccionDTO>> GetParaSeleccion()
    {
        return await _ninoService.ObtenerParaSeleccion();
    }

    //ACTUALIZAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut]
    public async Task<string> Put(UpdateNinoDTO nino)
    {
        await _ninoService.Modificar(nino);
        return "Niño modificado correctamente";
    }

    //ACTIVAR O DESACTIVAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut("Estado")]
    public async Task<string> PutEstado(CambiarEstadoNinoDTO cambio)
    {
        await _ninoService.CambiarEstado(cambio);

        return cambio.Activo
            ? "Niño activado correctamente"
            : "Niño desactivado correctamente";
    }

    //ELIMINAR
    // Solo prospera con fichas sin historial; si ya tiene registros, se desactiva.
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        await _ninoService.Eliminar(id);
        return "Niño eliminado correctamente";
    }
}
