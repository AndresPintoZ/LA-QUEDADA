namespace PlanVibe.Api;

/// <summary>
/// Modo de comprobación de salud para el <c>HEALTHCHECK</c> de Docker.
/// </summary>
/// <remarks>
/// <para>
/// La imagen final es «chiseled»: no tiene intérprete de órdenes, ni <c>curl</c>, ni
/// <c>wget</c>. Eso es deseable —cuanto menos haya dentro, menos herramientas encuentra
/// quien logre entrar—, pero deja a Docker sin nada con lo que comprobar si el proceso
/// responde.
/// </para>
/// <para>
/// La solución es que el propio ejecutable sepa autocomprobarse: arrancado con
/// <c>--comprobar-salud</c>, hace una petición a su propio endpoint de salud, imprime el
/// resultado y termina con código 0 o 1. No levanta el servidor web ni toca la base de datos.
/// </para>
/// </remarks>
public static class ComprobacionDeSalud
{
    public const string Argumento = "--comprobar-salud";

    /// <summary>Tiempo máximo de espera. Debe ser menor que el <c>timeout</c> del healthcheck.</summary>
    private static readonly TimeSpan Espera = TimeSpan.FromSeconds(4);

    /// <summary>Devuelve 0 si la API responde correctamente y 1 en cualquier otro caso.</summary>
    public static async Task<int> EjecutarAsync()
    {
        var puerto = Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "8080";

        using var cliente = new HttpClient { Timeout = Espera };

        try
        {
            var respuesta = await cliente.GetAsync(new Uri($"http://localhost:{puerto}/salud"));

            if (respuesta.IsSuccessStatusCode)
            {
                return 0;
            }

            await Console.Error.WriteLineAsync($"La comprobación de salud ha respondido {(int)respuesta.StatusCode}.");

            return 1;
        }
        catch (Exception excepcion) when (excepcion is HttpRequestException or TaskCanceledException)
        {
            await Console.Error.WriteLineAsync($"La API no responde: {excepcion.Message}");

            return 1;
        }
    }
}
