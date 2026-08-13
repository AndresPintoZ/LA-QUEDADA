# PlanVibe

Plataforma local para descubrir planes, publicar actividades y unirse a otras personas con
intereses afines. El piloto se centra en **Ávila** y su comarca.

> **Nombre.** «La Quedada» y «Me Apunto a la Quedada» quedaron descartados por estar en uso.
> El nombre de trabajo actual es **PlanVibe** (`planvibe.es`). Antes de fijarlo definitivamente hay
> que comprobar disponibilidad de dominio, redes sociales y marca registrada.

---

## Arrancar el entorno completo

Necesitas **Docker Desktop** con su motor en marcha. Los tres servicios (base de datos, API y web)
se levantan juntos:

```bash
docker compose up -d --build
```

Antes del primer arranque hay que crear el archivo `.env`. La guía completa, incluidos los
requisitos previos de Windows, está en **[docs/05-puesta-en-marcha.md](docs/05-puesta-en-marcha.md)**.

| Servicio | Dirección | Qué es |
|---|---|---|
| Web | http://localhost:3000 | La aplicación |
| API | http://localhost:8080 | El backend |
| Documentación de la API | http://localhost:8080/scalar | Interactiva, solo en desarrollo |
| Salud de la API | http://localhost:8080/salud | Comprobación de estado |
| PostgreSQL | localhost:5432 | Solo accesible desde este equipo |

---

## Estructura del repositorio

```
planvibe/
├── backend/                    Solución .NET con arquitectura limpia
│   ├── src/
│   │   ├── PlanVibe.Domain/          Reglas de negocio. Sin dependencias externas.
│   │   ├── PlanVibe.Application/     Casos de uso y puertos hacia el exterior.
│   │   ├── PlanVibe.Infrastructure/  PostgreSQL, Identity, JWT, mapas, auditoría.
│   │   └── PlanVibe.Api/             Endpoints HTTP, seguridad y composición.
│   └── tests/
│       ├── PlanVibe.Domain.UnitTests/       Reglas de negocio (rápidas, sin E/S)
│       ├── PlanVibe.Application.UnitTests/  Casos de uso con dobles de prueba
│       └── PlanVibe.Api.IntegrationTests/   API real + PostgreSQL en contenedor
│
├── frontend/                   Aplicación web Next.js (App Router)
│   ├── app/                          Páginas y rutas
│   ├── components/                   Componentes de interfaz
│   └── lib/                          BFF, sesión, cliente de API y formateo
│
├── docker/                     Configuración de contenedores
├── docs/                       Documentación del proyecto
└── docker-compose.yml          Orquestación local de los tres servicios
```

---

## Documentación

### Producto

| Documento | Contenido |
|---|---|
| [00 · Visión y MVP](docs/00-vision-y-mvp.md) | Problema, público, alcance y métricas de validación |
| [01 · Requisitos funcionales](docs/01-requisitos-funcionales.md) | RF-01 a RF-24 y requisitos no funcionales |
| [03 · Diseño visual](docs/03-diseno-visual.md) | Sistema visual, navegación y estados de pantalla |
| [04 · Seguridad, privacidad y moderación](docs/04-seguridad-privacidad-moderacion.md) | Decisiones de producto sobre datos personales |

### Técnica

| Documento | Contenido |
|---|---|
| [02 · Arquitectura](docs/02-arquitectura.md) | Capas, módulos, flujos y decisiones estructurales |
| [05 · Puesta en marcha](docs/05-puesta-en-marcha.md) | **Empieza por aquí** si acabas de llegar |
| [06 · Modelo de datos](docs/06-modelo-de-datos.md) | Esquemas, tablas, índices y migraciones |
| [07 · Estrategia de pruebas](docs/07-estrategia-de-pruebas.md) | Qué se prueba, dónde y por qué |
| [08 · Hoja de ruta](docs/08-hoja-de-ruta.md) | Qué está hecho, qué falta y en qué orden |
| [09 · Modelo de amenazas](docs/09-modelo-de-amenazas.md) | Riesgos, mitigaciones aplicadas y deuda pendiente |
| [10 · Contrato de la API](docs/10-contrato-de-api.md) | Endpoints, códigos de estado y errores |
| [11 · Guía de contribución](docs/11-guia-de-contribucion.md) | Convenciones de código, ramas y revisión |
| [ADR](docs/adr/) | Registro de decisiones de arquitectura, con su porqué |

---

## Estado actual

La primera entrega cubre el recorrido completo **registro → verificación → publicar un plan →
explorar → apuntarse**, con base de datos real, autenticación propia y los tres servicios en
contenedores.

**Funciona:**

- Registro, inicio de sesión y renovación de sesión con detección de robo de token.
- Verificación de organizador con proveedor simulado (RF-20 a RF-22).
- Publicar, editar y cancelar quedadas (RF-09 a RF-11).
- Explorar en lista y en mapa, con filtros por categoría, fecha, cercanía y plazas (RF-05, RF-06).
- Apuntarse, retirarse y lista de espera con promoción automática (RF-14, RF-15).
- Registro de auditoría de las acciones sensibles (RNF-04).

**Todavía no:** comentarios, reportes, cola de moderación, favoritos, notificaciones por correo
y eventos públicos con quedada vinculada. El detalle y el orden están en la
[hoja de ruta](docs/08-hoja-de-ruta.md).

---

## Comandos habituales

```bash
docker compose up -d --build
```

Datos de demostración (crea una organizadora verificada y cuatro planes de Ávila):

```bash
./scripts/sembrar-datos-de-demostracion.ps1
```

```bash
docker compose logs -f api
```

```bash
dotnet test backend/PlanVibe.slnx
```

```bash
npm test --prefix frontend
```

---

## Antes de abrir el piloto a personas reales

La lista completa está en [docs/09-modelo-de-amenazas.md](docs/09-modelo-de-amenazas.md). Lo
imprescindible:

1. Sustituir el proveedor de verificación **simulado** por uno real. El actual aprueba a todo el
   mundo y solo debe usarse en desarrollo.
2. Separar los usuarios de base de datos: uno para migraciones y otro, sin permisos de esquema,
   para la aplicación.
3. Servir todo por HTTPS con certificado válido.
4. Redactar términos de uso, política de privacidad y normas de comunidad.
5. Revisar el diseño con asesoramiento jurídico en protección de datos y menores.
