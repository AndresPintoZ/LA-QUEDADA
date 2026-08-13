# Estrategia de pruebas

Qué se prueba, dónde y por qué. La idea de fondo: **cada prueba debe poder fallar por un motivo
real**. Una prueba que solo comprueba que un `get` devuelve lo que puso un `set` no aporta nada y
sí cuesta mantenerla.

---

## Reparto

| Proyecto | Cuántas | Velocidad | Necesita |
|---|---|---|---|
| `PlanVibe.Domain.UnitTests` | 50 | ~340 ms | Nada |
| `PlanVibe.Application.UnitTests` | 9 | ~500 ms | Nada |
| `PlanVibe.Api.IntegrationTests` | 9 | ~5 s | Docker |
| `frontend` (vitest) | 15 | ~2 s | Nada |

Que las de dominio tarden 125 ms no es un dato de vanidad: es lo que hace que se ejecuten en cada
guardado sin pensárselo. Una suite que tarda dos minutos se acaba ejecutando solo antes de subir
los cambios, y entonces deja de servir para desarrollar.

---

## Pruebas de dominio

Cubren las reglas de negocio. Son la parte donde más se aplicó TDD: primero la prueba que describe
la regla, después la implementación.

Ejemplos de lo que cubren:

- Publicar sin verificación o siendo menor de edad falla (RF-09, RF-24).
- Apuntarse a un plan lleno entra en lista de espera por orden de llegada (RF-15).
- Retirarse promueve automáticamente a quien está primero.
- El organizador no puede abandonar su propio plan; tiene que cancelarlo.
- Cancelar exige motivo y deja preparado a quién avisar (RF-11).
- La dirección exacta solo se revela a quien tiene plaza confirmada.
- Reducir la capacidad por debajo de las plazas ya concedidas no se permite.

### Los nombres son la especificación

```csharp
[Fact]
public void Abandonar_promueve_a_la_primera_persona_de_la_lista_de_espera()
```

El nombre es lo que se lee en el informe de fallos. Por eso se permiten guiones bajos aquí y solo
aquí, con una regla específica en `.editorconfig` que lo justifica.

### Una prueba que es un candado

```csharp
[Fact]
public void La_verificacion_no_guarda_ningun_dato_del_documento()
```

Recorre por reflexión las propiedades de `DatosDeVerificacion` y falla si aparece una nueva. No
prueba un comportamiento: impide que alguien añada un campo `FotoDelDocumento` sin darse cuenta de
que está rompiendo un requisito (RF-22). Ver
[ADR-006](adr/006-verificacion-sin-almacenar-documentos.md).

### El TDD encontró un fallo en la propia prueba

Una prueba comprobaba que reducir la capacidad a 1 fallaba con el código
`quedada.capacidad_menor_que_asistentes`. Falló, pero con el código `capacidad.fuera_de_rango`:
`new Capacidad(1)` ya es inválido de por sí, porque el mínimo son dos personas.

El escenario no probaba lo que decía probar. Se reescribió con tres asistentes reduciendo a dos, y
se añadió el caso simétrico —reducir exactamente hasta las plazas ocupadas **sí** está permitido—,
que no estaba cubierto.

---

## Pruebas de aplicación

Comprueban la **orquestación**, no las reglas: que se verifique quién pide la acción, que se
guarde y que quede auditado. Las reglas del agregado ya tienen sus pruebas y no se repiten.

Los puertos se sustituyen con NSubstitute. Ejemplo de lo que cubren:

- Sin sesión no se publica, y **no se toca la base de datos** en ese intento.
- Sin verificación no se publica.
- Al publicar correctamente, se guarda **y** se audita (RNF-04).
- Se guarda **antes** de auditar, para no registrar algo que luego falló.

También se prueba el decorador de validación, porque es la garantía de que ningún comando llega al
dominio sin comprobarse:

- Un comando inválido no llega al manejador interno.
- Los errores se devuelven todos juntos, agrupados por campo.
- Sin validadores registrados, el comando pasa: muchos comandos no tienen datos que validar.

### El reloj se inyecta

Los métodos del dominio reciben el instante actual como parámetro y la capa de aplicación usa
`TimeProvider`. Las pruebas usan un reloj fijo, así que no fallan de madrugada ni en el cambio de
horario de verano.

---

## Pruebas de integración

Levantan la API real contra un **PostgreSQL con PostGIS efímero** creado con Testcontainers.

Se usa una base de datos real, no un proveedor en memoria. La diferencia importa: la columna
geográfica calculada, el índice espacial, los arrays nativos y la concurrencia con `xmin` son
características de PostgreSQL. Un proveedor en memoria daría por buenas cosas que en producción
fallarían.

Además se aplican las **migraciones reales**, no un `EnsureCreated`, así que de paso se comprueba
que se aplican limpiamente sobre una base de datos vacía.

La prueba principal recorre el flujo completo:

