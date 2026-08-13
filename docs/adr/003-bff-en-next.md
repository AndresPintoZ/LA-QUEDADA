# ADR-003 · BFF en Next en lugar de llamadas directas del navegador

**Estado:** aceptada · 2026-08-11

## Contexto

Con un frontend en Next y una API en .NET, hay dos formas de conectarlos:

1. **Directa**: el navegador llama a la API. Hace falta CORS y hay que guardar el token en algún
   sitio accesible desde JavaScript (almacenamiento local, memoria o cookie legible).
2. **BFF**: el navegador solo habla con el servidor de Next, que reenvía a la API.

La primera es menos código. La segunda cambia sustancialmente qué pasa si alguien logra ejecutar
JavaScript en la página.

## Decisión

BFF. El servidor de Next es el único que habla con la API .NET.

- La sesión viaja en una **cookie `httpOnly`, `SameSite=Lax`, cifrada** con `jose` (A256GCM).
- Los tokens de la API se guardan **dentro** de esa cookie cifrada; el navegador no puede leerlos.
- Las mutaciones se hacen con **acciones de servidor**, no con endpoints propios de Next.
- En Docker, la API **no publica puerto hacia fuera** salvo en la interfaz local para depurar.

## Consecuencias

**A favor:**

- **Un XSS no se lleva la sesión.** Podría hacer peticiones en nombre de la persona mientras la
  página está abierta, pero no exfiltrar el token para usarlo desde otro sitio o más tarde. La
  diferencia entre «daño mientras estás en la página» y «acceso permanente a tu cuenta» es enorme.
- **No hace falta CORS**: el navegador solo habla con su propio origen.
- **La política de seguridad de contenido puede llevar `connect-src 'self'`**, que es de las
  restricciones más útiles y la que primero se rompe al llamar a una API en otro dominio.
- **CSRF cubierto sin token propio**: Next verifica el origen de las acciones de servidor, y
  `SameSite=Lax` impide que la cookie viaje en peticiones lanzadas desde otra web.
- La renovación del token es transparente: `api-servidor.ts` la hace sola cuando quedan menos de
  30 segundos, y la persona no ve nunca una sesión cortada a mitad de un formulario.

**En contra:**

- Un salto de red más por petición. En la red interna de Docker es despreciable.
- El servidor de Next pasa a ser una pieza con estado sensible: si se compromete, se compromete la
  sesión. Por eso su contenedor va con `read_only`, sin privilegios y sin capacidades.
- Hay que mantener tipos del contrato de API en dos sitios. Se mitiga con `lib/tipos.ts` y las
  pruebas de contrato.

## Alternativas descartadas

**Token en `localStorage`.** Es el patrón más extendido y el más frágil: cualquier script que se
ejecute en la página puede leerlo. Un paquete de npm comprometido basta.

**Token en cookie legible desde JavaScript.** Tiene el mismo problema que `localStorage` y además
viaja en cada petición.

**Token en memoria, renovando en cada carga.** Resiste el XSS pasivo, pero obliga a renovar en
cada pestaña y cada recarga, y el token de renovación acaba en una cookie de todos modos. Se
complica el código sin llegar a la garantía del BFF.
