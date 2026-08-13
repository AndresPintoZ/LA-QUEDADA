import Link from 'next/link';
import { redirect } from 'next/navigation';

import AppShell from '@/components/AppShell';
import { EstadoVacio } from '@/components/ui';
import { completarVerificacion } from '@/lib/acciones/identidad';
import { obtenerMiPerfil } from '@/lib/datos';

/**
 * Vuelta desde la pasarela del proveedor de verificación (RF-21).
 *
 * La página recibe una referencia en la URL, pero NO se fía de ella: se la pasa al
 * servidor, que comprueba que corresponde a esta cuenta y va a preguntarle el resultado
 * al proveedor. Aceptar un «verificado: sí» que llega por la barra de direcciones
 * permitiría a cualquiera concederse el rol de organizador.
 */

export const metadata = {
  title: 'Resultado de la verificación — PlanVibe',
};

interface Parametros {
  searchParams: Promise<{ referencia?: string }>;
}

export default async function ResultadoDeVerificacion({ searchParams }: Parametros) {
  const { referencia } = await searchParams;

  if (!(await obtenerMiPerfil())) {
    redirect('/acceso?volverA=/verificacion');
  }

  if (!referencia) {
    return (
      <PantallaDeAviso
        icono="🤔"
        titulo="Falta la referencia de la verificación"
        texto="Vuelve a empezar el proceso desde tu perfil. Si el problema se repite, escríbenos."
      />
    );
  }

  // Si sale bien, la acción redirige a /crear y esto no devuelve nada.
  const resultado = await completarVerificacion(referencia);

  return (
    <PantallaDeAviso
      icono="⏳"
      titulo="Todavía no hemos podido confirmarla"
      texto={resultado?.error ?? 'El proveedor aún no ha respondido. Vuelve a intentarlo en unos minutos.'}
    />
  );
}

function PantallaDeAviso({ icono, titulo, texto }: { icono: string; titulo: string; texto: string }) {
  return (
    <AppShell anchura="estrecha">
      <div className="p-5 pt-10">
        <EstadoVacio
          icono={icono}
          titulo={titulo}
          texto={texto}
          acciones={
            <Link href="/perfil" className="w-full rounded-2xl bg-brand px-4 py-3 text-center text-sm font-bold text-white">
              Volver a mi perfil
            </Link>
          }
        />
      </div>
    </AppShell>
  );
}
