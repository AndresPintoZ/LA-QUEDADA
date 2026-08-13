import Link from 'next/link';
import { redirect } from 'next/navigation';

import AppShell from '@/components/AppShell';
import { Badge, Etiqueta, VerificadoBadge } from '@/components/ui';
import { cerrarSesion } from '@/lib/acciones/identidad';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Perfil, verificación y ajustes (RF-02, RF-03).
 *
 * El estado de verificación se muestra tal cual, sin adornos, y con un enlace a lo que
 * la plataforma guarda: si se pide un documento de identidad, hay que poder consultar
 * en todo momento qué se hizo con él.
 */

export const metadata = {
  title: 'Mi perfil — PlanVibe',
};

const ESTADOS_DE_VERIFICACION: Record<string, { texto: string; tono: 'ok' | 'warn' | 'danger' | 'neutro' }> = {
  NoIniciada: { texto: 'Sin verificar', tono: 'neutro' },
  Pendiente: { texto: 'Comprobando…', tono: 'warn' },
  Verificada: { texto: 'Verificado', tono: 'ok' },
  Rechazada: { texto: 'No se pudo verificar', tono: 'danger' },
  Revocada: { texto: 'Verificación retirada', tono: 'danger' },
};

export default async function Perfil() {
  const perfil = await obtenerMiPerfil();

  if (!perfil) {
    redirect('/acceso?volverA=/perfil');
  }

  const verificacion = ESTADOS_DE_VERIFICACION[perfil.estadoVerificacion] ?? ESTADOS_DE_VERIFICACION.NoIniciada;

  return (
    <AppShell
      anchura="estrecha"
      cabecera={
        <div className="px-5 py-4 md:px-6">
          <h1 className="font-display text-[22px] font-bold tracking-tight">Mi perfil</h1>
        </div>
      }
    >
      <div className="flex flex-col gap-4 p-5 md:px-6 md:py-6">
        <section className="flex items-center gap-3.5 rounded-[18px] bg-white px-4 py-4">
          <span
            className="flex h-14 w-14 items-center justify-center rounded-full bg-brand-tint text-lg font-bold text-brand-dark"
            aria-hidden
          >
            {perfil.nombreVisible.slice(0, 2).toUpperCase()}
          </span>

          <div className="flex flex-1 flex-col gap-1">
            <span className="flex items-center gap-2">
              <span className="font-display text-lg font-bold">{perfil.nombreVisible}</span>
              {perfil.puedeOrganizar && <VerificadoBadge />}
            </span>
            <span className="text-[13px] text-muted">{perfil.ciudad ?? 'Sin ciudad'}</span>
          </div>
        </section>

        <section className="flex flex-col gap-3 rounded-[18px] bg-white px-4 py-4">
          <div className="flex items-center justify-between">
            <Etiqueta>VERIFICACIÓN DE ORGANIZADOR</Etiqueta>
            <Badge tono={verificacion.tono}>{verificacion.texto}</Badge>
          </div>

          <p className="text-[13px] leading-relaxed text-body">
            {perfil.puedeOrganizar
              ? 'Puedes publicar planes. Guardamos solo el resultado de la comprobación, su fecha y el proveedor: ninguna imagen ni número de tu documento.'
              : 'Para publicar planes hace falta verificar tu identidad una sola vez. No guardamos ninguna imagen ni número de tu documento.'}
          </p>

          {!perfil.puedeOrganizar && perfil.estadoVerificacion !== 'Pendiente' && (
            <Link
              href="/verificacion"
              className="rounded-2xl bg-brand px-4 py-3 text-center text-sm font-bold text-white"
            >
              Verificar mi identidad
            </Link>
          )}
        </section>

        <section className="flex flex-col rounded-[18px] bg-white">
          <EnlaceDeAjuste href="/perfil/editar" texto="Editar mi perfil e intereses" />
          <EnlaceDeAjuste href="/normas" texto="Normas de la comunidad" />
          <EnlaceDeAjuste href="/privacidad" texto="Política de privacidad" />
          <EnlaceDeAjuste href="/perfil/mis-datos" texto="Mis datos y eliminar mi cuenta" ultimo />
        </section>

        <section className="rounded-[18px] bg-white px-4 py-3">
          <Etiqueta>SESIÓN</Etiqueta>
          <p className="pb-2 pt-1.5 text-[13px] text-muted">{perfil.correo}</p>

          {/* Cerrar sesión es una acción de servidor y va en un formulario, no en un
              enlace: cambia el estado del servidor y no debe poder dispararse desde
              una precarga del navegador ni desde un enlace de otra web. */}
          <form action={cerrarSesion}>
            <button
              type="submit"
              className="w-full rounded-2xl border border-line px-4 py-3 text-sm font-bold text-danger"
            >
              Cerrar sesión
            </button>
          </form>
        </section>
      </div>
    </AppShell>
  );
}

function EnlaceDeAjuste({ href, texto, ultimo = false }: { href: string; texto: string; ultimo?: boolean }) {
  return (
    <Link
      href={href}
      className={`flex items-center justify-between px-4 py-3.5 text-[15px] text-ink ${ultimo ? '' : 'border-b border-line'}`}
    >
      {texto}
      <span aria-hidden className="text-muted">
        ›
      </span>
    </Link>
  );
}
