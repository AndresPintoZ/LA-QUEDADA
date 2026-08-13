using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Cuándo ocurre la quedada: instante de inicio y duración prevista.
/// </summary>
/// <remarks>
/// El inicio se guarda como <see cref="DateTimeOffset"/> en UTC. La conversión a hora local
/// («sábado a las 19:00») es responsabilidad de la interfaz, no del dominio: guardar horas locales
/// provoca errores en los cambios de horario de verano.
/// </remarks>
public readonly record struct FranjaTemporal
{
    public static readonly TimeSpan DuracionMinima = TimeSpan.FromMinutes(15);
    public static readonly TimeSpan DuracionMaxima = TimeSpan.FromHours(24);

    public FranjaTemporal(DateTimeOffset inicio, TimeSpan duracion)
    {
        ExcepcionDeDominio.SiNo(
            duracion >= DuracionMinima && duracion <= DuracionMaxima,
            "franja.duracion_invalida",
            $"La duración debe estar entre {DuracionMinima.TotalMinutes:0} minutos y {DuracionMaxima.TotalHours:0} horas.");

        Inicio = inicio.ToUniversalTime();
        Duracion = duracion;
    }

    public DateTimeOffset Inicio { get; }

    public TimeSpan Duracion { get; }

    public DateTimeOffset Fin => Inicio + Duracion;

    public bool YaComenzoEn(DateTimeOffset instante) => instante >= Inicio;

    public bool YaTerminoEn(DateTimeOffset instante) => instante >= Fin;
}
