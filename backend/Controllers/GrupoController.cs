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
    public string Post(CreateGrupoDTO grupo)
    {
        _grupoService.Guardar(grupo);
        return "Grupo creado correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public List<GrupoDTO> Get()
    {
        return _grupoService.ObtenerTodos();
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public GrupoDTO GetById(int id)
    {
        return _grupoService.ObtenerPorId(id);
    }

    //ACTUALIZAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut]
    public string Put(UpdateGrupoDTO grupo)
    {
        _grupoService.Modificar(grupo);
        return "Grupo modificado correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public string Delete(int id)
    {
        _grupoService.Eliminar(id);
        return "Grupo eliminado correctamente";
    }
}
