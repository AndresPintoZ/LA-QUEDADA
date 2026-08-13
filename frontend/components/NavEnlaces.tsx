'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

/**
 * Enlaces de la barra superior de escritorio, con el estado activo resaltado.
 *
 * Es el único trozo de la barra que necesita ejecutarse en el navegador: saber en qué
 * ruta estamos requiere `usePathname`. El resto de la barra (sesión, logo) se resuelve
 * en el servidor.
 */

const ENLACES = [
  { href: '/explorar', etiqueta: 'Explorar' },
  { href: '/mapa', etiqueta: 'Mapa' },
  { href: '/calendario', etiqueta: 'Calendario' },
  { href: '/mis-planes', etiqueta: 'Mis planes' },
] as const;

export default function NavEnlaces() {
  const ruta = usePathname();

  return (
    <nav className="flex items-center gap-1" aria-label="Secciones">
      {ENLACES.map(({ href, etiqueta }) => {
        const activo = ruta?.startsWith(href);

        return (
          <Link
            key={href}
            href={href}
            aria-current={activo ? 'page' : undefined}
            className={`rounded-xl px-3.5 py-2 text-sm transition-colors ${
              activo ? 'bg-brand-tint font-bold text-brand-dark' : 'font-medium text-body hover:bg-paper'
            }`}
          >
            {etiqueta}
          </Link>
        );
      })}
    </nav>
  );
}
