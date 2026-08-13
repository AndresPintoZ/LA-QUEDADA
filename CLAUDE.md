# Instrucciones para Claude Code en este repositorio

## Idioma

**Todo en español**: respuestas, código, comentarios, documentación y mensajes de commit.

Se mantienen en inglés solo los identificadores impuestos por el lenguaje o los frameworks
(`Task`, `IEnumerable`, nombres de paquetes, `builder` cuando lo exige una interfaz de .NET).

## Qué es esto

PlanVibe: plataforma para descubrir planes locales y apuntarse a ellos. Piloto en Ávila.

- **Backend**: .NET 10, arquitectura limpia, DDD táctico, PostgreSQL con PostGIS.
- **Frontend**: Next.js 16, App Router, componentes de servidor, BFF con cookie cifrada.
- **Entorno**: tres contenedores (db, api, web) con `docker compose`.

## Antes de tocar nada

Lee, por este orden:

1. `docs/05-puesta-en-marcha.md` — cómo levantar el entorno.
2. `docs/02-arquitectura.md` — cómo está construido y por qué.
3. `docs/adr/` — las decisiones estructurales, con sus alternativas descartadas.
4. `docs/08-hoja-de-ruta.md` — qué está hecho y qué falta.

## Reglas que no se negocian

**Las reglas de negocio van en `PlanVibe.Domain`.** Si el dominio necesita `using
Microsoft.EntityFrameworkCore`, la regla está en el sitio equivocado.

**Ningún dato de documento de identidad entra en el sistema.** Ni foto, ni número, ni fecha de
nacimiento completa. Hay una prueba que falla si alguien añade un campo así. Ver
`docs/adr/006-verificacion-sin-almacenar-documentos.md`.

**La dirección exacta de un punto de encuentro solo se revela a quien tiene plaza confirmada.** La
decisión la toma `Quedada.DireccionExactaVisiblePara`, no la vista.

**Los avisos del compilador son errores.** Si una regla estorba, se desactiva con justificación
escrita, no en silencio.

**En los registros no entra ningún dato personal.** Solo identificadores internos, nombres de tipo
y contadores. Los mensajes están centralizados en las clases `Registro*` para poder revisarlos de
una sentada.

## Cómo se escribe código aquí

- **Primero la prueba.** El dominio está escrito con TDD y sus 50 pruebas tardan 125 ms.
- **Comentar el porqué, no el qué.** El código ya dice qué hace.
- Los métodos del dominio reciben `ahora` como parámetro, no leen un reloj estático.
- Un tipo por caso de uso. Comandos y consultas separados.
- Componentes de servidor por defecto en el frontend; `'use client'` solo si hace falta.

## Comprobaciones

```bash
dotnet build backend/PlanVibe.slnx && dotnet test backend/PlanVibe.slnx
```

```bash
npm run typecheck --prefix frontend && npm test --prefix frontend
```

## Detalles del entorno que sorprenden

- Las pruebas de integración **se omiten** si no hay Docker en marcha. Es intencionado.
- El proveedor de verificación es **simulado** y solo se registra en desarrollo. Fuera de
  desarrollo, el contenedor de dependencias lanza una excepción explícita.
- Las migraciones se aplican solas **solo en desarrollo**. En producción son un paso del despliegue.
- Los arrays de EF Core necesitan `ValueComparer` o los cambios se pierden en silencio.
