'use server';

import { revalidatePath } from 'next/cache';
import { redirect } from 'next/navigation';
import { z } from 'zod';

import { ErrorDeApi, llamarApi } from '../api-servidor';
import type { ResultadoDeInscripcion } from '../tipos';

/** Acciones de participación y publicación de planes. */

export interface EstadoDeAccion {
  error?: string;
  errores?: Record<string, string[]>;
  mensaje?: string;
}

/**
 * Apuntarse a un plan (RF-14).
 *
 * Devuelve un mensaje distinto según se obtenga plaza o se entre en lista de espera:
 * la persona necesita saber cuál de las dos cosas ha pasado antes de contar con el plan.
 */
export async function apuntarse(quedadaId: string): Promise<EstadoDeAccion> {
  try {
    const resultado = await llamarApi<ResultadoDeInscripcion>(`/api/quedadas/${quedadaId}/asistencia`, {
      metodo: 'POST',
    });

    revalidatePath(`/plan/${quedadaId}`);
    revalidatePath('/mis-planes');

    return {
      mensaje: resultado.confirmada
        ? '¡Tienes plaza! Ya puedes ver el punto de encuentro exacto.'
        : `Estás en lista de espera, en la posición ${resultado.posicionEnListaDeEspera}. Te avisamos si se libera una plaza.`,
    };
  } catch (error) {
    return traducirError(error);
  }
}

export async function abandonar(quedadaId: string): Promise<EstadoDeAccion> {
  try {
    await llamarApi(`/api/quedadas/${quedadaId}/asistencia`, { metodo: 'DELETE' });

    revalidatePath(`/plan/${quedadaId}`);
    revalidatePath('/mis-planes');

    return { mensaje: 'Ya no vas a este plan.' };
  } catch (error) {
    return traducirError(error);
  }
}

const esquemaDeCancelacion = z.object({
  motivo: z
    .string()
    .trim()
    .min(1, 'Explica por qué lo cancelas: quien se había apuntado merece saberlo.')
    .max(300, 'El motivo es demasiado largo.'),
});

export async function cancelarQuedada(quedadaId: string, datos: FormData): Promise<EstadoDeAccion> {
  const validacion = esquemaDeCancelacion.safeParse(Object.fromEntries(datos));

  if (!validacion.success) {
    return { errores: validacion.error.flatten().fieldErrors as Record<string, string[]> };
  }

  try {
    await llamarApi(`/api/quedadas/${quedadaId}/cancelacion`, {
      metodo: 'POST',
      cuerpo: { motivo: validacion.data.motivo },
    });

    revalidatePath(`/plan/${quedadaId}`);
    revalidatePath('/mis-planes');

    return { mensaje: 'Plan cancelado. Hemos avisado a quien se había apuntado.' };
  } catch (error) {
    return traducirError(error);
  }
}

const esquemaDeCreacion = z.object({
  titulo: z.string().trim().min(3, 'Ponle un título al plan.').max(120),
  descripcion: z.string().trim().max(2000).optional().default(''),
  categoriaId: z.string().uuid('Elige una categoría.'),

  // La fecha y la hora llegan en hora local; se convierten a UTC antes de enviarlas.
  fecha: z.string().min(1, 'Elige el día.'),
  hora: z.string().min(1, 'Elige la hora.'),
  duracionEnMinutos: z.coerce.number().int().min(15, 'Indica cuánto va a durar.').max(1440),

  lugar: z.string().trim().min(3, 'Indica dónde quedáis.').max(120),
  referencia: z.string().trim().max(200).optional(),
  direccionExacta: z.string().trim().max(200).optional(),
  latitud: z.coerce.number().min(-90).max(90),
  longitud: z.coerce.number().min(-180).max(180),

  confirmaQueEsLugarPublico: z.literal('on', {
    errorMap: () => ({
      message: 'El punto de encuentro debe ser un lugar público. No se permiten domicilios particulares.',
    }),
  }),

  capacidad: z.coerce.number().int().min(2, 'Mínimo 2 personas.').max(500, 'Máximo 500 personas.'),
  normas: z.string().optional(),
});

/** Publica una quedada (RF-09, RF-10). */
export async function crearQuedada(_estado: EstadoDeAccion, datos: FormData): Promise<EstadoDeAccion> {
  const validacion = esquemaDeCreacion.safeParse(Object.fromEntries(datos));

  if (!validacion.success) {
    return { errores: validacion.error.flatten().fieldErrors as Record<string, string[]> };
  }

  const valores = validacion.data;
  const inicio = new Date(`${valores.fecha}T${valores.hora}`);

  if (Number.isNaN(inicio.getTime())) {
    return { errores: { fecha: ['La fecha o la hora no son válidas.'] } };
  }

  if (inicio.getTime() <= Date.now()) {
    return { errores: { fecha: ['El plan tiene que ser en el futuro.'] } };
  }

  let id: string;

  try {
    const respuesta = await llamarApi<{ id: string }>('/api/quedadas', {
      metodo: 'POST',
      cuerpo: {
        titulo: valores.titulo,
        descripcion: valores.descripcion,
        categoriaId: valores.categoriaId,
        inicio: inicio.toISOString(),
        duracionEnMinutos: valores.duracionEnMinutos,
        lugar: valores.lugar,
        referencia: valores.referencia || null,
        direccionExacta: valores.direccionExacta || null,
        latitud: valores.latitud,
        longitud: valores.longitud,
        confirmaQueEsLugarPublico: true,
        capacidad: valores.capacidad,
        // Las normas llegan una por línea desde un área de texto.
        normas: (valores.normas ?? '')
          .split('\n')
          .map((n) => n.trim())
          .filter(Boolean),
      },
    });

    id = respuesta.id;
  } catch (error) {
    return traducirError(error);
  }

  revalidatePath('/explorar');
  revalidatePath('/mis-planes');
  redirect(`/plan/${id}`);
}

function traducirError(error: unknown): EstadoDeAccion {
  if (error instanceof ErrorDeApi) {
    return { error: error.message, errores: error.errores };
  }

  return { error: 'No hemos podido completar la operación. Inténtalo de nuevo.' };
}
