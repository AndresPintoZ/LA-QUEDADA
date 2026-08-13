using Microsoft.Extensions.Logging;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Infrastructure;

/// <summary>
/// Mensajes de registro de la infraestructura, generados en tiempo de compilación.
/// </summary>
/// <remarks>
/// Igual que en la capa de aplicación: tenerlos juntos permite revisar de una vez qué se escribe
/// en los registros y comprobar que no se cuela ningún dato personal. Aquí solo hay
/// identificadores, nombres de tipo y contadores.
/// </remarks>
public static partial class RegistroDeInfraestructura
{
    [LoggerMessage(
        EventId = 3001,
        Level = LogLevel.Critical,
        Message = "Se ha reutilizado un token de renovación del usuario {UsuarioId}. Se revoca la familia de sesiones completa.")]
    public static partial void ReutilizacionDeTokenDetectada(this ILogger registro, Guid usuarioId);

    [LoggerMessage(
        EventId = 3002,
        Level = LogLevel.Warning,
        Message = "Verificación SIMULADA iniciada para el usuario {UsuarioId}. No comprueba ninguna identidad real.")]
    public static partial void VerificacionSimuladaIniciada(this ILogger registro, UsuarioId usuarioId);

    [LoggerMessage(
        EventId = 3003,
        Level = LogLevel.Warning,
        Message = "La geocodificación ha fallado. Se continúa sin sugerencias de lugar.")]
    public static partial void FalloDeGeocodificacion(this ILogger registro, Exception excepcion);

    [LoggerMessage(
        EventId = 3004,
        Level = LogLevel.Warning,
        Message = "La geocodificación ha superado el tiempo de espera.")]
    public static partial void TiempoAgotadoDeGeocodificacion(this ILogger registro);

    [LoggerMessage(
        EventId = 3005,
        Level = LogLevel.Error,
        Message = "No se ha podido procesar el evento de dominio {NombreDelEvento}. Los datos ya están guardados.")]
    public static partial void FalloAlPublicarEvento(this ILogger registro, string nombreDelEvento, Exception excepcion);

    [LoggerMessage(
        EventId = 3006,
        Level = LogLevel.Debug,
        Message = "Evento de dominio publicado: {NombreDelEvento}")]
    public static partial void EventoDeDominioPublicado(this ILogger registro, string nombreDelEvento);

    [LoggerMessage(
        EventId = 3007,
        Level = LogLevel.Information,
        Message = "Notificación simulada a {NumeroDeDestinatarios} destinatarios. Asunto: {Asunto}")]
    public static partial void NotificacionSimulada(this ILogger registro, int numeroDeDestinatarios, string asunto);

    [LoggerMessage(
        EventId = 3008,
        Level = LogLevel.Information,
        Message = "Migraciones aplicadas y datos iniciales comprobados.")]
    public static partial void BaseDeDatosPreparada(this ILogger registro);
}
