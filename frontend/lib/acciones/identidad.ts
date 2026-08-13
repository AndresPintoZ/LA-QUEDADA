'use server';

import { redirect } from 'next/navigation';
import { revalidatePath } from 'next/cache';
import { z } from 'zod';

import { ErrorDeApi, llamarApi } from '../api-servidor';
import { borrarSesion, guardarSesion, leerSesion, type PerfilDeSesion } from '../sesion';

/**
 * Acciones de registro y sesión.
 *
 * Son acciones de servidor: se invocan desde un formulario y se ejecutan en el
 * servidor de Next. Next comprueba el origen de la petición automáticamente, así
 * que no hace falta gestionar un token CSRF a mano.
 *
 * Ninguna acción devuelve nunca el token al cliente: se guarda cifrado en la cookie.
 */

/** Versión de las normas de comunidad que se está mostrando en el registro. */
const VERSION_DE_NORMAS = '2026-08';

const LONGITUD_MINIMA_DE_CONTRASENA = 12;

export interface EstadoDeFormulario {
  /** Mensaje general de error, ya redactado para mostrar. */
  error?: string;
  /** Errores por campo, para pintarlos junto a cada entrada. */
  errores?: Record<string, string[]>;
}

const esquemaDeRegistro = z.object({
  correo: z.string().trim().min(1, 'Escribe tu correo electrónico.').email('Ese correo no parece válido.'),
  contrasena: z
    .string()
    .min(LONGITUD_MINIMA_DE_CONTRASENA, `Usa al menos ${LONGITUD_MINIMA_DE_CONTRASENA} caracteres. Una frase que recuerdes vale.`),
  nombreVisible: z.string().trim().min(2, 'Escribe el nombre con el que quieres que te vean.').max(60),
  ciudad: z.string().trim().max(80).optional(),
  anioDeNacimiento: z.coerce
    .number()
    .int()
    .min(1900, 'Indica tu año de nacimiento.')
    .max(new Date().getFullYear(), 'Indica tu año de nacimiento.'),
  aceptaNormas: z.literal('on', { errorMap: () => ({ message: 'Hay que aceptar las normas de la comunidad.' }) }),
});

export async function registrarse(_estado: EstadoDeFormulario, datos: FormData): Promise<EstadoDeFormulario> {
  const validacion = esquemaDeRegistro.safeParse(Object.fromEntries(datos));

  if (!validacion.success) {
    // Se valida también en el servidor de la API. Esta comprobación no sustituye
    // a aquella: solo evita un viaje de ida y vuelta por un campo vacío.
    return { errores: validacion.error.flatten().fieldErrors as Record<string, string[]> };
  }

  const { correo, contrasena, nombreVisible, ciudad, anioDeNacimiento } = validacion.data;

  try {
    await llamarApi('/api/identidad/registro', {
      metodo: 'POST',
      conSesion: false,
      cuerpo: {
        correo,
        contrasena,
        nombreVisible,
        ciudad: ciudad || null,
        anioDeNacimiento,
        versionNormasAceptada: VERSION_DE_NORMAS,
      },
    });
  } catch (error) {
    return traducirError(error);
  }

  // El registro deja la sesión iniciada: pedir la contraseña otra vez justo después
  // de escribirla dos veces es un paso extra que no protege nada.
  const inicio = await iniciarSesionInterno(correo, contrasena);

  if (inicio) {
    return inicio;
  }

  redirect('/onboarding');
}

const esquemaDeInicioDeSesion = z.object({
  correo: z.string().trim().min(1, 'Escribe tu correo.'),
  contrasena: z.string().min(1, 'Escribe tu contraseña.'),
});

export async function iniciarSesion(_estado: EstadoDeFormulario, datos: FormData): Promise<EstadoDeFormulario> {
  const validacion = esquemaDeInicioDeSesion.safeParse(Object.fromEntries(datos));

  if (!validacion.success) {
    return { errores: validacion.error.flatten().fieldErrors as Record<string, string[]> };
  }

  const resultado = await iniciarSesionInterno(validacion.data.correo, validacion.data.contrasena);

  if (resultado) {
    return resultado;
  }

  redirect('/explorar');
}

