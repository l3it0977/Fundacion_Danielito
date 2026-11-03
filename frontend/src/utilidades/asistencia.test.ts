import { describe, expect, it } from 'vitest';
import type { Asistencia } from '../tipos';
import { fechaConListaPendiente, marcasGuardadasDelDia } from './asistencia';

function registro(ninoId: number, fecha: string, estado: Asistencia['estado'], motivo: string | null = null): Asistencia {
  return {
    id: ninoId * 100,
    fecha,
    estado,
    motivo_Justificacion: motivo,
    observacion: null,
    actividadId: 1,
    nombre_Actividad: 'Actividad',
    ninoId,
    nombre_Nino: `Niño ${ninoId}`,
    registradoPorId: 1,
    nombre_Registrador: 'Maestra',
  };
}

describe('fechaConListaPendiente', () => {
  it('devuelve el día más antiguo que tiene la lista pendiente', () => {
    const registradas = [
      registro(1, '2026-10-20', 'pendiente'),
      registro(2, '2026-10-13', 'pendiente'),
      registro(3, '2026-10-06', 'presente'),
    ];

    expect(fechaConListaPendiente(registradas)).toBe('2026-10-13');
  });

  it('devuelve null si no hay nada pendiente o no hay registros', () => {
    expect(fechaConListaPendiente([registro(1, '2026-10-06', 'presente')])).toBeNull();
    expect(fechaConListaPendiente([])).toBeNull();
  });

  it('acepta fechas que traen hora', () => {
    expect(fechaConListaPendiente([registro(1, '2026-10-13T00:00:00', 'pendiente')])).toBe('2026-10-13');
  });
});

describe('marcasGuardadasDelDia', () => {
  const registradas = [
    registro(1, '2026-10-13', 'presente'),
    registro(2, '2026-10-13', 'justificado', 'Enfermo'),
    registro(1, '2026-10-20', 'ausente'),
  ];

  it('trae solo lo de esa fecha, indexado por niño', () => {
    expect(marcasGuardadasDelDia(registradas, '2026-10-13')).toEqual({
      1: { estado: 'presente', motivo: '' },
      2: { estado: 'justificado', motivo: 'Enfermo' },
    });
  });

  it('un día sin registros da un objeto vacío', () => {
    expect(marcasGuardadasDelDia(registradas, '2026-01-01')).toEqual({});
  });
});
