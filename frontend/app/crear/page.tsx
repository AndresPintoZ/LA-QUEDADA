import Link from 'next/link';
import { redirect } from 'next/navigation';

import FormularioDeQuedada from '@/components/FormularioDeQuedada';
import AppShell from '@/components/AppShell';
import { EstadoVacio } from '@/components/ui';
import { obtenerCategorias, obtenerMiPerfil } from '@/lib/datos';

/**
 * Crear un plan (RF-09, RF-10).
 *
 * La comprobación de si se puede organizar se hace en tres sitios, y los tres son
 * necesarios: aquí para no enseñar un formulario que va a fallar, en la política de
 * autorización de la API para rechazar la petición, y en el agregado de dominio para
 * que la regla se cumpla venga la orden de donde venga.
 */

export const metadata = {
  title: 'Crear un plan — PlanVibe',
};

export default async function Crear() {
  const perfil = await obtenerMiPerfil();

  if (!perfil) {
    redirect('/acceso?volverA=/crear');
  }

  if (!perfil.puedeOrganizar) {
    return <AvisoDeVerificacion estadoDeVerificacion={perfil.estadoVerificacion} />;
  }

  const categorias = await obtenerCategorias();

  return (
    <AppShell
      anchura="estrecha"
      cabecera={
        <div className="flex items-center gap-3 px-5 py-3.5 md:px-6">
          <Link href="/explorar" aria-label="Volver" className="text-xl">
            ←
          </Link>
          <h1 className="font-display text-[19px] font-semibold tracking-tight">Crear un plan</h1>
        </div>
      }
    >
      <div className="p-5 md:px-6 md:py-6">
        <FormularioDeQuedada categorias={categorias} />
      </div>
    </AppShell>
  );
}

/**
 * Pantalla de «aún no puedes organizar».
 *
 * Explica por qué se pide la verificación y, sobre todo, qué se guarda y qué no.
 * Pedir un documento de identidad sin explicar el destino de los datos es la forma
 * más rápida de que alguien abandone el proceso, y con razón.
 */
function AvisoDeVerificacion({ estadoDeVerificacion }: { estadoDeVerificacion: string }) {
  const estaPendiente = estadoDeVerificacion === 'Pendiente';

  return (
    <AppShell anchura="estrecha">
      <div className="flex flex-col gap-5 p-5 pt-8 md:px-6">
        <EstadoVacio
          icono={estaPendiente ? '⏳' : '🪪'}
          titulo={estaPendiente ? 'Estamos comprobando tu identidad' : 'Verifica tu identidad para organizar'}
          texto={
            estaPendiente
              ? 'En cuanto el proveedor responda, podrás publicar tu primer plan. Suele tardar unos minutos.'
              : 'Quien organiza un encuentro público responde de él. Por eso pedimos una verificación antes del primer plan: reduce las cuentas falsas y las suplantaciones.'
          }
          acciones={
            estaPendiente ? undefined : (
              <Link
                href="/verificacion"
                className="w-full rounded-2xl bg-brand px-4 py-4 text-center text-[17px] font-bold text-white"
              >
                Verificar mi identidad
              </Link>
            )
          }
        />

        <div className="flex flex-col gap-2.5 rounded-[18px] bg-white px-4 py-4">
          <h2 className="font-display text-base font-bold">Qué guardamos y qué no</h2>

          <ul className="flex flex-col gap-1.5 text-[13px] text-body">
            <li>✅ Que la verificación salió bien, cuándo y con qué proveedor.</li>
            <li>✅ Una referencia técnica de la comprobación.</li>
            <li>✅ Si tienes 18 años cumplidos. Solo eso: un sí o un no.</li>
            <li>❌ Ninguna foto ni escaneo de tu documento.</li>
            <li>❌ Ningún número de DNI, NIE ni pasaporte.</li>
            <li>❌ Tu fecha de nacimiento completa.</li>
          </ul>

          <p className="text-[13px] text-muted">
            La comprobación la hace un proveedor externo especializado. Tu documento no llega a los
            servidores de PlanVibe en ningún momento.
          </p>
        </div>
      </div>
    </AppShell>
  );
}
