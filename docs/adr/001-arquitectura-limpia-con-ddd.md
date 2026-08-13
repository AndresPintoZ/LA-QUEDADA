# ADR-001 · Arquitectura limpia con DDD táctico

**Estado:** aceptada · 2026-08-11

## Contexto

PlanVibe es un MVP para validar una hipótesis con veinte personas en Ávila. Podría escribirse en
una tarde con un CRUD sobre EF Core y salir adelante.

Pero el producto tiene reglas que **no son CRUD** y que, si se equivocan, hacen daño real:

- Que dos personas se apunten a la última plaza y ambas se presenten.
- Que alguien publique un plan sin estar verificado (RF-09, RF-20).
- Que una persona menor de edad organice un encuentro público (RF-24).
- Que la dirección exacta de un punto de encuentro se filtre a quien no tiene plaza.
- Que se revoque una verificación y el usuario siga pudiendo publicar (RF-23).

Ninguna de estas reglas es una validación de formulario. Son invariantes: propiedades que el
sistema debe cumplir **siempre**, venga la orden de donde venga.

## Decisión

Arquitectura limpia en cuatro capas, con DDD táctico en el dominio.

```
Api → Infrastructure → Application → Domain
```

Las dependencias apuntan hacia dentro. El proyecto `PlanVibe.Domain` no referencia ningún paquete
de infraestructura: ni EF Core, ni ASP.NET, ni Npgsql.

Del catálogo de DDD se usan **agregados, objetos de valor, eventos de dominio y repositorios**.
No se usan sagas, event sourcing ni contextos delimitados separados: el sistema no es lo bastante
grande y añadirlos sería complejidad sin contrapartida.

Los agregados son `Usuario`, `Quedada` y `Categoria`. `Asistencia` es entidad hija de `Quedada` y
solo se modifica a través de ella.

El lenguaje del dominio está **en español**, porque es el idioma en el que se habla del producto.
`quedada.Apuntar(usuarioId, ahora)` se lee igual que la conversación en la que se decidió esa regla.

## Consecuencias

**A favor:**

- Las reglas están en un solo sitio y probadas sin base de datos: las 50 pruebas de dominio tardan
  125 ms, así que se ejecutan en cada guardado sin pensárselo.
- Es imposible saltarse la comprobación de capacidad: no hay ningún camino que inserte una
  asistencia sin pasar por `Quedada.Apuntar`.
- Cambiar de proveedor de mapas o de verificación afecta a una carpeta.
- Las pruebas de casos de uso no necesitan base de datos.

**En contra:**

- Más archivos y más ceremonia que un CRUD. Para las categorías, que sí son un CRUD, sobra
  estructura.
- El mapeo de objetos de valor con EF Core dio guerra: los tipos complejos anidados no se pueden
  materializar y hubo que aplanar `PuntoEncuentro`. Está documentado en el propio tipo.
- Quien llegue esperando controladores con `DbContext` dentro necesitará leer esto primero.

## Alternativas descartadas

**CRUD directo con EF Core en los endpoints.** Más rápido de escribir. Descartado porque las
reglas de capacidad y verificación acabarían repetidas en cada endpoint que las necesita, y
bastaría con olvidarlas una vez para tener a dos personas en la misma plaza.

**Arquitectura vertical por funcionalidad, sin capa de dominio.** Encaja bien cuando cada
funcionalidad es independiente. Aquí no lo son: capacidad, lista de espera y privacidad de la
dirección son la misma regla vista desde tres pantallas distintas.

**DDD completo con contextos delimitados separados.** Para un MVP de veinte personas, dos
servicios con su propia base de datos es infraestructura que hay que operar sin resolver ningún
problema que hoy exista.
