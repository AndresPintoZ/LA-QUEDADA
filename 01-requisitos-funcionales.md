# Requisitos funcionales

## Roles

| Rol | Descripción |
|---|---|
| Visitante | Consulta la página pública y decide registrarse. |
| Usuario registrado | Descubre planes, se apunta, comenta y reporta. |
| Organizador verificado | Puede crear y gestionar quedadas o eventos. |
| Moderador | Revisa reportes y modera contenido. |
| Administrador | Gestiona usuarios, categorías y reglas. |

## Registro y perfiles

- RF-01: El usuario podrá crear una cuenta con correo electrónico y contraseña o proveedor de identidad autorizado.
- RF-02: El usuario podrá completar un perfil con nombre visible, foto opcional, ciudad e intereses.
- RF-03: El usuario podrá editar su perfil y eliminar su cuenta.
- RF-04: La aplicación mostrará una política de privacidad y normas de comunidad antes del registro.

## Descubrimiento

- RF-05: El usuario podrá consultar planes en un mapa y en una lista.
- RF-06: El usuario podrá filtrar por categoría, fecha, distancia y estado de plazas.
- RF-07: El detalle de un plan mostrará título, organizador, descripción, fecha, hora, punto de encuentro, plazas, asistentes y comentarios.
- RF-08: El usuario podrá guardar planes como favoritos.

## Eventos y quedadas

- RF-09: Solo un organizador verificado podrá crear una quedada.
- RF-10: Una quedada tendrá título, categoría, descripción, fecha, hora, lugar, punto de encuentro, capacidad máxima y reglas específicas.
- RF-11: El organizador podrá editar o cancelar su quedada; los asistentes recibirán una notificación.
- RF-12: Un organizador verificado podrá publicar un evento público con la información disponible y, opcionalmente, crear una quedada asociada.
- RF-13: Varias personas podrán proponer correcciones o información adicional de un evento; el organizador o moderador aprobará los cambios antes de publicarlos.
- RF-14: Un usuario podrá apuntarse o retirarse mientras existan plazas y se cumplan las reglas del plan.
- RF-15: La aplicación podrá mantener una lista de espera cuando se alcance el máximo de asistentes.

## Participación y moderación

- RF-16: Los usuarios registrados podrán comentar en un plan o evento.
- RF-17: Los usuarios podrán reportar publicaciones, comentarios o perfiles.
- RF-18: Los moderadores podrán ocultar contenido, suspender cuentas y registrar la razón de la decisión.
- RF-19: El sistema enviará avisos de cambios, cancelaciones y recordatorios próximos.

## Verificación de organizadores

- RF-20: Antes de crear su primera quedada, el usuario deberá completar una verificación de identidad mediante documento oficial válido.
- RF-21: La verificación deberá realizarse por un proveedor especializado o un proceso interno aprobado tras revisión jurídica; la plataforma no guardará una copia del documento salvo que exista una necesidad legal concreta y documentada.
- RF-22: La plataforma conservará únicamente el estado de verificación, identificador de la verificación, fecha, proveedor y trazabilidad de aceptación de normas; no mostrará datos documentales a otros usuarios.
- RF-23: Un organizador podrá perder su condición de verificado tras una revisión de seguridad o moderación.
- RF-24: Para el piloto, solo podrán crear planes los organizadores mayores de 18 años. Los menores podrán usar la plataforma conforme a la política específica aplicable, pero no organizar encuentros públicos hasta definir un proceso reforzado.

## Requisitos no funcionales

- RNF-01: Diseño adaptable a móvil desde 360 píxeles de ancho.
- RNF-02: La carga inicial debe ser rápida en conexiones móviles normales.
- RNF-03: Contraseñas almacenadas con hash seguro; nunca en texto plano.
- RNF-04: Registro de auditoría para verificaciones, publicaciones, reportes y acciones de moderación.
- RNF-05: Accesibilidad básica: contraste suficiente, navegación por teclado y etiquetas en controles.
