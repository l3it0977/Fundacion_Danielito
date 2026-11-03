import { describe, expect, it } from 'vitest';
import { interpretarCoordenadas } from './coordenadas';

describe('interpretarCoordenadas', () => {
  it('acepta el formato de Google Maps: dos números separados por coma', () => {
    expect(interpretarCoordenadas('-17.783, -63.182')).toEqual({
      tipo: 'valida',
      latitud: -17.783,
      longitud: -63.182,
    });
  });

  it('acepta números separados solo por espacios o sin espacio tras la coma', () => {
    const esperado = { tipo: 'valida', latitud: -17.78, longitud: -63.18 };

    expect(interpretarCoordenadas('-17.78 -63.18')).toEqual(esperado);
    expect(interpretarCoordenadas('-17.78,-63.18')).toEqual(esperado);
    expect(interpretarCoordenadas('  -17.78 ,  -63.18  ')).toEqual(esperado);
  });

  it('un texto en blanco es "vacía": quien llama borra las coordenadas', () => {
    expect(interpretarCoordenadas('')).toEqual({ tipo: 'vacia' });
    expect(interpretarCoordenadas('   ')).toEqual({ tipo: 'vacia' });
  });

  it('pide dos números cuando hay uno o más de dos', () => {
    expect(interpretarCoordenadas('-17.78').tipo).toBe('error');
    expect(interpretarCoordenadas('-17.78, -63.18, 400').tipo).toBe('error');
  });

  it('rechaza lo que no es un número', () => {
    expect(interpretarCoordenadas('norte, sur')).toEqual({
      tipo: 'error',
      mensaje: 'Alguno de los dos valores no es un número.',
    });
    expect(interpretarCoordenadas('Infinity, 10').tipo).toBe('error');
  });

  it('respeta los límites de latitud y longitud, incluidos los extremos', () => {
    expect(interpretarCoordenadas('90, 180').tipo).toBe('valida');
    expect(interpretarCoordenadas('-90, -180').tipo).toBe('valida');

    expect(interpretarCoordenadas('90.1, 0')).toEqual({
      tipo: 'error',
      mensaje: 'La latitud debe estar entre -90 y 90.',
    });
    expect(interpretarCoordenadas('0, -180.5')).toEqual({
      tipo: 'error',
      mensaje: 'La longitud debe estar entre -180 y 180.',
    });
  });
});
