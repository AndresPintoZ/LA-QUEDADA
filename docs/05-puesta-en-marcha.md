# Puesta en marcha

Guía para dejar PlanVibe funcionando en un equipo desde cero. Si acabas de llegar al proyecto,
este es el documento por el que empezar.

---

## 1. Requisitos previos

| Herramienta | Versión | Para qué |
|---|---|---|
| Docker Desktop | 4.30 o superior | Levantar los tres servicios |
| .NET SDK | 10.0 | Desarrollar el backend fuera de Docker |
| Node.js | 22 o superior | Desarrollar el frontend fuera de Docker |
| Git | cualquiera reciente | Control de versiones |

Para **solo levantar la aplicación** basta con Docker. El SDK y Node hacen falta para desarrollar
con recarga automática, que es bastante más cómodo.

### Windows: instalar WSL2 antes que Docker

Docker Desktop en Windows necesita WSL2 (o Hyper-V) para arrancar su motor. Si `docker info` da
error o dice que el motor no responde, casi siempre falta esto.

En PowerShell **como administrador**:

```powershell
wsl --install
```

Reinicia el equipo, abre Docker Desktop y espera a que el icono deje de estar en amarillo.
Después comprueba que responde:

```bash
docker info
```

Si `docker` no se reconoce como comando pero Docker Desktop está instalado, el CLI no está en el
PATH. Suele estar en `%LOCALAPPDATA%\Programs\DockerDesktop\resources\bin`. Añádelo al PATH del
usuario y abre una terminal nueva.

---

## 2. Preparar la configuración

El repositorio no contiene ningún secreto. Hay que crear el archivo `.env` a partir de la plantilla:

```bash
cp docker/.env.example .env
```

En PowerShell:

```powershell
Copy-Item docker\.env.example .env
```

Ahora rellena los **tres valores obligatorios**. La aplicación se niega a arrancar si falta
alguno, a propósito: es preferible un fallo claro al arrancar que un sistema en marcha con una
clave de firma vacía.

### `POSTGRES_PASSWORD`

Contraseña de la base de datos.

```powershell
[Convert]::ToBase64String((1..24 | ForEach-Object { Get-Random -Maximum 256 }))
```

### `JWT_CLAVE`

Clave con la que se firman los tokens de acceso. **Mínimo 64 caracteres.**

```powershell
[Convert]::ToBase64String((1..64 | ForEach-Object { Get-Random -Maximum 256 }))
```

### `SESION_SECRETO`

Clave con la que el frontend cifra la cookie de sesión. **Mínimo 32 caracteres.**

```powershell
[Convert]::ToBase64String((1..32 | ForEach-Object { Get-Random -Maximum 256 }))
```

> Genera valores propios. No reutilices los de ningún ejemplo ni los compartas entre entornos:
> una clave que ha estado en un chat o en un documento ya no es un secreto.

---

## 3. Levantar todo

```bash
docker compose up -d --build
```

La primera vez tarda unos minutos porque compila las dos imágenes. Después, comprueba que los
tres servicios están sanos:

```bash
docker compose ps
```

Los tres deben aparecer como `healthy`. Si `api` se queda en `starting` mucho rato:

```bash
docker compose logs api
```

Cuando todo esté en marcha, abre **http://localhost:3000**.

### Qué ocurre en el primer arranque

1. PostgreSQL crea la base de datos y ejecuta `docker/postgres/01-extensiones.sql`, que instala
   PostGIS y crea los esquemas `app`, `identidad` y `auditoria`.
2. La API espera a que la base de datos esté sana, aplica las migraciones de EF Core y siembra
   las cinco categorías iniciales del piloto.
3. La web arranca y empieza a servir páginas.

La aplicación arranca **sin ningún plan**: la base de datos está vacía.

Para verla con contenido sin rellenar formularios a mano, hay un script que crea una organizadora
verificada, cuatro planes reales de Ávila y una persona apuntada:

```bash
./scripts/sembrar-datos-de-demostracion.ps1
```

Imprime al final las cuentas creadas y sus contraseñas, para que puedas entrar con ellas. Solo
funciona en desarrollo: usa el proveedor de verificación simulado.

Si prefieres comprobarlo tú mismo paso a paso, sigue el recorrido de la sección 5.

---

## 4. Comandos habituales

Ver los registros de un servicio en tiempo real:

```bash
docker compose logs -f api
```

Reconstruir solo la API tras cambiar código:

```bash
docker compose up -d --build api
```

Parar todo conservando los datos:

```bash
docker compose down
```

Parar y **borrar la base de datos** (empezar de cero):

```bash
docker compose down -v
```

Abrir una consola de PostgreSQL:

```bash
docker compose exec db psql -U planvibe -d planvibe
```

---

## 5. Recorrido de comprobación

