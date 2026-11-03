/*
  Tipos que corresponden a los DTOs del backend.
  El backend usa nombres con guion bajo (Nombre_Grupo) y ASP.NET los envía en JSON
  como camelCase conservando el guion (nombre_Grupo). Por eso se ven así.
*/

export type NombreRol = 'Directora' | 'Administrador' | 'Maestro';

/** Lo que devuelve POST /Auth/Login */
export interface RespuestaAutenticacion {
  token: string;
  expiracion: string;
}

/** Datos que vienen dentro del token, una vez decodificado. */
export interface UsuarioAutenticado {
  perfilId: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: NombreRol;
  expiracion: number;
}

export interface Perfil {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  activo: boolean;
  rol: string;
  mis_Grupos: string[];
}

export interface Grupo {
  id: number;
  nombre: string;
  descripcion: string | null;
  activo: boolean;
  cantidad_Ninos: number;
  /** Un grupo puede tener varios maestros a cargo. */
  maestrosId: number[];
  nombres_Maestros: string[];
}

export interface Nino {
  id: number;
  codigo: string;
  nombres: string;
  apellidos: string;
  fecha_Nacimiento: string | null;
  contacto: string | null;
  curso: string | null;
  colegio: string | null;
  vulnerable: boolean;
  motivo_Vulnerabilidad: string | null;
  observaciones: string | null;
  fue_Al_Encuentro: boolean;
  bautizado: boolean;
  asiste_Iglesia: boolean;
  nombre_Iglesia: string | null;
  nombre_Celula: string | null;
  activo: boolean;
  latitud: number | null;
  longitud: number | null;
  grupoId: number;
  nombre_Grupo: string;
  /** Vienen del grupo, que puede tener más de un maestro. */
  maestrosId: number[];
  nombres_Maestros: string[];
  /** La calcula el backend desde la fecha de nacimiento; no se guarda en la base. */
  edad: number | null;
}

export type TipoActividad = 'normal' | 'intervencion';

export interface Actividad {
  id: number;
  nombre: string;
  descripcion: string | null;
  /** Obligatoria: con ella se genera la lista de asistencia. */
  fecha_Actividad: string;
  tipo: TipoActividad;
  activa: boolean;
  creadoPorId: number;
  nombre_Creador: string;
  ninos: number[];
  /** Nombres de los grupos, deducidos de los niños participantes. */
  grupos: string[];
}

/**
 * Versión reducida de un niño, para elegir participantes al armar una actividad.
 * Solo trae lo necesario para identificarlo, sin datos sensibles.
 */
export interface NinoSeleccion {
  id: number;
  codigo: string;
  nombre_Completo: string;
  grupoId: number;
  nombre_Grupo: string;
}

export type EstadoAsistencia = 'pendiente' | 'presente' | 'ausente' | 'justificado';

export interface Asistencia {
  id: number;
  fecha: string;
  estado: EstadoAsistencia;
  motivo_Justificacion: string | null;
  observacion: string | null;
  actividadId: number;
  nombre_Actividad: string;
  ninoId: number;
  nombre_Nino: string;
  registradoPorId: number;
  nombre_Registrador: string;
}

export interface Visita {
  id: number;
  fecha_Visita: string;
  motivo: string | null;
  observacion: string | null;
  seguimiento: string | null;
  ninoId: number;
  nombre_Nino: string;
  realizadoPorId: number;
  nombre_Realizador: string;
}

export interface Usuario {
  id: number;
  nombre: string;
  apellido: string;
  email: string;
  rol: string;
  activo: boolean;
  mis_Grupos: string[];
}

/* ---------- Paneles ---------- */

export interface AsistenciaPorGrupo {
  grupo: string;
  total: number;
  presentes: number;
  ausentes: number;
  justificados: number;
}

export interface ActividadResumen {
  id: number;
  nombre: string;
  fecha_Actividad: string | null;
  tipo: string;
  grupos: string[];
}

export interface NinoResumen {
  id: number;
  codigo: string;
  nombre_Completo: string;
  grupo: string;
  motivo_Vulnerabilidad: string | null;
}

export interface PanelGeneral {
  total_Ninos: number;
  total_Grupos: number;
  total_Actividades: number;
  total_Asistencias: number;
  ninos_Vulnerables: number;
  presentes: number;
  ausentes: number;
  justificados: number;
  porcentaje_Asistencia: number;
  asistencia_Por_Grupo: AsistenciaPorGrupo[];
  ultimas_Actividades: ActividadResumen[];
  lista_Vulnerables: NinoResumen[];
}

export interface Cumpleanos {
  ninoId: number;
  nombre_Completo: string;
  fecha_Nacimiento: string;
  edad_Que_Cumple: number;
  dias_Restantes: number;
}

export interface TopAsistente {
  ninoId: number;
  nombre_Completo: string;
  total_Asistencias: number;
}

export interface ResumenAsistencia {
  total: number;
  presentes: number;
  ausentes: number;
  justificados: number;
  porcentaje_Asistencia: number;
}

export interface PanelMaestro {
  total_Mis_Ninos: number;
  total_Mis_Actividades: number;
  total_Mis_Asistencias: number;
  cumpleanos_Cercanos: Cumpleanos[];
  mis_Mas_Asistentes: TopAsistente[];
  asistencia_Del_Mes: ResumenAsistencia;
}
