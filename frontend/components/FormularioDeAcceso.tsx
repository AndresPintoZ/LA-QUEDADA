'use client';

import Link from 'next/link';
import { useActionState } from 'react';

import { iniciarSesion, registrarse, type EstadoDeFormulario } from '@/lib/acciones/identidad';
import { Campo } from './Campo';

/**
 * Formulario de entrada y de registro.
 *
 * Usa acciones de servidor con `useActionState`, así que funciona incluso antes de que
 * cargue el JavaScript de la página: el navegador envía el formulario y el servidor
 * responde. Con la red móvil de un pueblo, eso no es un detalle menor.
 */

const ESTADO_INICIAL: EstadoDeFormulario = {};

export default function FormularioDeAcceso({ esRegistro }: { esRegistro: boolean }) {
  const [estado, enviar, enCurso] = useActionState(esRegistro ? registrarse : iniciarSesion, ESTADO_INICIAL);

  return (
    <form action={enviar} className="flex flex-col gap-4" noValidate>
      {estado.error && (
        <p role="alert" className="rounded-[14px] bg-danger-bg px-3.5 py-3 text-sm text-danger">
          {estado.error}
        </p>
      )}

      <Campo
        nombre="correo"
        etiqueta="Correo electrónico"
        tipo="email"
        autoComplete="email"
        requerido
        errores={estado.errores?.correo}
      />

      <Campo
        nombre="contrasena"
        etiqueta="Contraseña"
        tipo="password"
        // En el registro se le dice al gestor de contraseñas que proponga una nueva;
        // al entrar, que rellene la guardada.
        autoComplete={esRegistro ? 'new-password' : 'current-password'}
        requerido
        ayuda={esRegistro ? 'Al menos 12 caracteres. Una frase que recuerdes funciona muy bien.' : undefined}
        errores={estado.errores?.contrasena}
      />

      {esRegistro && (
        <>
          <Campo
            nombre="nombreVisible"
            etiqueta="¿Cómo quieres que te vean?"
            autoComplete="nickname"
            requerido
            ayuda="No tiene que ser tu nombre completo."
            errores={estado.errores?.nombreVisible}
          />

          <Campo nombre="ciudad" etiqueta="Ciudad" defaultValue="Ávila" errores={estado.errores?.ciudad} />

          <Campo
            nombre="anioDeNacimiento"
            etiqueta="Año de nacimiento"
            tipo="number"
            requerido
            // Solo el año, nunca la fecha completa: es el dato mínimo que permite
            // comprobar la edad mínima (docs/04-seguridad-privacidad-moderacion.md).
            ayuda="Solo el año. No guardamos tu fecha de nacimiento completa."
            errores={estado.errores?.anioDeNacimiento}
          />

          <label className="flex items-start gap-3 rounded-[14px] bg-paper px-3.5 py-3">
            <input type="checkbox" name="aceptaNormas" required className="mt-0.5 h-5 w-5 shrink-0 accent-[#0B7C9B]" />
            <span className="text-[13px] leading-snug text-body">
              He leído y acepto las{' '}
              <Link href="/normas" className="font-bold text-brand underline">
                normas de la comunidad
              </Link>{' '}
              y la{' '}
              <Link href="/privacidad" className="font-bold text-brand underline">
                política de privacidad
              </Link>
              .
            </span>
          </label>

          {estado.errores?.aceptaNormas && (
            <p role="alert" className="text-[13px] text-danger">
              {estado.errores.aceptaNormas[0]}
            </p>
          )}
        </>
      )}

      <button
        type="submit"
        disabled={enCurso}
        className="mt-2 w-full rounded-2xl bg-brand px-4 py-4 text-[17px] font-bold text-white disabled:opacity-60"
      >
        {enCurso ? 'Un momento…' : esRegistro ? 'Crear mi cuenta' : 'Entrar'}
      </button>
    </form>
  );
}
