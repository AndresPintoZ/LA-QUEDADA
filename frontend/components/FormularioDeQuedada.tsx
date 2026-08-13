'use client';

import { useActionState } from 'react';

import { crearQuedada, type EstadoDeAccion } from '@/lib/acciones/quedadas';
import type { Categoria } from '@/lib/tipos';
import { Campo, CampoLargo } from './Campo';

/**
 * Formulario de creación de un plan (RF-10).
 *
 * Está agrupado en tres bloques —qué, cuándo y dónde, y condiciones— siguiendo la
 * división en pasos cortos de `docs/03-diseno-visual.md`, pero en una sola página con
 * secciones en lugar de en un asistente de varios pasos. La razón es práctica: un
 * asistente obliga a guardar estado intermedio y, si alguien recarga a mitad, pierde
 * lo escrito. Con un solo formulario, el navegador conserva los valores.
 */

const ESTADO_INICIAL: EstadoDeAccion = {};

/** Coordenadas del centro de Ávila, como punto de partida del formulario. */
const CENTRO_DE_AVILA = { latitud: 40.6565, longitud: -4.7009 };

export default function FormularioDeQuedada({ categorias }: { categorias: Categoria[] }) {
  const [estado, enviar, enCurso] = useActionState(crearQuedada, ESTADO_INICIAL);

  return (
    <form action={enviar} className="flex flex-col gap-6" noValidate>
      {estado.error && (
        <p role="alert" className="rounded-[14px] bg-danger-bg px-3.5 py-3 text-sm text-danger">
          {estado.error}
        </p>
      )}

      <fieldset className="flex flex-col gap-4">
        <legend className="pb-2 font-display text-base font-bold">1. De qué va el plan</legend>

        <Campo
          nombre="titulo"
          etiqueta="Título"
          requerido
          placeholder="Ruta en bici por el Valle Amblés"
          ayuda="Que se entienda de un vistazo en la lista."
          errores={estado.errores?.titulo}
        />

        <div className="flex flex-col gap-1.5">
          <label htmlFor="categoriaId" className="text-sm font-semibold text-ink">
            Categoría <span className="text-danger">*</span>
          </label>

          <select
            id="categoriaId"
            name="categoriaId"
            required
            defaultValue=""
            className="rounded-[14px] border border-line bg-white px-3.5 py-3.5 text-base text-ink focus-visible:ring-2 focus-visible:ring-brand"
          >
            <option value="" disabled>
              Elige una categoría
            </option>
            {categorias.map((categoria) => (
              <option key={categoria.id} value={categoria.id}>
                {categoria.nombre}
              </option>
            ))}
          </select>

          {estado.errores?.categoriaId && (
            <p role="alert" className="text-[13px] font-medium text-danger">
              {estado.errores.categoriaId[0]}
            </p>
          )}
        </div>

        <CampoLargo
          nombre="descripcion"
          etiqueta="Descripción"
          placeholder="Pedaleamos 35 km sin prisa. Si te descuelgas, te esperamos."
          ayuda="Cuenta el nivel, el ritmo y lo que hace falta llevar."
          errores={estado.errores?.descripcion}
        />
      </fieldset>

      <fieldset className="flex flex-col gap-4 border-t border-line pt-5">
        <legend className="pb-2 font-display text-base font-bold">2. Cuándo y dónde</legend>

        <div className="grid grid-cols-2 gap-3">
          <Campo nombre="fecha" etiqueta="Día" tipo="date" requerido errores={estado.errores?.fecha} />
          <Campo nombre="hora" etiqueta="Hora" tipo="time" requerido errores={estado.errores?.hora} />
        </div>

        <Campo
          nombre="duracionEnMinutos"
          etiqueta="Duración (minutos)"
          tipo="number"
          requerido
          defaultValue={120}
          ayuda="Aproximada. Sirve para que aparezca bien en el calendario."
          errores={estado.errores?.duracionEnMinutos}
        />

        <Campo
          nombre="lugar"
          etiqueta="Punto de encuentro"
          requerido
          placeholder="Puente Adaja"
          ayuda="Nombre público del sitio. Lo ve todo el mundo."
          errores={estado.errores?.lugar}
        />

        <Campo
          nombre="referencia"
          etiqueta="Referencia para encontraros"
          placeholder="Junto al quiosco"
          errores={estado.errores?.referencia}
        />

        <Campo
          nombre="direccionExacta"
          etiqueta="Dirección exacta"
          placeholder="Av. de Juan Carlos I, 12"
          // Es la regla de privacidad del punto de encuentro, y conviene decirla
          // aquí para que quien publica sepa quién va a ver este dato.
          ayuda="Solo la ven quienes tengan plaza confirmada, nunca el público general."
          errores={estado.errores?.direccionExacta}
        />

        {/* Las coordenadas son campos ocultos que rellena el selector de mapa.
            Se parte del centro de Ávila para que el formulario sea válido desde el inicio. */}
        <input type="hidden" name="latitud" defaultValue={CENTRO_DE_AVILA.latitud} />
        <input type="hidden" name="longitud" defaultValue={CENTRO_DE_AVILA.longitud} />

        <label className="flex items-start gap-3 rounded-[14px] bg-paper px-3.5 py-3">
          <input type="checkbox" name="confirmaQueEsLugarPublico" required className="mt-0.5 h-5 w-5 shrink-0 accent-[#0B7C9B]" />
          <span className="text-[13px] leading-snug text-body">
            Confirmo que el punto de encuentro es un <strong>lugar público</strong>. No se permiten
            domicilios particulares.
          </span>
        </label>

        {estado.errores?.confirmaQueEsLugarPublico && (
          <p role="alert" className="text-[13px] font-medium text-danger">
            {estado.errores.confirmaQueEsLugarPublico[0]}
          </p>
        )}
      </fieldset>

      <fieldset className="flex flex-col gap-4 border-t border-line pt-5">
        <legend className="pb-2 font-display text-base font-bold">3. Plazas y normas</legend>

        <Campo
          nombre="capacidad"
          etiqueta="Cuántas personas caben"
          tipo="number"
          requerido
          defaultValue={10}
          ayuda="Te incluye a ti. Si se llena, la gente entra en lista de espera."
          errores={estado.errores?.capacidad}
        />

        <CampoLargo
          nombre="normas"
          etiqueta="Normas del plan"
          filas={4}
          placeholder={'Casco obligatorio\nNivel medio\n+16 años'}
          ayuda="Una por línea. Indica coste, material, edad mínima o nivel si aplica."
          errores={estado.errores?.normas}
        />
      </fieldset>

      <button
        type="submit"
        disabled={enCurso}
        className="w-full rounded-2xl bg-brand px-4 py-4 text-[17px] font-bold text-white disabled:opacity-60"
      >
        {enCurso ? 'Publicando…' : 'Publicar el plan'}
      </button>

      <p className="pb-4 text-center text-[13px] text-muted">
        Al publicar aceptas las normas de la comunidad. Puedes editar o cancelar el plan más adelante.
      </p>
    </form>
  );
}
