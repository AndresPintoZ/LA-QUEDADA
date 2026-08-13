# Modelo de amenazas

Qué puede salir mal, qué se ha hecho al respecto y qué queda pendiente. Este documento es honesto
sobre lo que **no** está resuelto: una lista de medidas sin sus huecos da una falsa sensación de
seguridad.

---

## Qué protegemos

Por orden de gravedad si falla:

1. **La seguridad física de las personas.** PlanVibe organiza encuentros presenciales. Una
   suplantación o una dirección filtrada tienen consecuencias fuera de la pantalla.
2. **Los datos de identidad.** RF-20 exige verificar la identidad. Cómo se maneja eso decide si una
   filtración es un incidente o una catástrofe.
3. **Las cuentas.** Suplantar a una organizadora verificada permitiría convocar encuentros con su
   credibilidad.
4. **La disponibilidad.** Menos grave, pero un piloto caído no valida nada.

---

## Amenazas y medidas

### A1 · Suplantación de una persona organizadora

**Riesgo: alto.** Alguien publica un encuentro haciéndose pasar por otra persona o por una entidad.

| Medida | Estado | Dónde |
|---|---|---|
| Verificación obligatoria antes de la primera publicación | ✅ | `Quedada.Crear`, política `puede-organizar` |
| Comprobación en tres capas: interfaz, política de API y agregado | ✅ | Frontend, `PoliticasDeAutorizacion`, dominio |
| El resultado se consulta al proveedor, no se acepta del cliente | ✅ | `CompletarVerificacionManejador` |
| La referencia debe corresponder a la cuenta que la reclama | ✅ | `CompletarVerificacionManejador` |
| Revocación de la verificación (RF-23) | ✅ | `Usuario.RevocarVerificacion` |
| **Proveedor real** en lugar del simulado | ❌ | **Bloqueante para el piloto** |
| Revocación efectiva inmediata | ⚠️ | El token vive 15 min: hay ventana |

> **Ventana de revocación.** Al revocar una verificación, el token de acceso ya emitido sigue
> siendo válido hasta 15 minutos. Es una consecuencia conocida de usar JWT sin estado. Se acepta
> porque la alternativa —consultar la base de datos en cada petición— anula la ventaja del token.
> Si un caso lo requiere, hay que llamar además a `CerrarTodasLasSesionesAsync`.

### A2 · Filtración de datos de identidad

**Riesgo: alto si ocurre, pero muy improbable por diseño.**

| Medida | Estado |
|---|---|
| No se guarda imagen ni número de documento | ✅ |
| Solo el año de nacimiento, no la fecha completa | ✅ |
| La mayoría de edad se guarda como sí/no | ✅ |
| La interfaz del proveedor **no admite** documentos | ✅ |
| Prueba automática que falla si se añade un campo así | ✅ |

Ver [ADR-006](adr/006-verificacion-sin-almacenar-documentos.md). Una filtración de la base de datos
no expone ningún documento porque no hay ninguno.

### A3 · Exposición de ubicaciones privadas

**Riesgo: alto.** La dirección exacta de un punto de encuentro en manos equivocadas es un problema
de seguridad física.

| Medida | Estado | Dónde |
|---|---|---|
| La dirección exacta solo se devuelve a quien tiene plaza confirmada | ✅ | `Quedada.DireccionExactaVisiblePara` |
| La decisión la toma el **dominio**, no la vista | ✅ | Ninguna vista puede saltársela |
| Prohibición de domicilios particulares, con declaración auditable | ✅ | `PuntoEncuentro.EsLugarPublico` |
| Coordenadas redondeadas a ~1 metro | ✅ | `Coordenadas` |
| Prueba de integración que verifica la restricción | ✅ | `FlujoCompletoTests` |
| Verificar que el lugar es realmente público | ❌ | No es automatizable; queda en moderación |

### A4 · Robo de sesión

**Riesgo: medio.**

