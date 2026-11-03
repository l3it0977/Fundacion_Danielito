using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Interfaces;

namespace SISTEMADANIELITO.Controllers;

[ApiController]
[Route("[controller]")]
public class GrupoController : ControllerBase
{
    private readonly IGrupoService _grupoService;

    public GrupoController(IGrupoService grupoService)
    {
        _grupoService = grupoService;
    }

    //CREAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPost]
    public async Task<string> Post(CreateGrupoDTO grupo)
    {
        await _grupoService.Guardar(grupo);
        return "Grupo creado correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public async Task<List<GrupoDTO>> Get()
    {
        return await _grupoService.ObtenerTodos();
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public async Task<GrupoDTO> GetById(int id)
    {
        return await _grupoService.ObtenerPorId(id);
    }

    //ACTUALIZAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut]
    public async Task<string> Put(UpdateGrupoDTO grupo)
    {
        await _grupoService.Modificar(grupo);
        return "Grupo modificado correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public async Task<string> Delete(int id)
    {
        await _grupoService.Eliminar(id);
        return "Grupo eliminado correctamente";
    }
}