1. Registro de la organizadora.
2. Intento de publicar sin verificar → **403**.
3. Verificación con el proveedor simulado.
4. Publicación del plan → **201**.
5. El plan aparece en explorar **sin sesión**.
6. La dirección exacta **no** se ve sin plaza.
7. Otra persona se registra y se apunta.
8. Ahora **sí** ve la dirección exacta.
9. El plan aparece en «mis planes».

Es la prueba que responde a «¿funciona esto de verdad?»: comprueba que encajan el mapeo de EF, las
migraciones, la autenticación, las políticas de autorización y la serialización JSON.

### Se omiten si no hay Docker

```csharp
[RequiereDocker]
public async Task ...
```

Sin motor de Docker, las pruebas se **omiten** en lugar de fallar. Un fallo por falta de entorno se
acaba ignorando por costumbre, y entonces deja de detectarse el fallo de verdad.

### Los modelos de respuesta se declaran aparte

Las pruebas declaran sus propios `record` para deserializar en lugar de reutilizar los de la API.
Es deliberado: si el contrato cambia sin querer, estas pruebas lo notan. Reutilizando los tipos de
la API, el cambio pasaría desapercibido.

**Esto ya evitó un fallo grave.** La primera vez que se ejecutaron contra PostgreSQL real, la
prueba del flujo completo falló al deserializar `miAsistencia`: la API enviaba el enumerado como
número (`1`) y el frontend lo comparaba como texto (`'Confirmada'`). En producción, un plan
cancelado se habría mostrado como activo y el botón «Me apunto» habría aparecido a quien ya tenía
plaza. Se corrigió en la API con `JsonStringEnumConverter`.

### Codificación de caracteres

`CodificacionTests` comprueba que «Ávila», «Amblés» y «El Barraco de la Peña» sobreviven el viaje
completo: petición, PostgreSQL, respuesta y listado.

Parece una prueba tonta hasta que falla. Son nombres reales del piloto: si la codificación se
rompe en algún punto, la aplicación se vuelve inutilizable en su propia ciudad.

La comprobación es por **longitud en caracteres** además de por igualdad de cadena. Un texto
doblemente codificado a veces se ve bien según la consola, pero «Ávila» siempre tiene 5 caracteres:
si salen 6, hay un problema aunque parezca correcto.

### Errores de cliente frente a errores nuestros

`ManejoDeErroresTests` verifica que un cuerpo mal formado devuelve **400 y no 500**. Nació de un
fallo real: un cuerpo con codificación inválida provocaba un 500, que significa «el fallo es
nuestro» y acaba en las alertas del equipo. También comprueba que ninguna respuesta de error
filtra trazas de pila ni nombres de tipos internos.

---

## Pruebas del frontend

Cubren la lógica pura: formateo de fechas, distancias y estados. No se prueba el renderizado de
cada componente, porque las páginas son componentes de servidor con muy poca lógica.

Lo que sí puede fallar en silencio es un cálculo de fechas, y ahí está el foco:

```typescript
it('cuenta los días por la zona horaria del piloto, no por UTC', () => {
  // 00:30 del día 16 en Madrid es 22:30 del 15 en UTC.
  // Contando en UTC, este plan diría «HOY» en lugar de «MAÑANA».
  expect(cuandoCorto('2026-09-15T22:30:00Z', ahora)).toBe('MAÑANA 00:30');
});
```

Ese es exactamente el tipo de error que nadie ve hasta que alguien se presenta un día tarde.

La zona horaria se fija en `vitest.config.ts` para que las pruebas den el mismo resultado en
cualquier equipo y en integración continua.

---

## Lo que no está cubierto

Es tan importante saber qué falta como qué hay:

| Sin cubrir | Riesgo | Cuándo abordarlo |
|---|---|---|
| Concurrencia real: dos inscripciones simultáneas a la última plaza | Medio. Hay dos barreras (agregado e índice único), pero no probadas bajo carga | Antes de abrir el piloto |
| Renovación de token y detección de reutilización, de extremo a extremo | Medio. La lógica está, sin prueba de integración | Antes de abrir el piloto |
| Accesibilidad automatizada (axe) | Medio. RNF-05 es un requisito | Al cerrar las pantallas |
| Recorridos de navegador (Playwright) | Bajo mientras el flujo esté cubierto por integración | Cuando haya más pantallas |
| Carga y rendimiento | Bajo con veinte personas | Si el piloto crece |
| Componentes de React | Bajo: tienen poca lógica | Si aparecen componentes complejos |

---

## Ejecutar las pruebas

Todo el backend:

```bash
dotnet test backend/PlanVibe.slnx
```

Solo el dominio, que es lo que se ejecuta mientras se desarrolla:

```bash
dotnet test backend/tests/PlanVibe.Domain.UnitTests
```

El frontend:

```bash
npm test --prefix frontend
```

Con cobertura:

```bash
dotnet test backend/PlanVibe.slnx --collect:"XPlat Code Coverage"
```

> No hay umbral de cobertura configurado, a propósito. Un porcentaje obligatorio empuja a escribir
> pruebas de getters para llegar a la cifra. Lo que importa es que las reglas de negocio y los
> caminos de seguridad estén cubiertos, y eso se revisa leyendo, no midiendo.
