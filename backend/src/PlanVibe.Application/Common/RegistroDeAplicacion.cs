using Microsoft.Extensions.Logging;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Application.Common;

/// <summary>
/// Mensajes de registro de la capa de aplicación, generados en tiempo de compilación.
/// </summary>
/// <remarks>
/// <para>
/// El generador de <c>LoggerMessage</c> produce código que no reserva memoria ni convierte
/// argumentos a texto cuando el nivel está desactivado. Con una llamada suelta a
/// <c>LogInformation</c>, ese trabajo se hace aunque el mensaje acabe descartándose.
/// </para>
/// <para>
/// Tenerlos todos en un archivo tiene además un efecto útil para la privacidad: se puede revisar
/// de una sentada qué se está escribiendo en los registros. Aquí no debe aparecer nunca un correo,
/// una contraseña, un token ni una dirección; solo identificadores internos.
/// </para>
/// </remarks>
public static partial class RegistroDeAplicacion
{
    [LoggerMessage(
        EventId = 1001,
        Level = LogLevel.Information,
        Message = "Quedada {QuedadaId} publicada por el usuario {UsuarioId}")]
    public static partial void QuedadaPublicada(this ILogger registro, QuedadaId quedadaId, UsuarioId usuarioId);

    [LoggerMessage(
        EventId = 1002,
        Level = LogLevel.Information,
        Message = "Usuario {UsuarioId} apuntado a la quedada {QuedadaId}. Plaza confirmada: {Confirmada}")]
    public static partial void UsuarioApuntado(this ILogger registro, UsuarioId usuarioId, QuedadaId quedadaId, bool confirmada);

    [LoggerMessage(
        EventId = 2001,
        Level = LogLevel.Warning,
        Message = "Intento de registro con un correo que ya existe")]
    public static partial void RegistroConCorreoExistente(this ILogger registro);

    [LoggerMessage(
        EventId = 2002,
        Level = LogLevel.Warning,
        Message = "Intento fallido de inicio de sesión")]
    public static partial void InicioDeSesionFallido(this ILogger registro);

    [LoggerMessage(
        EventId = 2003,
        Level = LogLevel.Warning,
        Message = "Referencia de verificación que no corresponde al usuario {UsuarioId}")]
    public static partial void ReferenciaDeVerificacionNoCoincide(this ILogger registro, UsuarioId usuarioId);
}
