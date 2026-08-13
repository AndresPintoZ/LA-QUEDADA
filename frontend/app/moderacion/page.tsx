import Link from 'next/link';

import AppShell from '@/components/AppShell';
import Logo from '@/components/Logo';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Panel de moderación (RF-17, RF-18).
 *
 * PENDIENTE DE IMPLEMENTAR. La cola de reportes y las acciones de moderación quedaron
 * fuera de la primera entrega, que cubre el recorrido registro → publicar → apuntarse.
 *
 * Lo que ya existe y sobre lo que se construirá esta pantalla:
 *
 *   - `Quedada.OcultarPorModeracion(motivo, ahora)` y `Usuario.Suspender(motivo, ahora)`
 *     en el dominio, ambos con motivo obligatorio.
 *   - `IRegistroDeAuditoria`, que ya deja constancia de cada decisión (RNF-04).
 *   - La política de autorización `es-moderador` en la API.
 *
 * Falta el agregado `Reporte`, sus endpoints y esta interfaz. Ver
 * `docs/08-hoja-de-ruta.md`.
 *
 * La pantalla se deja accesible y protegida por rol, en lugar de eliminarla, para que
 * el acceso quede probado desde el principio: es más fácil añadir una tabla a una
 * pantalla que ya comprueba permisos que acordarse de proteger una pantalla nueva.
 */

export const metadata = {
  title: 'Moderación — PlanVibe',
};

export default async function Moderacion() {
  const perfil = await obtenerMiPerfil();

  const esModerador = perfil?.roles.some((rol) => rol === 'Moderador' || rol === 'Administrador') ?? false;

  if (!esModerador) {
    // Mismo mensaje tanto si no hay sesión como si el rol no alcanza: no se confirma
    // que exista un panel de moderación a quien no debe entrar en él.
    return (
      <main className="mx-auto flex min-h-dvh max-w-[560px] flex-col items-center justify-center gap-4 px-6 text-center">
        <Logo size={40} />
        <h1 className="font-display text-xl font-bold">No hemos encontrado esta página</h1>
        <Link href="/explorar" className="rounded-2xl bg-brand px-5 py-3 text-sm font-bold text-white">
          Volver a explorar
        </Link>
      </main>
    );
  }

  return (
    <AppShell
      anchura="media"
      cabecera={
        <div className="px-5 py-4 md:px-6">
          <h1 className="font-display text-[22px] font-bold tracking-tight">Moderación</h1>
        </div>
      }
    >
      <div className="flex flex-col gap-6 p-5 md:px-6 md:py-6">

      <section className="flex flex-col gap-3 rounded-[18px] border border-line bg-white px-5 py-5">
        <h2 className="font-display text-lg font-bold">Cola de reportes: en construcción</h2>

        <p className="text-sm leading-relaxed text-body">
          La cola de revisión todavía no está implementada. El dominio ya sabe ocultar una
          publicación y suspender una cuenta, y toda decisión queda registrada en la auditoría con
          su motivo, fecha y responsable. Lo que falta es el agregado de reportes, sus endpoints y
          esta interfaz.
        </p>

        <p className="text-sm text-muted">
          Mientras tanto, los avisos se atienden por el canal de contacto. Ver la hoja de ruta en{' '}
          <code className="rounded bg-paper px-1.5 py-0.5 font-mono text-[13px]">docs/08-hoja-de-ruta.md</code>.
        </p>
      </section>
      </div>
    </AppShell>
  );
}
