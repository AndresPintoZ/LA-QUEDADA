namespace PlanVibe.Domain.Quedadas;

/// <summary>Identificador de quedada fuertemente tipado.</summary>
public readonly record struct QuedadaId(Guid Valor)
{
    public static QuedadaId Nuevo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
