/**
 * Tipos del contrato con la API .NET.
 *
 * Están escritos a mano y no generados a partir de OpenAPI para que el equipo de
 * frontend controle qué parte del contrato consume. Si la API cambia y esto no,
 * lo detectan las pruebas de contrato descritas en `docs/07-estrategia-de-pruebas.md`.
 */

export type EstadoQuedada = 'Publicada' | 'Cancelada' | 'Finalizada' | 'OcultaPorModeracion';

export type EstadoAsistencia = 'Confirmada' | 'EnListaDeEspera' | 'Retirada';

/** Tarjeta de la lista de explorar. */
export interface ResumenDePlan {
  id: string;
  titulo: string;
  categoria: string;
  inicio: string;
  lugar: string;
  latitud: number;
  longitud: number;
  /** Distancia al centro de búsqueda, si se indicó uno. */
  distanciaEnMetros: number | null;
  capacidad: number;
  plazasOcupadas: number;
  estado: EstadoQuedada;
  organizadorNombre: string;
  organizadorVerificado: boolean;
}

export interface OrganizadorDeLectura {
  id: string;
  nombre: string;
  iniciales: string;
  verificado: boolean;
  quedadasOrganizadas: number;
}

/** Detalle completo de un plan. */
export interface DetalleDePlan {
  id: string;
  titulo: string;
  descripcion: string;
  categoria: string;
  inicio: string;
  fin: string;
  lugar: string;
  referencia: string | null;
  /** Solo llega con valor si tienes plaza confirmada. La decisión la toma el servidor. */
  direccionExacta: string | null;
  latitud: number;
  longitud: number;
  capacidad: number;
  plazasOcupadas: number;
  enListaDeEspera: number;
  estado: EstadoQuedada;
  motivoDeCancelacion: string | null;
  normas: string[];
  organizador: OrganizadorDeLectura;
  miAsistencia: EstadoAsistencia | null;
  miPosicionEnListaDeEspera: number | null;
}

export interface PaginaDe<T> {
  elementos: T[];
  total: number;
  pagina: number;
  tamanoDePagina: number;
  hayMas: boolean;
}

export interface Categoria {
  id: string;
  nombre: string;
  clave: string;
  color: string;
}

export interface ResultadoDeInscripcion {
  confirmada: boolean;
  posicionEnListaDeEspera: number | null;
}

/** Respuesta de error de la API, en formato ProblemDetails (RFC 9457). */
export interface ProblemaDeApi {
  title?: string;
  detail?: string;
  status?: number;
  /** Código estable del dominio, p. ej. `quedada.ya_apuntado`. */
  codigo?: string;
  /** Errores de validación agrupados por campo. */
  errors?: Record<string, string[]>;
  traceId?: string;
}
