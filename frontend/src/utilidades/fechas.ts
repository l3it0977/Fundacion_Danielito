/**
 * Fecha de hoy en formato AAAA-MM-DD, según el reloj de quien usa el sistema.
 *
 * No se usa toISOString(): esa función da la fecha en hora universal, y en
 * Bolivia (UTC-4) después de las 20:00 devolvería el día siguiente. Eso hacía
 * que una actividad creada de noche quedara con fecha de mañana.
 */
export function fechaLocalDeHoy(): string {
  const ahora = new Date();

  const anio = ahora.getFullYear();
  const mes = String(ahora.getMonth() + 1).padStart(2, '0');
  const dia = String(ahora.getDate()).padStart(2, '0');

  return `${anio}-${mes}-${dia}`;
}

/** Quita la hora de una fecha que llega del backend ("2026-10-13T00:00:00"). */
export function soloFecha(fechaDelServidor: string): string {
  return fechaDelServidor.split('T')[0];
}
