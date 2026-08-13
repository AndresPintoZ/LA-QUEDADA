/**
 * Listas cerradas que la interfaz necesita y que todavía no gestiona la API.
 *
 * Están aquí, y no en un archivo de datos de ejemplo, porque son contenido real de
 * producto: los intereses del perfil y los motivos de reporte no son datos de prueba.
 *
 * Cuando administración pueda editarlos (RF: «Administrador gestiona categorías y
 * reglas»), pasarán a ser un catálogo de la API igual que las categorías.
 */

/** Intereses que se pueden elegir en el perfil (RF-02). */
export const INTERESES = [
  'Senderismo',
  'Bici',
  'Running',
  'Conciertos',
  'Festivales',
  'Cultura y museos',
  'Juegos de mesa',
  'Tecnología',
  'Arte y creatividad',
  'Voluntariado',
  'Escalada',
  'Tapas y quedadas',
] as const;

/**
 * Motivos de reporte (RF-17).
 *
 * La lista es corta a propósito. Un desplegable con veinte categorías legales hace que
 * la gente elija la primera que ve o abandone; con cinco opciones claras, el motivo que
 * llega a moderación es más fiable.
 */
export const MOTIVOS_DE_REPORTE = [
  { clave: 'peligroso', texto: 'Es peligroso o inseguro' },
  { clave: 'spam', texto: 'Es spam o publicidad' },
  { clave: 'acoso', texto: 'Contenido ofensivo o acoso' },
  { clave: 'informacion_falsa', texto: 'La información es falsa' },
  { clave: 'datos_personales', texto: 'Expone datos personales de alguien' },
] as const;

/** Versión vigente de las normas de comunidad que se muestra en el registro. */
export const VERSION_DE_NORMAS = '2026-08';
