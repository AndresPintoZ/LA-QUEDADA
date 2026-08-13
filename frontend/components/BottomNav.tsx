'use client';

import Link from 'next/link';
import { usePathname } from 'next/navigation';

/**
 * Barra de navegación inferior, solo en móvil (`md:hidden`).
 *
 * En escritorio navega la barra superior (`BarraSuperior`): una barra pegada al borde
 * inferior de un monitor queda lejos del contenido y no es un patrón de escritorio.
 * Ambas barras ofrecen las mismas secciones para que cambiar de dispositivo no cambie
 * el mapa mental de la aplicación.
 */

const items = [
  { href: '/explorar', label: 'Explorar', icono: '🧭' },
  { href: '/calendario', label: 'Calendario', icono: '🗓' },
  { href: '/crear', label: 'Crear', icono: '+' },
  { href: '/mis-planes', label: 'Mis planes', icono: '🎟' },
  { href: '/perfil', label: 'Perfil', icono: '🙂' },
];

export default function BottomNav() {
  const ruta = usePathname();
  return (
    <nav className="grid shrink-0 grid-cols-5 items-end border-t border-line bg-white px-3 pb-6 pt-2.5 md:hidden">
      {items.map((it) => {
        const activo = ruta?.startsWith(it.href);
        if (it.href === '/crear') {
          return (
            <Link key={it.href} href={it.href} className="flex justify-center" aria-label="Crear un plan">
              <span className="flex items-center justify-center rounded-[18px] bg-lime text-2xl font-bold text-ink" style={{ height: 52, width: 52 }}>
                +
              </span>
            </Link>
          );
        }
        return (
          <Link key={it.href} href={it.href} className={`flex flex-col items-center gap-1 ${activo ? 'text-brand' : 'text-muted'}`} aria-current={activo ? 'page' : undefined}>
            <span className="text-[19px]" aria-hidden>{it.icono}</span>
            <span className={`text-[10px] ${activo ? 'font-bold' : 'font-semibold'}`}>{it.label}</span>
          </Link>
        );
      })}
    </nav>
  );
}
