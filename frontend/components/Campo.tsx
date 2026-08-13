'use client';

import { useId } from 'react';

/**
 * Campo de formulario accesible.
 *
 * Concentra aquí lo que es fácil olvidar campo a campo y que decide si un formulario
 * se puede usar con lector de pantalla:
 *
 * - la etiqueta va asociada al control con `htmlFor`, no colocada al lado;
 * - el texto de ayuda y el de error se enlazan con `aria-describedby`, así que se leen
 *   junto al campo en lugar de quedar huérfanos;
 * - `aria-invalid` marca el campo con error para quien no ve el color rojo.
 */

interface Propiedades {
  nombre: string;
  etiqueta: string;
  tipo?: 'text' | 'email' | 'password' | 'number' | 'date' | 'time';
  autoComplete?: string;
  requerido?: boolean;
  ayuda?: string;
  errores?: string[];
  defaultValue?: string | number;
  placeholder?: string;
}

export function Campo({
  nombre,
  etiqueta,
  tipo = 'text',
  autoComplete,
  requerido = false,
  ayuda,
  errores,
  defaultValue,
  placeholder,
}: Propiedades) {
  const id = useId();
  const idDeAyuda = `${id}-ayuda`;
  const idDeError = `${id}-error`;
  const tieneError = Boolean(errores?.length);

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-semibold text-ink">
        {etiqueta}
        {requerido && <span className="text-danger"> *</span>}
      </label>

      <input
        id={id}
        name={nombre}
        type={tipo}
        autoComplete={autoComplete}
        required={requerido}
        defaultValue={defaultValue}
        placeholder={placeholder}
        aria-invalid={tieneError}
        aria-describedby={[ayuda ? idDeAyuda : null, tieneError ? idDeError : null].filter(Boolean).join(' ') || undefined}
        // El área de toque mínima (py-3.5 sobre texto de 16 px) da unos 48 px de alto,
        // que es la recomendación para que se pueda pulsar con el pulgar sin fallar.
        className={`rounded-[14px] border px-3.5 py-3.5 text-base text-ink outline-none focus-visible:ring-2 focus-visible:ring-brand ${
          tieneError ? 'border-danger bg-danger-bg' : 'border-line bg-white'
        }`}
      />

      {ayuda && (
        <p id={idDeAyuda} className="text-[13px] text-muted">
          {ayuda}
        </p>
      )}

      {tieneError && (
        <p id={idDeError} role="alert" className="text-[13px] font-medium text-danger">
          {errores![0]}
        </p>
      )}
    </div>
  );
}

/** Área de texto con las mismas garantías de accesibilidad que <see cref="Campo"/>. */
export function CampoLargo({
  nombre,
  etiqueta,
  ayuda,
  errores,
  filas = 4,
  requerido = false,
  defaultValue,
  placeholder,
}: {
  nombre: string;
  etiqueta: string;
  ayuda?: string;
  errores?: string[];
  filas?: number;
  requerido?: boolean;
  defaultValue?: string;
  placeholder?: string;
}) {
  const id = useId();
  const idDeAyuda = `${id}-ayuda`;
  const idDeError = `${id}-error`;
  const tieneError = Boolean(errores?.length);

  return (
    <div className="flex flex-col gap-1.5">
      <label htmlFor={id} className="text-sm font-semibold text-ink">
        {etiqueta}
        {requerido && <span className="text-danger"> *</span>}
      </label>

      <textarea
        id={id}
        name={nombre}
        rows={filas}
        required={requerido}
        defaultValue={defaultValue}
        placeholder={placeholder}
        aria-invalid={tieneError}
        aria-describedby={[ayuda ? idDeAyuda : null, tieneError ? idDeError : null].filter(Boolean).join(' ') || undefined}
        className={`rounded-[14px] border px-3.5 py-3 text-base text-ink outline-none focus-visible:ring-2 focus-visible:ring-brand ${
          tieneError ? 'border-danger bg-danger-bg' : 'border-line bg-white'
        }`}
      />

      {ayuda && (
        <p id={idDeAyuda} className="text-[13px] text-muted">
          {ayuda}
        </p>
      )}

      {tieneError && (
        <p id={idDeError} role="alert" className="text-[13px] font-medium text-danger">
          {errores![0]}
        </p>
      )}
    </div>
  );
}
