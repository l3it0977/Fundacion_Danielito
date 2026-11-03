import { useEffect, useRef } from 'react';
import { AlertTriangle } from 'lucide-react';
import { Boton } from './Boton';
import './DialogoConfirmacion.css';

interface Props {
  titulo: string;
  mensaje: string;
  textoConfirmar?: string;
  /** Marca la acción como destructiva: pinta el botón de rojo. */
  esDestructiva?: boolean;
  procesando?: boolean;
  alConfirmar: () => void;
  alCancelar: () => void;
}

/**
 * Pide confirmación antes de una acción que no se puede deshacer.
 * Usa el elemento <dialog> del navegador, que ya se encarga de atrapar el foco
 * dentro de la ventana y de bloquear el resto de la página.
 */
export function DialogoConfirmacion({
  titulo,
  mensaje,
  textoConfirmar = 'Confirmar',
  esDestructiva = false,
  procesando = false,
  alConfirmar,
  alCancelar,
}: Props) {
  const dialogo = useRef<HTMLDialogElement>(null);

  useEffect(() => {
    dialogo.current?.showModal();
  }, []);

  return (
    <dialog
      ref={dialogo}
      className="dialogo"
      /* La tecla Escape cierra el diálogo: el navegador dispara este evento. */
      onCancel={(evento) => {
        evento.preventDefault();
        alCancelar();
      }}
    >
      <div className="dialogo__contenido">
        <div className={`dialogo__icono ${esDestructiva ? 'dialogo__icono--peligro' : ''}`}>
          <AlertTriangle size={24} aria-hidden="true" />
        </div>

        <h2 className="dialogo__titulo">{titulo}</h2>
        <p className="dialogo__mensaje">{mensaje}</p>

        <div className="dialogo__acciones">
          <Boton variante="contorno" onClick={alCancelar} disabled={procesando}>
            Cancelar
          </Boton>

          <Boton
            variante={esDestructiva ? 'peligro' : 'primario'}
            onClick={alConfirmar}
            cargando={procesando}
          >
            {textoConfirmar}
          </Boton>
        </div>
      </div>
    </dialog>
  );
}
