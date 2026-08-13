import Image from 'next/image';
import Link from 'next/link';

import Logo from '@/components/Logo';
import PlanCard from '@/components/PlanCard';
import { buscarPlanes, obtenerMiPerfil } from '@/lib/datos';

/**
 * Portada pública de PlanVibe.
 *
 * Es la única pantalla con cabecera oscura: es la página de presentación, no la
 * aplicación. Aun así comparte el sistema visual (logo, lima, tipografías) y es
 * responsive: en móvil el menú se reduce a las dos acciones que importan.
 */

const ventajas = [
  { icono: '🔎', titulo: 'Mira qué hay cerca', texto: 'Mapa y lista con lo que pasa hoy, a tu radio y de lo que te gusta.' },
  { icono: '✋', titulo: 'Apúntate en dos toques', texto: 'Ves quién va, las normas y el punto de encuentro antes de decidir.' },
  { icono: '🛡', titulo: 'Organizadores verificados', texto: 'Solo publica quien ha pasado la verificación de identidad. Sin guardar documentos.' },
];

export default async function Landing() {
  // La portada pública enseña planes reales: es la prueba de que la plataforma está viva.
  // Si la API no responde todavía, se muestra la portada sin ellos en lugar de un error.
  const [destacados, perfil] = await Promise.all([
    buscarPlanes({ radioEnMetros: 20_000 })
      .then((r) => r.elementos.slice(0, 4))
      .catch(() => []),
    obtenerMiPerfil(),
  ]);

  return (
    <main className="bg-white">
      <header className="bg-ink px-5 py-5 md:px-14 md:py-6">
        <div className="mx-auto flex max-w-[1280px] items-center justify-between gap-4">
          <Logo size={30} tono="lime" conTexto oscuro />
          <nav className="flex items-center gap-4 text-sm md:gap-8">
            {/* Los enlaces secundarios solo caben en escritorio. */}
            <Link href="/explorar" className="hidden text-[#A9BCC1] md:inline">Explorar planes</Link>
            <Link href="/#como-funciona" className="hidden text-[#A9BCC1] md:inline">Cómo funciona</Link>
            <Link href="/verificacion" className="hidden text-[#A9BCC1] md:inline">Para asociaciones</Link>

            {perfil ? (
              <Link href="/explorar" className="rounded-xl bg-lime px-4 py-3 font-bold text-ink">Ir a la aplicación</Link>
            ) : (
              <>
                <Link href="/acceso" className="font-bold text-white">Entrar</Link>
                <Link href="/acceso?modo=registro" className="rounded-xl bg-lime px-4 py-3 font-bold text-ink">Crear cuenta</Link>
              </>
            )}
          </nav>
        </div>
      </header>

      <section className="bg-ink px-5 pb-14 pt-10 md:px-14 md:pb-16 md:pt-14">
        <div className="mx-auto grid max-w-[1280px] items-center gap-10 lg:grid-cols-[1.05fr_0.95fr] lg:gap-12">
          <div className="flex flex-col gap-5">
            <span className="font-mono text-xs tracking-[0.16em] text-lime">PILOTO EN ÁVILA</span>
            <h1 className="font-display text-[38px] font-bold leading-[1.05] tracking-[-0.035em] text-white text-pretty md:text-[60px] md:leading-[1.02]">
              Hay plan cerca.<br />Y sitio para ti.
            </h1>
            <p className="max-w-[520px] text-base leading-relaxed text-[#A9BCC1] text-pretty md:text-lg">
              Mira qué se mueve hoy en Ávila, apúntate en dos toques o monta tu propia quedada. Sin grupos infinitos de WhatsApp.
            </p>
            <div className="flex flex-wrap items-center gap-3">
              <Link href="/explorar" className="rounded-2xl bg-lime px-6 py-4 text-base font-bold text-ink">Ver planes de esta semana</Link>
              <Link href="/verificacion" className="rounded-2xl border border-white/25 px-6 py-4 text-base font-semibold text-white">Soy organizador</Link>
            </div>
            <dl className="flex flex-wrap gap-7 pt-3">
              {[['+', 'planes cada semana'], ['5', 'categorías'], ['100%', 'organizadores verificados']].map(([n, t]) => (
                <div key={t} className="flex flex-col">
                  <dt className="font-display text-2xl font-bold text-white">{n}</dt>
                  <dd className="text-xs text-[#8FA3A9]">{t}</dd>
                </div>
              ))}
            </dl>
          </div>
          <Image
            src="https://commons.wikimedia.org/wiki/Special:FilePath/Panoramica_de_avila_-_panoramio.jpg?width=1400"
            alt="Panorámica de Ávila"
            width={1000}
            height={800}
            className="h-[240px] w-full rounded-3xl object-cover md:h-[400px]"
            priority
          />
        </div>
      </section>

      {destacados.length > 0 && (
        <section className="mx-auto max-w-[1280px] px-5 py-10 md:px-14 md:py-14">
          <div className="flex flex-wrap items-end justify-between gap-2">
            <h2 className="font-display text-2xl font-bold tracking-tight md:text-[32px]">Lo que se mueve estos días</h2>
            <Link href="/explorar" className="text-sm font-bold text-brand">Ver todos los planes →</Link>
          </div>
          <div className="mt-6 grid gap-5 sm:grid-cols-2 lg:grid-cols-4">
            {destacados.map((plan) => (
              <PlanCard key={plan.id} plan={plan} />
            ))}
          </div>
        </section>
      )}

      <section id="como-funciona" className="bg-paper px-5 py-10 md:px-14 md:py-12">
        <div className="mx-auto grid max-w-[1280px] gap-6 md:grid-cols-3">
          {ventajas.map((v) => (
            <div key={v.titulo} className="flex flex-col gap-2">
              <span className="text-2xl" aria-hidden>{v.icono}</span>
              <h3 className="font-display text-[19px] font-bold">{v.titulo}</h3>
              <p className="text-sm leading-relaxed text-body">{v.texto}</p>
            </div>
          ))}
        </div>
      </section>

      <footer className="bg-ink px-5 py-6 md:px-14">
        <div className="mx-auto flex max-w-[1280px] flex-wrap items-center justify-between gap-3">
          <span className="font-mono text-[11px] text-[#8FA3A9]">© 2026 PLANVIBE.ES · ÁVILA</span>
          <div className="flex gap-6 text-[13px] text-[#A9BCC1]">
            <Link href="/normas">Normas de comunidad</Link>
            <Link href="/privacidad">Privacidad</Link>
          </div>
        </div>
      </footer>
    </main>
  );
}
