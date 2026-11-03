import type { Asistencia, EstadoAsistencia } from '../tipos';
import { soloFecha } from './fechas';

/** Lo marcado para cada niño antes de guardar. */
export interface MarcaAsistencia {
  estado: EstadoAsistencia;
  motivo: string;
}

/**
 * Día en el que conviene abrir la pantalla de asistencia: el más antiguo que todavía tiene la
 * lista pendiente (la que se armó sola al crear la actividad). Si no hay nada pendiente
 * devuelve null y quien llama usa hoy, que es lo habitual para una sesión nueva.
 */
export function fechaConListaPendiente(registradas: Asistencia[]): string | null {
  const fechas = registradas
    .filter((registro) => registro.estado === 'pendiente')
    .map((registro) => soloFecha(registro.fecha))
    .sort();

  return fechas[0] ?? null;
}

/** Lo ya guardado de una fecha, por niño, para poder corregirlo sin volver a marcar todo. */
export function marcasGuardadasDelDia(
  registradas: Asistencia[],
  fecha: string,
): Record<number, MarcaAsistencia> {
  const marcas: Record<number, MarcaAsistencia> = {};

  for (const registro of registradas) {
    if (soloFecha(registro.fecha) === fecha) {
      marcas[registro.ninoId] = {
        estado: registro.estado,
        motivo: registro.motivo_Justificacion ?? '',
      };
    }
  }

  return marcas;
}
