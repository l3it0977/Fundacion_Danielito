import { useEffect, useState } from 'react';

/**
 * Devuelve el valor recibido, pero solo después de que deje de cambiar durante `milisegundos`.
 * Sirve para no pedir datos al servidor con cada letra que se escribe en una búsqueda.
 */
export function useRetraso<T>(valor: T, milisegundos = 350): T {
  const [valorConRetraso, setValorConRetraso] = useState(valor);

  useEffect(() => {
    // setState dentro del temporizador, no de forma síncrona en el efecto.
    const temporizador = setTimeout(() => setValorConRetraso(valor), milisegundos);

    return () => clearTimeout(temporizador);
  }, [valor, milisegundos]);

  return valorConRetraso;
}
