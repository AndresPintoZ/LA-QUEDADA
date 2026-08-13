namespace PlanVibe.Domain.Quedadas;

/// <summary>Ciclo de vida de una quedada.</summary>
public enum EstadoQuedada
{
    /// <summary>Visible en explorar y admitiendo inscripciones.</summary>
    Publicada = 1,

    /// <summary>El organizador la canceló. Se mantiene visible para quien estaba apuntado (RF-11).</summary>
    Cancelada = 2,

    /// <summary>Ya ha terminado. Sigue consultable pero no admite cambios.</summary>
    Finalizada = 3,

    /// <summary>Retirada de la vista pública por moderación (RF-18). No se borra: la auditoría la necesita.</summary>
    OcultaPorModeracion = 4,
}
