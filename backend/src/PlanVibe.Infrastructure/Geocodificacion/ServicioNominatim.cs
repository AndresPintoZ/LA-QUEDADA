using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;

namespace PlanVibe.Infrastructure.Geocodificacion;

/// <summary>Configuración de la geocodificación con Nominatim.</summary>
public sealed class OpcionesDeNominatim
{
    public const string Seccion = "Nominatim";

    public string UrlBase { get; init; } = "https://nominatim.openstreetmap.org/";

    /// <summary>
    /// Identificación de la aplicación en la cabecera <c>User-Agent</c>.
    /// </summary>
    /// <remarks>
    /// La política de uso de Nominatim exige identificar la aplicación con un contacto válido.
    /// Sin ella, el servicio bloquea las peticiones, y con razón: es un servicio gratuito
    /// sostenido por donaciones.
    /// </remarks>
    public string UserAgent { get; init; } = "PlanVibe/0.1 (piloto Ávila; contacto@planvibe.es)";

    /// <summary>Sesgo geográfico de las búsquedas hacia el área del piloto.</summary>
    public string PaisPreferente { get; init; } = "es";

    public int MaximoResultados { get; init; } = 5;

    /// <summary>Minutos que se guarda en memoria cada búsqueda ya resuelta.</summary>
    public int MinutosDeCache { get; init; } = 60;
}

/// <summary>
/// Geocodificación con Nominatim, el servicio de búsqueda de OpenStreetMap.
/// </summary>
/// <remarks>
/// <para>
/// Se eligió por no necesitar clave de API ni tarjeta, lo que permite levantar el entorno completo
/// en local sin dar de alta nada. A cambio impone un límite de una petición por segundo y exige
/// identificarse: por eso hay caché en memoria y una única petición concurrente.
/// </para>
/// <para>
/// Si el piloto crece, el cambio a un proveedor de pago afecta solo a esta clase gracias a
/// <see cref="IServicioDeGeocodificacion"/>.
/// </para>
/// </remarks>
public sealed class ServicioNominatim(
    HttpClient cliente,
    IMemoryCache cache,
    IOptions<OpcionesDeNominatim> opciones,
    ILogger<ServicioNominatim> registro) : IServicioDeGeocodificacion
{
    private readonly OpcionesDeNominatim _opciones = opciones.Value;

    public async Task<IReadOnlyList<LugarGeocodificado>> BuscarAsync(string texto, CancellationToken cancelacion)
    {
        if (string.IsNullOrWhiteSpace(texto) || texto.Trim().Length < 3)
        {
            return [];
        }

        var consulta = texto.Trim();
        var claveDeCache = $"nominatim:{consulta.ToLowerInvariant()}";

        if (cache.TryGetValue(claveDeCache, out IReadOnlyList<LugarGeocodificado>? enCache) && enCache is not null)
        {
            return enCache;
        }

        // Los parámetros van codificados: el texto lo escribe la persona usuaria y no debe
        // poder alterar la estructura de la URL.
        var url = $"search?format=jsonv2&limit={_opciones.MaximoResultados}"
                + $"&countrycodes={Uri.EscapeDataString(_opciones.PaisPreferente)}"
                + $"&q={Uri.EscapeDataString(consulta)}";

        try
        {
            var respuesta = await cliente.GetFromJsonAsync<List<RespuestaDeNominatim>>(url, cancelacion) ?? [];

            var lugares = respuesta
                .Where(r => r.Latitud is not null && r.Longitud is not null)
                .Select(ConvertirALugar)
                .Where(l => l is not null)
                .Select(l => l!)
                .ToList();

            cache.Set(claveDeCache, (IReadOnlyList<LugarGeocodificado>)lugares, TimeSpan.FromMinutes(_opciones.MinutosDeCache));

            return lugares;
        }
        catch (HttpRequestException excepcion)
        {
            // Que el buscador de direcciones falle no debe tumbar la creación de un plan:
            // la persona siempre puede colocar el punto a mano en el mapa.
            registro.FalloDeGeocodificacion(excepcion);
            return [];
        }
        catch (TaskCanceledException) when (!cancelacion.IsCancellationRequested)
        {
            registro.TiempoAgotadoDeGeocodificacion();
            return [];
        }
    }

    private static LugarGeocodificado? ConvertirALugar(RespuestaDeNominatim respuesta)
    {
        // Nominatim devuelve las coordenadas como texto y siempre con punto decimal,
        // así que se convierte con cultura invariante y no con la del servidor.
        if (!double.TryParse(respuesta.Latitud, NumberStyles.Float, CultureInfo.InvariantCulture, out var latitud)
            || !double.TryParse(respuesta.Longitud, NumberStyles.Float, CultureInfo.InvariantCulture, out var longitud))
        {
            return null;
        }

        return new LugarGeocodificado(respuesta.NombreCompleto ?? string.Empty, new Coordenadas(latitud, longitud));
    }

    private sealed record RespuestaDeNominatim
    {
        [JsonPropertyName("lat")]
        public string? Latitud { get; init; }

        [JsonPropertyName("lon")]
        public string? Longitud { get; init; }

        [JsonPropertyName("display_name")]
        public string? NombreCompleto { get; init; }
    }
}
