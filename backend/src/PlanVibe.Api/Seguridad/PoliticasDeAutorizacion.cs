using Microsoft.AspNetCore.Authorization;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Api.Seguridad;

/// <summary>
/// Políticas de autorización de la API.
/// </summary>
/// <remarks>
/// <para>
/// Se definen políticas con nombre en lugar de repartir <c>[Authorize(Roles = "...")]</c> por los
/// endpoints. Así el criterio de «quién puede organizar» está escrito una sola vez: si mañana se
/// añade una condición, se cambia aquí y aplica en todas partes.
/// </para>
/// <para>
/// Esta comprobación es la primera barrera, no la única. El caso de uso vuelve a preguntárselo al
/// agregado <c>Usuario</c>, porque el token pudo emitirse antes de que se le revocara la
/// verificación y sigue siendo válido hasta que caduque.
/// </para>
/// </remarks>
public static class PoliticasDeAutorizacion
{
    /// <summary>Puede publicar quedadas y eventos.</summary>
    public const string PuedeOrganizar = "puede-organizar";

    /// <summary>Puede revisar reportes y moderar contenido.</summary>
    public const string EsModerador = "es-moderador";

    /// <summary>Puede gestionar categorías, usuarios y reglas.</summary>
    public const string EsAdministrador = "es-administrador";

    public static AuthorizationBuilder AgregarPoliticasDePlanVibe(this AuthorizationBuilder constructor)
    {
        ArgumentNullException.ThrowIfNull(constructor);

        return constructor
            .AddPolicy(PuedeOrganizar, politica => politica
                .RequireAuthenticatedUser()
                // Se comprueba la reclamación calculada al emitir el token, que ya combina
                // verificación vigente y mayoría de edad, y no solo la presencia del rol.
                .RequireClaim("puede_organizar", "true"))

            .AddPolicy(EsModerador, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(nameof(Rol.Moderador), nameof(Rol.Administrador)))

            .AddPolicy(EsAdministrador, politica => politica
                .RequireAuthenticatedUser()
                .RequireRole(nameof(Rol.Administrador)));
    }
}
