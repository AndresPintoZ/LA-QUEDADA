using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Número máximo de personas que caben en una quedada, contando al organizador.
/// </summary>
/// <remarks>
/// El mínimo es 2 porque una quedada de una sola persona no es un encuentro.
/// El máximo de 500 es un límite operativo del piloto: por encima de esa cifra el encuentro
/// deja de ser gestionable por una persona particular y entra en terreno de evento organizado,
/// con obligaciones distintas.
/// </remarks>
public readonly record struct Capacidad
{
    public const int Minima = 2;
    public const int Maxima = 500;

    public Capacidad(int maximo)
    {
        ExcepcionDeDominio.SiNo(
            maximo is >= Minima and <= Maxima,
            "capacidad.fuera_de_rango",
            $"La capacidad debe estar entre {Minima} y {Maxima} personas.");

        Maximo = maximo;
    }

    public int Maximo { get; }

    public override string ToString() => Maximo.ToString(System.Globalization.CultureInfo.InvariantCulture);
}
