import Link from 'next/link';

import AppShell from '@/components/AppShell';
import Logo from '@/components/Logo';
import PlanCard from '@/components/PlanCard';
import { Chip, Etiqueta, EstadoVacio } from '@/components/ui';
import { buscarPlanes, CENTRO_DEL_PILOTO, obtenerCategorias, obtenerMiPerfil } from '@/lib/datos';

/**
 * Explorar: la pantalla principal (RF-05, RF-06).
 *
 * Es un componente de servidor, así que el HTML llega con los planes dentro y no hay
 * un momento de pantalla vacía esperando a que responda una petición desde el navegador.
 * Es lo que hace que la primera carga sea rápida en móvil (RNF-02).
 *
 * Responsive: una columna de tarjetas en móvil; rejilla de dos y tres columnas en
 * pantallas mayores. La cabecera con el logo solo aparece en móvil, porque en
 * escritorio ya está la barra superior compartida.
 */

export const metadata = {
  title: 'Explorar planes — PlanVibe',
  description: 'Descubre qué hay cerca y pronto en Ávila.',
};

interface Parametros {
  searchParams: Promise<{ texto?: string; categoria?: string; conPlazas?: string; radio?: string }>;
}

export default async function Explorar({ searchParams }: Parametros) {
  const filtros = await searchParams;
  const radioEnMetros = Number(filtros.radio) || 5000;

  const [resultado, categorias, perfil] = await Promise.all([
    buscarPlanes({
      texto: filtros.texto,
      categorias: filtros.categoria ? [filtros.categoria] : undefined,
      soloConPlazas: filtros.conPlazas === '1',
      latitud: CENTRO_DEL_PILOTO.latitud,
      longitud: CENTRO_DEL_PILOTO.longitud,
      radioEnMetros,
    }),
    obtenerCategorias(),
    obtenerMiPerfil(),
  ]);

  const categoriaActiva = categorias.find((c) => c.id === filtros.categoria);

  return (
    <AppShell
      cabecera={
        <div className="flex flex-col gap-3.5 px-5 pb-4 pt-3 md:px-6">
          {/* Cabecera propia de móvil: en escritorio, el logo y la sesión ya están en la barra superior. */}
          <div className="flex items-center justify-between md:hidden">
            <span className="flex items-center gap-2.5">
              <Logo size={26} />
              <span className="font-display text-[19px] font-semibold tracking-tight">Ávila</span>
            </span>

            {perfil ? (
              <Link
                href="/perfil"
                className="flex h-10 w-10 items-center justify-center rounded-[14px] bg-brand-tint text-[13px] font-bold text-brand-dark"
                aria-label="Ir a mi perfil"
              >
                {perfil.nombreVisible.slice(0, 2).toUpperCase()}
              </Link>
            ) : (
              <Link href="/acceso" className="rounded-[14px] bg-ink px-4 py-2.5 text-sm font-bold text-white">
                Entrar
              </Link>
            )}
          </div>

          <div className="hidden items-center justify-between md:flex">
            <h1 className="font-display text-[22px] font-bold tracking-tight">Planes cerca de Ávila</h1>
          </div>

          {/* Filtros. Van como enlaces y no como botones de JavaScript para que
              funcionen sin scripts y para que cada combinación tenga su propia URL
              compartible. */}
          <div className="flex gap-2 overflow-x-auto pb-1">
            <FiltroChip href="/explorar" activo={!filtros.categoria && filtros.conPlazas !== '1'}>
              Todo
            </FiltroChip>

            <FiltroChip href="/explorar?conPlazas=1" activo={filtros.conPlazas === '1'}>
              Con plazas
            </FiltroChip>

            {categorias.map((categoria) => (
              <FiltroChip
                key={categoria.id}
                href={`/explorar?categoria=${categoria.id}`}
                activo={filtros.categoria === categoria.id}
              >
                {categoria.nombre}
              </FiltroChip>
            ))}
          </div>

          <div className="flex rounded-[14px] bg-paper p-1 md:max-w-xs">
            <span className="flex-1 rounded-xl bg-white py-2.5 text-center text-sm font-bold shadow-sm">Lista</span>
            <Link href="/mapa" className="flex-1 rounded-xl py-2.5 text-center text-sm font-medium text-muted">
              Mapa
            </Link>
          </div>
        </div>
      }
    >
      <div className="flex flex-col gap-3.5 p-5 md:px-6 md:py-6">
        <Etiqueta>
          {resultado.total === 0
            ? 'NINGÚN PLAN CON ESTOS FILTROS'
            : `${resultado.total} ${resultado.total === 1 ? 'PLAN' : 'PLANES'} CERCA DE TI`}
        </Etiqueta>

        {resultado.elementos.length === 0 ? (
          <div className="md:max-w-md">
            <EstadoVacio
              icono="🗺"
              titulo={categoriaActiva ? `Aún no hay planes de ${categoriaActiva.nombre.toLowerCase()}` : 'Todavía no hay planes por aquí'}
              texto="Prueba a ampliar el radio o la fecha. Y si no encuentras nada que te encaje, monta tú el primero: alguien lo estará buscando."
              acciones={
                <div className="flex w-full flex-col gap-2">
                  {filtros.categoria || filtros.conPlazas ? (
                    <Link href="/explorar" className="rounded-2xl bg-paper px-4 py-3 text-center text-sm font-bold text-ink">
                      Quitar los filtros
                    </Link>
                  ) : null}

                  <Link href="/crear" className="rounded-2xl bg-brand px-4 py-3 text-center text-sm font-bold text-white">
                    Crear un plan
                  </Link>
                </div>
              }
            />
          </div>
        ) : (
          <div className="grid gap-3.5 sm:grid-cols-2 xl:grid-cols-3">
            {resultado.elementos.map((plan) => (
              <PlanCard key={plan.id} plan={plan} />
            ))}
          </div>
        )}
      </div>
    </AppShell>
  );
}

function FiltroChip({ href, activo, children }: { href: string; activo: boolean; children: React.ReactNode }) {
  return (
    <Link href={href} aria-current={activo ? 'true' : undefined}>
      <Chip activo={activo}>{children}</Chip>
    </Link>
  );
}
