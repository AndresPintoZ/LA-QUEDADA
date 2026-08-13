import Link from 'next/link';
import { redirect } from 'next/navigation';

import BotonDeVerificacion from '@/components/BotonDeVerificacion';
import AppShell from '@/components/AppShell';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Verificación de organizador (RF-20, RF-21, RF-22).
 *
 * Esta pantalla es, sobre todo, una explicación. Pedir un documento de identidad sin
 * decir qué se hace con él es la forma más segura de perder a quien iba a organizar,
 * y además sería incompatible con el criterio de transparencia que recoge
 * `docs/04-seguridad-privacidad-moderacion.md`.
 */

export const metadata = {
  title: 'Verificar mi identidad — PlanVibe',
};

export default async function Verificacion() {
  const perfil = await obtenerMiPerfil();

  if (!perfil) {
    redirect('/acceso?volverA=/verificacion');
  }

  if (perfil.puedeOrganizar) {
    redirect('/crear');
  }

  return (
    <AppShell
      anchura="estrecha"
      cabecera={
        <div className="flex items-center gap-3 px-5 py-3.5 md:px-6">
          <Link href="/perfil" aria-label="Volver" className="text-xl">
            ←
          </Link>
          <h1 className="font-display text-[19px] font-semibold tracking-tight">Verificar mi identidad</h1>
        </div>
      }
    >
      <div className="flex flex-col gap-4 p-5 md:px-6 md:py-6">
        <section className="flex flex-col gap-2.5 rounded-[18px] bg-white px-4 py-4">
          <h2 className="font-display text-base font-bold">Por qué te lo pedimos</h2>
          <p className="text-sm leading-relaxed text-body">
            Quien organiza un encuentro público responde de él. Comprobar la identidad una sola vez
            reduce las cuentas falsas y las suplantaciones, y hace que quien se apunta a tu plan sepa
            con quién va.
          </p>
        </section>

        <section className="flex flex-col gap-2.5 rounded-[18px] bg-white px-4 py-4">
          <h2 className="font-display text-base font-bold">Cómo funciona</h2>
          <ol className="flex flex-col gap-2 text-sm text-body">
            <li>
              <strong>1.</strong> Te llevamos a la pasarela de un proveedor externo especializado.
            </li>
            <li>
              <strong>2.</strong> Le enseñas tu documento <em>a él</em>, no a nosotros.
            </li>
            <li>
              <strong>3.</strong> Nos devuelve solo un «sí» o un «no» y una referencia técnica.
            </li>
            <li>
              <strong>4.</strong> Vuelves aquí y ya puedes publicar tu primer plan.
            </li>
          </ol>
        </section>

        <section className="flex flex-col gap-2.5 rounded-[18px] bg-brand-tint px-4 py-4">
          <h2 className="font-display text-base font-bold text-brand-dark">Qué guarda PlanVibe</h2>

          <ul className="flex flex-col gap-1.5 text-[13px] text-body">
            <li>✅ El resultado: verificado o no.</li>
            <li>✅ Qué proveedor lo comprobó y cuándo.</li>
            <li>✅ Una referencia técnica de la comprobación.</li>
            <li>✅ Si tienes 18 años cumplidos: un sí o un no, nada más.</li>
          </ul>

          <p className="pt-1 text-[13px] font-semibold text-brand-dark">Y esto no se guarda nunca:</p>

          <ul className="flex flex-col gap-1.5 text-[13px] text-body">
            <li>❌ Fotos ni escaneos de tu documento.</li>
            <li>❌ El número de tu DNI, NIE o pasaporte.</li>
            <li>❌ Tu fecha de nacimiento completa.</li>
            <li>❌ Datos biométricos.</li>
          </ul>
        </section>

        <BotonDeVerificacion />

        <p className="pb-4 text-center text-[13px] text-muted">
          Durante el piloto, solo pueden organizar planes las personas mayores de edad. Puedes seguir
          apuntándote a los planes de otras personas sin verificarte.
        </p>
      </div>
    </AppShell>
  );
}
