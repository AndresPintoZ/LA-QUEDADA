import Link from 'next/link';
import { redirect } from 'next/navigation';

import AppShell from '@/components/AppShell';
import PlanCard from '@/components/PlanCard';
import { Etiqueta, EstadoVacio } from '@/components/ui';
import { obtenerMiPerfil, obtenerMisPlanes } from '@/lib/datos';

/**
 * Mis planes: los que organizo y aquellos a los que voy.
 *
 * Incluye a propósito los cancelados. Un plan que desaparece sin explicación deja a la
 * persona pensando que se equivocó de día; uno marcado como cancelado, no
 * (docs/03-diseno-visual.md, estados importantes).
 */

export const metadata = {
  title: 'Mis planes — PlanVibe',
};

export default async function MisPlanes() {
  const perfil = await obtenerMiPerfil();

  if (!perfil) {
    redirect('/acceso?volverA=/mis-planes');
  }

  const planes = await obtenerMisPlanes();

  const organizo = planes.filter((plan) => plan.organizadorNombre === perfil.nombreVisible);
  const voy = planes.filter((plan) => plan.organizadorNombre !== perfil.nombreVisible);

  return (
    <AppShell
      cabecera={
        <div className="px-5 py-4 md:px-6">
          <h1 className="font-display text-[22px] font-bold tracking-tight">Mis planes</h1>
        </div>
      }
    >
      <div className="flex flex-col gap-5 p-5 md:px-6 md:py-6">
        {planes.length === 0 ? (
          <EstadoVacio
            icono="🎟"
            titulo="Todavía no tienes ningún plan"
            texto="Apúntate a algo que te apetezca o monta el tuyo. En Ávila siempre hay alguien buscando con quién ir."
            acciones={
              <div className="flex w-full flex-col gap-2">
                <Link href="/explorar" className="rounded-2xl bg-brand px-4 py-3 text-center text-sm font-bold text-white">
                  Ver qué hay cerca
                </Link>
                <Link href="/crear" className="rounded-2xl bg-paper px-4 py-3 text-center text-sm font-bold text-ink">
                  Crear un plan
                </Link>
              </div>
            }
          />
        ) : (
          <>
            {voy.length > 0 && (
              <section className="flex flex-col gap-3.5">
                <Etiqueta>VOY A ESTOS PLANES ({voy.length})</Etiqueta>
                <div className="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
                  {voy.map((plan) => (
                    <PlanCard key={plan.id} plan={plan} />
                  ))}
                </div>
              </section>
            )}

            {organizo.length > 0 && (
              <section className="flex flex-col gap-3.5">
                <Etiqueta>ORGANIZO ({organizo.length})</Etiqueta>
                <div className="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
                  {organizo.map((plan) => (
                    <PlanCard key={plan.id} plan={plan} />
                  ))}
                </div>
              </section>
            )}
          </>
        )}
      </div>
    </AppShell>
  );
}
