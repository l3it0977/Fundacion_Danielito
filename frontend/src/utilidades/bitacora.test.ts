import { describe, expect, it } from 'vitest';
import { etiquetaDeCampo, nombreDeEntidad, textoDeValor } from './bitacora';

describe('etiquetaDeCampo', () => {
  it('cambia los guiones bajos por espacios', () => {
    expect(etiquetaDeCampo('Fecha_Nacimiento')).toBe('Fecha Nacimiento');
    expect(etiquetaDeCampo('Motivo_Vulnerabilidad')).toBe('Motivo Vulnerabilidad');
    expect(etiquetaDeCampo('Nombres')).toBe('Nombres');
  });
});

describe('textoDeValor', () => {
  it('muestra "(vacío)" para null y para texto vacío', () => {
    expect(textoDeValor(null)).toBe('(vacío)');
    expect(textoDeValor('')).toBe('(vacío)');
  });

  it('traduce los booleanos del backend', () => {
    expect(textoDeValor('True')).toBe('Sí');
    expect(textoDeValor('False')).toBe('No');
  });

  it('deja intacto cualquier otro valor, incluido el cero', () => {
    expect(textoDeValor('Ana')).toBe('Ana');
    expect(textoDeValor('0')).toBe('0');
  });
});

describe('nombreDeEntidad', () => {
  it('traduce las conocidas y deja pasar las demás', () => {
    expect(nombreDeEntidad('Nino')).toBe('Niño');
    expect(nombreDeEntidad('Inventada')).toBe('Inventada');
  });
});
