import { useEffect, useState } from 'react';
import { ClipboardPaste, MapPin } from 'lucide-react';
import { interpretarCoordenadas } from '../../utilidades/coordenadas';
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

  function aplicarTextoPegado(texto: string) {
    setTextoPegado(texto);
    setError('');

    const lectura = interpretarCoordenadas(texto);

    switch (lectura.tipo) {
      case 'vacia':
        alCambiar(null, null);
        break;
      case 'error':
        setError(lectura.mensaje);
        break;
      case 'valida':
        alCambiar(lectura.latitud, lectura.longitud);
        break;
    }
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
