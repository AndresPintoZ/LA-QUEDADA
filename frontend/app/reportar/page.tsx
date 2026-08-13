'use client';
import { useState } from 'react';
import { EstadoVacio } from '@/components/ui';
import { MOTIVOS_DE_REPORTE } from '@/lib/catalogos';

/** RF-17: hoja de reporte. Nunca revelar la identidad de quien reporta. */
export default function Reportar() {
  const [motivo, setMotivo] = useState<string>(MOTIVOS_DE_REPORTE[0].texto);
  const [enviado, setEnviado] = useState(false);

  if (enviado) {
    return (
      <main className="mx-auto flex min-h-dvh w-full max-w-md items-center p-5">
        <EstadoVacio icono="✓" titulo="Reporte recibido"
          texto="Gracias. Lo revisamos en menos de 24 h. No compartimos información sobre otras personas." />
      </main>
    );
  }

  return (
    <main className="mx-auto flex min-h-dvh w-full flex-col justify-end bg-ink/40 md:items-center md:justify-center">
      {/* En móvil es una hoja que sube desde abajo; en escritorio, un diálogo centrado. */}
      <div className="w-full max-w-md">
        <div className="flex flex-col gap-4 rounded-t-[26px] bg-white px-5 pb-6 pt-5 md:rounded-[26px]">
        <span className="mx-auto h-1 w-11 rounded-full bg-[#DCE4E2]" />
        <div className="flex flex-col gap-1.5">
          <h1 className="font-display text-[22px] font-bold tracking-tight">¿Qué pasa con esta publicación?</h1>
          <p className="text-[13px] text-body">Lo revisa una persona del equipo. El autor no sabe quién lo ha reportado.</p>
        </div>
        <fieldset className="flex flex-col gap-2.5">
          {MOTIVOS_DE_REPORTE.map(({ texto: m }) => {
            const on = motivo === m;
            return (
              <label key={m} className={'flex items-center gap-3 rounded-[14px] p-3.5 text-sm ' + (on ? 'border-2 border-brand bg-[#F5FBFD] font-semibold' : 'border border-[#E0E7E5]')}>
                <input type="radio" name="motivo" checked={on} onChange={() => setMotivo(m)} className="h-5 w-5 accent-brand" />
                {m}
              </label>
            );
          })}
        </fieldset>
        <textarea rows={2} placeholder="Cuéntanos algo más (opcional)"
          className="rounded-[14px] border border-[#DCE4E2] p-3.5 text-[13px] outline-none focus:border-brand" />
        <button onClick={() => setEnviado(true)} className="rounded-2xl bg-ink py-4 text-base font-bold text-white">Enviar el reporte</button>
        </div>
      </div>
    </main>
  );
}