Este recorrido confirma que todo funciona de verdad. Lleva unos tres minutos.

1. **Crea una cuenta.** Abre http://localhost:3000/acceso?modo=registro y regístrate. La
   contraseña debe tener al menos 12 caracteres.

2. **Intenta crear un plan.** Ve a «Crear». Te dirá que necesitas verificarte: es la regla RF-09
   funcionando.

3. **Verifícate.** Pulsa «Verificar mi identidad» y luego «Empezar la verificación». En
   desarrollo, el proveedor es **simulado** y aprueba automáticamente sin pedir ningún documento.

4. **Publica un plan.** Rellena el formulario. Marca la casilla de lugar público: es obligatoria.

5. **Míralo en explorar.** El plan aparece en la lista y en el mapa.

6. **Apúntate con otra cuenta.** Abre una ventana privada, crea otra cuenta y apúntate al plan.
   Fíjate en que la dirección exacta solo aparece **después** de tener plaza confirmada.

7. **Comprueba la auditoría.** En la consola de PostgreSQL:

   ```sql
   SELECT accion, tipo_de_objeto, ocurrido_en FROM auditoria.entradas ORDER BY ocurrido_en DESC;
   ```

---

## 6. Desarrollar sin Docker

Para desarrollar con recarga automática conviene dejar solo la base de datos en Docker y
ejecutar API y web en local.

Levanta la base de datos:

```bash
docker compose up -d db
```

Arranca la API (desde `backend/src/PlanVibe.Api`):

```bash
dotnet watch run
```

Necesita estas variables de entorno. En PowerShell:

```powershell
$env:ConnectionStrings__PlanVibe = "Host=localhost;Port=5432;Database=planvibe;Username=planvibe;Password=LA_QUE_PUSISTE"
$env:Jwt__Clave = "LA_QUE_PUSISTE_EN_JWT_CLAVE"
$env:ASPNETCORE_ENVIRONMENT = "Development"
```

Arranca la web (desde `frontend`):

```bash
npm run dev
```

Con estas variables:

```powershell
$env:API_URL_INTERNA = "http://localhost:8080"
$env:SESION_SECRETO = "EL_QUE_PUSISTE_EN_SESION_SECRETO"
```

---

## 7. Ejecutar las pruebas

Todas las del backend:

```bash
dotnet test backend/PlanVibe.slnx
```

Las de dominio y aplicación no necesitan nada: van en memoria. Las de integración levantan un
PostgreSQL efímero con Testcontainers y **se omiten automáticamente** si no hay Docker en marcha.

Las del frontend:

```bash
npm test --prefix frontend
```

Comprobar tipos sin compilar:

```bash
npm run typecheck --prefix frontend
```

---

## 8. Migraciones de base de datos

En desarrollo se aplican solas al arrancar la API. Para crear una nueva tras cambiar el modelo:

```bash
dotnet ef migrations add NombreDescriptivo --project backend/src/PlanVibe.Infrastructure --startup-project backend/src/PlanVibe.Api --output-dir Persistencia/Migraciones
```

Si `dotnet ef` no está instalado:

```bash
dotnet tool install --global dotnet-ef
```

Revisa siempre el SQL generado antes de confirmar el cambio:

```bash
dotnet ef migrations script --project backend/src/PlanVibe.Infrastructure --startup-project backend/src/PlanVibe.Api
```

> En producción las migraciones **no** se aplican al arrancar: son un paso explícito del
> despliegue. Ver [06-modelo-de-datos.md](06-modelo-de-datos.md).

---

## 9. Problemas frecuentes

**`docker: command not found` aunque Docker Desktop esté instalado.**
El CLI no está en el PATH. Ver la sección 1.

**La API se reinicia en bucle.**
Mira `docker compose logs api`. Lo más habitual es que falte `JWT_CLAVE` o que tenga menos de 64
caracteres. La aplicación lo comprueba al arrancar y se niega a levantarse.

**`SESION_SECRETO` demasiado corto.**
El frontend exige 32 caracteres como mínimo. Con menos, el cifrado de la cookie no aportaría
seguridad real y se prefiere fallar antes que aparentar que protege algo.

**El puerto 5432 está ocupado.**
Ya tienes un PostgreSQL local. Cambia `POSTGRES_PORT` en `.env` a otro puerto, por ejemplo 5433.

**Cambié `01-extensiones.sql` y no se aplica.**
Ese script solo se ejecuta al crear el volumen de datos. Para forzarlo:
`docker compose down -v && docker compose up -d`. Esto **borra la base de datos**.

**El mapa no carga las teselas.**
Las teselas vienen de `tile.openstreetmap.org` y necesitan salida a internet. Comprueba también
que la política de seguridad de contenido de `frontend/next.config.mjs` incluye ese dominio.