| Medida | Estado |
|---|---|
| Token en cookie `httpOnly` cifrada: JavaScript no puede leerlo | ✅ |
| Política de seguridad de contenido sin `unsafe-inline` en scripts | ✅ |
| Token de acceso de 15 minutos, sin margen de reloj | ✅ |
| Renovación rotativa de un solo uso | ✅ |
| Detección de reutilización → revoca la familia entera | ✅ |
| Tokens de renovación guardados como hash SHA-256 | ✅ |
| `SameSite=Lax` y verificación de origen en acciones de servidor | ✅ |
| Prefijo `__Host-` en la cookie en producción | ✅ |
| Aviso a la persona cuando se detecta reutilización | ❌ Pendiente |

### A5 · Fuerza bruta y enumeración de cuentas

**Riesgo: medio.**

| Medida | Estado |
|---|---|
| Bloqueo tras 5 intentos fallidos, 15 minutos | ✅ |
| Límite de 10 peticiones cada 5 minutos en autenticación | ✅ |
| Mismo mensaje de error exista o no la cuenta | ✅ |
| Tiempo constante al validar credenciales (mínimo 250 ms) | ✅ |
| El registro no confirma si un correo ya existe | ✅ |
| Contraseñas de 12 caracteres mínimo | ✅ |
| Comprobar contra listas de contraseñas filtradas | ❌ Pendiente |
| CAPTCHA o similar en el registro | ❌ Pendiente si aparece abuso |

### A6 · Inyección de SQL

**Riesgo: bajo.**

| Medida | Estado |
|---|---|
| Todo el acceso a datos va por EF Core, parametrizado | ✅ |
| Ninguna consulta se construye por concatenación | ✅ |
| `CA2100` tratado como error de compilación | ✅ |
| La búsqueda de texto usa `EF.Functions.ILike` con parámetro | ✅ |
| **Usuario de base de datos sin permisos de esquema** | ❌ **Pendiente para producción** |

> Hoy la aplicación se conecta con el usuario propietario de la base de datos. Es cómodo para
> desarrollar, pero significa que una inyección que llegara a ejecutarse tendría control total del
> motor. En producción hacen falta dos usuarios: uno para migraciones y otro, sin permisos de
> esquema, para la aplicación. Está anotado en `docker/postgres/01-extensiones.sql`.

### A7 · XSS

**Riesgo: bajo.**

| Medida | Estado |
|---|---|
| React escapa el contenido por defecto | ✅ |
| No se usa `dangerouslySetInnerHTML` en ningún sitio | ✅ |
| Los globos del mapa se construyen con la API del DOM, no con HTML en texto | ✅ |
| Política de seguridad de contenido estricta | ✅ |
| `X-Content-Type-Options: nosniff` | ✅ |
| Longitud máxima en todos los campos de texto | ✅ |

`'unsafe-inline'` sigue permitido en **estilos**, que lo necesitan Tailwind y Leaflet. En scripts no.

### A8 · Denegación de servicio

**Riesgo: medio.** Un piloto pequeño se satura con poco.

| Medida | Estado |
|---|---|
| Límite general de 200 peticiones/minuto por cliente | ✅ |
| Límites más estrictos en autenticación, escritura y geocodificación | ✅ |
| Tope de 50 elementos por página, aplicado en servidor | ✅ |
| Radio de búsqueda limitado a 100 km | ✅ |
| Tamaño máximo del cuerpo de la petición | ✅ |
| Índices en todas las consultas frecuentes | ✅ |
| Tiempo de espera corto en llamadas externas | ✅ |
| Proxy inverso con protección adicional | ❌ Pendiente para producción |

### A9 · Contenido dañino y acoso

**Riesgo: alto en una plataforma social.**

| Medida | Estado |
|---|---|
| Suspensión de cuentas con motivo obligatorio | ✅ Dominio |
| Ocultación de publicaciones con motivo obligatorio | ✅ Dominio |
| Auditoría de las decisiones de moderación | ✅ |
| Política de autorización de moderador | ✅ |
| **Reportes y cola de revisión (RF-17, RF-18)** | ❌ **Pendiente** |
| Comentarios (RF-16) | ❌ Pendiente |

