import 'server-only';

import { llamarApi } from './api-servidor';
import { leerSesion } from './sesion';
import type { Categoria, DetalleDePlan, PaginaDe, ResumenDePlan } from './tipos';

/**
 * Lecturas que hacen los componentes de servidor.
 *
 * Se llaman directamente desde las páginas: no hay estado de carga ni de error que
 * gestionar en el cliente porque el HTML llega ya con los datos dentro. Es lo que
 * hace que la primera pantalla se pinte rápido en una conexión móvil (RNF-02).
 */

export interface FiltrosDeExploracion {
  texto?: string;
  categorias?: string[];
  desde?: string;
  hasta?: string;
  latitud?: number;
  longitud?: number;
  radioEnMetros?: number;
  soloConPlazas?: boolean;
  pagina?: number;
}

/** Centro del piloto: la Plaza del Mercado Chico de Ávila. */
export const CENTRO_DEL_PILOTO = { latitud: 40.6565, longitud: -4.7009 } as const;

export async function buscarPlanes(filtros: FiltrosDeExploracion = {}): Promise<PaginaDe<ResumenDePlan>> {
  const parametros = new URLSearchParams();

  if (filtros.texto) parametros.set('texto', filtros.texto);
  if (filtros.desde) parametros.set('desde', filtros.desde);
  if (filtros.hasta) parametros.set('hasta', filtros.hasta);
  if (filtros.latitud !== undefined) parametros.set('latitud', String(filtros.latitud));
  if (filtros.longitud !== undefined) parametros.set('longitud', String(filtros.longitud));
  if (filtros.radioEnMetros !== undefined) parametros.set('radio', String(filtros.radioEnMetros));
  if (filtros.soloConPlazas) parametros.set('soloConPlazas', 'true');
  if (filtros.pagina) parametros.set('pagina', String(filtros.pagina));

  // Cada categoría va como un parámetro repetido: es lo que espera el enlazador de la API.
  for (const categoria of filtros.categorias ?? []) {
    parametros.append('categorias', categoria);
  }

  return llamarApi<PaginaDe<ResumenDePlan>>(`/api/quedadas?${parametros.toString()}`);
}

export async function obtenerPlan(id: string): Promise<DetalleDePlan> {
  return llamarApi<DetalleDePlan>(`/api/quedadas/${id}`);
}

export async function obtenerMisPlanes(): Promise<ResumenDePlan[]> {
  return llamarApi<ResumenDePlan[]>('/api/quedadas/mios');
}

/**
 * Catálogo de categorías.
 *
 * Es de los pocos datos que se cachean: cambia muy de vez en cuando y es igual para
 * todo el mundo, así que no tiene sentido pedirlo en cada visita.
 */
export async function obtenerCategorias(): Promise<Categoria[]> {
  return llamarApi<Categoria[]>('/api/categorias', { conSesion: false, revalidarEn: 300 });
}

/** Perfil de quien está navegando, o `null` si no ha iniciado sesión. */
export async function obtenerMiPerfil() {
  const sesion = await leerSesion();

  return sesion?.perfil ?? null;
}
