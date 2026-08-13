import Link from 'next/link';
import { notFound } from 'next/navigation';

import AppShell from '@/components/AppShell';
import BotonDeAsistencia from '@/components/BotonDeAsistencia';
import MapaDelPunto from '@/components/MapaDelPunto';
import { Badge, Etiqueta, VerificadoBadge } from '@/components/ui';
import { obtenerMiPerfil, obtenerPlan } from '@/lib/datos';
import { ErrorDeApi } from '@/lib/api-servidor';
import { estadoDePlazas, fechaLarga } from '@/lib/formato';

/**
 * Detalle de un plan (RF-07).
 *
 * La dirección exacta del punto de encuentro solo llega en la respuesta si quien
 * consulta tiene plaza confirmada. Esa decisión la toma el servidor, no esta vista:
 * aquí basta con comprobar si el campo viene con valor.
 *
 * Responsive: una columna en móvil con el botón de apuntarse fijado abajo; en
 * escritorio, dos columnas con la acción y el organizador en una tarjeta lateral
 * que queda a la vista mientras se lee la descripción.
 */

interface Parametros {
  params: Promise<{ id: string }>;
}

export default async function DetalleDelPlan({ params }: Parametros) {
  const { id } = await params;

  let plan;

  try {
    plan = await obtenerPlan(id);
  } catch (error) {
    if (error instanceof ErrorDeApi && error.estado === 404) {
      notFound();
    }

    throw error;
  }

  const perfil = await obtenerMiPerfil();
  const estado = estadoDePlazas(plan.capacidad, plan.plazasOcupadas, plan.estado);
  const soyElOrganizador = perfil?.id === plan.organizador.id;

  const accion = (
    <BotonDeAsistencia
      quedadaId={plan.id}
      estadoDelPlan={plan.estado}
      miAsistencia={plan.miAsistencia}
      miPosicionEnListaDeEspera={plan.miPosicionEnListaDeEspera}
      hayPlazas={plan.capacidad - plan.plazasOcupadas > 0}
      soyElOrganizador={soyElOrganizador}
      haIniciadoSesion={perfil !== null}
    />
  );

  const tarjetaDelOrganizador = (
    <div className="flex items-center gap-3 rounded-[18px] border border-line bg-white px-3.5 py-3">
      <span
        className="flex h-10 w-10 items-center justify-center rounded-full bg-brand-tint font-bold text-brand-dark"
        aria-hidden
      >
        {plan.organizador.iniciales}
      </span>

      <div className="flex flex-1 flex-col">
        <span className="flex items-center gap-1.5">
          <span className="text-[15px] font-bold">{plan.organizador.nombre}</span>
          {plan.organizador.verificado && <VerificadoBadge />}
        </span>
        <span className="text-xs text-muted">
          {plan.organizador.quedadasOrganizadas}{' '}
          {plan.organizador.quedadasOrganizadas === 1 ? 'plan organizado' : 'planes organizados'}
        </span>
      </div>
    </div>
  );

  return (
    <AppShell
      conNav={false}
      anchura="ancha"
      // En móvil, la acción principal queda siempre a mano en el pie. En escritorio
      // vive en la columna lateral, así que el pie solo existe en pantallas pequeñas.
      pie={<div className="px-5 pb-6 pt-3.5 md:hidden">{accion}</div>}
    >
      <div className="p-0 md:px-6 md:py-6">
        {/* Cabecera con el color de la categoría. */}
        <div className="relative flex h-[150px] items-end bg-brand-tint px-5 pb-4 md:rounded-[22px]">
          <Link
            href="/explorar"
            aria-label="Volver a explorar"
            className="absolute left-5 top-5 flex h-9 w-9 items-center justify-center rounded-full bg-white shadow-card"
          >
            ←
          </Link>

          <span className="rounded-full bg-lime px-3 py-1.5 text-xs font-bold text-ink">{plan.categoria}</span>
        </div>

        <div className="grid gap-6 px-5 pt-4 md:grid-cols-[1fr_320px] md:px-0 md:pt-6">
          {/* --- Columna principal: qué, cuándo y dónde --- */}
          <div className="flex min-w-0 flex-col gap-4">
            {plan.estado === 'Cancelada' && (
              <div className="rounded-[18px] bg-danger-bg px-4 py-3.5">
                <p className="text-sm font-bold text-danger">Este plan se ha cancelado</p>
                {plan.motivoDeCancelacion && <p className="mt-1 text-sm text-body">{plan.motivoDeCancelacion}</p>}
                <Link href="/explorar" className="mt-2 inline-block text-[13px] font-bold text-brand">
                  Ver planes parecidos →
                </Link>
              </div>
            )}

            <div className="flex flex-col gap-2">
              <h1 className="font-display text-[25px] font-bold leading-tight tracking-tight md:text-[32px]">
                {plan.titulo}
              </h1>
              {plan.descripcion && <p className="text-sm leading-relaxed text-body md:text-[15px]">{plan.descripcion}</p>}
            </div>

            <dl className="flex flex-col rounded-[18px] bg-white px-4 py-3.5 text-sm">
              <div className="flex items-center gap-3 py-2">
                <span aria-hidden>🗓</span>
                <span className="font-semibold">{fechaLarga(plan.inicio)}</span>
              </div>

              <div className="h-px bg-[#E4EAE8]" />

              <div className="flex items-center gap-3 py-2">
                <span aria-hidden>📍</span>
                <span className="font-semibold">
                  {plan.lugar}
                  {plan.referencia ? `, ${plan.referencia}` : ''}
                </span>
              </div>

              <div className="h-px bg-[#E4EAE8]" />

              <div className="flex items-center gap-3 py-2">
                <span aria-hidden>👥</span>
                <span className="font-semibold">
                  {plan.plazasOcupadas} de {plan.capacidad} plazas
                </span>
                <Badge tono={estado.tono}>{estado.texto}</Badge>
              </div>

              {plan.enListaDeEspera > 0 && (
                <>
                  <div className="h-px bg-[#E4EAE8]" />
                  <div className="flex items-center gap-3 py-2">
                    <span aria-hidden>⏳</span>
                    <span className="text-body">
                      {plan.enListaDeEspera} {plan.enListaDeEspera === 1 ? 'persona' : 'personas'} en lista de espera
                    </span>
                  </div>
                </>
              )}
            </dl>

            <MapaDelPunto
              latitud={plan.latitud}
              longitud={plan.longitud}
              lugar={plan.lugar}
              direccionExacta={plan.direccionExacta}
            />

            {/* En móvil el organizador va en el flujo; en escritorio, en la columna lateral. */}
            <div className="md:hidden">{tarjetaDelOrganizador}</div>

            {plan.normas.length > 0 && (
              <div className="flex flex-col gap-2">
                <Etiqueta>NORMAS DEL PLAN</Etiqueta>
                <div className="flex flex-wrap gap-2">
                  {plan.normas.map((norma) => (
                    <span key={norma} className="rounded-full bg-white px-3 py-1.5 text-[13px]">
                      {norma}
                    </span>
                  ))}
                </div>
              </div>
            )}

            <div className="flex items-center justify-between border-t border-line py-3.5">
              <Etiqueta>¿ALGO RARO?</Etiqueta>
              <Link href={`/reportar?objeto=${plan.id}`} className="text-xs text-muted underline">
                Reportar este plan 🚩
              </Link>
            </div>
          </div>

          {/* --- Columna lateral de escritorio: la acción siempre a la vista --- */}
          <aside className="hidden md:block">
            <div className="sticky top-6 flex flex-col gap-4">
              <div className="rounded-[18px] bg-white p-4 shadow-card">{accion}</div>
              {tarjetaDelOrganizador}
            </div>
          </aside>
        </div>
      </div>
    </AppShell>
  );
}
