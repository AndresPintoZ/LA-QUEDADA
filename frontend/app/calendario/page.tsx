import Link from 'next/link';

import AppShell from '@/components/AppShell';
import { Badge, Etiqueta, EstadoVacio } from '@/components/ui';
import { buscarPlanes } from '@/lib/datos';
import { estadoDePlazas } from '@/lib/formato';
import type { ResumenDePlan } from '@/lib/tipos';

/**
 * Planes agrupados por día.
 *
 * La agrupación se hace en el servidor con la zona horaria del piloto. Hacerla en el
 * navegador daría días distintos según dónde esté configurado el dispositivo, y un plan
 * de las 23:30 podría aparecer en el día siguiente para unas personas y no para otras.
 */

export const metadata = {
  title: 'Calendario — PlanVibe',
};

const ZONA_HORARIA = 'Europe/Madrid';

const formateadorDeDia = new Intl.DateTimeFormat('es-ES', {
  weekday: 'long',
  day: 'numeric',
  month: 'long',
  timeZone: ZONA_HORARIA,
});

const formateadorDeHora = new Intl.DateTimeFormat('es-ES', {
  hour: '2-digit',
  minute: '2-digit',
  timeZone: ZONA_HORARIA,
});

const formateadorDeClaveDeDia = new Intl.DateTimeFormat('en-CA', {
  year: 'numeric',
  month: '2-digit',
  day: '2-digit',
  timeZone: ZONA_HORARIA,
});

export default async function Calendario() {
  // Dos semanas por delante: es el horizonte con el que la gente planifica un fin de semana.
  const dentroDeDosSemanas = new Date(Date.now() + 14 * 86_400_000).toISOString();

  const resultado = await buscarPlanes({ hasta: dentroDeDosSemanas, radioEnMetros: 20_000 });
  const dias = agruparPorDia(resultado.elementos);

  return (
    <AppShell
      anchura="media"
      cabecera={
        <div className="px-5 py-4 md:px-6">
          <h1 className="font-display text-[22px] font-bold tracking-tight">Próximos días</h1>
        </div>
      }
    >
      <div className="flex flex-col gap-5 p-5 md:px-6 md:py-6">
        {dias.length === 0 ? (
          <EstadoVacio
            icono="🗓"
            titulo="No hay nada previsto todavía"
            texto="En las próximas dos semanas no hay ningún plan publicado. Puede ser buen momento para proponer tú el primero."
            acciones={
              <Link href="/crear" className="w-full rounded-2xl bg-brand px-4 py-3 text-center text-sm font-bold text-white">
                Crear un plan
              </Link>
            }
          />
        ) : (
          dias.map((dia) => (
            <section key={dia.clave} className="flex flex-col gap-2.5">
              <Etiqueta>{dia.titulo.toUpperCase()}</Etiqueta>

              <div className="flex flex-col rounded-[18px] bg-white">
                {dia.planes.map((plan, indice) => {
                  const estado = estadoDePlazas(plan.capacidad, plan.plazasOcupadas, plan.estado);

                  return (
                    <Link
                      key={plan.id}
                      href={`/plan/${plan.id}`}
                      className={`flex items-center gap-3.5 px-4 py-3.5 ${indice > 0 ? 'border-t border-line' : ''}`}
                    >
                      <span className="font-mono text-[13px] font-semibold text-brand">
                        {formateadorDeHora.format(new Date(plan.inicio))}
                      </span>

                      <span className="flex min-w-0 flex-1 flex-col">
                        <span className="truncate text-[15px] font-semibold">{plan.titulo}</span>
                        <span className="truncate text-[13px] text-muted">{plan.lugar}</span>
                      </span>

                      <Badge tono={estado.tono}>{estado.texto}</Badge>
                    </Link>
                  );
                })}
              </div>
            </section>
          ))
        )}
      </div>
    </AppShell>
  );
}

interface DiaConPlanes {
  clave: string;
  titulo: string;
  planes: ResumenDePlan[];
}

function agruparPorDia(planes: ResumenDePlan[]): DiaConPlanes[] {
  const porDia = new Map<string, DiaConPlanes>();

  for (const plan of planes) {
    const fecha = new Date(plan.inicio);
    const clave = formateadorDeClaveDeDia.format(fecha);

    let dia = porDia.get(clave);

    if (!dia) {
      dia = { clave, titulo: formateadorDeDia.format(fecha), planes: [] };
      porDia.set(clave, dia);
    }

    dia.planes.push(plan);
  }

  // La API ya devuelve los planes ordenados por fecha, así que basta con ordenar
  // las claves de día para conservar el orden dentro de cada uno.
  return [...porDia.values()].sort((a, b) => a.clave.localeCompare(b.clave));
}
