/*
  Punto único por donde pasan todas las llamadas a la API.
  Se encarga de adjuntar el token, interpretar los errores del backend y avisar
  cuando la sesión dejó de ser válida.
*/

const URL_BASE = import.meta.env.VITE_API_URL ?? 'http://localhost:5002';

const CLAVE_TOKEN = 'danielito_token';

/** Error con el mensaje que devuelve el backend y su código HTTP. */
export class ErrorApi extends Error {
  readonly codigoHttp: number;

  constructor(mensaje: string, codigoHttp: number) {
    super(mensaje);
    this.name = 'ErrorApi';
    this.codigoHttp = codigoHttp;
  }

  /** La sesión expiró o la cuenta fue desactivada. */
  get esSesionInvalida(): boolean {
    return this.codigoHttp === 401;
  }

  /** El usuario está autenticado pero su rol no alcanza para esta acción. */
  get esAccesoDenegado(): boolean {
    return this.codigoHttp === 403;
  }
}

export function guardarToken(token: string): void {
  localStorage.setItem(CLAVE_TOKEN, token);
}

export function leerToken(): string | null {
  return localStorage.getItem(CLAVE_TOKEN);
}

export function borrarToken(): void {
  localStorage.removeItem(CLAVE_TOKEN);
}

/**
 * Se dispara cuando el backend responde 401, para que la aplicación cierre la sesión
 * sin que cada pantalla tenga que ocuparse de ello.
 */
type ManejadorSesionExpirada = () => void;
let alExpirarSesion: ManejadorSesionExpirada | null = null;

export function registrarManejadorSesionExpirada(manejador: ManejadorSesionExpirada): void {
  alExpirarSesion = manejador;
}

interface OpcionesPeticion {
  metodo?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  cuerpo?: unknown;
  /** Para subir archivos: se envía tal cual, sin convertir a JSON. */
  formulario?: FormData;
}

async function peticion<T>(ruta: string, opciones: OpcionesPeticion = {}): Promise<T> {
  const { metodo = 'GET', cuerpo, formulario } = opciones;

  const cabeceras: Record<string, string> = {};
  const token = leerToken();

  if (token) {
    cabeceras.Authorization = `Bearer ${token}`;
  }

  // Con FormData el navegador define el Content-Type por su cuenta, incluyendo
  // el separador que necesita. Ponerlo a mano rompe la subida de archivos.
  if (!formulario && cuerpo !== undefined) {
    cabeceras['Content-Type'] = 'application/json';
  }

  const respuesta = await fetch(`${URL_BASE}${ruta}`, {
    method: metodo,
    headers: cabeceras,
    body: formulario ?? (cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined),
  });

  if (respuesta.status === 401) {
    alExpirarSesion?.();
    throw new ErrorApi('Tu sesión expiró. Vuelve a iniciar sesión.', 401);
  }

  if (!respuesta.ok) {
    throw new ErrorApi(await extraerMensajeDeError(respuesta), respuesta.status);
  }

  return leerCuerpo<T>(respuesta);
}

/** El middleware del backend responde { mensaje: "..." }; ASP.NET usa otro formato al validar. */
async function extraerMensajeDeError(respuesta: Response): Promise<string> {
  try {
    const datos = await respuesta.json();

    if (typeof datos?.mensaje === 'string') {
      return datos.mensaje;
    }

    // Errores de validación de ASP.NET: { errors: { campo: ["mensaje"] } }
    if (datos?.errors && typeof datos.errors === 'object') {
      const mensajes = Object.values(datos.errors).flat();
      if (mensajes.length > 0) {
        return String(mensajes[0]);
      }
    }

    if (typeof datos?.title === 'string') {
      return datos.title;
    }
  } catch {
    // La respuesta no era JSON; se usa el mensaje genérico de abajo.
  }

  return 'No se pudo completar la operación. Intenta nuevamente.';
}

/** Varios endpoints devuelven un texto plano ("Niño creado correctamente"), no JSON. */
async function leerCuerpo<T>(respuesta: Response): Promise<T> {
  if (respuesta.status === 204) {
    return undefined as T;
  }

  const texto = await respuesta.text();

  if (texto === '') {
    return undefined as T;
  }

  try {
    return JSON.parse(texto) as T;
  } catch {
    return texto as T;
  }
}

export const api = {
  obtener: <T>(ruta: string) => peticion<T>(ruta),

  crear: <T>(ruta: string, cuerpo: unknown) => peticion<T>(ruta, { metodo: 'POST', cuerpo }),

  actualizar: <T>(ruta: string, cuerpo: unknown) => peticion<T>(ruta, { metodo: 'PUT', cuerpo }),

  eliminar: <T>(ruta: string) => peticion<T>(ruta, { metodo: 'DELETE' }),

  subirArchivo: <T>(ruta: string, formulario: FormData) =>
    peticion<T>(ruta, { metodo: 'POST', formulario }),

  /** Para los reportes en PDF y la plantilla de Excel, que llegan como archivo. */
  descargar: async (ruta: string, nombreArchivo: string): Promise<void> => {
    const token = leerToken();

    const respuesta = await fetch(`${URL_BASE}${ruta}`, {
      headers: token ? { Authorization: `Bearer ${token}` } : {},
    });

    if (respuesta.status === 401) {
      alExpirarSesion?.();
      throw new ErrorApi('Tu sesión expiró. Vuelve a iniciar sesión.', 401);
    }

    if (!respuesta.ok) {
      throw new ErrorApi(await extraerMensajeDeError(respuesta), respuesta.status);
    }

    const contenido = await respuesta.blob();
    const urlTemporal = URL.createObjectURL(contenido);

    const enlace = document.createElement('a');
    enlace.href = urlTemporal;
    enlace.download = nombreArchivo;
    enlace.click();

    // Sin esto el archivo queda retenido en memoria mientras dure la pestaña.
    URL.revokeObjectURL(urlTemporal);
  },
};
