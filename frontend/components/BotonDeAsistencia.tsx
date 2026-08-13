'use client';

import Link from 'next/link';
import { useState, useTransition } from 'react';

import { abandonar, apuntarse } from '@/lib/acciones/quedadas';
import type { EstadoAsistencia, EstadoQuedada } from '@/lib/tipos';

/**
 * Botón principal del detalle de un plan (RF-14, RF-15).
 *
 * Es el único componente de cliente de la pantalla: todo lo demás se renderiza en el
 * servidor. Necesita estado propio porque tiene que mostrar el resultado de la acción
 * sin recargar la página, y distinguir entre «tienes plaza» y «estás en lista de espera»,
 * que son cosas muy distintas para quien está decidiendo si contar con el sábado.
 */

interface Propiedades {
  quedadaId: string;
  estadoDelPlan: EstadoQuedada;
  miAsistencia: EstadoAsistencia | null;
  miPosicionEnListaDeEspera: number | null;
  hayPlazas: boolean;
  soyElOrganizador: boolean;
  haIniciadoSesion: boolean;
}

export default function BotonDeAsistencia({
  quedadaId,
  estadoDelPlan,
  miAsistencia,
  miPosicionEnListaDeEspera,
  hayPlazas,
  soyElOrganizador,
  haIniciadoSesion,
}: Propiedades) {
  const [enCurso, iniciarTransicion] = useTransition();
  const [aviso, setAviso] = useState<{ texto: string; esError: boolean } | null>(null);

  if (!haIniciadoSesion) {
    return (
      <Link
        href={`/acceso?volverA=/plan/${quedadaId}`}
        className="block w-full rounded-2xl bg-brand px-4 py-4 text-center text-[17px] font-bold text-white"
      >
        Entra para apuntarte
      </Link>
    );
  }

  if (soyElOrganizador) {
    return (
      <div className="flex flex-col gap-2">
        <p className="text-center text-sm text-muted">Este plan lo organizas tú.</p>
        <Link
          href={`/plan/${quedadaId}/gestionar`}
          className="block w-full rounded-2xl bg-paper px-4 py-4 text-center text-[17px] font-bold text-ink"
        >
          Gestionar el plan
        </Link>
      </div>
    );
  }

  if (estadoDelPlan !== 'Publicada') {
    return (
      <Link
        href="/explorar"
        className="block w-full rounded-2xl bg-paper px-4 py-4 text-center text-[17px] font-bold text-ink"
      >
        Buscar otros planes
      </Link>
    );
  }

  const estoyApuntado = miAsistencia === 'Confirmada' || miAsistencia === 'EnListaDeEspera';

  function ejecutar(accion: () => Promise<{ error?: string; mensaje?: string }>) {
    setAviso(null);

    iniciarTransicion(async () => {
      const resultado = await accion();

      setAviso(
        resultado.error
          ? { texto: resultado.error, esError: true }
          : { texto: resultado.mensaje ?? 'Hecho.', esError: false },
      );
    });
  }

  return (
    <div className="flex flex-col gap-2.5">
      {/* El aviso usa aria-live para que un lector de pantalla lo anuncie al aparecer:
          si no, el cambio pasaría desapercibido para quien no ve la pantalla. */}
      {aviso && (
        <p
          role="status"
          aria-live="polite"
          className={`rounded-[14px] px-3.5 py-2.5 text-sm ${aviso.esError ? 'bg-danger-bg text-danger' : 'bg-ok-bg text-ok'}`}
        >
          {aviso.texto}
        </p>
      )}

      {miAsistencia === 'EnListaDeEspera' && miPosicionEnListaDeEspera !== null && (
        <p className="rounded-[14px] bg-warn-bg px-3.5 py-2.5 text-sm text-warn">
          Estás en lista de espera, en la posición {miPosicionEnListaDeEspera}. Te avisamos si se libera una plaza.
        </p>
      )}

      {estoyApuntado ? (
        <button
          type="button"
          disabled={enCurso}
          onClick={() => ejecutar(() => abandonar(quedadaId))}
          className="w-full rounded-2xl border border-line bg-white px-4 py-4 text-[17px] font-bold text-ink disabled:opacity-60"
        >
          {enCurso ? 'Un momento…' : 'Ya no puedo ir'}
        </button>
      ) : (
        <button
          type="button"
          disabled={enCurso}
          onClick={() => ejecutar(() => apuntarse(quedadaId))}
          className="w-full rounded-2xl bg-brand px-4 py-4 text-[17px] font-bold text-white disabled:opacity-60"
        >
          {enCurso ? 'Un momento…' : hayPlazas ? '¡Me apunto!' : 'Apuntarme a la lista de espera'}
        </button>
      )}
    </div>
  );
}
