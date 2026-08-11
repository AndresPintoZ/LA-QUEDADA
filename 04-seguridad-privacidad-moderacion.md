# Seguridad, privacidad y moderación

## Decisión de producto

Para publicar una quedada, el organizador debe estar verificado. El fin es contar con trazabilidad de quién publicó el plan y reducir abuso, suplantación y cuentas falsas.

## Principio clave

La aplicación debe verificar identidad sin almacenar una fotografía, número o copia del documento oficial en sus propios sistemas. Se recomienda integrar un proveedor de verificación que devuelva una confirmación y referencia técnica. Esto reduce el riesgo y aplica el criterio de minimización de datos.

## Datos a conservar

- Identificador interno del usuario.
- Estado de verificación: pendiente, verificado, rechazado o revocado.
- Proveedor de verificación.
- Referencia técnica de la transacción.
- Fecha y hora de verificación.
- Evidencia de aceptación de términos y normas.
- Registro de creación, edición o cancelación de cada quedada.

## Datos que no se deben conservar en el MVP

- Fotografía o escaneo del DNI, NIE, pasaporte u otro documento.
- Número completo de documento.
- Dirección personal.
- Fecha de nacimiento completa si no es imprescindible.
- Datos biométricos.

## Menores

El piloto debe restringir la creación de quedadas públicas a mayores de 18 años. Si la aplicación admite usuarios menores, se requerirá una política específica, diseño de seguridad reforzado y revisión jurídica antes de habilitar funciones sociales sensibles.

## Moderación operativa

1. Todos los usuarios pueden reportar contenido o perfiles.
2. El reporte entra en una cola de revisión.
3. El moderador puede ocultar el contenido, solicitar cambios, suspender una cuenta o cerrar el reporte.
4. Cada decisión queda registrada con fecha, motivo y responsable.
5. Los incidentes graves se escalan a un administrador.

## Reglas de publicación

- Prohibidos planes en domicilios particulares.
- No se permite publicar datos personales de terceros.
- Debe indicarse si existe un coste, material necesario, edad mínima o nivel físico requerido.
- El punto de encuentro debe ser un lugar público y razonablemente seguro.
- Las publicaciones deben respetar las normas de convivencia y legislación aplicable.

## Próximos pasos antes de lanzamiento público

1. Elegir proveedor de verificación y revisar su contrato de tratamiento de datos.
2. Redactar términos de uso, política de privacidad y normas de comunidad.
3. Definir retención y borrado de registros.
4. Revisar el diseño con asesoramiento jurídico especializado en protección de datos y menores.
5. Realizar una prueba cerrada con usuarios adultos antes de abrir el registro.

## Referencias de orientación

- La Agencia Española de Protección de Datos indica que la privacidad debe configurarse por defecto y que deben tratarse solo los datos necesarios para una finalidad definida.
- Para menores, el diseño debe priorizar simultáneamente seguridad y privacidad.

Estas pautas son de producto y no sustituyen asesoramiento jurídico profesional.
