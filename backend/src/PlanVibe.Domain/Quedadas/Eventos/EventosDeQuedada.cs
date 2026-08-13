using PlanVibe.Domain.Common;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Domain.Quedadas.Eventos;

/// <summary>Se ha publicado una quedada nueva. Dispara la entrada en el registro de auditoría (RNF-04).</summary>
public sealed record QuedadaPublicada(
    QuedadaId QuedadaId,
    UsuarioId OrganizadorId,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>Alguien ha obtenido plaza o ha entrado en la lista de espera.</summary>
public sealed record UsuarioApuntado(
    QuedadaId QuedadaId,
    UsuarioId UsuarioId,
    bool Confirmada,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>Alguien ha dejado la quedada, liberando su plaza si la tenía confirmada.</summary>
public sealed record UsuarioRetirado(
    QuedadaId QuedadaId,
    UsuarioId UsuarioId,
    bool TeniaPlazaConfirmada,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>
/// Alguien de la lista de espera ha pasado a tener plaza. Merece aviso inmediato:
/// la persona dejó de contar con ir y ahora sí puede (RF-19).
/// </summary>
public sealed record AsistentePromovido(
    QuedadaId QuedadaId,
    UsuarioId UsuarioId,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>
/// El organizador ha cancelado. <see cref="UsuariosAAvisar"/> ya viene resuelto por el agregado
/// para que quien envía las notificaciones no tenga que volver a consultar la base de datos
/// ni decidir a quién avisar (RF-11).
/// </summary>
public sealed record QuedadaCancelada(
    QuedadaId QuedadaId,
    UsuarioId CanceladaPor,
    string Motivo,
    IReadOnlyCollection<UsuarioId> UsuariosAAvisar,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;

/// <summary>Han cambiado datos que afectan a quien ya se apuntó (fecha, lugar o capacidad).</summary>
public sealed record QuedadaModificada(
    QuedadaId QuedadaId,
    UsuarioId ModificadaPor,
    IReadOnlyCollection<string> CamposModificados,
    IReadOnlyCollection<UsuarioId> UsuariosAAvisar,
    DateTimeOffset OcurridoEn) : IEventoDeDominio;
