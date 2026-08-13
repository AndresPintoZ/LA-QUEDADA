using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Infrastructure.Persistencia;

namespace PlanVibe.Infrastructure.Identidad;

/// <summary>
/// Emite el token de acceso (JWT firmado) y el de renovación (valor opaco y rotativo).
/// </summary>
/// <remarks>
/// <para>
/// Son dos cosas distintas a propósito. El de acceso lleva la identidad y los roles dentro y se
/// valida sin tocar la base de datos, lo que lo hace rápido pero irrevocable hasta que caduca:
/// por eso vive quince minutos. El de renovación no contiene nada, es un valor aleatorio cuya
/// única propiedad es figurar en la base de datos, y por eso sí se puede revocar al instante.
/// </para>
/// <para>
/// La rotación con detección de reutilización es lo que convierte el robo de un token de
/// renovación en un incidente contenido: en cuanto el token robado y el legítimo se usan ambos,
/// se cierra la familia entera de sesiones.
/// </para>
/// </remarks>
public sealed class EmisorDeTokens(
    PlanVibeDbContext contexto,
    IOptions<OpcionesDeJwt> opciones,
    TimeProvider reloj,
    ILogger<EmisorDeTokens> registro) : IEmisorDeTokens
{
    /// <summary>Bytes de entropía del token de renovación. 256 bits: no es adivinable por fuerza bruta.</summary>
    private const int BytesDeEntropia = 32;

    private readonly OpcionesDeJwt _opciones = opciones.Value;

    public async Task<ParDeTokens> EmitirAsync(Usuario usuario, string? dispositivo, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(usuario);

        return await EmitirParAsync(usuario, Guid.CreateVersion7(), dispositivo, cancelacion);
    }

    public async Task<ParDeTokens?> RenovarAsync(string tokenDeRenovacion, CancellationToken cancelacion)
    {
        var hash = CalcularHash(tokenDeRenovacion);
        var ahora = reloj.GetUtcNow();

        var almacenado = await contexto.TokensDeRenovacion.FirstOrDefaultAsync(t => t.HashDelToken == hash, cancelacion);

        if (almacenado is null)
        {
            return null;
        }

        // Un token ya usado que vuelve a aparecer solo tiene una explicación razonable:
        // alguien tiene una copia. No se puede saber si es la persona legítima o quien lo robó,
        // así que se invalidan todas las sesiones nacidas de ese inicio de sesión.
        if (almacenado.UsadoEn is not null)
        {
            registro.ReutilizacionDeTokenDetectada(almacenado.UsuarioId);
            await RevocarFamiliaAsync(almacenado.Familia, ahora, cancelacion);
            await contexto.SaveChangesAsync(cancelacion);

            return null;
        }

        if (!almacenado.EstaVigenteEn(ahora))
        {
            return null;
        }

        var usuario = await contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == new UsuarioId(almacenado.UsuarioId), cancelacion);

        if (usuario is null || usuario.Estado != EstadoCuenta.Activa)
        {
            return null;
        }

        almacenado.UsadoEn = ahora;

        return await EmitirParAsync(usuario, almacenado.Familia, almacenado.Dispositivo, cancelacion);
    }

    public async Task RevocarAsync(string tokenDeRenovacion, CancellationToken cancelacion)
    {
        var hash = CalcularHash(tokenDeRenovacion);

        var almacenado = await contexto.TokensDeRenovacion.FirstOrDefaultAsync(t => t.HashDelToken == hash, cancelacion);

        if (almacenado is not null)
        {
            // Al cerrar sesión se revoca la familia entera: si la persona pide salir,
            // debe salir de verdad, no solo del último token emitido.
            await RevocarFamiliaAsync(almacenado.Familia, reloj.GetUtcNow(), cancelacion);
            await contexto.SaveChangesAsync(cancelacion);
        }
    }

    private async Task<ParDeTokens> EmitirParAsync(Usuario usuario, Guid familia, string? dispositivo, CancellationToken cancelacion)
    {
        var ahora = reloj.GetUtcNow();
        var expiraAcceso = ahora.AddMinutes(_opciones.MinutosDeAcceso);
        var expiraRenovacion = ahora.AddDays(_opciones.DiasDeRenovacion);

        var tokenDeAcceso = ConstruirJwt(usuario, ahora, expiraAcceso);
        var (valorEnClaro, hash) = GenerarTokenDeRenovacion();

        contexto.TokensDeRenovacion.Add(new TokenDeRenovacion
        {
            Id = Guid.CreateVersion7(),
            UsuarioId = usuario.Id.Valor,
            HashDelToken = hash,
            Familia = familia,
            CreadoEn = ahora,
            ExpiraEn = expiraRenovacion,
            Dispositivo = dispositivo,
        });

        await contexto.SaveChangesAsync(cancelacion);

        return new ParDeTokens(tokenDeAcceso, expiraAcceso, valorEnClaro, expiraRenovacion);
    }

    private string ConstruirJwt(Usuario usuario, DateTimeOffset ahora, DateTimeOffset expira)
    {
        var clave = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_opciones.Clave));

        var reclamaciones = new List<Claim>
        {
            new(JwtRegisteredClaimNames.Sub, usuario.Id.Valor.ToString()),
            new(JwtRegisteredClaimNames.Jti, Guid.CreateVersion7().ToString()),

            // Se incluye si puede organizar, ya resuelto, para que la API no tenga que consultar
            // el usuario en cada petición solo para comprobar un permiso.
            new("puede_organizar", usuario.PuedeOrganizar.ToString().ToLowerInvariant()),
        };

        // El correo y el nombre NO viajan en el token: un JWT va firmado, pero no cifrado.
        // Cualquiera que lo intercepte puede leer su contenido en claro.
        reclamaciones.AddRange(usuario.Roles.Select(rol => new Claim(ClaimTypes.Role, rol.ToString())));

        var descriptor = new SecurityTokenDescriptor
        {
            Issuer = _opciones.Emisor,
            Audience = _opciones.Audiencia,
            Subject = new ClaimsIdentity(reclamaciones),
            IssuedAt = ahora.UtcDateTime,
            NotBefore = ahora.UtcDateTime,
            Expires = expira.UtcDateTime,
            SigningCredentials = new SigningCredentials(clave, SecurityAlgorithms.HmacSha256),
        };

        return new JsonWebTokenHandler().CreateToken(descriptor);
    }

    /// <summary>
    /// Genera el token de renovación y su hash.
    /// </summary>
    /// <remarks>
    /// El valor en claro se devuelve una sola vez, para entregarlo al navegador. En base de datos
    /// solo queda el hash, igual que se hace con las contraseñas.
    /// </remarks>
    private static (string EnClaro, string Hash) GenerarTokenDeRenovacion()
    {
        var bytes = RandomNumberGenerator.GetBytes(BytesDeEntropia);
        var enClaro = Convert.ToBase64String(bytes);

        return (enClaro, CalcularHash(enClaro));
    }

    private static string CalcularHash(string valor) =>
        Convert.ToHexStringLower(SHA256.HashData(Encoding.UTF8.GetBytes(valor)));

    private async Task RevocarFamiliaAsync(Guid familia, DateTimeOffset ahora, CancellationToken cancelacion) =>
        await contexto.TokensDeRenovacion
            .Where(t => t.Familia == familia && t.RevocadoEn == null)
            .ExecuteUpdateAsync(t => t.SetProperty(x => x.RevocadoEn, ahora), cancelacion);
}
