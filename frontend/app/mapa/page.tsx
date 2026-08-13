import Link from 'next/link';

import BarraSuperior from '@/components/BarraSuperior';
import BottomNav from '@/components/BottomNav';
import MapaDePlanes from '@/components/MapaDePlanes';
import { buscarPlanes, CENTRO_DEL_PILOTO } from '@/lib/datos';

/**
 * Explorar en mapa (RF-05).
 *
 * No usa el armazón estándar porque el mapa debe ocupar todo el espacio disponible,
 * sin el ancho máximo de contenido. Mantiene la coherencia con el resto de la
 * aplicación por las mismas dos piezas de navegación: barra superior en escritorio
 * y barra inferior en móvil.
 */

export const metadata = {
  title: 'Mapa de planes — PlanVibe',
};

export default async function Mapa() {
  const resultado = await buscarPlanes({
    latitud: CENTRO_DEL_PILOTO.latitud,
    longitud: CENTRO_DEL_PILOTO.longitud,
    radioEnMetros: 10_000,
    pagina: 1,
  });

  return (
    <main className="flex min-h-dvh flex-col bg-[#E4EDEA]">
      <BarraSuperior />

      <div className="relative flex min-h-0 flex-1 flex-col">
        <MapaDePlanes
          planes={resultado.elementos}
          centro={{ latitud: CENTRO_DEL_PILOTO.latitud, longitud: CENTRO_DEL_PILOTO.longitud }}
        />

        <div className="pointer-events-none absolute inset-x-0 top-0 z-[500] p-5">
          <div className="pointer-events-auto mx-auto flex max-w-sm rounded-[14px] bg-white p-1 shadow-md">
            <Link href="/explorar" className="flex-1 rounded-xl py-2.5 text-center text-sm font-medium text-muted">
              Lista
            </Link>
            <span className="flex-1 rounded-xl bg-paper py-2.5 text-center text-sm font-bold">Mapa</span>
          </div>
        </div>
      </div>

      <BottomNav />
    </main>
  );
}
