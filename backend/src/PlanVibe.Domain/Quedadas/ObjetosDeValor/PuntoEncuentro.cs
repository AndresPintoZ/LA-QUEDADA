using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Dónde se queda el grupo. Distingue dos niveles de detalle a propósito:
/// <list type="bullet">
///   <item><description><see cref="Lugar"/> y <see cref="Referencia"/> son públicos y se muestran a cualquiera.</description></item>
///   <item><description><see cref="DireccionExacta"/> solo se revela a quien tiene plaza confirmada.</description></item>
/// </list>
/// Es la aplicación del principio de no exponer ubicaciones concretas descrito en
/// <c>docs/03-diseno-visual.md</c>.
/// </summary>
/// <remarks>
/// La latitud y la longitud se guardan como dos valores primitivos en lugar de anidar un
/// <see cref="ObjetosDeValor.Coordenadas"/>: Entity Framework no sabe materializar un tipo complejo
/// anidado dentro de otro cuando ambos tienen constructor con parámetros. La expresividad no se
/// pierde, porque <see cref="Coordenadas"/> sigue disponible como propiedad calculada y
/// <see cref="Crear"/> permite construirlo pasando el objeto de valor completo.
/// </remarks>
/// <param name="Lugar">Nombre público del sitio, p. ej. «Puente Adaja».</param>
/// <param name="Referencia">Pista visible para encontrarse, p. ej. «junto al quiosco».</param>
/// <param name="DireccionExacta">Dirección postal, opcional y de visibilidad restringida.</param>
/// <param name="EsLugarPublico">
/// Declaración del organizador de que el punto es un espacio público. Las normas de publicación
/// prohíben quedar en domicilios particulares (<c>docs/04-seguridad-privacidad-moderacion.md</c>);
/// el sistema no puede verificarlo automáticamente, así que exige la declaración y la deja auditable.
/// </param>
public sealed record PuntoEncuentro(
    string Lugar,
    string? Referencia,
    string? DireccionExacta,
    double Latitud,
    double Longitud,
    bool EsLugarPublico)
{
    public const int LongitudMaximaLugar = 120;
    public const int LongitudMaximaReferencia = 200;
    public const int LongitudMaximaDireccion = 200;

    public string Lugar { get; } = ValidarLugar(Lugar);

    public string? Referencia { get; } = Recortar(Referencia, LongitudMaximaReferencia, "punto_encuentro.referencia_demasiado_larga");

    public string? DireccionExacta { get; } = Recortar(DireccionExacta, LongitudMaximaDireccion, "punto_encuentro.direccion_demasiado_larga");

    public double Latitud { get; } = new Coordenadas(Latitud, Longitud).Latitud;

    public double Longitud { get; } = new Coordenadas(Latitud, Longitud).Longitud;

    public bool EsLugarPublico { get; } = ValidarEsPublico(EsLugarPublico);

    /// <summary>Punto en el mapa, reconstruido a partir de la latitud y la longitud almacenadas.</summary>
    public Coordenadas Coordenadas => new(Latitud, Longitud);

    /// <summary>Construye el punto de encuentro a partir del objeto de valor de coordenadas.</summary>
    public static PuntoEncuentro Crear(
        string lugar,
        string? referencia,
        string? direccionExacta,
        Coordenadas coordenadas,
        bool esLugarPublico) =>
        new(lugar, referencia, direccionExacta, coordenadas.Latitud, coordenadas.Longitud, esLugarPublico);

    private static string ValidarLugar(string lugar)
    {
        var limpio = lugar?.Trim() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            limpio.Length is >= 3 and <= LongitudMaximaLugar,
            "punto_encuentro.lugar_invalido",
            $"El lugar debe tener entre 3 y {LongitudMaximaLugar} caracteres.");

        return limpio;
    }

    private static bool ValidarEsPublico(bool esLugarPublico)
    {
        ExcepcionDeDominio.SiNo(
            esLugarPublico,
            "punto_encuentro.no_es_lugar_publico",
            "El punto de encuentro debe ser un lugar público. No se permiten domicilios particulares.");

        return esLugarPublico;
    }

    private static string? Recortar(string? valor, int longitudMaxima, string codigoDeError)
    {
        if (string.IsNullOrWhiteSpace(valor))
        {
            return null;
        }

        var limpio = valor.Trim();

        ExcepcionDeDominio.SiNo(
            limpio.Length <= longitudMaxima,
            codigoDeError,
            $"El texto no puede superar {longitudMaxima} caracteres.");

        return limpio;
    }
}
