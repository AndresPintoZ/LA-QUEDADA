import 'server-only';

import { borrarSesion, guardarSesion, leerSesion, type Sesion } from './sesion';
import type { ProblemaDeApi } from './tipos';

/**
 * Cliente de la API .NET. Solo se ejecuta en el servidor.
 *
 * Es el único punto por el que PlanVibe habla con su backend. Concentra aquí tres
 * cosas que, repartidas por las páginas, acabarían aplicándose de forma desigual:
 * añadir el token, renovarlo cuando caduca y traducir los errores.
 */

/** Margen con el que se considera caducado un token antes de que lo esté de verdad. */
const MARGEN_DE_RENOVACION_MS = 30_000;

function urlBase(): string {
  const url = process.env.API_URL_INTERNA;

  if (!url) {
    throw new Error('Falta API_URL_INTERNA. Es la dirección de la API en la red interna de Docker.');
  }

  return url.replace(/\/$/, '');
}

/** Error de la API ya traducido, con la información que la interfaz necesita para reaccionar. */
export class ErrorDeApi extends Error {
  constructor(
    readonly estado: number,
    mensaje: string,
    readonly codigo?: string,
    readonly errores?: Record<string, string[]>,
  ) {
    super(mensaje);
    this.name = 'ErrorDeApi';
  }

  /** La sesión no vale: hay que volver a iniciarla. */
  get requiereIniciarSesion(): boolean {
    return this.estado === 401;
  }
}

interface OpcionesDePeticion {
  metodo?: 'GET' | 'POST' | 'PUT' | 'DELETE';
  cuerpo?: unknown;
  /** Si es `false`, no se adjunta el token aunque haya sesión (endpoints públicos). */
  conSesion?: boolean;
  /** Segundos de caché de Next. Solo para datos públicos que pueden ir un poco desfasados. */
  revalidarEn?: number;
}

/**
 * Llama a la API y devuelve el cuerpo ya deserializado.
 *
 * Si el token de acceso está a punto de caducar, lo renueva antes de la llamada de
 * forma transparente. La persona no ve nunca una sesión cortada a mitad de un formulario.
 */
export async function llamarApi<T>(ruta: string, opciones: OpcionesDePeticion = {}): Promise<T> {
  const { metodo = 'GET', cuerpo, conSesion = true, revalidarEn } = opciones;

  const cabeceras: Record<string, string> = { Accept: 'application/json' };

  if (cuerpo !== undefined) {
    cabeceras['Content-Type'] = 'application/json';
  }

  if (conSesion) {
    const sesion = await obtenerSesionVigente();

    if (sesion) {
      cabeceras.Authorization = `Bearer ${sesion.tokenDeAcceso}`;
    }
  }

  const respuesta = await fetch(`${urlBase()}${ruta}`, {
    method: metodo,
    headers: cabeceras,
    body: cuerpo !== undefined ? JSON.stringify(cuerpo) : undefined,
    // Sin caché por defecto: la mayoría de respuestas dependen de quién pregunta.
    cache: revalidarEn === undefined ? 'no-store' : undefined,
    next: revalidarEn !== undefined ? { revalidate: revalidarEn } : undefined,
  });

  if (respuesta.status === 204) {
    return undefined as T;
  }

  if (!respuesta.ok) {
    throw await construirError(respuesta);
  }

  return (await respuesta.json()) as T;
}

/**
 * Devuelve la sesión con un token de acceso utilizable, renovándolo si hace falta.
 */
async function obtenerSesionVigente(): Promise<Sesion | null> {
  const sesion = await leerSesion();

  if (!sesion) {
    return null;
  }

  if (Date.now() < sesion.expiraEn - MARGEN_DE_RENOVACION_MS) {
    return sesion;
  }

  return renovarSesion(sesion);
}

/**
 * Rota el token de renovación.
 *
 * Si la API lo rechaza, se borra la cookie. Insistir con un token de renovación
 * inválido dispararía la detección de reutilización del servidor, que revocaría
 * todas las sesiones de la cuenta.
 */
async function renovarSesion(sesion: Sesion): Promise<Sesion | null> {
  try {
    const respuesta = await fetch(`${urlBase()}/api/identidad/sesion/renovar`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body: JSON.stringify({ tokenDeRenovacion: sesion.tokenDeRenovacion }),
      cache: 'no-store',
    });

    if (!respuesta.ok) {
      await borrarSesion();
      return null;
    }

    const tokens = (await respuesta.json()) as {
      tokenDeAcceso: string;
      expiraEn: string;
      tokenDeRenovacion: string;
      renovacionExpiraEn: string;
    };

    const renovada: Sesion = {
      tokenDeAcceso: tokens.tokenDeAcceso,
      expiraEn: new Date(tokens.expiraEn).getTime(),
      tokenDeRenovacion: tokens.tokenDeRenovacion,
      perfil: sesion.perfil,
    };

    await guardarSesion(renovada, new Date(tokens.renovacionExpiraEn));

    return renovada;
  } catch {
    await borrarSesion();
    return null;
  }
}

/**
 * Traduce la respuesta de error.
 *
 * Si el cuerpo no es un ProblemDetails legible, se usa un mensaje genérico en lugar
 * de mostrar lo que haya devuelto el servidor: podría contener detalles internos.
 */
async function construirError(respuesta: Response): Promise<ErrorDeApi> {
  let problema: ProblemaDeApi = {};

  try {
    problema = (await respuesta.json()) as ProblemaDeApi;
  } catch {
    // Respuesta sin cuerpo JSON: se sigue con el mensaje por defecto.
  }

  const mensaje = problema.detail ?? problema.title ?? mensajePorDefecto(respuesta.status);

  return new ErrorDeApi(respuesta.status, mensaje, problema.codigo, problema.errors);
}

function mensajePorDefecto(estado: number): string {
  switch (estado) {
    case 400:
      return 'Revisa los datos: hay algo que no encaja.';
    case 401:
      return 'Tu sesión ha caducado. Vuelve a iniciar sesión.';
    case 403:
      return 'No tienes permiso para hacer esto.';
    case 404:
      return 'No hemos encontrado lo que buscabas.';
    case 409:
      return 'Alguien se te ha adelantado. Vuelve a intentarlo.';
    case 429:
      return 'Has hecho demasiadas peticiones seguidas. Espera un momento.';
    default:
      return 'Algo ha ido mal. Inténtalo de nuevo en unos segundos.';
  }
}
