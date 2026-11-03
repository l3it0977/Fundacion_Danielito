import './Insignia.css';

type TonoInsignia =
  | 'teal'
  | 'naranja'
  | 'exito'
  | 'error'
  | 'advertencia'
  | 'intervencion'
  | 'neutro';

interface Props {
  texto: string;
  tono?: TonoInsignia;
}

/**
 * Etiqueta de estado. Siempre lleva texto, nunca solo color:
 * quien no distingue colores debe poder leer igual de qué se trata.
 */
export function Insignia({ texto, tono = 'teal' }: Props) {
  return <span className={`insignia insignia--${tono}`}>{texto}</span>;
}

/** Traduce el estado de asistencia que usa el backend al tono que le corresponde. */
export function InsigniaAsistencia({ estado }: { estado: string }) {
  const tonos: Record<string, TonoInsignia> = {
    // 'pendiente' usa tono neutro a propósito: no es una falta, es una lista
    // que todavía nadie pasó. Pintarla de rojo daría a entender lo contrario.
    pendiente: 'neutro',
    presente: 'exito',
    ausente: 'error',
    justificado: 'advertencia',
  };

  const etiquetas: Record<string, string> = {
    pendiente: 'Sin pasar lista',
    presente: 'Presente',
    ausente: 'Ausente',
    justificado: 'Justificado',
  };

  return <Insignia texto={etiquetas[estado] ?? estado} tono={tonos[estado] ?? 'neutro'} />;
}
