import { Component } from 'react';
import type { ErrorInfo, ReactNode } from 'react';
import { MensajeError } from './comunes/Estados';
import { Boton } from './comunes/Boton';

interface Props {
  children: ReactNode;
}

interface Estado {
  fallo: Error | null;
}

/**
 * Atrapa los errores de render de lo que contiene y muestra un aviso en su lugar, en vez de
 * dejar toda la aplicación en blanco. Debe ser una clase: React no ofrece otra forma.
 *
 * Para que se rearme al cambiar de pantalla, quien lo usa le pasa una `key` que cambie con la
 * ruta: así, si una sección falla, ir a otra funciona sin recargar el navegador.
 */
export class LimiteDeErrores extends Component<Props, Estado> {
  state: Estado = { fallo: null };

  static getDerivedStateFromError(fallo: Error): Estado {
    return { fallo };
  }

  componentDidCatch(fallo: Error, info: ErrorInfo) {
    // Queda en la consola del navegador para quien dé soporte.
    console.error('Error en la interfaz:', fallo, info.componentStack);
  }

  render() {
    if (!this.state.fallo) {
      return this.props.children;
    }

    return (
      <MensajeError mensaje="Algo salió mal al mostrar esta pantalla. Puedes volver a intentarlo; si el problema sigue, avisa al administrador.">
        <Boton variante="contorno" onClick={() => window.location.reload()}>
          Recargar la página
        </Boton>
      </MensajeError>
    );
  }
}
