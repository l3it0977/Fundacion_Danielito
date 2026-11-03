import { useEffect, useRef } from 'react';
import L from 'leaflet';
import 'leaflet/dist/leaflet.css';

/*
  Leaflet carga los iconos del marcador desde rutas relativas que no sobreviven
  al empaquetado de Vite, así que se define un icono propio en SVG.
*/
const ICONO_MARCADOR = L.divIcon({
  className: 'marcador-personalizado',
  html: `<svg width="30" height="42" viewBox="0 0 30 42" xmlns="http://www.w3.org/2000/svg">
    <path d="M15 0C6.7 0 0 6.7 0 15c0 11 15 27 15 27s15-16 15-27C30 6.7 23.3 0 15 0z" fill="#ea8028"/>
    <circle cx="15" cy="15" r="6" fill="#ffffff"/>
  </svg>`,
  iconSize: [30, 42],
  iconAnchor: [15, 42],
});

/** Plaza principal de Santa Cruz: punto de partida cuando no hay coordenadas. */
const CENTRO_POR_DEFECTO: [number, number] = [-17.5147, -63.1678];

/** Acercamiento al que se lleva la vista cuando se escriben coordenadas. */
const ZOOM_AL_UBICAR = 16;

interface Props {
  latitud: number | null;
  longitud: number | null;
  /** Sin esta función el mapa es solo de lectura. */
  alCambiarUbicacion?: (latitud: number, longitud: number) => void;
}

export function MapaUbicacion({ latitud, longitud, alCambiarUbicacion }: Props) {
  const contenedor = useRef<HTMLDivElement>(null);
  const mapa = useRef<L.Map | null>(null);
  const marcador = useRef<L.Marker | null>(null);

  // Último punto que el propio mapa dejó fijado (inicial, clic o arrastre). Sirve para
  // distinguirlo de unas coordenadas escritas desde fuera, que sí deben mover la vista.
  const ultimaUbicacionDelMapa = useRef<{ lat: number; lng: number } | null>(null);

  // Se guarda en una referencia para que el mapa no se reconstruya en cada render.
  const alCambiar = useRef(alCambiarUbicacion);
  alCambiar.current = alCambiarUbicacion;

  useEffect(() => {
    if (!contenedor.current || mapa.current) {
      return;
    }

    const posicionInicial: [number, number] =
      latitud != null && longitud != null ? [latitud, longitud] : CENTRO_POR_DEFECTO;

    mapa.current = L.map(contenedor.current).setView(posicionInicial, 14);
    ultimaUbicacionDelMapa.current = { lat: posicionInicial[0], lng: posicionInicial[1] };

    L.tileLayer('https://{s}.tile.openstreetmap.org/{z}/{x}/{y}.png', {
      attribution: '© OpenStreetMap',
      maxZoom: 19,
    }).addTo(mapa.current);

    const esEditable = Boolean(alCambiar.current);

    marcador.current = L.marker(posicionInicial, {
      icon: ICONO_MARCADOR,
      draggable: esEditable,
    }).addTo(mapa.current);

    if (esEditable) {
      // Se puede arrastrar el marcador o tocar el mapa para reubicarlo.
      marcador.current.on('dragend', () => {
        const posicion = marcador.current!.getLatLng();
        ultimaUbicacionDelMapa.current = { lat: posicion.lat, lng: posicion.lng };
        alCambiar.current?.(posicion.lat, posicion.lng);
      });

      mapa.current.on('click', (evento: L.LeafletMouseEvent) => {
        marcador.current?.setLatLng(evento.latlng);
        ultimaUbicacionDelMapa.current = { lat: evento.latlng.lat, lng: evento.latlng.lng };
        alCambiar.current?.(evento.latlng.lat, evento.latlng.lng);
      });
    }

    return () => {
      mapa.current?.remove();
      mapa.current = null;
    };
    // Solo al montar: las actualizaciones de posición se manejan en el efecto de abajo.
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, []);

  // Mueve el marcador cuando las coordenadas cambian desde fuera del mapa y lleva
  // la vista hasta ahí, para que quien las escribió pueda verificar el lugar.
  useEffect(() => {
    if (!mapa.current || !marcador.current || latitud == null || longitud == null) {
      return;
    }

    const destino = L.latLng(latitud, longitud);
    marcador.current.setLatLng(destino);

    // Si el cambio vino de un clic o de arrastrar el marcador, el usuario ya está
    // mirando ese punto: mover la vista aquí la haría saltar bajo su mano.
    const ultima = ultimaUbicacionDelMapa.current;
    if (ultima && ultima.lat === latitud && ultima.lng === longitud) {
      return;
    }

    // Se recuerda este punto para que, si el usuario vuelve a escribir uno anterior, sí se mueva.
    ultimaUbicacionDelMapa.current = { lat: latitud, lng: longitud };

    // Zoom suficiente para reconocer la calle; no se aleja si el usuario ya acercó más.
    const zoom = Math.max(mapa.current.getZoom(), ZOOM_AL_UBICAR);
    const sinMovimiento = window.matchMedia('(prefers-reduced-motion: reduce)').matches;

    if (sinMovimiento) {
      mapa.current.setView(destino, zoom, { animate: false });
    } else {
      mapa.current.flyTo(destino, zoom, { duration: 0.8 });
    }
  }, [latitud, longitud]);

  return (
    <div
      ref={contenedor}
      className="mapa-contenedor"
      role="application"
      aria-label={
        alCambiarUbicacion
          ? 'Mapa para marcar la ubicación de la vivienda. Toca el mapa o arrastra el marcador.'
          : 'Mapa con la ubicación de la vivienda'
      }
    />
  );
}
