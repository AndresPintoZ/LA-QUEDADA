# Contrato de la API

Referencia de los endpoints. La documentación interactiva y siempre actualizada está en
**http://localhost:8080/scalar** cuando la API corre en desarrollo; este documento explica las
convenciones y el porqué de algunas respuestas.

Base: `/api`. Todo el cuerpo va en JSON, **codificado en UTF-8**.

## Dos convenciones que conviene leer antes de integrarse

**Los enumerados viajan como texto**, no como número: `"estado": "Publicada"`, nunca `"estado": 1`.
Con números, el contrato dependería del orden de los valores del enumerado y añadir uno en medio
cambiaría el significado de los datos ya enviados.

**El cuerpo debe ir en UTF-8.** Suena obvio, pero algunos clientes no lo hacen por defecto: en
PowerShell 5.1, `Invoke-RestMethod -Body "texto"` codifica en ISO-8859-1 y rompe cualquier tilde.
Hay que enviar bytes explícitos:

```bash
$cuerpo = [Text.Encoding]::UTF8.GetBytes(($datos | ConvertTo-Json -Compress))
```

Un cuerpo mal codificado devuelve **400**, no 500: es un error del cliente.

---

## Autenticación

Cabecera `Authorization: Bearer <token>`. El token lo pone el BFF de Next, no el navegador: ver
[ADR-003](adr/003-bff-en-next.md).

| Vida | Renovación |
|---|---|
| 15 minutos | Rotativa, de un solo uso, 14 días |

Sin margen de tolerancia de reloj: un token caducado lo está de verdad.

---

## Convenciones de error

Todos los errores usan `ProblemDetails` (RFC 9457):

```json
{
  "status": 422,
  "title": "No se puede realizar la operación",
  "detail": "Ya estás apuntado a esta quedada.",
  "codigo": "quedada.ya_apuntado",
  "instance": "/api/quedadas/0195.../asistencia",
  "traceId": "0HN7GK8..."
}
```

| Código | Cuándo | Notas |
|---|---|---|
| 400 | Los datos no superan la validación | Incluye `errors` agrupados por campo |
| 401 | Falta el token o no es válido | |
| 403 | Identificado, pero sin permiso | |
| 404 | No existe **o no se puede ver** | Ver más abajo |
| 409 | Choca con el estado actual | Correo ya registrado, conflicto de concurrencia |
| 422 | Una regla de negocio lo impide | Incluye `codigo` estable |
| 429 | Demasiadas peticiones | |
| 500 | Fallo nuestro | Mensaje genérico; el detalle solo en el registro |

### Por qué 404 y no 403 cuando no se puede ver

Distinguir «no existe» de «existe pero no puedes verlo» permitiría a un atacante averiguar qué
identificadores son válidos observando los códigos de respuesta. Ante la duda, 404.

### `codigo` frente a `detail`

`detail` es texto para mostrar y puede cambiar. `codigo` es estable y sirve para que el cliente
decida qué hacer. Si necesitas reaccionar a un error concreto, compara el `codigo`, nunca el texto.

Códigos habituales: `quedada.ya_apuntado`, `quedada.no_admite_inscripciones`,
`quedada.organizador_no_verificado`, `quedada.capacidad_menor_que_asistentes`,
`concurrencia.conflicto`.

### `traceId`

Va en todas las respuestas de error. Permite que alguien informe de un fallo y se pueda localizar
en los registros sin exponerle ningún detalle técnico.

---

## Identidad

### `POST /api/identidad/registro` · anónimo

```json
{
  "correo": "lucia@example.com",
  "contrasena": "una frase larga que recuerdo",
  "nombreVisible": "Lucía R.",
  "ciudad": "Ávila",
  "anioDeNacimiento": 1996,
  "versionNormasAceptada": "2026-08"
}
```

**201** con `{ "id": "..." }`.

Contraseña de **12 caracteres mínimo**, sin exigir composición
([ADR-002](adr/002-autenticacion-propia-con-jwt.md)). De la edad solo se pide el año.

**409** si no se puede completar. El mensaje es deliberadamente genérico y **no confirma si el
correo ya existe**: el formulario de registro no debe servir para averiguar quién tiene cuenta.

Límite: 10 peticiones cada 5 minutos.

### `POST /api/identidad/sesion` · anónimo

```json
{ "correo": "lucia@example.com", "contrasena": "...", "dispositivo": "web" }
```

**200** con tokens y perfil:

```json
{
  "tokens": {
    "tokenDeAcceso": "eyJ...",
    "expiraEn": "2026-08-11T15:15:00Z",
    "tokenDeRenovacion": "aBc...",
    "renovacionExpiraEn": "2026-08-25T15:00:00Z"
  },
  "perfil": {
    "id": "...",
    "nombreVisible": "Lucía R.",
    "roles": ["Registrado", "OrganizadorVerificado"],
    "estadoVerificacion": "Verificada",
    "puedeOrganizar": true
  }
}
```

**403** con el mismo mensaje tanto si el correo no existe como si la contraseña es incorrecta.
La validación tarda un mínimo de 250 ms en ambos casos, para no filtrar por tiempo qué correos
están registrados.

`puedeOrganizar` viene ya resuelto por el servidor: combina verificación vigente, mayoría de edad y
cuenta activa. El cliente no debe recalcular esa regla.

### `POST /api/identidad/sesion/renovar` · anónimo

```json
{ "tokenDeRenovacion": "aBc..." }
```

Devuelve un par nuevo e invalida el anterior.

