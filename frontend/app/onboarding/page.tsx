'use client';
import Link from 'next/link';
import { useState } from 'react';
import Logo from '@/components/Logo';
import { INTERESES } from '@/lib/catalogos';

export default function Onboarding() {
  const [elegidos, setElegidos] = useState<string[]>(['Senderismo', 'Bici', 'Conciertos']);
  const alternar = (i: string) =>
    setElegidos((prev) => (prev.includes(i) ? prev.filter((x) => x !== i) : [...prev, i]));

  return (
    <main className="flex min-h-dvh flex-col bg-ink md:items-center md:justify-center">
      <div className="mx-auto flex min-h-dvh w-full max-w-md flex-col px-6 pb-7 pt-4 md:min-h-0 md:py-10">
      <div className="flex items-center justify-between">
        <Logo size={24} tono="lime" conTexto oscuro />
        <span className="font-mono text-[11px] text-[#8FA3A9]">2 DE 3</span>
      </div>

      <div className="mt-8 flex flex-col gap-2.5">
        <h1 className="font-display text-3xl font-bold leading-[1.1] tracking-[-0.03em] text-white">¿Qué te mola hacer?</h1>
        <p className="text-[15px] leading-relaxed text-[#A9BCC1]">Elige 3 o más y te ponemos delante los planes que van contigo.</p>
      </div>

      <div className="mt-6 flex flex-wrap gap-2.5">
        {INTERESES.map((i) => {
          const on = elegidos.includes(i);
          return (
            <button key={i} onClick={() => alternar(i)} aria-pressed={on}
              className={`rounded-full border px-4 py-3 text-sm font-semibold ${on ? 'border-lime bg-lime text-ink' : 'border-white/20 text-white'}`}>
              {i}
            </button>
          );
        })}
      </div>

      <div className="mt-auto flex flex-col gap-3.5 pt-8">
        <div className="flex items-center gap-3 rounded-[18px] bg-white/[0.07] p-4">
          <span className="text-lg" aria-hidden>📍</span>
          <div className="flex flex-1 flex-col">
            <span className="text-sm font-bold text-white">Ávila</span>
            <span className="text-xs text-[#8FA3A9]">Radio de 10 km · lo cambias cuando quieras</span>
          </div>
          <button className="text-xs font-bold text-lime">Cambiar</button>
        </div>
        <Link href="/explorar" className="rounded-2xl bg-lime py-4 text-center text-[17px] font-bold text-ink">
          Vamos allá {elegidos.length > 0 && `(${elegidos.length})`}
        </Link>
        <Link href="/explorar" className="text-center text-xs text-[#8FA3A9]">Saltar por ahora</Link>
      </div>
      </div>
    </main>
  );
}
