import { useCallback, useEffect, useState } from 'react';

interface Resultado<T> {
  datos: T | null;
  cargando: boolean;
  error: string;
  /** Vuelve a pedir los datos; se usa tras crear, editar o eliminar algo. */
  recargar: () => void;
}

/**
 * Pide datos a la API y expone el estado de la petición.
 * Evita repetir el mismo useState triple y useEffect en cada pantalla.
 */
export function useCargarDatos<T>(
  obtener: () => Promise<T>,
  dependencias: unknown[] = [],
): Resultado<T> {
  const [datos, setDatos] = useState<T | null>(null);
  const [cargando, setCargando] = useState(true);
  const [error, setError] = useState('');

  // eslint-disable-next-line react-hooks/exhaustive-deps
  const obtenerEstable = useCallback(obtener, dependencias);

  const cargar = useCallback(() => {
    setCargando(true);
    setError('');

    obtenerEstable()
      .then(setDatos)
      .catch((fallo: Error) => setError(fallo.message))
      .finally(() => setCargando(false));
  }, [obtenerEstable]);

  useEffect(() => {
    cargar();
  }, [cargar]);

  return { datos, cargando, error, recargar: cargar };
}
