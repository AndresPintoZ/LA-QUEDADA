using System.ComponentModel.DataAnnotations;

namespace PlanVibe.Infrastructure.Identidad;

/// <summary>
/// Configuración de la emisión de tokens.
/// </summary>
/// <remarks>
/// <see cref="Clave"/> nunca debe estar en <c>appsettings.json</c> ni en el repositorio: llega por
/// variable de entorno o por el gestor de secretos. La validación con atributos hace que la
/// aplicación se niegue a arrancar si falta o es demasiado corta, en lugar de funcionar con una
/// firma débil sin que nadie se entere.
/// </remarks>
public sealed class OpcionesDeJwt
{
    public const string Seccion = "Jwt";

    /// <summary>
    /// Longitud mínima de la clave de firma en caracteres.
    /// </summary>
    /// <remarks>
    /// HMAC-SHA256 trabaja con bloques de 256 bits; una clave más corta reduce la seguridad real
    /// de la firma por debajo de lo que anuncia el algoritmo.
    /// </remarks>
    public const int LongitudMinimaDeClave = 64;

    [Required(ErrorMessage = "Falta la clave de firma de los tokens (Jwt__Clave).")]
    [MinLength(LongitudMinimaDeClave, ErrorMessage = "La clave de firma debe tener al menos 64 caracteres.")]
    public string Clave { get; init; } = string.Empty;

    [Required]
    public string Emisor { get; init; } = "planvibe-api";

    [Required]
    public string Audiencia { get; init; } = "planvibe-web";

    /// <summary>
    /// Vida del token de acceso.
    /// </summary>
    /// <remarks>
    /// Corta a propósito: un token de acceso no se puede revocar una vez emitido, así que su
    /// caducidad es la única forma de limitar el daño si se filtra. La sesión no se corta porque
    /// el token de renovación la mantiene viva de forma transparente.
    /// </remarks>
    [Range(5, 60)]
    public int MinutosDeAcceso { get; init; } = 15;

    /// <summary>Vida del token de renovación, que sí se puede revocar.</summary>
    [Range(1, 90)]
    public int DiasDeRenovacion { get; init; } = 14;
}
