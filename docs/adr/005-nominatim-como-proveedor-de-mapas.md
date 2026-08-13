# ADR-005 · Leaflet y Nominatim como proveedor de mapas

**Estado:** aceptada · 2026-08-11

## Contexto

Hacen falta dos cosas distintas:

1. **Mostrar mapas**: el punto de encuentro de un plan y la vista general de explorar.
2. **Geocodificar**: convertir «Puente Adaja, Ávila» en coordenadas cuando alguien crea un plan.

Los proveedores comerciales (Mapbox, Google Maps) dan mejor calidad, pero exigen una clave de API y
una tarjeta de crédito antes de poder ver el mapa funcionando.

## Decisión

- **Mapas**: Leaflet con teselas de OpenStreetMap.
- **Geocodificación**: Nominatim, llamado **desde el servidor**, con caché en memoria.

Se usa la API de Leaflet directamente, sin `react-leaflet`: su versión estable no admite React 19 y
aquí solo hacen falta dos mapas sencillos.

La geocodificación va detrás de `IServicioDeGeocodificacion`, así que cambiar de proveedor afecta a
una sola clase.

## Consecuencias

**A favor:**

- `docker compose up` y el mapa funciona. Sin registros, sin claves, sin tarjeta.
- Sin coste ni cuotas mientras el piloto sea pequeño.
- La geocodificación se llama desde el servidor, así que la cuota no queda expuesta al público y
  el endpoint exige sesión.

**En contra:**

- Nominatim limita a **una petición por segundo** y exige identificarse con un `User-Agent` que
  incluya un contacto real. Se mitiga con caché de 60 minutos y un límite de peticiones propio.
- La calidad de la geocodificación es menor que la comercial, sobre todo con direcciones
  ambiguas. En Ávila, con nombres de sitios conocidos, es suficiente.
- Las teselas se piden desde el navegador, así que la política de seguridad de contenido debe
  permitir `tile.openstreetmap.org`. Es la única excepción de `img-src`.
- Si Nominatim no responde, no hay sugerencias de lugar. **No es un fallo bloqueante**: el
  formulario sigue funcionando y el punto se coloca a mano en el mapa. Está resuelto así en
  `ServicioNominatim`, que devuelve una lista vacía en lugar de propagar el error.

## Obligaciones que hay que cumplir

La política de uso de Nominatim no es opcional:

1. `User-Agent` con contacto real. Está en `NOMINATIM_USER_AGENT` del archivo `.env` y **hay que
   rellenarlo** con un correo válido.
2. Máximo una petición por segundo. Se cumple con la caché y el límite de peticiones.
3. Atribución visible a OpenStreetMap en el mapa. Está en ambos componentes de mapa.

Si el piloto crece, hay que pasar a un proveedor de pago o alojar una instancia propia de
Nominatim. Es un servicio gratuito sostenido por donaciones, no una API comercial.

## Alternativas descartadas

**Mapbox.** Mejor calidad y buen nivel gratuito. Descartada para el piloto porque obliga a dar de
alta una cuenta y configurar una clave antes de poder arrancar el entorno.

**Google Maps.** La mejor geocodificación. Descartada por lo mismo, más el coste, que empieza
antes.

**Geocodificación desde el navegador.** Un salto de red menos. Descartada porque expondría la
identificación de la aplicación y haría imposible cachear entre personas o limitar el uso.
