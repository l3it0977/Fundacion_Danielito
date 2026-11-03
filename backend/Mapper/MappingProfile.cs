using AutoMapper;
using SISTEMADANIELITO.DTOS;
using SISTEMADANIELITO.Models;

namespace SISTEMADANIELITO.Mapper;

public class MappingProfile : Profile
{
    public MappingProfile()
    {
        //GRUPO
        // La colección de maestros se arma a mano en el servicio, por eso se ignora aquí.
        CreateMap<CreateGrupoDTO, Grupo>()
            .ForMember(x => x.Ninos, x => x.Ignore())
            .ForMember(x => x.Grupo_Maestros, x => x.Ignore());

        CreateMap<UpdateGrupoDTO, Grupo>()
            .ForMember(x => x.Ninos, x => x.Ignore())
            .ForMember(x => x.Grupo_Maestros, x => x.Ignore());

        CreateMap<Grupo, GrupoDTO>()
            .ForMember(x => x.Cantidad_Ninos, x => x.MapFrom(src => src.Ninos.Count))
            .ForMember(x => x.MaestrosId,
                x => x.MapFrom(src => src.Grupo_Maestros.Select(gm => gm.PerfilId)))
            .ForMember(x => x.Nombres_Maestros,
                x => x.MapFrom(src => src.Grupo_Maestros
                    .Select(gm => gm.Perfil.Nombre + " " + gm.Perfil.Apellido)));

        //NINO
        CreateMap<CreateNinoDTO, Nino>()
            .ForMember(x => x.Asistencias, x => x.Ignore())
            .ForMember(x => x.Visitas, x => x.Ignore())
            .ForMember(x => x.Actividad_Ninos, x => x.Ignore());

        CreateMap<UpdateNinoDTO, Nino>()
            .ForMember(x => x.Asistencias, x => x.Ignore())
            .ForMember(x => x.Visitas, x => x.Ignore())
            .ForMember(x => x.Actividad_Ninos, x => x.Ignore());

        // Los maestros del niño salen de su grupo, que puede tener más de uno.
        CreateMap<Nino, NinoDTO>()
            .ForMember(x => x.Nombre_Grupo, x => x.MapFrom(src => src.Grupo.Nombre))
            .ForMember(x => x.MaestrosId,
                x => x.MapFrom(src => src.Grupo.Grupo_Maestros.Select(gm => gm.PerfilId)))
            .ForMember(x => x.Nombres_Maestros,
                x => x.MapFrom(src => src.Grupo.Grupo_Maestros
                    .Select(gm => gm.Perfil.Nombre + " " + gm.Perfil.Apellido)));

        CreateMap<Nino, NinoSeleccionDTO>()
            .ForMember(x => x.Nombre_Completo,
                x => x.MapFrom(src => src.Apellidos + ", " + src.Nombres))
            .ForMember(x => x.Nombre_Grupo, x => x.MapFrom(src => src.Grupo.Nombre));

        //ACTIVIDAD
        CreateMap<CreateActividadDTO, Actividad>()
            .ForMember(x => x.Actividad_Ninos, x => x.Ignore())
            .ForMember(x => x.Asistencias, x => x.Ignore());

        CreateMap<UpdateActividadDTO, Actividad>()
            .ForMember(x => x.Actividad_Ninos, x => x.Ignore())
            .ForMember(x => x.Asistencias, x => x.Ignore());

        // Los grupos no se guardan en la actividad: se deducen de los niños participantes.
        CreateMap<Actividad, ActividadDTO>()
            .ForMember(x => x.Nombre_Creador,
                x => x.MapFrom(src => src.CreadoPor.Nombre + " " + src.CreadoPor.Apellido))
            .ForMember(x => x.Ninos,
                x => x.MapFrom(src => src.Actividad_Ninos.Select(n => n.NinoId)))
            .ForMember(x => x.Grupos,
                x => x.MapFrom(src => src.Actividad_Ninos
                    .Select(n => n.Nino.Grupo.Nombre)
                    .Distinct()));

        //ASISTENCIA
        CreateMap<CreateAsistenciaDTO, Asistencia>();

        CreateMap<UpdateAsistenciaDTO, Asistencia>();

        CreateMap<Asistencia, AsistenciaDTO>()
            .ForMember(x => x.Nombre_Actividad, x => x.MapFrom(src => src.Actividad.Nombre))
            .ForMember(x => x.Nombre_Nino,
                x => x.MapFrom(src => src.Nino.Nombres + " " + src.Nino.Apellidos))
            .ForMember(x => x.Nombre_Registrador,
                x => x.MapFrom(src => src.RegistradoPor.Nombre + " " + src.RegistradoPor.Apellido));

        //VISITA
        CreateMap<CreateVisitaDTO, Visita>();

        CreateMap<UpdateVisitaDTO, Visita>();

        CreateMap<Visita, VisitaDTO>()
            .ForMember(x => x.Nombre_Nino,
                x => x.MapFrom(src => src.Nino.Nombres + " " + src.Nino.Apellidos))
            .ForMember(x => x.Nombre_Realizador,
                x => x.MapFrom(src => src.RealizadoPor.Nombre + " " + src.RealizadoPor.Apellido));
    }
}
