# ADR-002 · Autenticación propia con JWT y renovación rotativa

**Estado:** aceptada · 2026-08-11

## Contexto

`docs/02-arquitectura.md` proponía inicialmente un «servicio gestionado con correo» para evitar
implementar credenciales desde cero. Es un buen consejo por defecto: la autenticación es fácil de
hacer mal.

Al concretar, aparecieron dos restricciones:

1. El entorno debe levantarse **completo en local** con `docker compose up`, sin dar de alta
   ninguna cuenta externa ni configurar claves de API. Un proveedor gestionado obliga a registrarse
   antes de poder ver la aplicación funcionando.
2. El backend es .NET propio. ASP.NET Core Identity ya resuelve la parte peligrosa —hash de
   contraseñas, bloqueo de cuentas, marcas de seguridad— y está mantenido por Microsoft. Lo que
   queda por escribir es la emisión de tokens, no la criptografía.

La alternativa autoalojada (Keycloak en un cuarto contenedor) resolvía la primera restricción, pero
añade un servicio pesado que operar para un piloto de veinte personas.

## Decisión

Autenticación propia:

- **ASP.NET Core Identity** para credenciales. No se implementa hash de contraseñas a mano.
- **Token de acceso**: JWT firmado con HMAC-SHA256, 15 minutos de vida, sin margen de tolerancia
  de reloj.
- **Token de renovación**: valor aleatorio de 256 bits, guardado **como hash SHA-256**, rotativo y
  de un solo uso.
- **Detección de reutilización**: si aparece un token de renovación ya usado, se revoca toda la
  familia de sesiones nacida de ese inicio de sesión.

Política de contraseñas: **12 caracteres mínimo, sin exigir composición**. No se piden mayúsculas,
números ni símbolos.

## Consecuencias

**A favor:**

- El entorno completo arranca sin dar de alta nada.
- El token de acceso se valida sin tocar la base de datos: es rápido.
- El token de renovación **sí** se puede revocar al instante, porque su existencia depende de una
  fila en la base de datos.
- La detección de reutilización convierte un robo de token en un incidente contenido y detectable,
  en lugar de en un acceso permanente y silencioso.

**En contra:**

- Es código de seguridad propio y hay que mantenerlo. Está concentrado en `EmisorDeTokens` y
  `ServicioDeCredenciales`, con comentarios que explican cada decisión.
- No hay inicio de sesión con proveedores externos (RF-01 lo contempla como opción). Queda en la
  hoja de ruta.
- Faltan restablecimiento de contraseña y confirmación de correo, porque no hay servicio de envío
  todavía. Está anotado en el registro de dependencias.

### Sobre la política de contraseñas

Exigir «una mayúscula, un número y un símbolo» empuja a la gente hacia `Password1!` y variantes,
que son de las primeras que prueba cualquier ataque por diccionario. Una frase de doce caracteres
que la persona recuerde resiste mucho mejor y no acaba apuntada en un papel.

Se complementa con **bloqueo temporal tras cinco intentos fallidos**, que es lo que de verdad frena
un ataque de fuerza bruta contra una cuenta concreta.

## Alternativas descartadas

**Auth0, Microsoft Entra o similar.** Buena opción para producción. Descartada para el piloto
porque obliga a dar de alta una cuenta y configurar claves antes de poder arrancar el entorno, y
porque ata el desarrollo local a que un servicio externo esté disponible.

**Keycloak en un contenedor.** Resuelve el arranque local. Descartada porque es un servicio pesado
—arranque lento, memoria, su propia base de datos, su propia curva de aprendizaje— para un piloto
de veinte personas.

**Solo cookies de sesión de Identity, sin JWT.** Sería más simple. Descartada porque complica una
posible aplicación móvil nativa más adelante y porque, con el BFF, el token ya viaja dentro de una
cookie cifrada: se obtiene la ventaja de la cookie sin renunciar a la del token.
