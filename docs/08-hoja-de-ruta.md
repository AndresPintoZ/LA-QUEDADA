# Hoja de ruta

Qué está hecho, qué falta y en qué orden. Sirve para retomar el proyecto sin tener que deducirlo
leyendo el código.

---

## Hecho

Recorrido completo **registro → verificación → publicar → explorar → apuntarse**, con base de
datos real, autenticación propia y los tres servicios en contenedores.

| Requisito | Estado | Notas |
|---|---|---|
| RF-01 · Cuenta con correo y contraseña | ✅ | Falta el proveedor de identidad externo |
| RF-02 · Perfil con nombre, ciudad e intereses | ⚠️ | El dominio lo soporta; falta la pantalla de edición |
| RF-03 · Editar perfil y eliminar cuenta | ⚠️ | `Usuario.Eliminar` anonimiza; falta la pantalla |
| RF-04 · Normas antes del registro | ✅ | Con constancia de la versión aceptada |
| RF-05 · Mapa y lista | ✅ | Leaflet y OpenStreetMap |
| RF-06 · Filtros | ✅ | Categoría, fecha, cercanía y plazas |
| RF-07 · Detalle del plan | ✅ | Sin comentarios todavía |
| RF-08 · Favoritos | ❌ | |
| RF-09 · Solo organizadores verificados | ✅ | Comprobado en tres capas |
| RF-10 · Datos de la quedada | ✅ | |
| RF-11 · Editar y cancelar | ⚠️ | Cancelar funciona; falta la pantalla de edición |
| RF-12 · Evento público con quedada vinculada | ❌ | |
| RF-13 · Correcciones colaborativas | ❌ | |
| RF-14 · Apuntarse y retirarse | ✅ | |
| RF-15 · Lista de espera | ✅ | Con promoción automática |
| RF-16 · Comentarios | ❌ | |
| RF-17 · Reportes | ❌ | |
| RF-18 · Acciones de moderación | ⚠️ | El dominio sabe hacerlo; falta interfaz y reportes |
| RF-19 · Avisos | ⚠️ | Los eventos se disparan; falta el envío real |
| RF-20 · Verificación previa | ✅ | Con proveedor **simulado** |
| RF-21 · Proveedor especializado | ⚠️ | Adaptador listo; falta contratar uno real |
| RF-22 · Solo estado y referencia | ✅ | Protegido por una prueba |
| RF-23 · Pérdida de la verificación | ✅ | |
| RF-24 · Solo mayores de edad organizan | ✅ | |
| RNF-01 · Adaptable desde 360 px | ✅ | |
| RNF-02 · Carga rápida en móvil | ✅ | Componentes de servidor |
| RNF-03 · Contraseñas con hash | ✅ | ASP.NET Core Identity |
| RNF-04 · Auditoría | ✅ | En la misma transacción |
| RNF-05 · Accesibilidad básica | ⚠️ | Aplicada; falta comprobación automatizada |

---

## Siguiente: abrir el piloto

Lo que hay que resolver **antes** de que entren personas reales. Sin esto, no se abre.

### 1 · Proveedor de verificación real

**Bloqueante.** El actual aprueba a todo el mundo y solo se registra en desarrollo.

Trabajo: elegir proveedor, revisar su contrato de tratamiento de datos, implementar
`IProveedorDeVerificacion` e integrar su llamada de vuelta. La interfaz ya está diseñada para que
ningún documento pueda entrar en el sistema.

Comprobar que devuelve confirmación de mayoría de edad **sin** la fecha completa. Si solo devuelve
la fecha, hay que descartarla en el adaptador.

### 2 · Reportes y cola de moderación (RF-17, RF-18)

**Bloqueante.** Una plataforma social sin forma de avisar de un problema no debería abrirse.

Trabajo: agregado `Reporte`, endpoints, cola de revisión en el panel de moderación y conexión con
`OcultarPorModeracion` y `Suspender`, que ya existen. La pantalla `/moderacion` ya comprueba el rol.

### 3 · Comentarios (RF-16)

Van con el punto anterior: comentar sin poder reportar es abrir la puerta al acoso sin salida.

Trabajo: agregado `Comentario` con estado de moderación, endpoints y interfaz. Ojo con la longitud
máxima y con el escape del contenido.

### 4 · Separar usuarios de base de datos

**Bloqueante.** Hoy la aplicación se conecta con el propietario del esquema. Hacen falta dos
usuarios: uno para migraciones y otro, sin permisos de esquema, para la aplicación. Anotado en
`docker/postgres/01-extensiones.sql`.

### 5 · Notificaciones reales (RF-19)

Los eventos ya se disparan (`QuedadaCancelada`, `AsistentePromovido`, `QuedadaModificada`) y
`NotificacionesEnRegistro` los escribe en el registro. Falta un servicio de envío real.

Desbloquea además el restablecimiento de contraseña y la confirmación de correo, que hoy no existen
por este motivo.

### 6 · HTTPS y despliegue

Certificado válido, proxy inverso, copias de seguridad con restauración probada.

### 7 · Textos legales

Términos de uso, política de privacidad y normas de comunidad, revisados con asesoramiento
jurídico en protección de datos y menores. Las páginas `/normas` y `/privacidad` ya están
enlazadas desde el registro.

---

## Después: completar el MVP

| Trabajo | Requisito | Notas |
|---|---|---|
| Editar quedada | RF-11 | `CambiarDetalles` y `CambiarCuandoYDonde` ya existen |
| Editar perfil e intereses | RF-02 | `ActualizarPerfil` ya existe |
| Eliminar cuenta | RF-03 | `Eliminar` ya anonimiza |
| Favoritos | RF-08 | |
| Eventos públicos | RF-12, RF-13 | El agregado más grande que falta |
| Selector de lugar en el mapa | — | Hoy las coordenadas son campos ocultos |
| Comprobación de accesibilidad automatizada | RNF-05 | axe en integración continua |

---

## Deuda técnica anotada

Ninguna es urgente, pero conviene no olvidarlas:

- **Ventana de revocación de 15 minutos.** Al revocar una verificación, el token ya emitido sigue
  siendo válido. Consecuencia conocida de usar JWT; documentada en el modelo de amenazas.
- **Sin prueba de concurrencia real.** Hay dos barreras contra la doble inscripción, pero no están
  probadas bajo carga.
- **Sin prueba de extremo a extremo de la renovación de token.** La lógica está; falta la prueba.
- **La zona horaria está fija a `Europe/Madrid`.** Correcto para el piloto; cuando salga de Ávila,
  hay que sacarla a configuración por persona.
- **Sin revisión automática de dependencias.** La auditoría se ejecuta al compilar, pero nadie
  avisa de una vulnerabilidad publicada si no se compila.
- **Sin retención automática de la auditoría.** Las entradas se acumulan sin límite.

---

## Cómo retomar el trabajo

1. Lee [05-puesta-en-marcha.md](05-puesta-en-marcha.md) y levanta el entorno.
2. Haz el recorrido de comprobación: confirma que todo funciona antes de tocar nada.
3. Lee [02-arquitectura.md](02-arquitectura.md) y los [ADR](adr/) de la parte que vayas a tocar.
4. Ejecuta las pruebas para tener una base verde de referencia.
5. Empieza por la prueba, no por la implementación: es como está escrito el dominio y hace de red
   de seguridad al cambiar algo.
