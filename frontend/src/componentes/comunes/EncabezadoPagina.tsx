import type { ReactNode } from 'react';
import './EncabezadoPagina.css';

interface Props {
  titulo: string;
  descripcion?: string;
  /** Botones de acción, alineados a la derecha en pantallas anchas. */
  acciones?: ReactNode;
}

export function EncabezadoPagina({ titulo, descripcion, acciones }: Props) {
  return (
    <header className="encabezado-pagina">
      <div className="encabezado-pagina__texto">
        <h1 className="encabezado-pagina__titulo">{titulo}</h1>
        {descripcion && <p className="encabezado-pagina__descripcion">{descripcion}</p>}
      </div>

      {acciones && <div className="encabezado-pagina__acciones">{acciones}</div>}
    </header>
  );
}
