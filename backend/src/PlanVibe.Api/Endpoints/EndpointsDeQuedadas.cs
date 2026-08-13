using PlanVibe.Api.Seguridad;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Application.Quedadas;
using PlanVibe.Application.Quedadas.Comandos;
using PlanVibe.Application.Quedadas.Consultas;

namespace PlanVibe.Api.Endpoints;

/// <summary>Endpoints de descubrimiento, publicación y participación en planes.</summary>
public static class EndpointsDeQuedadas
{
    /// <summary>Límite de peticiones de las operaciones que crean contenido.</summary>
    public const string LimiteDeEscritura = "escritura";

    public static IEndpointRouteBuilder MapearEndpointsDeQuedadas(this IEndpointRouteBuilder rutas)
    {
        ArgumentNullException.ThrowIfNull(rutas);

        var grupo = rutas.MapGroup("/api/quedadas").WithTags("Quedadas");

        MapearDescubrimiento(grupo);
        MapearPublicacion(grupo);
        MapearParticipacion(grupo);

        return rutas;
    }

    private static void MapearDescubrimiento(RouteGroupBuilder grupo)
    {
        grupo.MapGet("/", async (
                IManejadorDeConsulta<BuscarPlanes, PaginaDe<ResumenDePlan>> manejador,
                CancellationToken cancelacion,
                string? texto = null,
                Guid[]? categorias = null,
                DateTimeOffset? desde = null,
                DateTimeOffset? hasta = null,
                double? latitud = null,
                double? longitud = null,
                int? radio = null,
                bool soloConPlazas = false,
                int pagina = 1,
                int tamano = 20) =>
            {
                // Los topes de página y radio los aplica el manejador de la consulta, no este
                // endpoint: así valen igual venga la petición de donde venga.
                var filtro = new FiltroDeBusqueda(
                    texto, categorias, desde, hasta, latitud, longitud, radio, soloConPlazas, pagina, tamano);

                return Results.Ok(await manejador.ManejarAsync(new BuscarPlanes(filtro), cancelacion));
            })
            .AllowAnonymous()
            .WithSummary("Busca planes")
            .WithDescription("RF-05 y RF-06. Accesible sin cuenta: la página pública debe poder enseñar qué hay cerca.");

        grupo.MapGet("/{id:guid}", async (
                Guid id,
                IManejadorDeConsulta<ObtenerPlan, DetalleDePlan> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(new ObtenerPlan(id), cancelacion)))
            .AllowAnonymous()
            .WithSummary("Detalle de un plan")
            .WithDescription("RF-07. La dirección exacta del punto de encuentro solo se incluye si tienes plaza confirmada.");

        grupo.MapGet("/mios", async (
                IManejadorDeConsulta<ObtenerMisPlanes, IReadOnlyList<ResumenDePlan>> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(new ObtenerMisPlanes(), cancelacion)))
            .RequireAuthorization()
            .WithSummary("Mis planes");
    }

    private static void MapearPublicacion(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/", async (
                CrearQuedada comando,
                IManejadorDeComando<CrearQuedada, Guid> manejador,
                CancellationToken cancelacion) =>
            {
                var id = await manejador.ManejarAsync(comando, cancelacion);
                return Results.Created($"/api/quedadas/{id}", new { id });
            })
            .RequireAuthorization(PoliticasDeAutorizacion.PuedeOrganizar)
            .RequireRateLimiting(LimiteDeEscritura)
            .WithSummary("Publica una quedada")
            .WithDescription("RF-09 y RF-10. Requiere verificación de organizador y ser mayor de edad.");

        grupo.MapPost("/{id:guid}/cancelacion", async (
                Guid id,
                CuerpoDeCancelacion cuerpo,
                IManejadorDeComando<CancelarQuedada, bool> manejador,
                CancellationToken cancelacion) =>
            {
                await manejador.ManejarAsync(new CancelarQuedada(id, cuerpo.Motivo), cancelacion);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .RequireRateLimiting(LimiteDeEscritura)
            .WithSummary("Cancela una quedada propia")
            .WithDescription("RF-11. Solo el organizador. Se avisa a quien estuviera apuntado.");
    }

    private static void MapearParticipacion(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/{id:guid}/asistencia", async (
                Guid id,
                IManejadorDeComando<ApuntarseAQuedada, ResultadoDeInscripcion> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(new ApuntarseAQuedada(id), cancelacion)))
            .RequireAuthorization()
            .RequireRateLimiting(LimiteDeEscritura)
            .WithSummary("Apuntarse a un plan")
            .WithDescription("RF-14. Si está completo, se entra en lista de espera y se devuelve la posición (RF-15).");

        grupo.MapDelete("/{id:guid}/asistencia", async (
                Guid id,
                IManejadorDeComando<AbandonarQuedada, bool> manejador,
                CancellationToken cancelacion) =>
            {
                await manejador.ManejarAsync(new AbandonarQuedada(id), cancelacion);
                return Results.NoContent();
            })
            .RequireAuthorization()
            .RequireRateLimiting(LimiteDeEscritura)
            .WithSummary("Retirarse de un plan")
            .WithDescription("RF-14. Al liberarse la plaza, entra automáticamente quien esté primero en lista de espera.");
    }

    /// <summary>Cuerpo de la petición de cancelación. El motivo se muestra a los asistentes y se audita.</summary>
    public sealed record CuerpoDeCancelacion(string Motivo);
}
