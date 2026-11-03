export type LecturaDeCoordenadas =
  | { tipo: 'vacia' }
  | { tipo: 'error'; mensaje: string }
  | { tipo: 'valida'; latitud: number; longitud: number };

/**
 * Interpreta unas coordenadas escritas o pegadas. Separa por coma o por espacios, de modo
 * que sirva tanto "-17.78, -63.18" (como las entrega Google Maps) como "-17.78 -63.18".
 *
 * Los límites son los mismos que valida el servidor, para avisar antes de enviar.
 */
export function interpretarCoordenadas(texto: string): LecturaDeCoordenadas {
  const limpio = texto.trim();

  if (limpio === '') {
    return { tipo: 'vacia' };
  }

  const partes = limpio.split(/[,\s]+/).filter((parte) => parte !== '');

  if (partes.length !== 2) {
    return {
      tipo: 'error',
      mensaje: 'Escribe dos números separados por coma. Ejemplo: -17.783, -63.182',
    };
  }

  const latitud = Number(partes[0]);
  const longitud = Number(partes[1]);

  if (!Number.isFinite(latitud) || !Number.isFinite(longitud)) {
    return { tipo: 'error', mensaje: 'Alguno de los dos valores no es un número.' };
  }

  if (latitud < -90 || latitud > 90) {
    return { tipo: 'error', mensaje: 'La latitud debe estar entre -90 y 90.' };
  }

  if (longitud < -180 || longitud > 180) {
    return { tipo: 'error', mensaje: 'La longitud debe estar entre -180 y 180.' };
  }

  return { tipo: 'valida', latitud, longitud };
}
