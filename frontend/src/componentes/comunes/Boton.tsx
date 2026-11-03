import type { ButtonHTMLAttributes, ReactNode } from 'react';
import './Boton.css';

type VarianteBoton = 'primario' | 'naranja' | 'secundario' | 'contorno' | 'peligro';
type TamanoBoton = 'normal' | 'chico';

interface Props extends ButtonHTMLAttributes<HTMLButtonElement> {
  variante?: VarianteBoton;
  tamano?: TamanoBoton;
  /** Muestra el estado de espera y bloquea el botón para evitar envíos repetidos. */
  cargando?: boolean;
  icono?: ReactNode;
  anchoCompleto?: boolean;
}

export function Boton({
  variante = 'primario',
  tamano = 'normal',
  cargando = false,
  icono,
  anchoCompleto = false,
  children,
  disabled,
  className = '',
  ...resto
}: Props) {
  const clases = [
    'boton',
    `boton--${variante}`,
    `boton--${tamano}`,
    anchoCompleto ? 'boton--ancho-completo' : '',
    className,
  ]
    .filter(Boolean)
    .join(' ');

  return (
    <button className={clases} disabled={disabled || cargando} {...resto}>
      {cargando ? <span className="boton__girador" aria-hidden="true" /> : icono}
      <span>{cargando ? 'Espera…' : children}</span>
    </button>
  );
}
