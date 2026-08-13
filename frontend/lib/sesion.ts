import 'server-only';

import { cookies } from 'next/headers';
import { EncryptJWT, jwtDecrypt } from 'jose';

/**
 * Gestión de la sesión en el BFF.
 *
 * Los tokens que emite la API .NET NUNCA llegan al navegador. Se guardan cifrados
 * dentro de una cookie httpOnly que solo el servidor de Next puede leer y descifrar.
 *
 * Por qué así:
 *
 * - `httpOnly` impide que JavaScript lea la cookie. Un XSS que consiguiera ejecutarse
 *   podría hacer peticiones en nombre de la persona, pero no llevarse la sesión a otro
 *   sitio ni reutilizarla desde fuera del navegador.
 * - El cifrado (no solo la firma) hace que el contenido sea opaco incluso si la cookie
 *   se filtra en un registro o en una captura de pantalla.
 * - `SameSite=Lax` evita que la cookie viaje en peticiones lanzadas desde otro sitio web,
 *   que es la base de los ataques de falsificación de petición (CSRF).
 */

/** Nombre de la cookie. El prefijo `__Host-` exige a la vez https, ruta `/` y ningún dominio. */
const NOMBRE_DE_COOKIE = process.env.NODE_ENV === 'production' ? '__Host-planvibe_sesion' : 'planvibe_sesion';

/** Longitud mínima del secreto. Menos de esto y el cifrado no aporta seguridad real. */
const LONGITUD_MINIMA_DEL_SECRETO = 32;

export interface Sesion {
  /** Token de acceso que se envía a la API .NET. Vive quince minutos. */
  tokenDeAcceso: string;
  /** Momento de caducidad del token de acceso, en milisegundos desde época. */
  expiraEn: number;
  /** Token de renovación, de un solo uso. */
  tokenDeRenovacion: string;
  /** Datos mínimos del perfil, para pintar la interfaz sin una petición extra. */
  perfil: PerfilDeSesion;
}

export interface PerfilDeSesion {
  id: string;
  nombreVisible: string;
  correo: string;
  ciudad: string | null;
  roles: string[];
  estadoVerificacion: string;
  puedeOrganizar: boolean;
}

function obtenerClave(): Uint8Array {
  const secreto = process.env.SESION_SECRETO;

  if (!secreto || secreto.length < LONGITUD_MINIMA_DEL_SECRETO) {
    // Se falla al arrancar y no en silencio: una sesión mal cifrada es peor que
    // no tener sesión, porque da una falsa sensación de estar protegida.
    throw new Error(
      `Falta SESION_SECRETO o es demasiado corto (mínimo ${LONGITUD_MINIMA_DEL_SECRETO} caracteres). Revisa el archivo .env.`,
    );
  }

  // A256GCM necesita exactamente 32 bytes de clave.
  return new TextEncoder().encode(secreto).slice(0, 32);
}

/** Cifra la sesión y la guarda en la cookie. */
export async function guardarSesion(sesion: Sesion, expiraLaRenovacionEn: Date): Promise<void> {
  const cifrada = await new EncryptJWT({ ...sesion })
    .setProtectedHeader({ alg: 'dir', enc: 'A256GCM' })
    .setIssuedAt()
    .setExpirationTime(Math.floor(expiraLaRenovacionEn.getTime() / 1000))
    .encrypt(obtenerClave());

  const almacen = await cookies();

  almacen.set(NOMBRE_DE_COOKIE, cifrada, {
    httpOnly: true,
    secure: process.env.NODE_ENV === 'production',
    sameSite: 'lax',
    path: '/',
    expires: expiraLaRenovacionEn,
  });
}

/** Lee y descifra la sesión. Devuelve `null` si no hay cookie o si no es válida. */
export async function leerSesion(): Promise<Sesion | null> {
  const almacen = await cookies();
  const cookie = almacen.get(NOMBRE_DE_COOKIE);

  if (!cookie?.value) {
    return null;
  }

  try {
    const { payload } = await jwtDecrypt(cookie.value, obtenerClave());

    return payload as unknown as Sesion;
  } catch {
    // Cookie manipulada, caducada o cifrada con un secreto anterior. Se trata
    // igual que si no hubiera sesión: no se distingue el motivo hacia fuera.
    return null;
  }
}

/** Borra la cookie de sesión. */
export async function borrarSesion(): Promise<void> {
  const almacen = await cookies();

  almacen.delete(NOMBRE_DE_COOKIE);
}
