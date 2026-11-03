import { useEffect, useState } from 'react';
import { ClipboardPaste, MapPin } from 'lucide-react';
import { Boton } from './Boton';
import './CampoCoordenadas.css';

interface Props {
  latitud: number | null;
  longitud: number | null;
  alCambiar: (latitud: number | null, longitud: number | null) => void;
}

/**
 * Permite escribir o pegar las coordenadas de la vivienda.
 * Acepta el formato que entrega Google Maps al copiar un punto, por ejemplo
 * "-17.783, -63.182", además de los valores escritos uno por uno.
 */
export function CampoCoordenadas({ latitud, longitud, alCambiar }: Props) {
  const [textoPegado, setTextoPegado] = useState('');
  const [error, setError] = useState('');

  // Si las coordenadas cambian desde el mapa, se refleja en la caja de texto.
  useEffect(() => {
    if (latitud != null && longitud != null) {
      setTextoPegado(`${latitud}, ${longitud}`);
    }
  }, [latitud, longitud]);

  /**
   * Interpreta el texto pegado. Separa por coma o por espacios, de modo que
   * sirva tanto "-17.78, -63.18" como "-17.78 -63.18".
   */
  function aplicarTextoPegado(texto: string) {
    setTextoPegado(texto);
    setError('');

    const limpio = texto.trim();

    if (limpio === '') {
      alCambiar(null, null);
      return;
    }

    const partes = limpio
      .split(/[,\s]+/)
      .map((parte) => parte.trim())
      .filter((parte) => parte !== '');

    if (partes.length !== 2) {
      setError('Escribe dos números separados por coma. Ejemplo: -17.783, -63.182');
      return;
    }

    const latitudLeida = Number(partes[0]);
    const longitudLeida = Number(partes[1]);

    if (Number.isNaN(latitudLeida) || Number.isNaN(longitudLeida)) {
      setError('Alguno de los dos valores no es un número.');
      return;
    }

    // Los mismos límites que valida el servidor, para avisar antes de enviar.
    if (latitudLeida < -90 || latitudLeida > 90) {
      setError('La latitud debe estar entre -90 y 90.');
      return;
    }

    if (longitudLeida < -180 || longitudLeida > 180) {
      setError('La longitud debe estar entre -180 y 180.');
      return;
    }

    alCambiar(latitudLeida, longitudLeida);
  }

  /** Lee el portapapeles cuando el navegador lo permite. */
  async function pegarDelPortapapeles() {
    try {
      const texto = await navigator.clipboard.readText();
      aplicarTextoPegado(texto);
    } catch {
      // Algunos navegadores lo bloquean si no hay permiso; se pega a mano.
      setError('No se pudo leer el portapapeles. Pega el texto en el campo.');
    }
  }

  return (
    <div className="campo-coordenadas">
      <label htmlFor="coordenadas" className="campo__etiqueta">
        Coordenadas
      </label>

      <div className="campo-coordenadas__fila">
        <div className="campo__con-boton campo-coordenadas__entrada">
          <MapPin size={18} aria-hidden="true" className="campo-coordenadas__icono" />

          <input
            id="coordenadas"
            type="text"
            className="campo__control campo-coordenadas__control"
            placeholder="-17.783, -63.182"
            value={textoPegado}
            onChange={(evento) => aplicarTextoPegado(evento.target.value)}
            aria-invalid={error ? true : undefined}
            aria-describedby={error ? 'coordenadas-error' : 'coordenadas-ayuda'}
          />
        </div>

        <Boton
          type="button"
          variante="contorno"
          tamano="chico"
          onClick={pegarDelPortapapeles}
          icono={<ClipboardPaste size={16} />}
        >
          Pegar
        </Boton>
      </div>

      {error ? (
        <p className="campo__error" id="coordenadas-error" role="alert">
          {error}
        </p>
      ) : (
        <p className="campo__ayuda" id="coordenadas-ayuda">
          Copia el punto en Google Maps y pégalo aquí. También puedes mover el marcador
          del mapa de abajo.
        </p>
      )}
    </div>
  );
}
