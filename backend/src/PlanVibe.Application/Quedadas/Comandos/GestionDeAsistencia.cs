using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Quedadas;

namespace PlanVibe.Application.Quedadas.Comandos;

/// <summary>Apuntarse a un plan (RF-14). Si está completo, se entra en lista de espera (RF-15).</summary>
public sealed record ApuntarseAQuedada(Guid QuedadaId) : IComando<ResultadoDeInscripcion>;

/// <param name="Confirmada">Cierto si se ha obtenido plaza.</param>
/// <param name="PosicionEnListaDeEspera">Puesto en la cola, o <c>null</c> si hay plaza.</param>
public sealed record ResultadoDeInscripcion(bool Confirmada, int? PosicionEnListaDeEspera);

/// <summary>Retirarse de un plan al que se estaba apuntado (RF-14).</summary>
public sealed record AbandonarQuedada(Guid QuedadaId) : IComando<bool>;

/// <summary>Cancelar un plan propio (RF-11).</summary>
public sealed record CancelarQuedada(Guid QuedadaId, string Motivo) : IComando<bool>;

/// <summary>
/// Inscripción en un plan.
/// </summary>
/// <remarks>
/// <para>
/// El agregado se carga entero y él decide si hay plaza. Un <c>SELECT COUNT</c> seguido de un
/// <c>INSERT</c> desde el manejador sería vulnerable a una condición de carrera: dos peticiones
/// simultáneas leerían «queda 1 plaza» y ambas entrarían.
/// </para>
/// <para>
/// La segunda barrera es la marca de concurrencia optimista del agregado: si dos transacciones
/// modifican la misma quedada a la vez, la segunda falla al guardar y se traduce en un 409.
/// </para>
/// </remarks>
public sealed class ApuntarseAQuedadaManejador(
    IRepositorioDeQuedadas quedadas,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    TimeProvider reloj,
    ILogger<ApuntarseAQuedadaManejador> registro) : IManejadorDeComando<ApuntarseAQuedada, ResultadoDeInscripcion>
{
    public async Task<ResultadoDeInscripcion> ManejarAsync(ApuntarseAQuedada comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión para apuntarte a un plan.");

        var id = new QuedadaId(comando.QuedadaId);

        var quedada = await quedadas.ObtenerPorIdAsync(id, cancelacion)
            ?? throw new NoEncontradoException("el plan", comando.QuedadaId.ToString());

        var resultado = quedada.Apuntar(usuarioId, reloj.GetUtcNow());

        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        registro.UsuarioApuntado(usuarioId, id, resultado.Confirmada);

        return new ResultadoDeInscripcion(resultado.Confirmada, resultado.PosicionEnListaDeEspera);
    }
}

/// <summary>Retirada de un plan. El agregado se encarga de promover a quien esperaba.</summary>
public sealed class AbandonarQuedadaManejador(
    IRepositorioDeQuedadas quedadas,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    TimeProvider reloj) : IManejadorDeComando<AbandonarQuedada, bool>
{
    public async Task<bool> ManejarAsync(AbandonarQuedada comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        var quedada = await quedadas.ObtenerPorIdAsync(new QuedadaId(comando.QuedadaId), cancelacion)
            ?? throw new NoEncontradoException("el plan", comando.QuedadaId.ToString());

        quedada.Abandonar(usuarioId, reloj.GetUtcNow());
        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        return true;
    }
}

/// <summary>
/// Cancelación de un plan por su organizador. Solo él puede hacerlo, y esa comprobación
/// la hace el agregado, no este manejador.
/// </summary>
public sealed class CancelarQuedadaManejador(
    IRepositorioDeQuedadas quedadas,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    IRegistroDeAuditoria auditoria,
    TimeProvider reloj) : IManejadorDeComando<CancelarQuedada, bool>
{
    public async Task<bool> ManejarAsync(CancelarQuedada comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        var quedada = await quedadas.ObtenerPorIdAsync(new QuedadaId(comando.QuedadaId), cancelacion)
            ?? throw new NoEncontradoException("el plan", comando.QuedadaId.ToString());

        quedada.Cancelar(usuarioId, comando.Motivo, reloj.GetUtcNow());
        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await auditoria.RegistrarAsync(
            usuarioId,
            "quedada.cancelada",
            "Quedada",
            comando.QuedadaId.ToString(),
            metadatos: null,
            cancelacion);

        return true;
    }
}
