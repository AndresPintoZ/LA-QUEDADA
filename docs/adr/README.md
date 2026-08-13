# Registro de decisiones de arquitectura

Cada archivo de esta carpeta recoge **una decisión estructural**: qué se decidió, qué alternativas
se descartaron y, sobre todo, **por qué**.

## Para qué sirve esto

El código dice *qué* hace el sistema. Estos documentos dicen *por qué* lo hace así, que es
justamente lo que se pierde cuando cambia la gente del equipo. Sin ellos, quien llegue dentro de un
año verá una decisión rara, asumirá que fue un descuido y la «arreglará» sin saber qué problema
resolvía.

También sirven para lo contrario: si el contexto cambió y la decisión ya no tiene sentido, aquí
está escrito qué la motivaba, así que se puede revisar con criterio en lugar de por intuición.

## Formato

Cada ADR tiene:

- **Estado**: propuesta, aceptada, sustituida por ADR-XXX, o descartada.
- **Contexto**: qué problema había y qué restricciones existían.
- **Decisión**: qué se hace.
- **Consecuencias**: lo bueno y lo malo. Toda decisión tiene coste; si un ADR no lo menciona,
  está incompleto.
- **Alternativas descartadas**: qué más se valoró y por qué no se eligió.

Un ADR **no se edita** una vez aceptado. Si la decisión cambia, se escribe uno nuevo que sustituye
al anterior y se marca el viejo como sustituido. El histórico es parte del valor.

## Índice

| ADR | Decisión | Estado |
|---|---|---|
| [001](001-arquitectura-limpia-con-ddd.md) | Arquitectura limpia con DDD táctico | Aceptada |
| [002](002-autenticacion-propia-con-jwt.md) | Autenticación propia con JWT y renovación rotativa | Aceptada |
| [003](003-bff-en-next.md) | BFF en Next en lugar de llamadas directas del navegador | Aceptada |
| [004](004-postgis-para-cercania.md) | PostGIS con columna calculada para la búsqueda por cercanía | Aceptada |
| [005](005-nominatim-como-proveedor-de-mapas.md) | Leaflet y Nominatim como proveedor de mapas | Aceptada |
| [006](006-verificacion-sin-almacenar-documentos.md) | Verificación de identidad sin almacenar documentos | Aceptada |
| [007](007-cqrs-ligero-sin-mediador.md) | CQRS ligero sin biblioteca mediadora | Aceptada |
