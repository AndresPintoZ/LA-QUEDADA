# ADR-007 · CQRS ligero sin biblioteca mediadora

**Estado:** aceptada · 2026-08-11

## Contexto

La arquitectura limpia con casos de uso suele implementarse en .NET con MediatR: comandos,
manejadores y una tubería de comportamientos para validación y registro.

Dos consideraciones al elegir:

1. **Licencia.** MediatR pasó a licencia comercial en su versión 13. No es un impedimento
   insalvable, pero introduce una decisión de compra en un MVP que aún no sabe si va a existir.
2. **Lo que aporta.** El valor de un mediador es la tubería de comportamientos y el desacoplamiento
   entre quien emite y quien maneja. En una API donde cada endpoint sabe perfectamente qué caso de
   uso invoca, ese desacoplamiento no se usa: se paga el precio de la indirección sin cobrarlo.

## Decisión

CQRS con interfaces propias, sin mediador:

```csharp
public interface IComando<TResultado>;

public interface IManejadorDeComando<in TComando, TResultado>
    where TComando : IComando<TResultado>
{
    Task<TResultado> ManejarAsync(TComando comando, CancellationToken cancelacion);
}
```

Cada endpoint pide su manejador por inyección de dependencias y lo invoca:

```csharp
grupo.MapPost("/", async (
    CrearQuedada comando,
    IManejadorDeComando<CrearQuedada, Guid> manejador,
    CancellationToken cancelacion) => ...);
```

La validación se aplica con un **decorador** registrado en el contenedor
(`ManejadorConValidacion<,>`), no con una tubería de comportamientos.

Los manejadores se registran **uno a uno** en `InyeccionDeDependencias`, sin escanear ensamblados.

## Consecuencias

**A favor:**

- Sin dependencia comercial ni decisión de compra pendiente.
- **Se puede navegar con «ir a la definición».** Con un mediador, `Send(comando)` lleva a la
  biblioteca; hay que buscar el manejador por convención de nombres. Aquí el tipo está en la firma
  del endpoint.
- `InyeccionDeDependencias.AgregarCapaDeAplicacion` es la **lista completa de lo que la aplicación
  sabe hacer**, legible de un vistazo. Con escaneo automático, esa lista no existe en ningún sitio.
- El decorador garantiza que ningún comando llega al dominio sin validarse, y eso lo aplica el
  contenedor, no la disciplina de quien programa. Hay pruebas específicas de ese decorador.

**En contra:**

- Cada caso de uso nuevo hay que registrarlo a mano. Es una línea, y si se olvida el error aparece
  al arrancar, no en producción.
- Añadir un comportamiento transversal nuevo (por ejemplo, medir tiempos de todos los casos de uso)
  requiere otro decorador y modificar el registro. Con una tubería sería una clase y una línea.
- El registro es algo más verboso que `AddMediatR(typeof(X).Assembly)`.

## Alternativas descartadas

**MediatR.** La opción estándar. Descartada por licencia comercial y porque su principal ventaja
—el desacoplamiento— no se aprovecha aquí.

**Una alternativa gratuita compatible con MediatR.** Resuelve la licencia pero mantiene la
indirección y añade una dependencia con menos recorrido y mantenimiento incierto.

**Servicios de aplicación con varios métodos, sin comandos.** Menos tipos. Descartada porque un
servicio con ocho métodos acaba con ocho dependencias inyectadas, de las que cada método usa dos, y
porque el decorador de validación deja de ser posible: no hay un tipo por operación al que
asociarle un validador.
