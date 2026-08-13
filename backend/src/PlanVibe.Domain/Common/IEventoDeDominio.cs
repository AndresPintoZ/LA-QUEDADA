namespace PlanVibe.Domain.Common;

/// <summary>
/// Hecho relevante que ya ha ocurrido dentro del dominio. Se nombra siempre en pasado
/// (QuedadaCancelada, UsuarioApuntado) porque describe algo consumado, no una orden.
/// </summary>
public interface IEventoDeDominio
{
    /// <summary>Momento en que ocurrió el hecho, en UTC.</summary>
    public DateTimeOffset OcurridoEn { get; }
}
