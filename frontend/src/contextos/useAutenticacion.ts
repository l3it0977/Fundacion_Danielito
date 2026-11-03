import { createContext, useContext } from 'react';
import type { NombreRol, UsuarioAutenticado } from '../tipos';

export interface ValorContexto {
  usuario: UsuarioAutenticado | null;
  iniciarSesion: (email: string, password: string) => Promise<void>;
  cerrarSesion: () => void;
  tieneRol: (...roles: NombreRol[]) => boolean;
}

export const ContextoAutenticacion = createContext<ValorContexto | null>(null);

/**
 * Acceso a la sesión desde cualquier componente.
 * Vive en su propio archivo, separado del proveedor, para que la recarga en caliente de
 * Vite siga funcionando: un archivo de componentes no puede exportar además otras cosas.
 */
export function useAutenticacion(): ValorContexto {
  const contexto = useContext(ContextoAutenticacion);

  if (!contexto) {
    throw new Error('useAutenticacion debe usarse dentro de ProveedorAutenticacion');
  }

  return contexto;
}
