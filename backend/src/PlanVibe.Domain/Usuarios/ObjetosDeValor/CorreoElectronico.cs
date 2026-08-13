using System.Text.RegularExpressions;
using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Usuarios.ObjetosDeValor;

/// <summary>
/// Dirección de correo normalizada. Se guarda siempre en minúsculas y sin espacios para que
/// «Lucia@Example.com» y «lucia@example.com» no puedan convertirse en dos cuentas distintas.
/// </summary>
public sealed partial record CorreoElectronico
{
    public const int LongitudMaxima = 254;  // límite del RFC 5321 para la dirección completa

    public CorreoElectronico(string valor)
    {
        var limpio = valor?.Trim().ToLowerInvariant() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            limpio.Length is > 0 and <= LongitudMaxima && PatronDeCorreo().IsMatch(limpio),
            "correo.formato_invalido",
            "El correo electrónico no tiene un formato válido.");

        Valor = limpio;
    }

    public string Valor { get; }

    /// <summary>
    /// Comprobación deliberadamente sencilla: solo descarta lo imposible. Validar el correo
    /// con una expresión regular exhaustiva es un problema conocido sin solución práctica;
    /// la prueba real de que una dirección existe es el mensaje de confirmación que se le envía.
    /// </summary>
    [GeneratedRegex(@"^[^@\s]+@[^@\s.]+(\.[^@\s.]+)+$", RegexOptions.CultureInvariant, matchTimeoutMilliseconds: 200)]
    private static partial Regex PatronDeCorreo();

    public override string ToString() => Valor;
}
