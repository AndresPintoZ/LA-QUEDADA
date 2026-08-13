using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Usuarios.Eventos;

/// <summary>Alta de una cuenta nueva. Dispara el correo de bienvenida y la entrada de auditoría.</summary>
public sealed record UsuarioRegistrado(UsuarioId UsuarioId, DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>Cambio de estado en la verificación de identidad. Se audita siempre (RNF-04).</summary>
public sealed record VerificacionActualizada(
    UsuarioId UsuarioId,
    EstadoVerificacion Estado,
    string? Proveedor,
    string? ReferenciaExterna,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>Una cuenta ha sido suspendida por moderación (RF-18).</summary>
public sealed record CuentaSuspendida(UsuarioId UsuarioId, string Motivo, DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>
/// La persona ha ejercido su derecho de supresión (RF-03). Quien escuche este evento debe
/// anonimizar también lo que guarde fuera del agregado: comentarios, reportes y notificaciones.
/// </summary>
public sealed record CuentaEliminada(UsuarioId UsuarioId, DateTimeOffset OcurridoEn) : IEventoDeDominio;
