type Props = { size?: number; tono?: 'brand' | 'lime'; conTexto?: boolean; oscuro?: boolean };

/** Pin de mapa con la "P" — marca de PlanVibe. */
export default function Logo({ size = 30, tono = 'brand', conTexto = false, oscuro = false }: Props) {
  const fondo = tono === 'lime' ? '#D8F45A' : '#0B7C9B';
  const letra = tono === 'lime' ? '#0C1A22' : '#D8F45A';
  return (
    <span className="flex items-center gap-3">
      <span
        className="flex items-center justify-center"
        style={{ width: size, height: size, background: fondo, borderRadius: '50% 50% 50% 12%', transform: 'rotate(-45deg)' }}
      >
        <span className="font-display font-bold leading-none" style={{ transform: 'rotate(45deg)', fontSize: size * 0.5, color: letra }}>
          P
        </span>
      </span>
      {conTexto && (
        <span className={`font-display text-xl font-semibold tracking-tight ${oscuro ? 'text-white' : 'text-ink'}`}>
          Plan<span className={oscuro ? 'text-lime' : 'text-brand'}>Vibe</span>
        </span>
      )}
    </span>
  );
}
