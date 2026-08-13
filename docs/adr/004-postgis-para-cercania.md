# ADR-004 · PostGIS con columna calculada para la búsqueda por cercanía

**Estado:** aceptada · 2026-08-11

## Contexto

RF-06 pide filtrar planes por distancia. La pantalla principal muestra «qué hay cerca y pronto»,
así que la consulta por cercanía se ejecuta en cada visita: es la consulta más caliente del
sistema.

Con latitud y longitud en dos columnas `double`, calcular la distancia obliga a evaluar una
fórmula por fila. Ningún índice puede ayudar, así que cada búsqueda recorre la tabla entera.

## Decisión

PostGIS, con una columna geográfica **calculada por la base de datos**:

```sql
ubicacion geography(Point,4326)
  GENERATED ALWAYS AS (ST_SetSRID(ST_MakePoint(longitud, latitud), 4326)::geography) STORED
```

Con un índice GIST sobre ella. En EF Core se declara como **propiedad sombra**: existe en el
modelo de datos pero no en el agregado.

## Consecuencias

**A favor:**

- «Qué hay a menos de 5 km» se resuelve por índice en lugar de recorriendo la tabla.
- Al ser **generada**, no puede quedar desincronizada con las coordenadas: no hay forma de
  escribirla a mano ni de olvidarse de actualizarla al mover un punto de encuentro.
- El tipo `geography` calcula sobre el elipsoide terrestre, así que las distancias son metros
  reales y no grados. Con `geometry` habría que proyectar antes.
- El dominio sigue sin conocer NetTopologySuite: `Coordenadas` es un objeto de valor propio, sin
  dependencias de infraestructura.

**En contra:**

- Ata el proyecto a PostgreSQL con PostGIS. No es un problema: ya está elegido en
  `docs/02-arquitectura.md` y la imagen `postgis/postgis` lo trae listo.
- Las consultas por cercanía usan `EF.Property<Point>(q, "ubicacion")`, que es menos legible que
  una propiedad normal. Está encapsulado en `ConsultasDeQuedadas` y la constante del nombre de
  columna vive junto a su explicación.
- Las pruebas de integración necesitan un PostgreSQL real: un proveedor en memoria no tiene
  PostGIS. Se resuelve con Testcontainers.

## Alternativas descartadas

**Haversine en SQL sobre latitud y longitud.** No usa índice: recorrido completo en cada búsqueda.
Aceptable con cien planes, no con los miles que se esperan si el piloto funciona.

**Filtrar por un rectángulo de latitud/longitud y afinar en memoria.** Es la aproximación clásica
sin PostGIS y funciona razonablemente. Descartada porque PostGIS ya está disponible sin coste y
resuelve el caso correctamente, incluida la ordenación por cercanía, que el rectángulo no da.

**Guardar la distancia precalculada a un punto fijo.** Solo sirve si todo el mundo busca desde el
mismo sitio. En cuanto se use la ubicación real de la persona, deja de valer.

**Mantener la columna con un disparador (trigger).** Equivalente en resultado, pero un disparador
es código que puede fallar o desactivarse. Una columna generada es una garantía del motor.
