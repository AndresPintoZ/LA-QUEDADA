/**
 * Formateo de fechas, distancias y plazas en español.
 *
 * Todo se calcula a partir de valores UTC que envía la API y se muestra en la zona
 * horaria del piloto. Guardar horas locales en la base de datos provoca errores en
 * los cambios de horario de verano; convertir aquí, en el borde, no.
 */

/** Zona horaria del piloto. Cuando la aplicación salga de Ávila, saldrá de la sesión. */
const ZONA_HORARIA = 'Europe/Madrid';

const formateadorDeFechaLarga = new Intl.DateTimeFormat('es-ES', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: ZONA_HORARIA,
});

const formateadorDeHora = new Intl.DateTimeFormat('es-ES', {
  hour: '2-digit',
  minute: '2-digit',
  timeZone: ZONA_HORARIA,
});

const formateadorDeDia = new Intl.DateTimeFormat('es-ES', {
  weekday: 'short',
  day: 'numeric',
  timeZone: ZONA_HORARIA,
});

/** «sábado, 15 de agosto, 19:00» */
export function fechaLarga(isoUtc: string): string {
  return formateadorDeFechaLarga.format(new Date(isoUtc));
}

/**
 * Etiqueta corta para las tarjetas: «HOY 19:00», «MAÑANA 10:00» o «SÁB 15 · 19:00».
 *
 * Decir «mañana» en lugar de la fecha es lo que hace que una lista de planes se lea
 * de un vistazo, que es el objetivo de la pantalla de explorar.
 */
export function cuandoCorto(isoUtc: string, ahora: Date = new Date()): string {
  const fecha = new Date(isoUtc);
  const diasDeDiferencia = diferenciaEnDias(ahora, fecha);

  if (diasDeDiferencia === 0) {
    return `HOY ${formateadorDeHora.format(fecha)}`;
  }

  if (diasDeDiferencia === 1) {
    return `MAÑANA ${formateadorDeHora.format(fecha)}`;
  }

  return `${formateadorDeDia.format(fecha).toUpperCase()} · ${formateadorDeHora.format(fecha)}`;
}

/** «600 m» o «1,2 km». */
export function distancia(metros: number | null): string | null {
  if (metros === null || Number.isNaN(metros)) {
    return null;
  }

  if (metros < 1000) {
    return `${Math.round(metros / 10) * 10} m`;
  }

  return `${(metros / 1000).toLocaleString('es-ES', { maximumFractionDigits: 1 })} km`;
}

/**
 * Texto del estado de las plazas.
 *
 * Devuelve también un tono, pero el texto siempre es explícito: el color nunca es
 * el único indicador de nada (requisito de accesibilidad, docs/03-diseno-visual.md).
 */
export function estadoDePlazas(
  capacidad: number,
  ocupadas: number,
  estado: string,
): { texto: string; tono: 'ok' | 'warn' | 'danger' | 'brand' } {
  if (estado === 'Cancelada') {
    return { texto: 'Cancelado', tono: 'danger' };
  }

  if (estado === 'Finalizada') {
    return { texto: 'Ya ha pasado', tono: 'brand' };
  }

  const libres = capacidad - ocupadas;

  if (libres <= 0) {
    return { texto: 'Completo · lista de espera', tono: 'warn' };
  }

  if (libres <= 3) {
    return { texto: libres === 1 ? 'Última plaza' : `${libres} plazas`, tono: 'warn' };
  }

  return { texto: `${libres} plazas libres`, tono: 'ok' };
}

/** Iniciales para un avatar cuando no vienen calculadas del servidor. */
export function iniciales(nombre: string): string {
  const partes = nombre.trim().split(/\s+/);

  if (partes.length === 0 || partes[0] === '') {
    return '??';
  }

  return partes.length === 1
    ? partes[0].slice(0, 2).toUpperCase()
    : `${partes[0][0]}${partes[1][0]}`.toUpperCase();
}

function diferenciaEnDias(desde: Date, hasta: Date): number {
  const aMedianoche = (fecha: Date) => {
    const partes = new Intl.DateTimeFormat('en-CA', {
      year: 'numeric',
      month: '2-digit',
      day: '2-digit',
      timeZone: ZONA_HORARIA,
    }).format(fecha);

    return new Date(`${partes}T00:00:00Z`).getTime();
  };

  return Math.round((aMedianoche(hasta) - aMedianoche(desde)) / 86_400_000);
}
