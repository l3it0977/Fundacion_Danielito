import type { ReactNode } from 'react';
import './Panel.css';

interface Props {
  titulo: string;
  /** Contenido alineado a la derecha del título: filtros, botones de acción. */
  acciones?: ReactNode;
  children: ReactNode;
}

/** Contenedor blanco con título, usado para tablas y bloques de contenido. */
export function Panel({ titulo, acciones, children }: Props) {
  return (
    <section className="panel">
      <header className="panel__encabezado">
        <h2 className="panel__titulo">{titulo}</h2>
        {acciones && <div className="panel__acciones">{acciones}</div>}
      </header>

      <div className="panel__contenido">{children}</div>
    </section>
  );
}
