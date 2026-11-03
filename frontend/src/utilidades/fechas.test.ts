import { describe, expect, it } from 'vitest';
import { fechaLocalDeHoy, formatearFecha, soloFecha } from './fechas';

describe('soloFecha', () => {
  it('deja intacta una fecha sin hora y quita la hora si la trae', () => {
    expect(soloFecha('2026-10-13')).toBe('2026-10-13');
    expect(soloFecha('2026-10-13T00:00:00')).toBe('2026-10-13');
  });
});

describe('formatearFecha', () => {
  // Se compara con el día, mes y año del propio objeto Date local: así la prueba pasa en
  // cualquier zona horaria y falla si la función desplazara el día (como hace new Date("AAAA-MM-DD")).
  function esperada(anio: number, mes: number, dia: number): string {
    return new Date(anio, mes - 1, dia).toLocaleDateString('es-BO');
  }

  it('muestra el mismo día que llegó, sin desplazarlo por la zona horaria', () => {
    expect(formatearFecha('2026-10-13')).toBe(esperada(2026, 10, 13));
    expect(formatearFecha('2026-01-01')).toBe(esperada(2026, 1, 1));
    expect(formatearFecha('2026-12-31')).toBe(esperada(2026, 12, 31));
  });

  it('acepta también una fecha con hora', () => {
    expect(formatearFecha('2026-10-13T00:00:00')).toBe(esperada(2026, 10, 13));
  });
});

describe('fechaLocalDeHoy', () => {
  it('devuelve AAAA-MM-DD', () => {
    expect(fechaLocalDeHoy()).toMatch(/^\d{4}-\d{2}-\d{2}$/);
  });
});
