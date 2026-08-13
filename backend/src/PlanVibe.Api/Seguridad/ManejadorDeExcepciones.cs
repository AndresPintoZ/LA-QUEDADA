using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Common;

namespace PlanVibe.Api.Seguridad;

/// <summary>
/// Convierte las excepciones conocidas en respuestas <c>ProblemDetails</c> y oculta el resto.
/// </summary>
/// <remarks>
/// <para>
/// La regla que gobierna esta clase: <strong>una excepción inesperada nunca sale al cliente</strong>.
/// Un mensaje de error de base de datos revela nombres de tablas, y una traza de pila revela rutas
/// del servidor, versiones de bibliotecas y estructura interna. Todo eso es material de trabajo
/// para quien esté buscando por dónde entrar.
/// </para>
/// <para>
/// Las excepciones que sí se traducen son las que el propio código lanza a propósito y cuyo
/// mensaje está redactado para leerse: reglas de dominio, validaciones y permisos.
/// </para>
/// </remarks>
public sealed class ManejadorDeExcepciones(
    IProblemDetailsService servicioDeProblemas,
    ILogger<ManejadorDeExcepciones> registro) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        // Se renombran a nombres del dominio del problema para que el resto del método se lea bien.
        var contexto = httpContext;
        var excepcion = exception;

        var (estado, titulo, detalle, extensiones) = Traducir(excepcion);

        if (estado == StatusCodes.Status500InternalServerError)
        {
            // Solo se registra la traza completa en el servidor. Al cliente le llega un mensaje neutro.
            registro.ErrorNoControlado(contexto.Request.Path, excepcion);
        }

        contexto.Response.StatusCode = estado;

        var problema = new ProblemDetails
        {
            Status = estado,
            Title = titulo,
            Detail = detalle,
            Instance = contexto.Request.Path,
        };

        foreach (var (clave, valor) in extensiones)
        {
            problema.Extensions[clave] = valor;
        }

        // El identificador de traza permite que alguien informe de un fallo y se pueda localizar
        // en los registros sin necesidad de exponerle ningún detalle técnico.
        problema.Extensions["traceId"] = contexto.TraceIdentifier;

        return await servicioDeProblemas.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = contexto,
            ProblemDetails = problema,
            Exception = excepcion,
        });
    }

    private static (int Estado, string Titulo, string Detalle, Dictionary<string, object?> Extensiones) Traducir(Exception excepcion) =>
        excepcion switch
        {
            ValidacionException validacion => (
                StatusCodes.Status400BadRequest,
                "Datos no válidos",
                validacion.Message,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["errors"] = validacion.Errores }),

            ExcepcionDeDominio dominio => (
                StatusCodes.Status422UnprocessableEntity,
                "No se puede realizar la operación",
                dominio.Message,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["codigo"] = dominio.Codigo }),

            AccesoDenegadoException acceso => (
                StatusCodes.Status403Forbidden,
                "Acceso denegado",
                acceso.Message,
                []),

            // Se responde 404 tanto si no existe como si existe pero no se puede ver. Distinguirlos
            // permitiría averiguar qué identificadores son válidos probándolos uno a uno.
            NoEncontradoException => (
                StatusCodes.Status404NotFound,
                "No encontrado",
                "No hemos encontrado lo que buscabas.",
                []),

            ConflictoException conflicto => (
                StatusCodes.Status409Conflict,
                "Conflicto",
                conflicto.Message,
                new Dictionary<string, object?>(StringComparer.Ordinal) { ["codigo"] = conflicto.Codigo }),

            // Cuerpo mal formado, con codificación inválida o que no encaja con el tipo esperado.
            // Es un error del cliente, así que 400: devolver 500 daría a entender que el fallo es
            // nuestro y, en la práctica, llenaría las alertas de errores que no podemos arreglar.
            //
            // El detalle no se propaga: describe la estructura interna que se esperaba recibir.
            BadHttpRequestException => (
                StatusCodes.Status400BadRequest,
                "Petición mal formada",
                "No hemos podido leer los datos enviados. Comprueba que el cuerpo es JSON válido en UTF-8.",
                []),

            OperationCanceledException => (
                CodigosDeEstadoAdicionales.ClienteCerroLaPeticion,
                "Petición cancelada",
                "La petición se canceló antes de completarse.",
                []),

            // Cualquier otra cosa es un fallo nuestro: mensaje genérico y detalle solo en el registro.
            _ => (
                StatusCodes.Status500InternalServerError,
                "Error interno",
                "Ha ocurrido un error inesperado. Ya estamos al tanto.",
                []),
        };
}

/// <summary>Códigos de estado que no están en <see cref="StatusCodes"/>.</summary>
internal static class CodigosDeEstadoAdicionales
{
    /// <summary>El cliente cerró la conexión antes de recibir respuesta (convención de nginx).</summary>
    public const int ClienteCerroLaPeticion = 499;
}
