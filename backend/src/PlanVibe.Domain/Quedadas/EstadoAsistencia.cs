namespace PlanVibe.Domain.Quedadas;

/// <summary>Situación de una persona respecto a una quedada concreta.</summary>
public enum EstadoAsistencia
{
    /// <summary>Tiene plaza y ve la dirección exacta del punto de encuentro.</summary>
    Confirmada = 1,

    /// <summary>Esperando a que se libere una plaza (RF-15).</summary>
    EnListaDeEspera = 2,

    /// <summary>Se retiró. La fila se conserva para poder auditar y para permitir volver a apuntarse.</summary>
    Retirada = 3,
}
