using Microsoft.EntityFrameworkCore;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Infrastructure.Persistencia;

namespace PlanVibe.Api.Endpoints;

/// <summary>Endpoints de apoyo: catálogo de categorías y búsqueda de lugares.</summary>
public static class EndpointsDeCatalogo
{
    /// <summary>Límite de peticiones de la geocodificación.</summary>
    /// <remarks>
    /// Nominatim es un servicio gratuito con una política de uso estricta. Este límite protege
    /// tanto a PlanVibe de quedarse sin servicio como a Nominatim de que le usemos de más.
    /// </remarks>
    public const string LimiteDeGeocodificacion = "geocodificacion";

    public static IEndpointRouteBuilder MapearEndpointsDeCatalogo(this IEndpointRouteBuilder rutas)
    {
        ArgumentNullException.ThrowIfNull(rutas);

        rutas.MapGet("/api/categorias", async (PlanVibeDbContext contexto, CancellationToken cancelacion) =>
            {
                var categorias = await contexto.Categorias
                    .AsNoTracking()
                    .Where(c => c.Activa)
                    .OrderBy(c => c.Orden)
                    .Select(c => new { id = c.Id.Valor, nombre = c.Nombre, clave = c.Clave, color = c.ColorHex })
                    .ToListAsync(cancelacion);

                return Results.Ok(categorias);
            })
            .AllowAnonymous()
            .WithTags("Catálogo")
            .WithSummary("Categorías activas")
            .WithDescription("Las categorías son un catálogo de administración: añadir una no requiere desplegar.");

        rutas.MapGet("/api/lugares", async (
                string texto,
                IServicioDeGeocodificacion geocodificacion,
                CancellationToken cancelacion) =>
            {
                var lugares = await geocodificacion.BuscarAsync(texto, cancelacion);

                return Results.Ok(lugares.Select(l => new
                {
                    nombre = l.NombreCompleto,
                    latitud = l.Coordenadas.Latitud,
                    longitud = l.Coordenadas.Longitud,
                }));
            })
            .RequireAuthorization()
            .RequireRateLimiting(LimiteDeGeocodificacion)
            .WithTags("Catálogo")
            .WithSummary("Busca un lugar por texto")
            .WithDescription("Requiere sesión para que la cuota del proveedor de mapas no quede expuesta al público.");

        return rutas;
    }
}
