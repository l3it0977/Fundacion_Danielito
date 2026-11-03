import type { LucideIcon } from 'lucide-react';
import './TarjetaResumen.css';

type AcentoTarjeta = 'teal' | 'naranja' | 'claro' | 'alerta';

interface Props {
  titulo: string;
  valor: number | string;
  Icono: LucideIcon;
  acento?: AcentoTarjeta;
  /** Texto pequeño bajo el número, por ejemplo "del mes actual". */
  detalle?: string;
}

/**
 * Tarjeta de cifra para los paneles.
 * La franja de color en el borde superior viene del diseño original del sistema.
 */
export function TarjetaResumen({ titulo, valor, Icono, acento = 'teal', detalle }: Props) {
  return (
    <article className={`tarjeta-resumen tarjeta-resumen--${acento}`}>
      <div className="tarjeta-resumen__encabezado">
        <Icono className="tarjeta-resumen__icono" size={20} aria-hidden="true" />
        <h3 className="tarjeta-resumen__titulo">{titulo}</h3>
      </div>

      <p className="tarjeta-resumen__valor">{valor}</p>

      {detalle && <p className="tarjeta-resumen__detalle">{detalle}</p>}
    </article>
  );
}
