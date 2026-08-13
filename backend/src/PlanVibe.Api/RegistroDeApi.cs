namespace PlanVibe.Api;

/// <summary>Mensajes de registro de la capa HTTP, generados en tiempo de compilación.</summary>
public static partial class RegistroDeApi
{
    [LoggerMessage(
        EventId = 4001,
        Level = LogLevel.Error,
        Message = "Error no controlado atendiendo {Ruta}")]
    public static partial void ErrorNoControlado(this ILogger registro, string ruta, Exception excepcion);

    [LoggerMessage(
        EventId = 4002,
        Level = LogLevel.Warning,
        Message = "Se ha superado el límite de peticiones en {Ruta}")]
    public static partial void LimiteDePeticionesSuperado(this ILogger registro, string ruta);

    [LoggerMessage(
        EventId = 4003,
        Level = LogLevel.Information,
        Message = "PlanVibe API iniciada en el entorno {Entorno}")]
    public static partial void ApiIniciada(this ILogger registro, string entorno);
}
