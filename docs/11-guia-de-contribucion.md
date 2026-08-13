# Guía de contribución

Convenciones del proyecto. La mayoría están automatizadas: si algo se puede comprobar con el
compilador, no se deja a la revisión manual.

---

## Idioma

**Todo en español**: código, comentarios, documentación, mensajes de commit y textos de interfaz.

El dominio usa el lenguaje del negocio, que es el mismo en el que se habla del producto:
`quedada.Apuntar(usuarioId, ahora)` se lee igual que la conversación en la que se decidió esa regla.

Se mantienen en inglés los identificadores impuestos por el lenguaje o los frameworks: nombres de
paquetes, `Task`, `IEnumerable`, `builder` cuando lo exige una interfaz de .NET.

---

## Comentarios

Se comenta **el porqué**, no el qué. El código ya dice qué hace.

```csharp
// Mal: repite lo que se lee justo debajo
// Comprueba si el usuario está verificado
if (!usuario.PuedeOrganizar) { ... }
```

```csharp
// Bien: explica una decisión que no se deduce del código
// No se detalla cuál de las condiciones falla: la interfaz ya guía a la persona
// según el estado de su verificación, y el mensaje de error no es el sitio para eso.
if (!usuario.PuedeOrganizar) { ... }
```

Merece comentario:

- Una decisión con alternativas razonables descartadas.
- Una restricción de seguridad o privacidad, y qué pasaría sin ella.
- Una limitación externa (EF Core no sabe hacer X, Nominatim limita a Y).
- Una referencia al requisito que origina la regla: `// RF-15`.

No merece comentario: lo que se lee en la línea siguiente.

---

## Reglas automatizadas

`.editorconfig` y `Directory.Build.props` aplican:

- **Los avisos son errores.** No se acumula deuda en silencio.
- **Las vulnerabilidades de dependencias son errores de compilación.** `NuGetAudit` en modo `all`,
  nivel `low`.
- Namespaces con ámbito de archivo, campos privados con guion bajo, llaves siempre.
- Análisis de nulabilidad como error.

Si una regla estorba, se desactiva **con justificación escrita**. Ejemplos en el repositorio:

- `CA1710` (sufijos ingleses): el dominio usa el lenguaje del negocio, que es español.
- `CA1707` (guiones bajos) solo en `tests/`: el nombre de la prueba **es** la especificación y se
  lee en el informe de fallos.
- Reglas de estilo en `Persistencia/Migraciones/`: es código generado que se regenera entero.

---

## Dónde va cada cosa

Antes de escribir código, la pregunta es en qué capa va:

| Si es… | Va en |
|---|---|
| Una regla que debe cumplirse **siempre** | `Domain` |
| Coordinar pasos de un caso de uso | `Application` |
| Hablar con una base de datos, una API externa o un archivo | `Infrastructure` |
| Traducir HTTP a un caso de uso | `Api` |

**Señales de que algo está en el sitio equivocado:**

- El dominio necesita `using Microsoft.EntityFrameworkCore` → la regla está mal ubicada.
- Un manejador cuenta asistentes y decide si cabe alguien → esa lógica es del agregado.
- Un endpoint comprueba roles con un `if` → debería ser una política de autorización.

---

## Cómo se escribe una regla nueva

1. **Primero la prueba**, con nombre que describa la regla en una frase.
2. Ejecuta: debe fallar. Si pasa a la primera, la prueba no prueba nada.
3. Implementa lo mínimo para que pase.
4. Ejecuta toda la suite del dominio: 125 ms, no hay excusa.
5. Refactoriza con la red puesta.

Los métodos del dominio reciben el instante actual como parámetro (`ahora`) en lugar de leer un
reloj estático. Así el dominio no depende de nada externo y las pruebas pueden situarse en
cualquier momento.

---

## Errores

**Excepciones**, no códigos de retorno, para lo que no debería pasar:

| Excepción | Traduce a | Cuándo |
|---|---|---|
| `ExcepcionDeDominio` | 422 | Una regla de negocio lo impide |
| `ValidacionException` | 400 | Los datos de entrada no son válidos |
| `AccesoDenegadoException` | 403 | Identificado, pero sin permiso |
| `NoEncontradoException` | 404 | No existe o no se puede ver |
| `ConflictoException` | 409 | Choca con el estado actual |

