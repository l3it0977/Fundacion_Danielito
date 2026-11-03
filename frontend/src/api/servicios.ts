/* Funciones que corresponden a cada endpoint del backend, agrupadas por controlador. */

import { api } from './cliente';
import type {
  Actividad,
  Asistencia,
  Grupo,
  Nino,
  NinoSeleccion,
  PanelGeneral,
  PanelMaestro,
  Perfil,
  RespuestaAutenticacion,
  Usuario,
  Visita,
} from '../tipos';

export const servicioAutenticacion = {
  iniciarSesion: (email: string, password: string) =>
    api.crear<RespuestaAutenticacion>('/Auth/Login', { email, password }),

  registrarUsuario: (datos: {
    email: string;
    password: string;
    nombre: string;
    apellido: string;
    rol: string;
  }) => api.crear<{ mensaje: string }>('/Auth/RegistrarUsuario', datos),
};

export const servicioPanel = {
  general: () => api.obtener<PanelGeneral>('/Dashboard/General'),
  maestro: () => api.obtener<PanelMaestro>('/Dashboard/Maestro'),
};

export const servicioGrupos = {
  listar: () => api.obtener<Grupo[]>('/Grupo'),
  obtener: (id: number) => api.obtener<Grupo>(`/Grupo/${id}`),
  crear: (datos: Partial<Grupo>) => api.crear<string>('/Grupo', datos),
  actualizar: (datos: Partial<Grupo>) => api.actualizar<string>('/Grupo', datos),
  eliminar: (id: number) => api.eliminar<string>(`/Grupo/${id}`),
};

export const servicioNinos = {
  listar: () => api.obtener<Nino[]>('/Nino'),
  obtener: (id: number) => api.obtener<Nino>(`/Nino/${id}`),
  crear: (datos: Partial<Nino>) => api.crear<string>('/Nino', datos),
  actualizar: (datos: Partial<Nino>) => api.actualizar<string>('/Nino', datos),
  /** Borrado real: solo prospera con fichas sin historial; si no, hay que desactivar. */
  eliminar: (id: number) => api.eliminar<string>(`/Nino/${id}`),
  cambiarEstado: (ninoId: number, activo: boolean) =>
    api.actualizar<string>('/Nino/Estado', { ninoId, activo }),

  /** Todos los niños en versión reducida, para elegir participantes. */
  paraSeleccion: () => api.obtener<NinoSeleccion[]>('/Nino/ParaSeleccion'),
};

export const servicioActividades = {
  listar: () => api.obtener<Actividad[]>('/Actividad'),
  obtener: (id: number) => api.obtener<Actividad>(`/Actividad/${id}`),
  crear: (datos: {
    nombre: string;
    descripcion?: string | null;
    fecha_Actividad: string;
    tipo: string;
    /** Participantes elegidos uno a uno, de cualquier grupo. */
    ninosId: number[];
  }) => api.crear<string>('/Actividad', datos),
  actualizar: (datos: {
    id: number;
    nombre: string;
    descripcion?: string | null;
    fecha_Actividad: string;
    tipo: string;
    activa: boolean;
    ninosId: number[];
  }) => api.actualizar<string>('/Actividad', datos),
  eliminar: (id: number) => api.eliminar<string>(`/Actividad/${id}`),
  asignarNinos: (actividadId: number, ninosId: number[]) =>
    api.crear<string>('/Actividad/AsignarNinos', { actividadId, ninosId }),
};

export const servicioAsistencias = {
  listar: () => api.obtener<Asistencia[]>('/Asistencia'),
  porActividad: (actividadId: number) =>
    api.obtener<Asistencia[]>(`/Asistencia/Actividad/${actividadId}`),
  registrarPorActividad: (datos: {
    actividadId: number;
    fecha: string;
    asistencias: {
      ninoId: number;
      estado: string;
      motivo_Justificacion?: string | null;
      observacion?: string | null;
    }[];
  }) => api.crear<string>('/Asistencia/RegistrarPorActividad', datos),
  eliminar: (id: number) => api.eliminar<string>(`/Asistencia/${id}`),
};

export const servicioVisitas = {
  listar: () => api.obtener<Visita[]>('/Visita'),
  obtener: (id: number) => api.obtener<Visita>(`/Visita/${id}`),
  crear: (datos: {
    ninoId: number;
    fecha_Visita: string;
    motivo?: string | null;
    observacion?: string | null;
    seguimiento?: string | null;
  }) => api.crear<string>('/Visita', datos),
  eliminar: (id: number) => api.eliminar<string>(`/Visita/${id}`),
};

export const servicioUsuarios = {
  listar: () => api.obtener<Usuario[]>('/Usuario'),
  obtener: (id: number) => api.obtener<Usuario>(`/Usuario/${id}`),
  cambiarEstado: (perfilId: number, activo: boolean) =>
    api.actualizar<string>('/Usuario/Estado', { perfilId, activo }),
  cambiarRol: (perfilId: number, rol: string) =>
    api.actualizar<string>('/Usuario/Rol', { perfilId, rol }),
};

export const servicioPerfil = {
  miPerfil: () => api.obtener<Perfil>('/Perfil'),
  actualizar: (datos: { nombre: string; apellido: string; email: string }) =>
    api.actualizar<string>('/Perfil', datos),
  cambiarPassword: (passwordActual: string, passwordNueva: string) =>
    api.actualizar<string>('/Perfil/Password', {
      password_Actual: passwordActual,
      password_Nueva: passwordNueva,
    }),
};

export interface FiltrosReporte {
  grupoId: number | null;
  mes: number | null;
  anio: number;
}

/** Los filtros en blanco no se envían: el servidor los entiende como "todos". */
function armarConsulta({ grupoId, mes, anio }: FiltrosReporte): string {
  const consulta = new URLSearchParams({ anio: String(anio) });

  if (grupoId != null) {
    consulta.set('grupoId', String(grupoId));
  }

  if (mes != null) {
    consulta.set('mes', String(mes));
  }

  return consulta.toString();
}

function nombrePeriodo({ grupoId, mes, anio }: FiltrosReporte): string {
  const grupo = grupoId != null ? `grupo-${grupoId}` : 'todos-los-grupos';
  const periodo = mes != null ? `${String(mes).padStart(2, '0')}-${anio}` : `todo-${anio}`;

  return `${grupo}-${periodo}`;
}

export const servicioReportes = {
  // grupoId o mes nulos = todos los grupos de edad / todos los meses del año.
  asistencia: (filtros: FiltrosReporte) =>
    api.descargar(
      `/Reporte/Asistencia?${armarConsulta(filtros)}`,
      `asistencia-${nombrePeriodo(filtros)}.pdf`,
    ),
  actividades: (filtros: FiltrosReporte) =>
    api.descargar(
      `/Reporte/Actividades?${armarConsulta(filtros)}`,
      `actividades-${nombrePeriodo(filtros)}.pdf`,
    ),
  asistenciaPorActividad: (actividadId: number) =>
    api.descargar(
      `/Reporte/AsistenciaPorActividad/${actividadId}`,
      `asistencia-actividad-${actividadId}.pdf`,
    ),
};

export const servicioImportacion = {
  importarNinos: (archivo: File) => {
    const formulario = new FormData();
    formulario.append('archivo', archivo);

    return api.subirArchivo<{
      filas_Leidas: number;
      importados: number;
      rechazados: number;
      errores: string[];
    }>('/Importacion/Ninos', formulario);
  },

  descargarPlantilla: () => api.descargar('/Importacion/Plantilla', 'plantilla-ninos.xlsx'),
};
