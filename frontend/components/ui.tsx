import Link from 'next/link';

export function Chip({ children, activo = false }: { children: React.ReactNode; activo?: boolean }) {
  return (
    <span className={`whitespace-nowrap rounded-full px-3.5 py-2 text-[13px] ${activo ? 'bg-ink font-semibold text-white' : 'bg-paper font-medium text-ink'}`}>
      {children}
    </span>
  );
}

const tonos = {
  ok: 'text-ok bg-ok-bg', warn: 'text-warn bg-warn-bg', danger: 'text-danger bg-danger-bg',
  brand: 'text-brand-dark bg-brand-tint', neutro: 'text-muted bg-[#F0F4F3]',
} as const;

export function Badge({ tono = 'neutro', children }: { tono?: keyof typeof tonos; children: React.ReactNode }) {
  return <span className={`rounded-full px-2.5 py-1 text-[11px] font-bold ${tonos[tono]}`}>{children}</span>;
}

export function VerificadoBadge() {
  return <Badge tono="brand">✓ Verificado</Badge>;
}

export function BotonPrincipal({ children, href }: { children: React.ReactNode; href?: string }) {
  const clases = 'block w-full rounded-2xl bg-brand px-4 py-4 text-center text-[17px] font-bold text-white';
  return href ? <Link href={href} className={clases}>{children}</Link> : <button className={clases}>{children}</button>;
}

export function Etiqueta({ children }: { children: React.ReactNode }) {
  return <span className="font-mono text-[11px] tracking-[0.1em] text-muted">{children}</span>;
}

export function EstadoVacio({ icono, titulo, texto, acciones }: { icono: string; titulo: string; texto: string; acciones?: React.ReactNode }) {
  return (
    <div className="flex flex-col items-center gap-3.5 rounded-[26px] border border-[#DCE4E2] bg-white px-6 py-8 text-center">
      <span className="flex h-14 w-14 items-center justify-center rounded-[18px] bg-paper text-2xl" aria-hidden>{icono}</span>
      <span className="font-display text-xl font-bold">{titulo}</span>
      <span className="text-sm leading-relaxed text-body">{texto}</span>
      {acciones}
    </div>
  );
}
