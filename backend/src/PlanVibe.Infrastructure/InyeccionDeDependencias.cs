using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Infrastructure.Auditoria;
using PlanVibe.Infrastructure.Eventos;
using PlanVibe.Infrastructure.Geocodificacion;
using PlanVibe.Infrastructure.Identidad;
using PlanVibe.Infrastructure.Persistencia;
using PlanVibe.Infrastructure.Verificacion;

namespace PlanVibe.Infrastructure;

/// <summary>Registro de las implementaciones concretas de los puertos de la capa de aplicación.</summary>
public static class InyeccionDeDependencias
{
    /// <summary>Nombre del cliente HTTP de geocodificación.</summary>
    public const string ClienteDeGeocodificacion = "nominatim";

    public static IServiceCollection AgregarCapaDeInfraestructura(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        bool esEntornoDeDesarrollo)
    {
        ArgumentNullException.ThrowIfNull(servicios);
        ArgumentNullException.ThrowIfNull(configuracion);

        servicios.AgregarPersistencia(configuracion);
        servicios.AgregarIdentidad(configuracion);
        servicios.AgregarServiciosDeSoporte(configuracion, esEntornoDeDesarrollo);

        // TimeProvider inyectado en lugar de DateTimeOffset.UtcNow: permite que las pruebas
        // sitúen el reloj donde quieran sin recurrir a trucos.
        servicios.AddSingleton(TimeProvider.System);

        return servicios;
    }

    private static void AgregarPersistencia(this IServiceCollection servicios, IConfiguration configuracion)
    {
        var cadenaDeConexion = configuracion.GetConnectionString("PlanVibe")
            ?? throw new InvalidOperationException(
                "Falta la cadena de conexión 'PlanVibe'. Defínela en ConnectionStrings__PlanVibe.");

        servicios.AddDbContext<PlanVibeDbContext>(opciones =>
        {
            opciones.UseNpgsql(cadenaDeConexion, npgsql =>
            {
                // NetTopologySuite habilita los tipos geográficos y las consultas por cercanía.
                npgsql.UseNetTopologySuite();

                // Reintentos ante fallos transitorios: en Docker es normal que la API arranque
                // antes de que PostgreSQL termine de aceptar conexiones.
                npgsql.EnableRetryOnFailure(maxRetryCount: 5, maxRetryDelay: TimeSpan.FromSeconds(10), errorCodesToAdd: null);

                npgsql.MigrationsHistoryTable("__historial_de_migraciones", PlanVibeDbContext.EsquemaApp);
            });

            // Los datos sensibles NO se registran nunca, ni siquiera en desarrollo: los parámetros
            // de una consulta incluyen correos y hashes, y los registros acaban en sitios
            // menos protegidos que la base de datos.
            opciones.EnableSensitiveDataLogging(false);
        });

        servicios.AddScoped<IRepositorioDeUsuarios, RepositorioDeUsuarios>();
        servicios.AddScoped<IRepositorioDeQuedadas, RepositorioDeQuedadas>();
        servicios.AddScoped<IConsultasDeQuedadas, ConsultasDeQuedadas>();
        servicios.AddScoped<IUnidadDeTrabajo, UnidadDeTrabajo>();
        servicios.AddScoped<IRegistroDeAuditoria, RegistroDeAuditoria>();
    }

    private static void AgregarIdentidad(this IServiceCollection servicios, IConfiguration configuracion)
    {
        servicios.AddIdentityCore<CuentaDeAcceso>(opciones =>
            {
                opciones.User.RequireUniqueEmail = true;

                // Se exige longitud y no composición. Las reglas de «una mayúscula, un número y
                // un símbolo» empujan hacia contraseñas del tipo «Password1!», que son
                // predecibles; una frase larga resiste mucho mejor y se recuerda sin apuntarla.
                opciones.Password.RequiredLength = 12;
                opciones.Password.RequireDigit = false;
                opciones.Password.RequireLowercase = false;
                opciones.Password.RequireUppercase = false;
                opciones.Password.RequireNonAlphanumeric = false;
                opciones.Password.RequiredUniqueChars = 4;

                // Bloqueo temporal tras varios fallos: frena la fuerza bruta contra una cuenta
                // concreta sin dejar que un tercero deje a nadie fuera de forma permanente.
                opciones.Lockout.MaxFailedAccessAttempts = 5;
                opciones.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                opciones.Lockout.AllowedForNewUsers = true;
            })
            .AddRoles<IdentityRole<Guid>>()
            .AddEntityFrameworkStores<PlanVibeDbContext>();

        // Nota: no se registran los proveedores de token de Identity (restablecimiento de
        // contraseña, confirmación de correo) porque todavía no hay servicio de envío de correo.
        // Habrá que añadir AddDefaultTokenProviders() junto con ese servicio.

        servicios.AddOptions<OpcionesDeJwt>()
            .Bind(configuracion.GetSection(OpcionesDeJwt.Seccion))
            .ValidateDataAnnotations()
            // Se valida al arrancar y no en la primera petición: es preferible que la aplicación
            // no llegue a levantarse a que atienda tráfico con una clave de firma inválida.
            .ValidateOnStart();

        servicios.AddScoped<IServicioDeCredenciales, ServicioDeCredenciales>();
        servicios.AddScoped<IEmisorDeTokens, EmisorDeTokens>();
    }

    private static void AgregarServiciosDeSoporte(
        this IServiceCollection servicios,
        IConfiguration configuracion,
        bool esEntornoDeDesarrollo)
    {
        servicios.AddMemoryCache();

        servicios.Configure<OpcionesDeNominatim>(configuracion.GetSection(OpcionesDeNominatim.Seccion));
        servicios.Configure<OpcionesDeVerificacionSimulada>(configuracion.GetSection(OpcionesDeVerificacionSimulada.Seccion));

        servicios.AddHttpClient<IServicioDeGeocodificacion, ServicioNominatim>(ClienteDeGeocodificacion, (proveedor, cliente) =>
        {
            var opciones = configuracion.GetSection(OpcionesDeNominatim.Seccion).Get<OpcionesDeNominatim>() ?? new OpcionesDeNominatim();

            cliente.BaseAddress = new Uri(opciones.UrlBase);
            cliente.DefaultRequestHeaders.UserAgent.ParseAdd(opciones.UserAgent);

            // Tiempo de espera corto: si el buscador de direcciones tarda, es mejor que la
            // persona coloque el punto a mano que dejar el formulario colgado.
            cliente.Timeout = TimeSpan.FromSeconds(5);
        });

        // El proveedor de verificación simulado solo se activa en desarrollo, aunque la
        // configuración diga lo contrario. Habilitarlo en producción concedería el rol de
        // organizador a cualquiera sin comprobar ninguna identidad.
        if (esEntornoDeDesarrollo)
        {
            servicios.AddScoped<IProveedorDeVerificacion, ProveedorDeVerificacionSimulado>();
        }
        else
        {
            servicios.AddScoped<IProveedorDeVerificacion>(_ => throw new InvalidOperationException(
                "No hay ningún proveedor de verificación de identidad configurado. "
                + "Antes de desplegar fuera de desarrollo hay que integrar uno real "
                + "(ver docs/04-seguridad-privacidad-moderacion.md)."));
        }

        servicios.AddScoped<IServicioDeNotificaciones, NotificacionesEnRegistro>();
        servicios.AddScoped<IPublicadorDeEventos, PublicadorDeEventos>();
    }
}
