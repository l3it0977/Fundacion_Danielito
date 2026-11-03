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
    public string Post(CreateNinoDTO nino)
    {
        _ninoService.Guardar(nino);
        return "Niño creado correctamente";
    }

    //OBTENER TODOS
    [Authorize]
    [HttpGet]
    public List<NinoDTO> Get()
    {
        return _ninoService.ObtenerTodos(User.FiltroMaestro());
    }

    //OBTENER POR ID
    [Authorize]
    [HttpGet("{Id}")]
    public NinoDTO GetById(int id)
    {
        return _ninoService.ObtenerPorId(id, User.FiltroMaestro());
    }

    //OBTENER PARA SELECCION
    // Lista completa y reducida, para elegir participantes al armar una actividad.
    // Un maestro puede ver aquí niños de otros grupos, pero solo su nombre y grupo:
    // la ficha con contacto, observaciones y domicilio sigue restringida.
    [Authorize]
    [HttpGet("ParaSeleccion")]
    public List<NinoSeleccionDTO> GetParaSeleccion()
    {
        return _ninoService.ObtenerParaSeleccion();
    }

    //ACTUALIZAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpPut]
    public string Put(UpdateNinoDTO nino)
    {
        _ninoService.Modificar(nino);
        return "Niño modificado correctamente";
    }

    //ELIMINAR
    [Authorize(Roles = "Directora,Administrador")]
    [HttpDelete("{id}")]
    public string Delete(int id)
    {
        _ninoService.Eliminar(id);
        return "Niño eliminado correctamente";
    }
}
