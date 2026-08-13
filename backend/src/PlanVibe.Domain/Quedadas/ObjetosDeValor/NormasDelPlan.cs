using System.Collections;
using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Condiciones que el organizador fija para su plan: «casco obligatorio», «+16 años», «nivel medio».
/// </summary>
/// <remarks>
/// Se limitan a frases cortas y a un número pequeño para que sigan siendo legibles de un vistazo
/// en el móvil y no se conviertan en un segundo campo de descripción.
/// Las normas de la comunidad, que son de la plataforma y no del organizador, viven aparte.
/// </remarks>
public sealed class NormasDelPlan : IReadOnlyCollection<string>, IEquatable<NormasDelPlan>
{
    public const int MaximoNormas = 8;
    public const int LongitudMaximaNorma = 60;

    private readonly string[] _normas;

    public NormasDelPlan(IEnumerable<string> normas)
    {
        ArgumentNullException.ThrowIfNull(normas);

        var limpias = normas
            .Select(n => n?.Trim() ?? string.Empty)
            .Where(n => n.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        ExcepcionDeDominio.SiNo(
            limpias.Length <= MaximoNormas,
            "normas.demasiadas",
            $"Se pueden indicar como máximo {MaximoNormas} normas.");

        ExcepcionDeDominio.SiNo(
            limpias.All(n => n.Length <= LongitudMaximaNorma),
            "normas.norma_demasiado_larga",
            $"Cada norma debe caber en {LongitudMaximaNorma} caracteres.");

        _normas = limpias;
    }

    public static NormasDelPlan Ninguna { get; } = new([]);

    public int Count => _normas.Length;

    public IEnumerator<string> GetEnumerator() => ((IEnumerable<string>)_normas).GetEnumerator();

    IEnumerator IEnumerable.GetEnumerator() => _normas.GetEnumerator();

    public bool Equals(NormasDelPlan? otra) => otra is not null && _normas.SequenceEqual(otra._normas, StringComparer.Ordinal);

    public override bool Equals(object? obj) => Equals(obj as NormasDelPlan);

    public override int GetHashCode()
    {
        var hash = default(HashCode);
        foreach (var norma in _normas)
        {
            hash.Add(norma, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