> **Cuidado.** Reutilizar un token ya usado se interpreta como robo y **revoca todas las sesiones**
> de la cuenta. No reintentes una renovación fallida con el mismo token.

### `POST /api/identidad/sesion/cerrar` · anónimo

Revoca la familia entera de sesiones. **204**.

### `GET /api/identidad/yo` · requiere sesión

Perfil de la persona autenticada.

### `POST /api/identidad/verificacion` · requiere sesión

Abre la verificación de organizador. **200**:

```json
{ "referenciaExterna": "sim-0195...", "urlDeRedireccion": "https://..." }
```

### `POST /api/identidad/verificacion/completar` · requiere sesión

```json
{ "referenciaExterna": "sim-0195..." }
```

**200** con `{ "estado": "Verificada" }`.

El resultado **se consulta al proveedor**; no se acepta el que envíe el cliente. Además se
comprueba que la referencia corresponde a esta cuenta.

---

## Quedadas

### `GET /api/quedadas` · anónimo

| Parámetro | Tipo | Notas |
|---|---|---|
| `texto` | string | Busca en título y descripción |
| `categorias` | guid (repetible) | `?categorias=a&categorias=b` |
| `desde`, `hasta` | ISO 8601 | Por defecto, desde ahora |
| `latitud`, `longitud` | double | Centro de la búsqueda |
| `radio` | int | Metros. Máximo 100 000 |
| `soloConPlazas` | bool | |
| `pagina` | int | Empieza en 1 |
| `tamano` | int | Máximo 50 |

Los topes los aplica el servidor: pedir `tamano=100000` devuelve 50, no un error.

Si se indica un centro, los resultados vienen **ordenados por cercanía** e incluyen
`distanciaEnMetros`. Si no, ordenados por lo que ocurre antes.

Solo devuelve planes **publicados**: los cancelados y los ocultos no aparecen en explorar, aunque
sigan siendo accesibles por enlace directo para quien ya estaba apuntado.

### `GET /api/quedadas/{id}` · anónimo

Detalle completo.

> **`direccionExacta` solo llega con valor si tienes plaza confirmada.** Para el resto es `null`.
> La decisión la toma el agregado de dominio, no la consulta.

`miAsistencia` y `miPosicionEnListaDeEspera` solo vienen si hay sesión.

### `GET /api/quedadas/mios` · requiere sesión

Planes organizados y a los que se va. **Incluye los cancelados**: un plan que desaparece sin
explicación deja a la persona pensando que se equivocó de día.

### `POST /api/quedadas` · requiere poder organizar

```json
{
  "titulo": "Ruta en bici por el Valle Amblés",
  "descripcion": "Pedaleamos 35 km sin prisa.",
  "categoriaId": "0195c1a0-0002-7000-8000-000000000002",
  "inicio": "2026-09-19T08:00:00Z",
  "duracionEnMinutos": 210,
  "lugar": "Puente Adaja",
  "referencia": "Junto al quiosco",
  "direccionExacta": "Av. de Juan Carlos I, 12",
  "latitud": 40.6565,
  "longitud": -4.7009,
  "confirmaQueEsLugarPublico": true,
  "capacidad": 15,
  "normas": ["Casco obligatorio", "Nivel medio"]
}
```

**201** con `{ "id": "..." }`. **403** sin verificación o siendo menor de edad.

`confirmaQueEsLugarPublico` debe ser `true`. No es una casilla decorativa: es la declaración de que
el punto no es un domicilio particular, y queda auditable.

Restricciones: título 3–120, capacidad 2–500, duración 15–1440 minutos, máximo 8 normas de 60
caracteres, fecha en el futuro.

### `POST /api/quedadas/{id}/asistencia` · requiere sesión

Sin cuerpo. **200**:

```json
{ "confirmada": true, "posicionEnListaDeEspera": null }
```

Si está completo, `confirmada: false` y la posición en la cola. **No es un error**: es el
comportamiento esperado (RF-15), y por eso devuelve 200 y no 409.

**422** con código si el plan no admite inscripciones, ya ha empezado o ya estás apuntado.

### `DELETE /api/quedadas/{id}/asistencia` · requiere sesión

**204**. Al liberarse una plaza confirmada, entra automáticamente quien esté primero en la lista de
espera.

**422 `quedada.organizador_no_puede_abandonar`** si lo intenta el organizador: tiene que cancelar
el plan, no desaparecer de él.

### `POST /api/quedadas/{id}/cancelacion` · requiere sesión

```json
{ "motivo": "Aviso de tormenta" }
```

**204**. Solo el organizador. El motivo es obligatorio: se muestra a los asistentes y queda en la
auditoría.

---

## Catálogo

### `GET /api/categorias` · anónimo

Categorías activas, ordenadas. Se cachea 5 minutos.

### `GET /api/lugares?texto=...` · requiere sesión

Geocodificación con Nominatim. Requiere sesión para que la cuota del proveedor no quede expuesta al
público.

Devuelve **lista vacía** si el proveedor falla o tarda demasiado, en lugar de un error: que el
buscador de direcciones no responda no debe impedir crear un plan, porque el punto siempre se puede
colocar a mano.

Límite: 20 peticiones por minuto.

---

## Salud

### `GET /salud` · anónimo

**200** si la API y su conexión a la base de datos responden.

---

## Límites de peticiones

| Ámbito | Límite |
|---|---|
| Autenticación | 10 cada 5 min |
| Escritura | 30 por min |
| Geocodificación | 20 por min |
| General | 200 por min |

Se reparten por persona autenticada o, si no la hay, por dirección IP. Al superarlos: **429**, sin
cola de espera.
