using System.Text;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using PlanVibe.Api;
using PlanVibe.Api.Endpoints;
using PlanVibe.Api.Seguridad;
using PlanVibe.Application;
using PlanVibe.Infrastructure;
using PlanVibe.Infrastructure.Identidad;
using PlanVibe.Infrastructure.Persistencia;
using Scalar.AspNetCore;
using Serilog;

// El healthcheck de Docker invoca este mismo ejecutable con --comprobar-salud. Se atiende
// antes de construir nada: no tiene sentido levantar la aplicación entera para hacer una
// petición HTTP a un proceso que ya está en marcha.
if (args.Contains(ComprobacionDeSalud.Argumento, StringComparer.Ordinal))
{
    return await ComprobacionDeSalud.EjecutarAsync();
}

var constructor = WebApplication.CreateBuilder(args);

// ---------------------------------------------------------------------------
// Registro estructurado
// ---------------------------------------------------------------------------
constructor.Host.UseSerilog((contexto, configuracion) => configuracion
    .ReadFrom.Configuration(contexto.Configuration)
    .Enrich.FromLogContext()
    .Enrich.WithEnvironmentName()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture));

// ---------------------------------------------------------------------------
// Capas de la aplicación
// ---------------------------------------------------------------------------
constructor.Services.AgregarCapaDeAplicacion();
constructor.Services.AgregarCapaDeInfraestructura(constructor.Configuration, constructor.Environment.IsDevelopment());

constructor.Services.AddHttpContextAccessor();
constructor.Services.AddScoped<PlanVibe.Application.Abstracciones.IContextoDeUsuarioActual, ContextoDeUsuarioActual>();

// ---------------------------------------------------------------------------
// Autenticación
// ---------------------------------------------------------------------------
var opcionesDeJwt = constructor.Configuration.GetSection(OpcionesDeJwt.Seccion).Get<OpcionesDeJwt>()
    ?? throw new InvalidOperationException("Falta la sección de configuración 'Jwt'.");

constructor.Services
    .AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(opciones =>
    {
        opciones.TokenValidationParameters = new TokenValidationParameters
        {
            // Todas las validaciones activadas. Desactivar cualquiera de ellas convierte el
            // token en un dato que el cliente puede fabricar a su gusto.
            ValidateIssuer = true,
            ValidIssuer = opcionesDeJwt.Emisor,
            ValidateAudience = true,
            ValidAudience = opcionesDeJwt.Audiencia,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(opcionesDeJwt.Clave)),
            ValidateLifetime = true,

            // Se restringe el algoritmo aceptado: sin esta lista, un token firmado con otro
            // algoritmo podría pasar la validación.
            ValidAlgorithms = [SecurityAlgorithms.HmacSha256],

            // Sin margen de tolerancia: el valor por defecto son cinco minutos, y con tokens de
            // quince minutos eso alarga un tercio la vida de un token robado.
            ClockSkew = TimeSpan.Zero,
        };

        // Los tokens no se guardan en memoria del servidor: no hay motivo y sería una copia más
        // de una credencial viva.
        opciones.SaveToken = false;

        // En producción no se detallan los motivos del fallo de autenticación.
        opciones.IncludeErrorDetails = constructor.Environment.IsDevelopment();
    });

constructor.Services.AddAuthorizationBuilder().AgregarPoliticasDePlanVibe();

// ---------------------------------------------------------------------------
// Límite de peticiones
// ---------------------------------------------------------------------------
constructor.Services.AddRateLimiter(opciones =>
{
    opciones.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

    // Autenticación: el límite más estricto. Frena la fuerza bruta contra contraseñas
    // y la creación masiva de cuentas.
    opciones.AddPolicy(EndpointsDeIdentidad.LimiteDeAutenticacion, ParticionarPorCliente(
        permitidas: 10, ventanaEnMinutos: 5));

    // Escritura: publicar planes, apuntarse y cancelar. Suficiente para el uso normal
    // y muy por debajo de lo que necesitaría un proceso automatizado.
    opciones.AddPolicy(EndpointsDeQuedadas.LimiteDeEscritura, ParticionarPorCliente(
        permitidas: 30, ventanaEnMinutos: 1));

    // Geocodificación: protege la cuota del proveedor de mapas.
    opciones.AddPolicy(EndpointsDeCatalogo.LimiteDeGeocodificacion, ParticionarPorCliente(
        permitidas: 20, ventanaEnMinutos: 1));

    // Límite general para todo lo demás, incluidas las consultas públicas.
    opciones.GlobalLimiter = PartitionedRateLimiter.Create<HttpContext, string>(contexto =>
        RateLimitPartition.GetFixedWindowLimiter(
            ObtenerClaveDeCliente(contexto),
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = 200,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0,
            }));

    opciones.OnRejected = (contexto, cancelacion) =>
    {
        contexto.HttpContext.RequestServices
            .GetRequiredService<ILoggerFactory>()
            .CreateLogger("PlanVibe.LimiteDePeticiones")
            .LimiteDePeticionesSuperado(contexto.HttpContext.Request.Path);

        return ValueTask.CompletedTask;
    };
});

// ---------------------------------------------------------------------------
// CORS
// ---------------------------------------------------------------------------
const string PoliticaDeCors = "planvibe-web";

var origenesPermitidos = constructor.Configuration.GetSection("Cors:OrigenesPermitidos").Get<string[]>() ?? [];

