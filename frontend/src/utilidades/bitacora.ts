/** Nombre legible de un campo del backend: "Fecha_Nacimiento" -> "Fecha Nacimiento". */
export function etiquetaDeCampo(campo: string): string {
  return campo.replaceAll('_', ' ');
}

/** Valor de un cambio tal como lo guarda el backend, en texto que se entienda. */
export function textoDeValor(valor: string | null): string {
  if (valor === null || valor === '') {
    return '(vacío)';
  }

  // El backend serializa los booleanos como "True" / "False".
  if (valor === 'True') {
    return 'Sí';
  }

  if (valor === 'False') {
    return 'No';
  }

  return valor;
}

/** Nombre en pantalla de cada tipo de registro de la bitácora. */
export const NOMBRES_DE_ENTIDAD: Record<string, string> = {
  Nino: 'Niño',
  Grupo: 'Grupo',
  Actividad: 'Actividad',
  Visita: 'Visita',
  Usuario: 'Usuario',
};

export function nombreDeEntidad(entidad: string): string {
  return NOMBRES_DE_ENTIDAD[entidad] ?? entidad;
}
