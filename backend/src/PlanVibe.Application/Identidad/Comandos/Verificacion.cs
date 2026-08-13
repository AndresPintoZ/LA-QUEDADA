using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Application.Identidad.Comandos;

/// <summary>Arranca la verificación de organizador (RF-20).</summary>
public sealed record IniciarVerificacion : IComando<SesionDeVerificacion>;

/// <summary>
/// Cierra la verificación consultando el veredicto al proveedor (RF-21).
/// </summary>
/// <remarks>
/// No recibe el resultado como parámetro: recibe solo la referencia y va a preguntárselo al
/// proveedor. Aceptar un «verificado: sí» que llega desde el navegador permitiría a cualquiera
/// concederse el rol de organizador falsificando la llamada de vuelta.
/// </remarks>
public sealed record CompletarVerificacion(string ReferenciaExterna) : IComando<string>;

public sealed class IniciarVerificacionManejador(
    IRepositorioDeUsuarios usuarios,
    IProveedorDeVerificacion proveedor,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    IRegistroDeAuditoria auditoria,
    TimeProvider reloj) : IManejadorDeComando<IniciarVerificacion, SesionDeVerificacion>
{
    public async Task<SesionDeVerificacion> ManejarAsync(IniciarVerificacion comando, CancellationToken cancelacion)
    {
        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId.ToString());

        var sesion = await proveedor.IniciarAsync(usuarioId, cancelacion);

        usuario.IniciarVerificacion(proveedor.Nombre, sesion.ReferenciaExterna, reloj.GetUtcNow());

        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await auditoria.RegistrarAsync(
            usuarioId,
            "verificacion.iniciada",
            "Usuario",
            usuarioId.ToString(),
            new Dictionary<string, string> { ["proveedor"] = proveedor.Nombre },
            cancelacion);

        return sesion;
    }
}

public sealed class CompletarVerificacionManejador(
    IRepositorioDeUsuarios usuarios,
    IProveedorDeVerificacion proveedor,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    IRegistroDeAuditoria auditoria,
    TimeProvider reloj,
    ILogger<CompletarVerificacionManejador> registro) : IManejadorDeComando<CompletarVerificacion, string>
{
    public async Task<string> ManejarAsync(CompletarVerificacion comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId.ToString());

        // La referencia enviada debe coincidir con la que se guardó al iniciar el proceso.
        // Sin esta comprobación, alguien podría reclamar el resultado de la verificación de otra persona.
        if (!string.Equals(usuario.Verificacion.ReferenciaExterna, comando.ReferenciaExterna, StringComparison.Ordinal))
        {
            registro.ReferenciaDeVerificacionNoCoincide(usuarioId);
            throw new AccesoDenegadoException("Esta verificación no se corresponde con tu cuenta.");
        }

        var resultado = await proveedor.ConsultarResultadoAsync(comando.ReferenciaExterna, cancelacion);
        var ahora = reloj.GetUtcNow();

        switch (resultado.Estado)
        {
            case EstadoVerificacion.Verificada:
                usuario.ConfirmarVerificacion(resultado.MayoriaDeEdadConfirmada, ahora);
                break;

            case EstadoVerificacion.Rechazada:
                usuario.RechazarVerificacion(resultado.Observacion ?? "El proveedor no pudo confirmar la identidad.", ahora);
                break;

            case EstadoVerificacion.Pendiente:
                // El proveedor todavía no ha resuelto: se deja como está y se le dice a la persona que espere.
                return EstadoVerificacion.Pendiente.ToString();

            default:
                throw new ConflictoException("verificacion.estado_inesperado", "No hemos podido completar la verificación. Inténtalo de nuevo.");
        }

        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await auditoria.RegistrarAsync(
            usuarioId,
            "verificacion.resuelta",
            "Usuario",
            usuarioId.ToString(),
            new Dictionary<string, string>
            {
                ["estado"] = resultado.Estado.ToString(),
                ["proveedor"] = proveedor.Nombre,
                ["referencia"] = comando.ReferenciaExterna,
            },
            cancelacion);

        return usuario.Verificacion.Estado.ToString();
    }
}
