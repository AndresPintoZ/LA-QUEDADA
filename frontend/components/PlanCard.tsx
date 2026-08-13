import Link from 'next/link';

import { cuandoCorto, distancia, estadoDePlazas, iniciales } from '@/lib/formato';
import type { ResumenDePlan } from '@/lib/tipos';
import { Badge, VerificadoBadge } from './ui';

/**
 * Tarjeta de un plan en la lista de explorar.
 *
 * Muestra en este orden cuándo, a qué distancia, de qué va y quién lo organiza:
 * es el orden en que se decide si un plan interesa o no.
 *
 * El estado de plazas va siempre con texto además de con color, porque el color
 * por sí solo no es un indicador accesible (docs/03-diseno-visual.md).
 */
export default function PlanCard({ plan }: { plan: ResumenDePlan }) {
  const estado = estadoDePlazas(plan.capacidad, plan.plazasOcupadas, plan.estado);
  const aQueDistancia = distancia(plan.distanciaEnMetros);

  return (
    <Link
      href={`/plan/${plan.id}`}
      className="shrink-0 overflow-hidden rounded-card bg-white text-ink shadow-card focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-brand"
    >
      {/* Franja de categoría. Sustituye a la foto mientras no haya imágenes propias:
          una tarjeta sin imagen se lee igual de bien y evita esperar a que cargue. */}
      <div className="flex items-center justify-between bg-brand-tint px-4 py-2.5">
        <span className="rounded-full bg-ink/80 px-2.5 py-1 text-[11px] font-semibold text-white">{plan.categoria}</span>
        <Badge tono={estado.tono}>{estado.texto}</Badge>
      </div>

      <div className="flex flex-col gap-2 px-4 pb-4 pt-3.5">
        <span className="font-mono text-[11px] text-brand">
          {cuandoCorto(plan.inicio)}
          {aQueDistancia ? ` · a ${aQueDistancia}` : ''}
        </span>

        <span className="font-display text-lg font-semibold leading-tight">{plan.titulo}</span>

        <span className="text-[13px] text-muted">📍 {plan.lugar}</span>

        <div className="flex items-center justify-between">
          <span className="flex items-center gap-1.5">
            <span
              className="flex h-[22px] w-[22px] items-center justify-center rounded-full bg-brand-tint text-[10px] font-bold text-brand-dark"
              aria-hidden
            >
              {iniciales(plan.organizadorNombre)}
            </span>
            <span className="text-[13px] text-body">{plan.organizadorNombre}</span>
            {plan.organizadorVerificado && <VerificadoBadge />}
          </span>

          <span className="text-[11px] text-muted">
            {plan.plazasOcupadas} de {plan.capacidad}
          </span>
        </div>
      </div>
    </Link>
  );
}