Toda excepción de dominio lleva un **código estable** en formato `area.motivo`. El texto puede
cambiar; el código no. Los clientes comparan el código, nunca el texto.

Cualquier otra excepción se convierte en un 500 con mensaje genérico. Un error de base de datos
revela nombres de tablas; una traza revela rutas del servidor y versiones de bibliotecas.

---

## Registro (logs)

Se usan métodos generados con `[LoggerMessage]`, agrupados en `RegistroDeAplicacion`,
`RegistroDeInfraestructura` y `RegistroDeApi`.

Dos motivos: no reservan memoria cuando el nivel está desactivado, y tenerlos juntos permite
**revisar de una sentada qué se escribe en los registros**.

> **En los registros no entra nunca**: correos, contraseñas, tokens, direcciones ni ningún dato
> personal. Solo identificadores internos, nombres de tipo y contadores.

---

## Base de datos

- Toda consulta que se ejecute con frecuencia necesita índice.
- Las consultas de lectura llevan `AsNoTracking`.
- Los agregados se cargan **enteros**: un `Include` de las asistencias no es optimizable, es la
  condición para que el agregado pueda decidir bien.
- Los arrays y colecciones con conversor **necesitan `ValueComparer`**. Sin él, EF no detecta que
  han cambiado y el cambio se pierde en silencio.
- Revisa siempre el SQL de una migración antes de confirmarla.

---

## Frontend

- **Componente de servidor por defecto.** `'use client'` solo si necesita estado, efectos o toca el
  DOM.
- Las mutaciones van por **acciones de servidor**, no por endpoints propios.
- Los campos de formulario usan `Campo` y `CampoLargo`: ya traen etiqueta asociada,
  `aria-describedby` y `aria-invalid`.
- El color **nunca** es el único indicador de estado. Siempre acompañado de texto.
- Área de toque mínima cómoda en móvil (unos 48 px de alto).
- Nada de `dangerouslySetInnerHTML`. Contenido dinámico en el mapa: con la API del DOM.

---

## Ramas y commits

Ramas: `tipo/descripcion-corta`, con tipo `feat`, `fix`, `docs`, `refactor`, `test` o `chore`.

Commits en imperativo y en español, explicando el porqué cuando no sea evidente:

```
feat: añadir lista de espera con promoción automática

Al liberarse una plaza confirmada, entra quien esté primero por orden de
llegada. Se usa un contador por quedada en lugar de la marca de tiempo
porque dos personas pueden apuntarse en el mismo milisegundo.

RF-15
```

---

## Antes de abrir una revisión

```bash
dotnet build backend/PlanVibe.slnx
```

```bash
dotnet test backend/PlanVibe.slnx
```

```bash
npm run typecheck --prefix frontend && npm test --prefix frontend
```

Y comprueba tú mismo:

- ¿La regla nueva está en la capa correcta?
- ¿Tiene prueba? ¿La prueba falla si quitas la implementación?
- ¿Hay algún dato personal nuevo? Si lo hay, ¿es imprescindible? Revisa
  [09-modelo-de-amenazas.md](09-modelo-de-amenazas.md).
- ¿Algún mensaje de error revela más de lo necesario?
- ¿Los comentarios explican decisiones, o repiten el código?
- ¿Hace falta un ADR? Si alguien puede preguntar «¿por qué así?» dentro de seis meses, sí.

---

## Qué se revisa en una revisión de código

Por orden de importancia:

1. **¿Es correcto?** Especialmente en reglas de negocio y caminos de seguridad.
2. **¿Está en la capa adecuada?** Una regla en el sitio equivocado es deuda que crece.
3. **¿Se puede probar?** Si cuesta probarlo, suele estar mal acoplado.
4. **¿Se entiende dentro de seis meses?** Nombres y comentarios del porqué.
5. **¿Es eficiente?** Solo después de lo anterior. Optimizar código incorrecto no sirve de nada.
