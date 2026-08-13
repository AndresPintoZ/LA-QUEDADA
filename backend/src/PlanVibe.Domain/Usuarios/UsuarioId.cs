namespace PlanVibe.Domain.Usuarios;

/// <summary>
/// Identificador de usuario fuertemente tipado. Impide pasar por error un identificador
/// de quedada donde se espera uno de usuario: el compilador lo rechaza.
/// </summary>
public readonly record struct UsuarioId(Guid Valor)
{
    /// <summary>
    /// Genera un identificador de versión 7: contiene marca de tiempo, así que los valores
    /// consecutivos quedan ordenados y no fragmentan el índice de PostgreSQL como un GUID aleatorio.
    /// </summary>
    public static UsuarioId Nuevo() => new(Guid.CreateVersion7());

    public override string ToString() => Valor.ToString();
}
