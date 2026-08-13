# PlanVibe · aplicación web

Next.js 16 con App Router, TypeScript y Tailwind. **Responsive**: se diseña primero para
móvil, pero todas las pantallas se adaptan al escritorio.

La coherencia entre pantallas la garantiza el armazón compartido `AppShell`: barra superior
de navegación en escritorio, barra inferior en móvil, mismo ancho de contenido y mismo fondo.
Toda pantalla nueva debe pasar por él; una pantalla con su propio armazón vuelve a parecer
de otra aplicación (es exactamente lo que pasó al conservar el marco de teléfono de los
mockups, y por eso `PhoneShell` ya no existe).

> Este directorio nació como exportación de los mockups de Claude Design y ahora es la aplicación
> real, conectada a la API .NET. El sistema visual original se ha conservado íntegro.

Para levantar todo el entorno, ver **[../docs/05-puesta-en-marcha.md](../docs/05-puesta-en-marcha.md)**.

---

## Desarrollo en local

```bash
npm install
```

```bash
npm run dev
```

Necesita dos variables de entorno:

```powershell
$env:API_URL_INTERNA = "http://localhost:8080"
$env:SESION_SECRETO = "al-menos-32-caracteres"
```

---

## Estructura

```
app/                  Páginas (App Router)
  acceso/             Entrar y crear cuenta
  explorar/           Lista de planes con filtros
  mapa/               Los mismos planes en mapa
  plan/[id]/          Detalle
  crear/              Publicar un plan
  calendario/         Planes por día
  mis-planes/         Los propios
  perfil/             Perfil y ajustes
  verificacion/       Verificación de organizador
  moderacion/         Panel de moderación (pendiente)
  api/salud/          Comprobación de estado del contenedor

components/           Componentes de interfaz
lib/
  sesion.ts           Cookie httpOnly cifrada
  api-servidor.ts     Cliente de la API con renovación automática
  datos.ts            Lecturas de las páginas
  acciones/           Acciones de servidor (mutaciones)
  formato.ts          Fechas, distancias y estados en español
  tipos.ts            Contrato con la API
  catalogos.ts        Listas cerradas de producto
```

---

## Decisiones que conviene conocer

**Componentes de servidor por defecto.** El HTML llega con los datos dentro: no hay pantalla vacía
esperando a que responda una petición. `'use client'` solo donde hace falta interacción real.

**Los tokens no llegan al navegador.** Viven cifrados en una cookie `httpOnly` que solo lee el
servidor de Next. Ver [ADR-003](../docs/adr/003-bff-en-next.md).

**Las mutaciones son acciones de servidor.** Next verifica el origen automáticamente, así que no
hay que gestionar CSRF a mano, y los formularios funcionan antes de que cargue el JavaScript.

**Leaflet directamente, sin `react-leaflet`.** Su versión estable no admite React 19 y aquí solo
hacen falta dos mapas sencillos. Se importa de forma diferida porque manipula el DOM.

**El color nunca es el único indicador de estado.** Siempre acompañado de texto.

---

## Sistema visual

Definido en `tailwind.config.ts`:

- `brand` #0B7C9B · `brand.dark` #075E77 · `brand.tint` #E6F3F7
- `lime` #D8F45A — acento y botón de crear
- `ink` #0C1A22 · `paper` #F3F6F5 · `line` #E7EDEB · `muted` #6E827D
- Estados: `ok` #1E8A5F · `warn` #8A5A0B · `danger` #C2413C
- Tipografía: Space Grotesk (títulos), Manrope (interfaz), JetBrains Mono (metadatos)

---

## Comandos

```bash
npm run typecheck
```

```bash
npm test
```

```bash
npm run build
```
