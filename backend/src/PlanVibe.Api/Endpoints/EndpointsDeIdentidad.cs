using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Application.Identidad.Comandos;

namespace PlanVibe.Api.Endpoints;

/// <summary>Endpoints de registro, sesión y verificación de organizador.</summary>
public static class EndpointsDeIdentidad
{
    /// <summary>
    /// Nombre de la política de límite de peticiones para las operaciones de credenciales.
    /// </summary>
    /// <remarks>
    /// Registro e inicio de sesión son los objetivos habituales de la fuerza bruta y de la
    /// creación masiva de cuentas, así que llevan un límite mucho más estricto que el resto.
    /// </remarks>
    public const string LimiteDeAutenticacion = "autenticacion";

    public static IEndpointRouteBuilder MapearEndpointsDeIdentidad(this IEndpointRouteBuilder rutas)
    {
        ArgumentNullException.ThrowIfNull(rutas);

        var grupo = rutas.MapGroup("/api/identidad").WithTags("Identidad");

        grupo.MapPost("/registro", async (
                RegistrarUsuario comando,
                IManejadorDeComando<RegistrarUsuario, Guid> manejador,
                CancellationToken cancelacion) =>
            {
                var id = await manejador.ManejarAsync(comando, cancelacion);

                // 201 con la ubicación del recurso, aunque el cuerpo solo lleve el identificador:
                // los datos del perfil se obtienen ya autenticado.
                return Results.Created($"/api/identidad/usuarios/{id}", new { id });
            })
            .RequireRateLimiting(LimiteDeAutenticacion)
            .AllowAnonymous()
            .WithSummary("Crea una cuenta nueva")
            .WithDescription("RF-01. Devuelve 409 si los datos no permiten completar el registro.");

        grupo.MapPost("/sesion", async (
                IniciarSesion comando,
                IManejadorDeComando<IniciarSesion, SesionIniciada> manejador,
                CancellationToken cancelacion) =>
            {
                var sesion = await manejador.ManejarAsync(comando, cancelacion);

                // Los tokens viajan en el cuerpo, no en una cookie de la API: quien los guarda en
                // una cookie httpOnly es el BFF de Next, que es el único que habla con el navegador.
                return Results.Ok(sesion);
            })
            .RequireRateLimiting(LimiteDeAutenticacion)
            .AllowAnonymous()
            .WithSummary("Inicia sesión")
            .WithDescription("Devuelve siempre el mismo error ante credenciales incorrectas, exista o no la cuenta.");

        grupo.MapPost("/sesion/renovar", async (
                RenovarSesion comando,
                IManejadorDeComando<RenovarSesion, ParDeTokens> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(comando, cancelacion)))
            .RequireRateLimiting(LimiteDeAutenticacion)
            .AllowAnonymous()
            .WithSummary("Renueva la sesión")
            .WithDescription("Rota el token de renovación. Reutilizar uno ya usado revoca todas las sesiones de la cuenta.");

        grupo.MapPost("/sesion/cerrar", async (
                CerrarSesion comando,
                IManejadorDeComando<CerrarSesion, bool> manejador,
                CancellationToken cancelacion) =>
            {
                await manejador.ManejarAsync(comando, cancelacion);
                return Results.NoContent();
            })
            .AllowAnonymous()
            .WithSummary("Cierra la sesión");

        grupo.MapGet("/yo", async (
                IManejadorDeConsulta<ObtenerMiPerfil, PerfilDeSesion> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(new ObtenerMiPerfil(), cancelacion)))
            .RequireAuthorization()
            .WithSummary("Perfil de la persona autenticada");

        MapearVerificacion(grupo);

        return rutas;
    }

    private static void MapearVerificacion(RouteGroupBuilder grupo)
    {
        grupo.MapPost("/verificacion", async (
                IManejadorDeComando<IniciarVerificacion, SesionDeVerificacion> manejador,
                CancellationToken cancelacion) => Results.Ok(await manejador.ManejarAsync(new IniciarVerificacion(), cancelacion)))
            .RequireAuthorization()
            .WithSummary("Inicia la verificación de organizador")
            .WithDescription("RF-20. Devuelve la URL del proveedor. PlanVibe no recibe ni guarda ningún documento.");

        grupo.MapPost("/verificacion/completar", async (
                CompletarVerificacion comando,
                IManejadorDeComando<CompletarVerificacion, string> manejador,
                CancellationToken cancelacion) =>
            {
                var estado = await manejador.ManejarAsync(comando, cancelacion);
                return Results.Ok(new { estado });
            })
            .RequireAuthorization()
            .WithSummary("Cierra la verificación")
            .WithDescription(
                "RF-21. El resultado se consulta al proveedor; no se acepta el que envíe el cliente. "
                + "Solo se guarda estado, proveedor, referencia y si se confirmó la mayoría de edad (RF-22).");
    }
}
