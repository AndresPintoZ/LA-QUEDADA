import Link from 'next/link';
import { redirect } from 'next/navigation';

import FormularioDeAcceso from '@/components/FormularioDeAcceso';
import Logo from '@/components/Logo';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Entrar o crear cuenta (RF-01, RF-04).
 *
 * Las normas de la comunidad y la política de privacidad se enlazan antes del registro,
 * no después: hay que poder leerlas antes de aceptarlas.
 *
 * Responsive: en móvil ocupa la pantalla; en escritorio es una tarjeta centrada sobre
 * el fondo neutro, el patrón habitual de una pantalla de acceso.
 */

export const metadata = {
  title: 'Entrar — PlanVibe',
};

interface Parametros {
  searchParams: Promise<{ modo?: string; volverA?: string }>;
}

export default async function Acceso({ searchParams }: Parametros) {
  const { modo } = await searchParams;

  // Quien ya tiene sesión no tiene nada que hacer aquí.
  if (await obtenerMiPerfil()) {
    redirect('/explorar');
  }

  const esRegistro = modo === 'registro';

  return (
    <main className="flex min-h-dvh flex-col bg-paper md:items-center md:justify-center md:py-10">
      <div className="mx-auto flex w-full max-w-md flex-col bg-white px-6 pb-10 pt-12 md:rounded-[26px] md:px-10 md:pt-10 md:shadow-card">
        <div className="flex flex-col items-center gap-3 pb-8">
          <Link href="/" aria-label="Volver a la portada">
            <Logo size={44} />
          </Link>
          <h1 className="font-display text-2xl font-bold tracking-tight">
            {esRegistro ? 'Crea tu cuenta' : 'Hola de nuevo'}
          </h1>
          <p className="text-center text-sm text-body">
            {esRegistro
              ? 'Para apuntarte a planes y montar los tuyos en Ávila.'
              : 'Entra para ver tus planes y apuntarte a los nuevos.'}
          </p>
        </div>

        <FormularioDeAcceso esRegistro={esRegistro} />

        <p className="pt-6 text-center text-sm text-body">
          {esRegistro ? (
            <>
              ¿Ya tienes cuenta?{' '}
              <Link href="/acceso" className="font-bold text-brand underline">
                Entra
              </Link>
            </>
          ) : (
            <>
              ¿Todavía no tienes cuenta?{' '}
              <Link href="/acceso?modo=registro" className="font-bold text-brand underline">
                Créala
              </Link>
            </>
          )}
        </p>
      </div>
    </main>
  );
}
