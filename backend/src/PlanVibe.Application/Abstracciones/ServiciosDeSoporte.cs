using PlanVibe.Domain.Common;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Application.Abstracciones;

/// <summary>
/// Quién está haciendo la petición. Lo rellena la capa HTTP a partir del token ya validado.
/// </summary>
/// <remarks>
/// Los casos de uso nunca leen el token ni las cabeceras: reciben esta abstracción. Así se pueden
/// probar sin montar un servidor y no dependen de que la autenticación sea con JWT.
/// </remarks>
public interface IContextoDeUsuarioActual
{
    /// <summary>Identificador de quien hace la petición, o <c>null</c> si es anónima.</summary>
    public UsuarioId? UsuarioId { get; }

    public bool EstaAutenticado { get; }

    public bool TieneRol(Rol rol);

    /// <summary>Dirección IP de origen, usada solo para la auditoría de acciones sensibles.</summary>
    public string? DireccionIp { get; }
}

/// <summary>
/// Puerta al proveedor externo de verificación de identidad.
/// </summary>
/// <remarks>
/// La interfaz está diseñada para que sea <em>imposible</em> que un documento entre en el sistema:
/// no hay ningún parámetro ni ningún resultado que admita una imagen o un número de documento.
/// PlanVibe solo genera una sesión, redirige a la persona al proveedor y recibe el veredicto.
/// Ver <c>docs/04-seguridad-privacidad-moderacion.md</c>.
/// </remarks>
public interface IProveedorDeVerificacion
{
    /// <summary>Nombre del proveedor tal como se guarda en la trazabilidad.</summary>
    public string Nombre { get; }

    /// <summary>Abre una sesión de verificación y devuelve a dónde hay que enviar a la persona.</summary>
    public Task<SesionDeVerificacion> IniciarAsync(UsuarioId usuarioId, CancellationToken cancelacion);

    /// <summary>
    /// Consulta el veredicto de una sesión. Se usa al volver de la pasarela y también al recibir
    /// la notificación del proveedor: nunca se da por buena la respuesta que trae el navegador.
    /// </summary>
    public Task<ResultadoDeVerificacion> ConsultarResultadoAsync(string referenciaExterna, CancellationToken cancelacion);
}

/// <param name="ReferenciaExterna">Identificador de la sesión en el proveedor.</param>
/// <param name="UrlDeRedireccion">Dónde se envía a la persona para completar el proceso.</param>
public sealed record SesionDeVerificacion(string ReferenciaExterna, string UrlDeRedireccion);

/// <param name="Estado">Veredicto del proveedor.</param>
/// <param name="MayoriaDeEdadConfirmada">Si confirmó 18 años cumplidos. Nunca la fecha de nacimiento.</param>
/// <param name="Observacion">Motivo legible en caso de rechazo.</param>
public sealed record ResultadoDeVerificacion(EstadoVerificacion Estado, bool MayoriaDeEdadConfirmada, string? Observacion);

/// <summary>Traduce una dirección escrita a coordenadas y al revés.</summary>
public interface IServicioDeGeocodificacion
{
    public Task<IReadOnlyList<LugarGeocodificado>> BuscarAsync(string texto, CancellationToken cancelacion);
}

/// <param name="NombreCompleto">Descripción del sitio devuelta por el proveedor de mapas.</param>
/// <param name="Coordenadas">Punto en el mapa.</param>
public sealed record LugarGeocodificado(string NombreCompleto, Coordenadas Coordenadas);

/// <summary>
/// Publica los eventos de dominio una vez confirmada la transacción.
/// </summary>
public interface IPublicadorDeEventos
{
    public Task PublicarAsync(IEnumerable<IEventoDeDominio> eventos, CancellationToken cancelacion);
}

/// <summary>
/// Registro de auditoría exigido por RNF-04: verificaciones, publicaciones, reportes y
/// decisiones de moderación.
/// </summary>
/// <remarks>
/// Guarda quién, qué, sobre qué y cuándo. Los metadatos deben ser mínimos y no deben contener
/// datos personales más allá del identificador del actor: la auditoría es una traza de acciones,
/// no una copia de seguridad del contenido.
/// </remarks>
public interface IRegistroDeAuditoria
{
    public Task RegistrarAsync(
        UsuarioId? actorId,
        string accion,
        string tipoDeObjeto,
        string objetoId,
        IReadOnlyDictionary<string, string>? metadatos,
        CancellationToken cancelacion);
}

/// <summary>Envío de avisos a las personas usuarias (RF-19).</summary>
public interface IServicioDeNotificaciones
{
    public Task NotificarAsync(IEnumerable<UsuarioId> destinatarios, string asunto, string cuerpo, CancellationToken cancelacion);
}