constructor.Services.AddCors(opciones => opciones.AddPolicy(PoliticaDeCors, politica =>
{
    // Lista explícita de orígenes. Nunca AllowAnyOrigin: con la arquitectura BFF, quien llama
    // a la API es el servidor de Next, así que la lista es corta y conocida.
    politica.WithOrigins(origenesPermitidos)
        .AllowAnyHeader()
        .WithMethods("GET", "POST", "PUT", "DELETE")
        .AllowCredentials();
}));

// ---------------------------------------------------------------------------
// Servicios de la API
// ---------------------------------------------------------------------------
constructor.Services.AddProblemDetails();
constructor.Services.AddExceptionHandler<ManejadorDeExcepciones>();
constructor.Services.AddOpenApi();

// Los enumerados viajan como texto ("Confirmada") y no como número (1).
//
// Con números, el contrato depende del ORDEN de los valores del enumerado: insertar
// uno nuevo en medio cambiaría el significado de los datos ya enviados y de los que
// tuviera guardados cualquier cliente. Además, un cuerpo JSON con "estado": 2 es
// indescifrable sin tener el código delante.
//
// Esto lo detectó una prueba de integración: el frontend comparaba con cadenas
// mientras la API enviaba números, así que un plan cancelado se habría mostrado
// como activo.
constructor.Services.ConfigureHttpJsonOptions(opciones =>
    opciones.SerializerOptions.Converters.Add(new JsonStringEnumConverter()));

constructor.Services.AddHealthChecks()
    .AddNpgSql(
        constructor.Configuration.GetConnectionString("PlanVibe")!,
        name: "postgres",
        tags: ["preparacion"]);

// Límite del tamaño del cuerpo: la API solo recibe JSON con texto. Sin este tope, una petición
// de varios gigabytes agotaría la memoria del proceso.
constructor.Services.Configure<Microsoft.AspNetCore.Http.Features.FormOptions>(opciones =>
    opciones.MultipartBodyLengthLimit = 1024 * 1024);

var aplicacion = constructor.Build();

// ---------------------------------------------------------------------------
// Tubería de peticiones. El orden importa.
// ---------------------------------------------------------------------------
aplicacion.UseExceptionHandler();
aplicacion.UsarCabecerasDeSeguridad();
aplicacion.UseSerilogRequestLogging();

if (aplicacion.Environment.IsDevelopment())
{
    // La documentación interactiva solo se publica en desarrollo: describe la superficie
    // completa de la API y no hay motivo para regalársela a nadie en producción.
    aplicacion.MapOpenApi();
    aplicacion.MapScalarApiReference(opciones => opciones.WithTitle("PlanVibe API"));
}
else
{
    // HSTS solo fuera de desarrollo: en local se trabaja con http y activarlo dejaría el
    // navegador redirigiendo a https durante meses.
    aplicacion.UseHsts();
    aplicacion.UseHttpsRedirection();
}

aplicacion.UseCors(PoliticaDeCors);
aplicacion.UseRateLimiter();
aplicacion.UseAuthentication();
aplicacion.UseAuthorization();

aplicacion.MapHealthChecks("/salud");
aplicacion.MapearEndpointsDeIdentidad();
aplicacion.MapearEndpointsDeQuedadas();
aplicacion.MapearEndpointsDeCatalogo();

// ---------------------------------------------------------------------------
// Preparación de la base de datos
// ---------------------------------------------------------------------------
if (aplicacion.Environment.IsDevelopment())
{
    // Solo en desarrollo. En producción las migraciones se aplican como un paso explícito del
    // despliegue: que un contenedor recién arrancado modifique el esquema por su cuenta es
    // una forma excelente de perder datos cuando escalan varias instancias a la vez.
    await aplicacion.PrepararBaseDeDatosAsync();
}

aplicacion.Logger.ApiIniciada(aplicacion.Environment.EnvironmentName);

await aplicacion.RunAsync();

return 0;

// ---------------------------------------------------------------------------
// Funciones de apoyo
// ---------------------------------------------------------------------------

// Reparte el límite por persona autenticada o, si no la hay, por dirección IP.
// Usar solo la IP castigaría a todo el mundo detrás de una misma conexión compartida; usar solo
// la identidad dejaría sin protección justo los endpoints anónimos, que son los que más interesa
// proteger.
static Func<HttpContext, RateLimitPartition<string>> ParticionarPorCliente(int permitidas, int ventanaEnMinutos) =>
    contexto => RateLimitPartition.GetFixedWindowLimiter(
        ObtenerClaveDeCliente(contexto),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = permitidas,
            Window = TimeSpan.FromMinutes(ventanaEnMinutos),

            // Sin cola: es mejor responder 429 de inmediato que hacer esperar a la persona
            // sin explicación mientras se acumulan peticiones en memoria.
            QueueLimit = 0,
        });

static string ObtenerClaveDeCliente(HttpContext contexto) =>
    contexto.User.Identity?.IsAuthenticated == true
        ? $"usuario:{contexto.User.Identity.Name ?? contexto.User.FindFirst("sub")?.Value}"
        : $"ip:{contexto.Connection.RemoteIpAddress}";

/// <summary>Punto de entrada expuesto para las pruebas de integración con WebApplicationFactory.</summary>
public partial class Program;
