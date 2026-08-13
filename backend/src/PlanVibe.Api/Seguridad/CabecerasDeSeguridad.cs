namespace PlanVibe.Api.Seguridad;

/// <summary>
/// Añade cabeceras de seguridad a todas las respuestas de la API.
/// </summary>
/// <remarks>
/// Son baratas y cubren clases enteras de ataque. Las de presentación (política de seguridad de
/// contenido, permisos del navegador) las aplica el frontend de Next, que es quien sirve HTML;
/// aquí se ponen las que tienen sentido en una API que solo devuelve JSON.
/// </remarks>
public static class CabecerasDeSeguridad
{
    public static IApplicationBuilder UsarCabecerasDeSeguridad(this IApplicationBuilder aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        return aplicacion.Use(async (contexto, siguiente) =>
        {
            var cabeceras = contexto.Response.Headers;

            // Impide que el navegador adivine el tipo de contenido. Sin esto, un JSON con
            // contenido controlado por un tercero podría acabar interpretándose como HTML.
            cabeceras["X-Content-Type-Options"] = "nosniff";

            // La API no debe mostrarse dentro de un marco: no tiene interfaz que enmarcar.
            cabeceras["X-Frame-Options"] = "DENY";

            // No se filtra a terceros a qué URL de la API se estaba llamando.
            cabeceras["Referrer-Policy"] = "no-referrer";

            // Las respuestas de la API no se guardan en caché: muchas dependen de quién pregunta,
            // y una caché intermedia podría servirle a una persona la respuesta de otra.
            cabeceras["Cache-Control"] = "no-store, no-cache, must-revalidate";

            // Una API JSON no necesita cámara, micrófono ni ubicación.
            cabeceras["Permissions-Policy"] = "geolocation=(), microphone=(), camera=(), payment=()";

            // No se anuncia con qué está construido el servidor: reduce lo que un atacante
            // sabe de antemano sobre qué vulnerabilidades conocidas probar.
            cabeceras.Remove("Server");
            cabeceras.Remove("X-Powered-By");

            await siguiente();
        });
    }
}
