/*
  Maneja la sesión: guarda el token, lo decodifica para saber quién es el usuario
  y qué rol tiene, y cierra la sesión cuando expira.
*/

import { createContext, useCallback, useContext, useEffect, useMemo, useState } from 'react';
import type { ReactNode } from 'react';
import {
  borrarToken,
  guardarToken,
  leerToken,
  registrarManejadorSesionExpirada,
} from '../api/cliente';
import { servicioAutenticacion } from '../api/servicios';
import type { NombreRol, UsuarioAutenticado } from '../tipos';

interface ValorContexto {
  usuario: UsuarioAutenticado | null;
  cargando: boolean;
  iniciarSesion: (email: string, password: string) => Promise<void>;
  cerrarSesion: () => void;
  tieneRol: (...roles: NombreRol[]) => boolean;
}

const ContextoAutenticacion = createContext<ValorContexto | null>(null);

/**
 * Lee el contenido del token sin validar la firma.
 * Sirve solo para saber qué mostrar en pantalla: la verificación real la hace el
 * backend en cada petición, así que alterar el token aquí no da ningún acceso.
 */
function leerDatosDelToken(token: string): UsuarioAutenticado | null {
  try {
    const cuerpoCodificado = token.split('.')[1];
    if (!cuerpoCodificado) {
      return null;
    }

    // El token usa base64url, que cambia algunos caracteres respecto de base64.
    const base64 = cuerpoCodificado.replace(/-/g, '+').replace(/_/g, '/');
    const datos = JSON.parse(decodeURIComponent(escape(atob(base64))));

    // El claim de rol viaja con el nombre largo que usa .NET.
    const rol =
      datos.role ??
      datos['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'];

    const email =
      datos.email ??
      datos['http://schemas.xmlsoap.org/ws/2005/05/identity/claims/emailaddress'];

    return {
      perfilId: Number(datos.PerfilId),
      nombre: datos.Nombre ?? '',
      apellido: datos.Apellido ?? '',
      email: email ?? '',
      rol: rol as NombreRol,
      expiracion: Number(datos.exp) * 1000,
    };
  } catch {
    return null;
  }
}

function tokenSigueVigente(usuario: UsuarioAutenticado): boolean {
  return usuario.expiracion > Date.now();
}

export function ProveedorAutenticacion({ children }: { children: ReactNode }) {
  const [usuario, setUsuario] = useState<UsuarioAutenticado | null>(null);
  const [cargando, setCargando] = useState(true);

  const cerrarSesion = useCallback(() => {
    borrarToken();
    setUsuario(null);
  }, []);

  // Al abrir la aplicación se recupera la sesión previa, si el token sigue vigente.
  useEffect(() => {
    const token = leerToken();

    if (token) {
      const datos = leerDatosDelToken(token);

      if (datos && tokenSigueVigente(datos)) {
        setUsuario(datos);
      } else {
        borrarToken();
      }
    }

    setCargando(false);
  }, []);

  // El cliente de la API avisa cuando el backend responde 401.
  useEffect(() => {
    registrarManejadorSesionExpirada(cerrarSesion);
  }, [cerrarSesion]);

  // El token dura 2 horas: se cierra la sesión sola al llegar a ese momento,
  // en lugar de esperar a que falle la siguiente petición.
  useEffect(() => {
    if (!usuario) {
      return;
    }

    const milisegundosRestantes = usuario.expiracion - Date.now();

    if (milisegundosRestantes <= 0) {
      cerrarSesion();
      return;
    }

    const temporizador = setTimeout(cerrarSesion, milisegundosRestantes);
    return () => clearTimeout(temporizador);
  }, [usuario, cerrarSesion]);

  const iniciarSesion = useCallback(async (email: string, password: string) => {
    const respuesta = await servicioAutenticacion.iniciarSesion(email, password);

    guardarToken(respuesta.token);

    const datos = leerDatosDelToken(respuesta.token);
    if (!datos) {
      borrarToken();
      throw new Error('La respuesta del servidor no se pudo interpretar.');
    }

    setUsuario(datos);
  }, []);

  const tieneRol = useCallback(
    (...roles: NombreRol[]) => (usuario ? roles.includes(usuario.rol) : false),
    [usuario],
  );

  const valor = useMemo(
    () => ({ usuario, cargando, iniciarSesion, cerrarSesion, tieneRol }),
    [usuario, cargando, iniciarSesion, cerrarSesion, tieneRol],
  );

  return (
    <ContextoAutenticacion.Provider value={valor}>{children}</ContextoAutenticacion.Provider>
  );
}

export function useAutenticacion(): ValorContexto {
  const contexto = useContext(ContextoAutenticacion);

  if (!contexto) {
    throw new Error('useAutenticacion debe usarse dentro de ProveedorAutenticacion');
  }

  return contexto;
}
