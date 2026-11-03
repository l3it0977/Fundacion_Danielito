import { useCallback } from 'react';
import { useAutenticacion } from '../contextos/useAutenticacion';
import type { Actividad } from '../tipos';

/**
 * Reglas de quién puede hacer qué con una actividad. Son el reflejo de las que
 * aplica el backend, que es quien realmente las hace cumplir: aquí solo sirven
 * para no mostrar botones que terminarían en un error.
 *
 * - Todos los roles crean actividades, eligen niños y pasan lista.
 * - Directora y Administrador editan, cambian participantes y eliminan cualquiera.
 * - Un maestro hace eso mismo, pero solo en las actividades que él creó.
 */
export function usePermisosActividad() {
  const { usuario, tieneRol } = useAutenticacion();

  const esDireccion = tieneRol('Directora', 'Administrador');

  /** Indica si la actividad la creó quien tiene la sesión abierta. */
  const esPropia = useCallback(
    (actividad: Actividad) => usuario != null && actividad.creadoPorId === usuario.perfilId,
    [usuario],
  );

  /** Editar, cambiar participantes y eliminar. */
  const puedeGestionar = useCallback(
    (actividad: Actividad) => esDireccion || esPropia(actividad),
    [esDireccion, esPropia],
  );

  return { esDireccion, esPropia, puedeGestionar };
}