/** Devuelve un estado de error, o `undefined` si la sesión se abrió correctamente. */
async function iniciarSesionInterno(correo: string, contrasena: string): Promise<EstadoDeFormulario | undefined> {
  try {
    const respuesta = await llamarApi<{
      tokens: { tokenDeAcceso: string; expiraEn: string; tokenDeRenovacion: string; renovacionExpiraEn: string };
      perfil: PerfilDeSesion;
    }>('/api/identidad/sesion', {
      metodo: 'POST',
      conSesion: false,
      cuerpo: { correo, contrasena, dispositivo: 'web' },
    });

    await guardarSesion(
      {
        tokenDeAcceso: respuesta.tokens.tokenDeAcceso,
        expiraEn: new Date(respuesta.tokens.expiraEn).getTime(),
        tokenDeRenovacion: respuesta.tokens.tokenDeRenovacion,
        perfil: respuesta.perfil,
      },
      new Date(respuesta.tokens.renovacionExpiraEn),
    );

    return undefined;
  } catch (error) {
    return traducirError(error);
  }
}

export async function cerrarSesion(): Promise<void> {
  const sesion = await leerSesion();

  if (sesion) {
    try {
      // Se avisa a la API para que revoque el token de renovación en su base de datos.
      // Borrar solo la cookia dejaría el token vivo y utilizable por quien lo tuviera.
      await llamarApi('/api/identidad/sesion/cerrar', {
        metodo: 'POST',
        conSesion: false,
        cuerpo: { tokenDeRenovacion: sesion.tokenDeRenovacion },
      });
    } catch {
      // Si la API no responde, se borra la cookie igualmente: para la persona,
      // cerrar sesión tiene que funcionar siempre.
    }
  }

  await borrarSesion();
  revalidatePath('/', 'layout');
  redirect('/');
}

/** Arranca la verificación de organizador y lleva a la pasarela del proveedor. */
export async function iniciarVerificacion(): Promise<EstadoDeFormulario | void> {
  let destino: string;

  try {
    const sesion = await llamarApi<{ referenciaExterna: string; urlDeRedireccion: string }>('/api/identidad/verificacion', {
      metodo: 'POST',
    });

    destino = sesion.urlDeRedireccion;
  } catch (error) {
    return traducirError(error);
  }

  redirect(destino);
}

/** Cierra la verificación consultando el resultado al proveedor. */
export async function completarVerificacion(referencia: string): Promise<EstadoDeFormulario | void> {
  try {
    await llamarApi<{ estado: string }>('/api/identidad/verificacion/completar', {
      metodo: 'POST',
      cuerpo: { referenciaExterna: referencia },
    });
  } catch (error) {
    return traducirError(error);
  }

  // El perfil de la cookie tiene ya `puedeOrganizar` desactualizado: se refresca
  // pidiéndolo de nuevo, para que la interfaz muestre el botón de crear.
  await refrescarPerfilEnSesion();

  revalidatePath('/perfil');
  redirect('/crear');
}

/** Vuelve a leer el perfil de la API y lo guarda en la cookie. */
export async function refrescarPerfilEnSesion(): Promise<void> {
  const sesion = await leerSesion();

  if (!sesion) {
    return;
  }

  try {
    const perfil = await llamarApi<PerfilDeSesion>('/api/identidad/yo');

    await guardarSesion({ ...sesion, perfil }, new Date(Date.now() + 14 * 86_400_000));
  } catch {
    // Si falla, se conserva el perfil anterior: no es motivo para cerrar la sesión.
  }
}

function traducirError(error: unknown): EstadoDeFormulario {
  if (error instanceof ErrorDeApi) {
    return { error: error.message, errores: error.errores };
  }

  // Un error inesperado no se muestra tal cual: podría contener detalles internos.
  return { error: 'No hemos podido completar la operación. Inténtalo de nuevo.' };
}
