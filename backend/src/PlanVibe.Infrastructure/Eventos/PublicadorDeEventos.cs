using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Common;
using PlanVibe.Domain.Quedadas.Eventos;

namespace PlanVibe.Infrastructure.Eventos;

/// <summary>
/// Publica los eventos de dominio ya confirmados.
/// </summary>
/// <remarks>
/// <para>
/// En el MVP se limita a registrarlos y a disparar las notificaciones que exige RF-19. Es
/// deliberadamente sencillo: no hay cola de mensajes ni proceso aparte porque, con el volumen del
/// piloto, montarlos añadiría operación sin resolver ningún problema real.
/// </para>
/// <para>
/// Un fallo aquí no revierte la operación: los datos ya están guardados y cancelarla a estas
/// alturas sería peor. Se registra el error para poder reaccionar.
/// </para>
/// </remarks>
public sealed class PublicadorDeEventos(
    IServicioDeNotificaciones notificaciones,
    ILogger<PublicadorDeEventos> registro) : IPublicadorDeEventos
{
    public async Task PublicarAsync(IEnumerable<IEventoDeDominio> eventos, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(eventos);

        foreach (var evento in eventos)
        {
            try
            {
                await ProcesarAsync(evento, cancelacion);
            }
            catch (Exception excepcion) when (excepcion is not OperationCanceledException)
            {
                registro.FalloAlPublicarEvento(evento.GetType().Name, excepcion);
            }
        }
    }

    private async Task ProcesarAsync(IEventoDeDominio evento, CancellationToken cancelacion)
    {
        switch (evento)
        {
            case QuedadaCancelada cancelada when cancelada.UsuariosAAvisar.Count > 0:
                await notificaciones.NotificarAsync(
                    cancelada.UsuariosAAvisar,
                    "Se ha cancelado un plan al que ibas",
                    $"El organizador ha cancelado el plan. Motivo: {cancelada.Motivo}",
                    cancelacion);
                break;

            case AsistentePromovido promovido:
                // Es el aviso más urgente de todos: la persona había dado el plan por perdido.
                await notificaciones.NotificarAsync(
                    [promovido.UsuarioId],
                    "¡Se ha liberado una plaza y ya tienes la tuya!",
                    "Estabas en lista de espera y acabas de entrar. Ya puedes ver el punto de encuentro exacto.",
                    cancelacion);
                break;

            case QuedadaModificada modificada when modificada.UsuariosAAvisar.Count > 0:
                await notificaciones.NotificarAsync(
                    modificada.UsuariosAAvisar,
                    "Han cambiado los detalles de un plan al que vas",
                    $"Se ha modificado: {string.Join(", ", modificada.CamposModificados)}.",
                    cancelacion);
                break;

            default:
                registro.EventoDeDominioPublicado(evento.GetType().Name);
                break;
        }
    }
}

/// <summary>
/// Notificaciones de desarrollo: escribe en el registro en lugar de enviar correos.
/// </summary>
/// <remarks>
/// Evita mandar correos de verdad desde un entorno local, que es una forma sorprendentemente
/// habitual de molestar a personas reales con datos de prueba. Antes de abrir el piloto hay que
/// sustituirlo por un servicio de envío real.
/// </remarks>
public sealed class NotificacionesEnRegistro(ILogger<NotificacionesEnRegistro> registro) : IServicioDeNotificaciones
{
    public Task NotificarAsync(IEnumerable<Domain.Usuarios.UsuarioId> destinatarios, string asunto, string cuerpo, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(destinatarios);

        var cuantos = destinatarios is ICollection<Domain.Usuarios.UsuarioId> coleccion
            ? coleccion.Count
            : destinatarios.Count();

        // Se registra cuántos son, nunca sus correos: los registros no son sitio para datos personales.
        registro.NotificacionSimulada(cuantos, asunto);

        return Task.CompletedTask;
    }
}
