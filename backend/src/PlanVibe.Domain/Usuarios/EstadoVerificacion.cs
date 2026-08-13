namespace PlanVibe.Domain.Usuarios;

/// <summary>Situación del proceso de verificación de identidad de un organizador.</summary>
public enum EstadoVerificacion
{
    /// <summary>Nunca se ha solicitado.</summary>
    NoIniciada = 0,

    /// <summary>Enviada al proveedor y esperando su respuesta.</summary>
    Pendiente = 1,

    /// <summary>El proveedor confirmó la identidad.</summary>
    Verificada = 2,

    /// <summary>El proveedor no pudo confirmarla. Se puede reintentar.</summary>
    Rechazada = 3,

    /// <summary>Se retiró tras una revisión de seguridad o moderación (RF-23).</summary>
    Revocada = 4,
}
