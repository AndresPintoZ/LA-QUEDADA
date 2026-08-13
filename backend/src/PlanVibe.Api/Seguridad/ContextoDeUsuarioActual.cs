using System.Security.Claims;
using Microsoft.IdentityModel.JsonWebTokens;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Api.Seguridad;

/// <summary>
/// Lee quién hace la petición a partir del token ya validado por el middleware de autenticación.
/// </summary>
/// <remarks>
/// <para>
/// Todo lo que se lee aquí viene de un JWT cuya firma ya se ha comprobado. Nunca se toma la
/// identidad de una cabecera, de un parámetro de consulta ni del cuerpo de la petición: eso
/// permitiría a cualquiera declararse administrador escribiéndolo en la URL.
/// </para>
/// <para>
/// La capa de aplicación depende de <see cref="IContextoDeUsuarioActual"/> y no de
/// <c>HttpContext</c>, de modo que los casos de uso se pueden probar sin levantar un servidor.
/// </para>
/// </remarks>
public sealed class ContextoDeUsuarioActual(IHttpContextAccessor accesor) : IContextoDeUsuarioActual
{
    public UsuarioId? UsuarioId
    {
        get
        {
            var valor = Usuario?.FindFirstValue(JwtRegisteredClaimNames.Sub)
                     ?? Usuario?.FindFirstValue(ClaimTypes.NameIdentifier);

            return Guid.TryParse(valor, out var id) ? new UsuarioId(id) : null;
        }
    }

    public bool EstaAutenticado => Usuario?.Identity?.IsAuthenticated == true;

    public string? DireccionIp => accesor.HttpContext?.Connection.RemoteIpAddress?.ToString();

    private ClaimsPrincipal? Usuario => accesor.HttpContext?.User;

    public bool TieneRol(Rol rol) => Usuario?.IsInRole(rol.ToString()) == true;
}
