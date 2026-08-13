using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Quedadas;

namespace PlanVibe.Application.Quedadas.Consultas;

/// <summary>Búsqueda de planes para la pantalla de explorar (RF-05, RF-06).</summary>
public sealed record BuscarPlanes(FiltroDeBusqueda Filtro) : IConsulta<PaginaDe<ResumenDePlan>>;

/// <summary>Detalle de un plan (RF-07).</summary>
public sealed record ObtenerPlan(Guid Id) : IConsulta<DetalleDePlan>;

/// <summary>Planes propios: organizados y apuntados.</summary>
public sealed record ObtenerMisPlanes : IConsulta<IReadOnlyList<ResumenDePlan>>;

/// <summary>
/// Aplica los topes de tamaño y radio antes de tocar la base de datos.
/// </summary>
/// <remarks>
/// Recortar aquí, y no confiar en que el cliente pida cifras razonables, es lo que impide que
/// una petición pública con <c>tamanoDePagina=100000</c> sature el servidor.
/// </remarks>
public sealed class BuscarPlanesManejador(
    IConsultasDeQuedadas consultas,
    TimeProvider reloj) : IManejadorDeConsulta<BuscarPlanes, PaginaDe<ResumenDePlan>>
{
    public async Task<PaginaDe<ResumenDePlan>> ManejarAsync(BuscarPlanes consulta, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        var filtro = consulta.Filtro with
        {
            Pagina = Math.Max(1, consulta.Filtro.Pagina),
            TamanoDePagina = Math.Clamp(consulta.Filtro.TamanoDePagina, 1, FiltroDeBusqueda.TamanoMaximoDePagina),
            RadioEnMetros = consulta.Filtro.RadioEnMetros is { } radio
                ? Math.Clamp(radio, 100, FiltroDeBusqueda.RadioMaximoEnMetros)
                : null,

            // Por defecto solo interesa lo que aún no ha pasado.
            Desde = consulta.Filtro.Desde ?? reloj.GetUtcNow(),
        };

        return await consultas.BuscarAsync(filtro, cancelacion);
    }
}

public sealed class ObtenerPlanManejador(
    IConsultasDeQuedadas consultas,
    IContextoDeUsuarioActual contexto) : IManejadorDeConsulta<ObtenerPlan, DetalleDePlan>
{
    public async Task<DetalleDePlan> ManejarAsync(ObtenerPlan consulta, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(consulta);

        // Se pasa quién pregunta para que la consulta decida si revela la dirección exacta.
        return await consultas.ObtenerDetalleAsync(new QuedadaId(consulta.Id), contexto.UsuarioId, cancelacion)
            ?? throw new NoEncontradoException("el plan", consulta.Id.ToString());
    }
}

public sealed class ObtenerMisPlanesManejador(
    IConsultasDeQuedadas consultas,
    IContextoDeUsuarioActual contexto) : IManejadorDeConsulta<ObtenerMisPlanes, IReadOnlyList<ResumenDePlan>>
{
    public async Task<IReadOnlyList<ResumenDePlan>> ManejarAsync(ObtenerMisPlanes consulta, CancellationToken cancelacion)
    {
        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        return await consultas.ObtenerMisPlanesAsync(usuarioId, cancelacion);
    }
}
