import { defineConfig } from 'vitest/config';

/**
 * Configuración de las pruebas del frontend.
 *
 * Se prueba la lógica pura —formateo de fechas, distancias y estados— y no el
 * renderizado de cada componente. El motivo: las páginas son componentes de servidor
 * que apenas contienen lógica, y lo que sí puede fallar en silencio es un cálculo de
 * fechas mal hecho, que es justo lo que estas pruebas cubren.
 */
export default defineConfig({
  test: {
    environment: 'node',
    include: ['**/*.test.ts', '**/*.test.tsx'],
    exclude: ['node_modules/**', '.next/**'],

    // La zona horaria se fija para que las pruebas de fechas den el mismo resultado
    // en el equipo de cualquier persona y en integración continua.
    env: { TZ: 'Europe/Madrid' },
  },
});
