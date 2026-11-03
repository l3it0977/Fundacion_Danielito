import { useId } from 'react';
import type { InputHTMLAttributes, ReactNode, SelectHTMLAttributes, TextareaHTMLAttributes } from 'react';
import './Campos.css';

interface PropsBase {
  etiqueta: string;
  /** Texto de ayuda bajo el campo, para explicar el formato esperado. */
  ayuda?: string;
  error?: string;
  requerido?: boolean;
}

/** Envoltorio común: etiqueta arriba, campo, y ayuda o error debajo. */
function Envoltorio({
  etiqueta,
  ayuda,
  error,
  requerido,
  idCampo,
  children,
}: PropsBase & { idCampo: string; children: ReactNode }) {
  return (
    <div className={`campo ${error ? 'campo--con-error' : ''}`}>
      <label htmlFor={idCampo} className="campo__etiqueta">
        {etiqueta}
        {requerido && (
          <span className="campo__requerido" aria-hidden="true">
            *
          </span>
        )}
      </label>

      {children}

      {/* El mensaje queda junto al campo, no agrupado al final del formulario. */}
      {error && (
        <p className="campo__error" id={`${idCampo}-error`} role="alert">
          {error}
        </p>
      )}

      {!error && ayuda && (
        <p className="campo__ayuda" id={`${idCampo}-ayuda`}>
          {ayuda}
        </p>
      )}
    </div>
  );
}

type PropsTexto = PropsBase & InputHTMLAttributes<HTMLInputElement>;

export function CampoTexto({ etiqueta, ayuda, error, requerido, ...resto }: PropsTexto) {
  const idCampo = useId();

  return (
    <Envoltorio
      etiqueta={etiqueta}
      ayuda={ayuda}
      error={error}
      requerido={requerido}
      idCampo={idCampo}
    >
      <input
        id={idCampo}
        className="campo__control"
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${idCampo}-error` : ayuda ? `${idCampo}-ayuda` : undefined}
        required={requerido}
        {...resto}
      />
    </Envoltorio>
  );
}

type PropsArea = PropsBase & TextareaHTMLAttributes<HTMLTextAreaElement>;

export function CampoTextoLargo({ etiqueta, ayuda, error, requerido, ...resto }: PropsArea) {
  const idCampo = useId();

  return (
    <Envoltorio
      etiqueta={etiqueta}
      ayuda={ayuda}
      error={error}
      requerido={requerido}
      idCampo={idCampo}
    >
      <textarea
        id={idCampo}
        className="campo__control campo__control--area"
        rows={3}
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${idCampo}-error` : ayuda ? `${idCampo}-ayuda` : undefined}
        required={requerido}
        {...resto}
      />
    </Envoltorio>
  );
}

type PropsSeleccion = PropsBase &
  SelectHTMLAttributes<HTMLSelectElement> & {
    opciones: { valor: string | number; etiqueta: string }[];
    /** Primera opción vacía, para obligar a elegir de forma consciente. */
    textoVacio?: string;
  };

export function CampoSeleccion({
  etiqueta,
  ayuda,
  error,
  requerido,
  opciones,
  textoVacio,
  ...resto
}: PropsSeleccion) {
  const idCampo = useId();

  return (
    <Envoltorio
      etiqueta={etiqueta}
      ayuda={ayuda}
      error={error}
      requerido={requerido}
      idCampo={idCampo}
    >
      <select
        id={idCampo}
        className="campo__control"
        aria-invalid={error ? true : undefined}
        aria-describedby={error ? `${idCampo}-error` : ayuda ? `${idCampo}-ayuda` : undefined}
        required={requerido}
        {...resto}
      >
        {textoVacio && <option value="">{textoVacio}</option>}

        {opciones.map((opcion) => (
          <option key={opcion.valor} value={opcion.valor}>
            {opcion.etiqueta}
          </option>
        ))}
      </select>
    </Envoltorio>
  );
}

interface PropsCasilla extends InputHTMLAttributes<HTMLInputElement> {
  etiqueta: string;
}

export function CampoCasilla({ etiqueta, ...resto }: PropsCasilla) {
  const idCampo = useId();

  return (
    <div className="casilla">
      <input id={idCampo} type="checkbox" className="casilla__control" {...resto} />
      <label htmlFor={idCampo} className="casilla__etiqueta">
        {etiqueta}
      </label>
    </div>
  );
}
