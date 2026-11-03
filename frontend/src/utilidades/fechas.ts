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

/**
 * Instante (UTC, en ISO) en que empieza un día del calendario local. Sirve para filtrar por
 * rangos contra fechas guardadas en UTC: el 3 de noviembre en Bolivia empieza a las 04:00 UTC.
 */
export function inicioDelDiaEnUtc(fecha: string): string {
  const [anio, mes, dia] = fecha.split('-').map(Number);

  return new Date(anio, mes - 1, dia, 0, 0, 0, 0).toISOString();
}

/** Último instante (UTC, en ISO) de un día del calendario local. */
export function finDelDiaEnUtc(fecha: string): string {
  const [anio, mes, dia] = fecha.split('-').map(Number);

  return new Date(anio, mes - 1, dia, 23, 59, 59, 999).toISOString();
}

/** Quita la hora de una fecha que llega del backend ("2026-10-13" o "2026-10-13T00:00:00"). */
export function soloFecha(fechaDelServidor: string): string {
  return fechaDelServidor.split('T')[0];
}

/**
 * Muestra una fecha del servidor en formato local (13/10/2026).
 *
 * No se usa new Date("2026-10-13"): JavaScript lo interpreta como medianoche UTC, y en Bolivia
 * (UTC-4) eso se vería como el 12. Las fechas del sistema son de calendario, sin hora ni zona,
 * así que se arman con el año, mes y día tal cual llegan.
 */
export function formatearFecha(fechaDelServidor: string): string {
  const [anio, mes, dia] = soloFecha(fechaDelServidor).split('-').map(Number);

  return new Date(anio, mes - 1, dia).toLocaleDateString('es-BO');
}
