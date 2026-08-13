import Link from 'next/link';
import { EstadoVacio } from '@/components/ui';

/** Estados de la guía de diseño: sin resultados, vacío por categoría y error de mapa. */
export default function Estados() {
  return (
    <main className="mx-auto flex min-h-dvh w-full max-w-2xl flex-col gap-4 p-5 md:py-10">
      <EstadoVacio icono="🔍" titulo="Aquí no hay nada… todavía"
        texto="No encontramos planes con esos filtros. Prueba a ampliar el radio o la fecha."
        acciones={
          <div className="flex flex-wrap justify-center gap-2 text-[13px]">
            <button className="rounded-full bg-ink px-3.5 py-2.5 font-bold text-white">Ampliar a 25 km</button>
            <button className="rounded-full bg-paper px-3.5 py-2.5 font-semibold">Ver toda la semana</button>
          </div>
        } />
      <EstadoVacio icono="🚀" titulo="Sé el primero en montar algo"
        texto="Nadie ha publicado planes de senderismo esta semana. Propón el tuyo, la gente se apunta."
        acciones={<Link href="/crear" className="rounded-2xl bg-lime px-5 py-3.5 text-[15px] font-bold text-ink">Crear una quedada</Link>} />
      <div className="flex gap-3.5 rounded-[26px] border border-[#F3D4D2] bg-danger-bg px-6 py-6">
        <span className="flex h-10 w-10 shrink-0 items-center justify-center rounded-xl bg-white text-lg" aria-hidden>⚠</span>
        <div className="flex flex-col gap-1 text-[13px] leading-snug text-[#7A2B27]">
          <span className="text-base font-bold">No hemos podido cargar el mapa</span>
          <span>Revisa tu conexión. Mientras tanto puedes seguir en la lista.</span>
          <button className="mt-1 self-start font-bold text-danger">Reintentar</button>
        </div>
      </div>
    </main>
  );
}
