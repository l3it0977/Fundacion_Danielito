import { AlertCircle, Inbox, Loader2 } from 'lucide-react';
import type { ReactNode } from 'react';
import './Estados.css';

/**
 * Se muestra mientras se esperan datos.
 * role="status" hace que un lector de pantalla anuncie el cambio sin interrumpir.
 */
export function Cargando({ mensaje = 'Cargando…' }: { mensaje?: string }) {
  return (
    <div className="estado" role="status">
      <Loader2 className="estado__girador" size={28} aria-hidden="true" />
      <p className="estado__texto">{mensaje}</p>
    </div>
  );
}

/** role="alert" hace que el error se anuncie de inmediato. */
export function MensajeError({ mensaje, children }: { mensaje: string; children?: ReactNode }) {
  return (
    <div className="estado estado--error" role="alert">
      <AlertCircle size={28} aria-hidden="true" />
      <p className="estado__texto">{mensaje}</p>
      {children}
    </div>
  );
}

/** Para listas sin resultados: explica qué falta en vez de dejar la pantalla en blanco. */
export function SinDatos({ mensaje, children }: { mensaje: string; children?: ReactNode }) {
  return (
    <div className="estado">
      <Inbox className="estado__icono-vacio" size={32} aria-hidden="true" />
      <p className="estado__texto">{mensaje}</p>
      {children}
    </div>
  );
}