> El dominio ya sabe moderar; falta la vía por la que llegan los avisos. Mientras tanto, hay que
> ofrecer un canal de contacto visible. Ver [08-hoja-de-ruta.md](08-hoja-de-ruta.md).

### A10 · Cadena de suministro

**Riesgo: medio.** Es el vector que más ha crecido.

| Medida | Estado |
|---|---|
| `NuGetAudit` en modo `all` y nivel `low` | ✅ |
| Vulnerabilidades tratadas como **error de compilación** | ✅ |
| Versiones centralizadas en `Directory.Packages.props` | ✅ |
| Fuente de paquetes limitada a nuget.org con mapeo | ✅ |
| `npm ci` en la imagen: instala exactamente el bloqueo de versiones | ✅ |
| `npm audit` sin vulnerabilidades | ✅ |
| Revisión automática periódica de dependencias | ❌ Pendiente |

Durante el desarrollo, la auditoría detuvo la compilación por dos paquetes transitivos con CVE
conocido (`Microsoft.OpenApi` y `System.Security.Cryptography.Xml`) y por vulnerabilidades críticas
en Next 15.1.0. Los tres se corrigieron fijando versiones parcheadas. Funcionó como debía.

### A11 · Fuga de información por errores y registros

**Riesgo: bajo.**

| Medida | Estado |
|---|---|
| Las excepciones inesperadas devuelven un mensaje genérico | ✅ |
| La traza solo va al registro del servidor | ✅ |
| 404 tanto si no existe como si no se puede ver | ✅ |
| `EnableSensitiveDataLogging` desactivado incluso en desarrollo | ✅ |
| Mensajes de registro centralizados y revisables de un vistazo | ✅ |
| Nunca se registran correos, contraseñas ni tokens | ✅ |
| Cabecera `Server` eliminada | ✅ |
| Documentación de la API solo en desarrollo | ✅ |

### A12 · Escape del contenedor

**Riesgo: bajo.**

| Medida | Estado |
|---|---|
| Imagen final «chiseled»: sin intérprete de órdenes ni gestor de paquetes | ✅ |
| Ambos contenedores corren con usuario sin privilegios | ✅ |
| Sistema de archivos de solo lectura | ✅ |
| `no-new-privileges` y todas las capacidades retiradas | ✅ |
| Directorios temporales en memoria, sin permiso de ejecución | ✅ |
| PostgreSQL solo escucha en 127.0.0.1 | ✅ |
| Compilación en varias etapas: el código fuente no viaja a la imagen | ✅ |

---

## Bloqueantes antes de abrir el piloto

Por orden:

1. **Proveedor de verificación real.** El simulado aprueba a todo el mundo. Sin esto, RF-20 no se
   cumple y la premisa de seguridad del producto se cae.
2. **Separar los usuarios de base de datos.** Uno para migraciones, otro sin permisos de esquema
   para la aplicación.
3. **HTTPS con certificado válido.** Sin él, la cookie no puede llevar el prefijo `__Host-` ni la
   marca `secure`, y todo lo demás pierde sentido.
4. **Reportes y cola de moderación.** Una plataforma social sin forma de avisar de un problema no
   debería abrirse.
5. **Términos, política de privacidad y normas de comunidad**, revisados con asesoramiento
   jurídico en protección de datos y menores.
6. **Copias de seguridad de la base de datos**, con restauración probada. Una copia que no se ha
   restaurado nunca no es una copia.

## Deuda anotada, no bloqueante

- Avisar a la persona cuando se detecta reutilización de token.
- Comprobar contraseñas contra listas de filtraciones conocidas.
- Restablecimiento de contraseña y confirmación de correo (necesitan servicio de envío).
- Revisión automática y periódica de dependencias.
- Proxy inverso con protección adicional.
- Retención y borrado automático del registro de auditoría.

---

Este documento **no sustituye** una auditoría de seguridad ni asesoramiento jurídico profesional.
