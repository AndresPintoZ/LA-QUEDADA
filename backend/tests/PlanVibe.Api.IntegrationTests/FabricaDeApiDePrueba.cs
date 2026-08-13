using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using PlanVibe.Infrastructure.Persistencia;
using Testcontainers.PostgreSql;

namespace PlanVibe.Api.IntegrationTests;

/// <summary>
/// Levanta la API real contra un PostgreSQL con PostGIS efímero.
/// </summary>
/// <remarks>
/// <para>
/// Se usa una base de datos real en contenedor y no un sustituto en memoria. La diferencia
/// importa: la columna geográfica calculada, el índice espacial, los arrays nativos y la
/// concurrencia optimista con <c>xmin</c> son características de PostgreSQL. Un proveedor
/// en memoria daría por buenas cosas que en producción fallarían.
/// </para>
/// <para>
/// Requiere un motor de Docker en marcha. Sin él, las pruebas de este proyecto se omiten
/// (ver <see cref="RequiereDockerAttribute"/>); las unitarias siguen ejecutándose.
/// </para>
/// </remarks>
public sealed class FabricaDeApiDePrueba : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder()
        // Misma imagen que docker-compose.yml: las pruebas deben correr contra lo mismo
        // que se despliega, no contra una versión parecida.
        .WithImage("postgis/postgis:17-3.5-alpine")
        .WithDatabase("planvibe_pruebas")
        .WithUsername("pruebas")
        .WithPassword("pruebas-solo-en-contenedor-efimero")
        .Build();

    /// <summary>Clave de firma para las pruebas. No sale de este proceso.</summary>
    private const string ClaveDeFirmaDePrueba =
        "clave-de-firma-exclusiva-de-las-pruebas-de-integracion-con-longitud-suficiente-1234";

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        // Se aplican las migraciones reales, no un EnsureCreated: así se comprueba de paso
        // que las migraciones se aplican sobre una base de datos limpia.
        using var ambito = Services.CreateScope();
        var contexto = ambito.ServiceProvider.GetRequiredService<PlanVibeDbContext>();
        await contexto.Database.MigrateAsync();
    }

    public override async ValueTask DisposeAsync()
    {
        await _postgres.DisposeAsync();
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        // Development activa el proveedor de verificación simulado, que es lo que permite
        // recorrer el flujo completo hasta publicar un plan sin un proveedor externo.
        builder.UseEnvironment(Environments.Development);

        builder.UseSetting("ConnectionStrings:PlanVibe", _postgres.GetConnectionString());
        builder.UseSetting("Jwt:Clave", ClaveDeFirmaDePrueba);
        builder.UseSetting("Cors:OrigenesPermitidos:0", "http://localhost:3000");
    }
}

/// <summary>
/// Marca una prueba que necesita Docker.
/// </summary>
/// <remarks>
/// Si no hay motor de Docker, la prueba se omite en lugar de fallar. Un fallo por falta de
/// entorno se acaba ignorando por costumbre, y entonces deja de detectarse el fallo de verdad.
/// </remarks>
public sealed class RequiereDockerAttribute : FactAttribute
{
    /// <summary>
    /// Los parámetros los rellena el compilador con la ubicación de la prueba; xUnit v3
    /// los necesita para poder señalar el archivo y la línea en el informe de resultados.
    /// </summary>
    public RequiereDockerAttribute(
        [System.Runtime.CompilerServices.CallerFilePath] string? archivo = null,
        [System.Runtime.CompilerServices.CallerLineNumber] int linea = -1)
        : base(archivo, linea)
    {
        if (!HayDocker())
        {
            Skip = "Se omite: no se ha detectado un motor de Docker en marcha.";
        }
    }

    private static bool HayDocker()
    {
        try
        {
            using var proceso = System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = "docker",
                Arguments = "info",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
            });

            if (proceso is null)
            {
                return false;
            }

            proceso.WaitForExit(5_000);

            return proceso.HasExited && proceso.ExitCode == 0;
        }
        catch
        {
            return false;
        }
    }
}
