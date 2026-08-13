import { describe, expect, it } from 'vitest';

import { cuandoCorto, distancia, estadoDePlazas, iniciales } from './formato';

/**
 * Pruebas del formateo.
 *
 * Parece código trivial, pero es donde se cuelan los errores que nadie ve hasta que
 * alguien se presenta un día tarde a un plan: un cálculo de «mañana» hecho con la zona
 * horaria equivocada, o un redondeo de distancia que dice 0 m cuando son 40.
 */

describe('cuandoCorto', () => {
  // Referencia fija: martes 15 de septiembre de 2026 a las 12:00 en Madrid.
  const ahora = new Date('2026-09-15T10:00:00Z');

  it('dice HOY para un plan del mismo día', () => {
    expect(cuandoCorto('2026-09-15T17:00:00Z', ahora)).toBe('HOY 19:00');
  });

  it('dice MAÑANA para el día siguiente', () => {
    expect(cuandoCorto('2026-09-16T08:00:00Z', ahora)).toBe('MAÑANA 10:00');
  });

  it('usa el día de la semana a partir de dos días', () => {
    // 19 de septiembre de 2026 es sábado.
    expect(cuandoCorto('2026-09-19T17:00:00Z', ahora)).toMatch(/^SÁB 19 · 19:00$/);
  });

  it('cuenta los días por la zona horaria del piloto, no por UTC', () => {
    // 23:30 hora de Madrid es el día 15; en UTC ya sería el 15 a las 21:30, mismo día.
    // El caso que importa: 00:30 del día 16 en Madrid es 22:30 del 15 en UTC.
    // Si se contara en UTC, este plan aparecería como «HOY» en lugar de «MAÑANA».
    expect(cuandoCorto('2026-09-15T22:30:00Z', ahora)).toBe('MAÑANA 00:30');
  });
});

describe('distancia', () => {
  it('devuelve null cuando no hay centro de búsqueda', () => {
    expect(distancia(null)).toBeNull();
  });

  it('usa metros por debajo del kilómetro', () => {
    expect(distancia(600)).toBe('600 m');
  });

  it('redondea a la decena para no dar una precisión que no tenemos', () => {
    // Las coordenadas se guardan con precisión de aproximadamente un metro:
    // anunciar «637 m» sugeriría una exactitud que no existe.
    expect(distancia(637)).toBe('640 m');
  });

  it('usa kilómetros con coma decimal a partir de mil metros', () => {
    expect(distancia(1200)).toBe('1,2 km');
  });
});

describe('estadoDePlazas', () => {
  it('avisa cuando queda una sola plaza', () => {
    const estado = estadoDePlazas(10, 9, 'Publicada');

    expect(estado.texto).toBe('Última plaza');
    expect(estado.tono).toBe('warn');
  });

  it('indica lista de espera cuando está completo', () => {
    const estado = estadoDePlazas(10, 10, 'Publicada');

    expect(estado.texto).toContain('Completo');
    expect(estado.tono).toBe('warn');
  });

  it('un plan cancelado lo dice con texto, no solo con color', () => {
    // El color nunca es el único indicador de estado (requisito de accesibilidad).
    const estado = estadoDePlazas(10, 3, 'Cancelada');

    expect(estado.texto).toBe('Cancelado');
    expect(estado.tono).toBe('danger');
  });

  it('nunca muestra un número negativo de plazas', () => {
    // La capacidad puede reducirse por debajo de las plazas concedidas en datos antiguos.
    const estado = estadoDePlazas(5, 8, 'Publicada');

    expect(estado.texto).not.toContain('-');
  });
});

describe('iniciales', () => {
  it('toma la inicial del nombre y del apellido', () => {
    expect(iniciales('Lucía Ramos')).toBe('LR');
  });

  it('usa las dos primeras letras si solo hay una palabra', () => {
    expect(iniciales('Ávila')).toBe('ÁV');
  });

  it('no revienta con un nombre vacío', () => {
    expect(iniciales('   ')).toBe('??');
  });
});
