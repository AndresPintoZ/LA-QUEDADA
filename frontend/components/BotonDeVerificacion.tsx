'use client';

import { useState, useTransition } from 'react';

import { iniciarVerificacion } from '@/lib/acciones/identidad';

/**
 * Botón que arranca la verificación.
 *
 * La acción devuelve una redirección al proveedor externo; si algo falla antes,
 * devuelve un mensaje de error que se muestra aquí sin sacar a la persona de la página.
 */
export default function BotonDeVerificacion() {
  const [enCurso, iniciarTransicion] = useTransition();
  const [error, setError] = useState<string | null>(null);

  return (
    <div className="flex flex-col gap-2">
      {error && (
        <p role="alert" className="rounded-[14px] bg-danger-bg px-3.5 py-3 text-sm text-danger">
          {error}
        </p>
      )}

      <button
        type="button"
        disabled={enCurso}
        onClick={() =>
          iniciarTransicion(async () => {
            setError(null);

            const resultado = await iniciarVerificacion();

            // Si la acción redirige, esto no llega a ejecutarse.
            if (resultado?.error) {
              setError(resultado.error);
            }
          })
        }
        className="w-full rounded-2xl bg-brand px-4 py-4 text-[17px] font-bold text-white disabled:opacity-60"
      >
        {enCurso ? 'Abriendo la pasarela…' : 'Empezar la verificación'}
      </button>
    </div>
  );
}
