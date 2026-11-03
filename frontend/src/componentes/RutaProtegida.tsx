import { Navigate, useLocation } from 'react-router-dom';
import type { ReactNode } from 'react';
import { useAutenticacion } from '../contextos/ContextoAutenticacion';
import { Cargando, MensajeError } from './comunes/Estados';
import type { NombreRol } from '../tipos';

interface Props {
  children: ReactNode;
  /** Si se indica, solo estos roles pueden entrar. Sin indicar, basta con tener sesión. */
  rolesPermitidos?: NombreRol[];
}

/**
 * Controla el acceso a una ruta.
 * Es una comodidad para el usuario, no una medida de seguridad: quien manipule el
 * navegador puede llegar igual a la pantalla, pero la API rechazará sus peticiones.
 */
export function RutaProtegida({ children, rolesPermitidos }: Props) {
  const { usuario, cargando } = useAutenticacion();
  const ubicacion = useLocation();

  // Mientras se recupera la sesión guardada no se decide nada, para no
  // mandar al login a alguien que sí tiene sesión válida.
  if (cargando) {
    return <Cargando mensaje="Verificando tu sesión…" />;
  }

  if (!usuario) {
    // Se recuerda a dónde iba para volver ahí después de iniciar sesión.
    return <Navigate to="/login" state={{ destino: ubicacion.pathname }} replace />;
  }

  if (rolesPermitidos && !rolesPermitidos.includes(usuario.rol)) {
    return (
      <MensajeError mensaje="No tienes permiso para ver esta sección." />
    );
  }

  return <>{children}</>;
}
