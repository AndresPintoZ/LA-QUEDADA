namespace PlanVibe.Domain.Common;

/// <summary>
/// Entidad del dominio: su igualdad se decide por identidad, no por sus atributos.
/// Dos quedadas con el mismo título y fecha siguen siendo quedadas distintas.
/// </summary>
/// <typeparam name="TId">Tipo del identificador, normalmente un identificador fuertemente tipado.</typeparam>
public abstract class Entidad<TId> : IEquatable<Entidad<TId>>
    where TId : struct
{
    protected Entidad(TId id) => Id = id;

    /// <summary>Constructor sin parámetros que necesita EF Core para materializar entidades.</summary>
    protected Entidad()
    {
    }

    public TId Id { get; protected init; }

    public bool Equals(Entidad<TId>? otra)
    {
        if (otra is null)
        {
            return false;
        }

        if (ReferenceEquals(this, otra))
        {
            return true;
        }

        // Comparar tipos evita que dos entidades distintas con el mismo Guid se consideren iguales.
        return GetType() == otra.GetType() && EqualityComparer<TId>.Default.Equals(Id, otra.Id);
    }

    public override bool Equals(object? obj) => Equals(obj as Entidad<TId>);

    public override int GetHashCode() => HashCode.Combine(GetType(), Id);

    public static bool operator ==(Entidad<TId>? izquierda, Entidad<TId>? derecha) =>
        izquierda?.Equals(derecha) ?? derecha is null;

    public static bool operator !=(Entidad<TId>? izquierda, Entidad<TId>? derecha) => !(izquierda == derecha);
}
