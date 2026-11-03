import { ChevronLeft, ChevronRight } from 'lucide-react';
import { Boton } from './Boton';
import './Paginacion.css';

interface Props {
  pagina: number;
  totalPaginas: number;
  /** Texto de resumen, por ejemplo "42 niños". */
  resumen: string;
  alCambiar: (pagina: number) => void;
  /** Bloquea los botones mientras se carga la página siguiente. */
  ocupado?: boolean;
}

/** Controles de "Anterior / Siguiente" con el número de página. No se muestra si hay una sola. */
export function Paginacion({ pagina, totalPaginas, resumen, alCambiar, ocupado = false }: Props) {
  if (totalPaginas <= 1) {
    return null;
  }

  return (
    <nav className="paginacion" aria-label="Paginación">
      <p className="paginacion__texto" aria-live="polite">
        Página {pagina} de {totalPaginas} · {resumen}
      </p>

      <div className="paginacion__botones">
        <Boton
          variante="contorno"
          tamano="chico"
          disabled={ocupado || pagina <= 1}
          onClick={() => alCambiar(pagina - 1)}
          icono={<ChevronLeft size={16} />}
        >
          Anterior
        </Boton>

        <Boton
          variante="contorno"
          tamano="chico"
          disabled={ocupado || pagina >= totalPaginas}
          onClick={() => alCambiar(pagina + 1)}
          icono={<ChevronRight size={16} />}
        >
          Siguiente
        </Boton>
      </div>
    </nav>
  );
}
