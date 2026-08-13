import Link from 'next/link';

import Logo from './Logo';
import NavEnlaces from './NavEnlaces';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Barra de navegación superior, visible solo en escritorio (en móvil navega la barra
 * inferior). Es LA pieza que da coherencia entre pantallas: todas las páginas de la
 * aplicación la comparten, así que el logo, las secciones y la sesión están siempre
 * en el mismo sitio.
 *
 * Es un componente de servidor: lee la sesión de la cookie sin ninguna petición extra
 * desde el navegador.
 */
export default async function BarraSuperior() {
  const perfil = await obtenerMiPerfil();

  return (
    <header className="hidden shrink-0 border-b border-line bg-white md:block">
      <div className="mx-auto flex w-full max-w-6xl items-center justify-between gap-6 px-6 py-3">
        <Link href="/" aria-label="Inicio de PlanVibe">
          <Logo size={30} conTexto />
        </Link>

        <NavEnlaces />

        <div className="flex items-center gap-3">
          <Link href="/crear" className="rounded-xl bg-lime px-4 py-2.5 text-sm font-bold text-ink">
            + Crear plan
          </Link>

          {perfil ? (
            <Link
              href="/perfil"
              aria-label="Ir a mi perfil"
              className="flex h-10 w-10 items-center justify-center rounded-full bg-brand-tint text-[13px] font-bold text-brand-dark"
            >
              {perfil.nombreVisible.slice(0, 2).toUpperCase()}
            </Link>
          ) : (
            <Link href="/acceso" className="rounded-xl bg-ink px-4 py-2.5 text-sm font-bold text-white">
              Entrar
            </Link>
          )}
        </div>
      </div>
    </header>
  );
}
