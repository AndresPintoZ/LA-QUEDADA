import BarraSuperior from './BarraSuperior';
import BottomNav from './BottomNav';

/**
 * Armazón responsive de todas las pantallas de la aplicación.
 *
 * Sustituye al antiguo `PhoneShell`, que encerraba cada pantalla en una columna de
 * 430 px: aquello venía de los mockups, donde cada pantalla se presentaba dentro de un
 * marco de teléfono, y en un monitor producía una franja estrecha rodeada de vacío.
 *
 * PlanVibe es una aplicación web responsive: una sola estructura que se adapta.
 *
 * - **Móvil (< md):** cabecera de página arriba, contenido a ancho completo y barra de
 *   navegación inferior, como en el diseño original.
 * - **Escritorio (≥ md):** barra de navegación superior compartida, contenido centrado
 *   con un ancho máximo legible y sin barra inferior.
 *
 * La coherencia visual entre pantallas depende de que TODAS pasen por aquí: si una
 * página monta su propio armazón, vuelve a parecer de otra aplicación.
 */

interface Propiedades {
  children: React.ReactNode;
  /** Contenido de la cabecera de página (título, filtros). El armazón pone el fondo y el borde. */
  cabecera?: React.ReactNode;
  /** Contenido fijado abajo (p. ej. el botón principal de una ficha). */
  pie?: React.ReactNode;
  /** Barra de navegación inferior en móvil. Se quita en flujos donde estorba (formularios largos). */
  conNav?: boolean;
  /**
   * Ancho máximo del contenido en escritorio.
   * `ancha` para rejillas de tarjetas; `media` para fichas; `estrecha` para formularios.
   */
  anchura?: 'ancha' | 'media' | 'estrecha';
}

const ANCHURAS = {
  ancha: 'max-w-6xl',
  media: 'max-w-3xl',
  estrecha: 'max-w-2xl',
} as const;

export default function AppShell({ children, cabecera, pie, conNav = true, anchura = 'ancha' }: Propiedades) {
  const anchoMaximo = ANCHURAS[anchura];

  return (
    <div className="flex min-h-dvh flex-col bg-paper">
      <BarraSuperior />

      {cabecera && (
        <div className="shrink-0 border-b border-line bg-white">
          <div className={`mx-auto w-full ${anchoMaximo}`}>{cabecera}</div>
        </div>
      )}

      <div className="min-h-0 flex-1 overflow-y-auto">
        <div className={`mx-auto w-full ${anchoMaximo}`}>{children}</div>
      </div>

      {pie && (
        <div className="shrink-0 border-t border-line bg-white">
          <div className={`mx-auto w-full ${anchoMaximo}`}>{pie}</div>
        </div>
      )}

      {conNav && <BottomNav />}
    </div>
  );
}
