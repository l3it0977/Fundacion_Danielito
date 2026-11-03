import { AlertCircle, CheckCircle2, Info, X } from 'lucide-react';
import type { ReactNode } from 'react';
import './Aviso.css';

type TipoAviso = 'exito' | 'error' | 'informacion';

interface Props {
  tipo: TipoAviso;
  children: ReactNode;
  /** Si se pasa, aparece una equis para descartar el aviso. */
  alCerrar?: () => void;
}

const ICONOS = {
  exito: CheckCircle2,
  error: AlertCircle,
  informacion: Info,
};

/**
 * Mensaje de resultado tras una acción.
 * Los errores usan role="alert" para que se anuncien de inmediato; el resto
 * usa role="status", que espera a que el lector de pantalla termine lo que decía.
 */
export function Aviso({ tipo, children, alCerrar }: Props) {
  const Icono = ICONOS[tipo];

  return (
    <div className={`aviso aviso--${tipo}`} role={tipo === 'error' ? 'alert' : 'status'}>
      <Icono size={20} aria-hidden="true" className="aviso__icono" />

      <div className="aviso__contenido">{children}</div>

      {alCerrar && (
        <button type="button" className="aviso__cerrar" onClick={alCerrar}>
          <X size={16} aria-hidden="true" />
          <span className="solo-lectores">Descartar aviso</span>
        </button>
      )}
    </div>
  );
}
