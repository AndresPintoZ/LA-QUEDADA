/**
 * Configuración de Next.js para PlanVibe.
 *
 * Aquí viven las cabeceras de seguridad de la parte que sirve HTML. La API .NET
 * pone las suyas por separado: cada una protege lo que sirve.
 */

/**
 * Política de seguridad de contenido.
 *
 * Es la defensa de fondo contra XSS: aunque alguien lograra inyectar un script,
 * el navegador se negaría a ejecutarlo si no cumple esta política.
 *
 * - `'unsafe-inline'` en los estilos es necesario para Tailwind y para los estilos
 *   en línea de Leaflet. En los scripts NO se permite.
 * - `connect-src 'self'` basta porque el navegador solo habla con el BFF de Next;
 *   quien llama a la API .NET es el servidor, no el navegador.
 * - Las teselas del mapa llegan de tile.openstreetmap.org, así que `img-src` las incluye.
 */
const politicaDeSeguridadDeContenido = [
  "default-src 'self'",
  "script-src 'self'",
  "style-src 'self' 'unsafe-inline'",
  "img-src 'self' data: blob: https://*.tile.openstreetmap.org https://commons.wikimedia.org https://upload.wikimedia.org",
  "font-src 'self' data:",
  "connect-src 'self'",
  // Sin formularios hacia terceros y sin poder ser enmarcada: cierra el camino
  // al robo de credenciales por superposición de un marco invisible.
  "form-action 'self'",
  "frame-ancestors 'none'",
  "base-uri 'self'",
  "object-src 'none'",
  "upgrade-insecure-requests",
].join('; ');

const cabecerasDeSeguridad = [
  { key: 'Content-Security-Policy', value: politicaDeSeguridadDeContenido },
  { key: 'X-Content-Type-Options', value: 'nosniff' },
  { key: 'X-Frame-Options', value: 'DENY' },
  // No se filtra a terceros qué plan concreto estaba viendo la persona.
  { key: 'Referrer-Policy', value: 'strict-origin-when-cross-origin' },
  { key: 'Permissions-Policy', value: 'camera=(), microphone=(), payment=(), geolocation=(self)' },
  { key: 'Cross-Origin-Opener-Policy', value: 'same-origin' },
];

/** @type {import('next').NextConfig} */
const configuracion = {
  // Genera un servidor autónomo con solo las dependencias que se usan: la imagen
  // de Docker baja de ~1 GB a poco más de 100 MB.
  output: 'standalone',

  reactStrictMode: true,

  // No se anuncia el framework ni su versión.
  poweredByHeader: false,

  images: {
    remotePatterns: [
      { protocol: 'https', hostname: 'commons.wikimedia.org' },
      { protocol: 'https', hostname: 'upload.wikimedia.org' },
    ],
  },

  async headers() {
    return [{ source: '/:ruta*', headers: cabecerasDeSeguridad }];
  },
};

export default configuracion;
