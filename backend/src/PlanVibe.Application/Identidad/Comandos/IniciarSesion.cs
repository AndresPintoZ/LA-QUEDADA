using FluentValidation;
using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Application.Identidad.Comandos;

/// <summary>Inicio de sesión con correo y contraseña.</summary>
public sealed record IniciarSesion(string Correo, string Contrasena, string? Dispositivo) : IComando<SesionIniciada>;

/// <param name="Tokens">Par de tokens de la sesión.</param>
/// <param name="Perfil">Datos mínimos para pintar la cabecera sin una segunda petición.</param>
public sealed record SesionIniciada(ParDeTokens Tokens, PerfilDeSesion Perfil);

/// <param name="Roles">Roles vigentes, para que la interfaz sepa qué opciones mostrar.</param>
/// <param name="PuedeOrganizar">Resuelto en servidor: la interfaz no debe recalcular esta regla.</param>
public sealed record PerfilDeSesion(
    Guid Id,
    string NombreVisible,
    string Correo,
    string? Ciudad,
    IReadOnlyList<string> Roles,
    string EstadoVerificacion,
    bool PuedeOrganizar);

public sealed class IniciarSesionValidador : AbstractValidator<IniciarSesion>
{
    public IniciarSesionValidador()
    {
        RuleFor(c => c.Correo).NotEmpty();
        RuleFor(c => c.Contrasena).NotEmpty();
    }
}

/// <summary>
/// Comprueba las credenciales y emite la sesión.
/// </summary>
/// <remarks>
/// <para>
/// Todos los caminos de fallo devuelven exactamente el mismo error, «Correo o contraseña
/// incorrectos», tanto si la dirección no existe como si la contraseña no coincide. Diferenciarlos
/// permitiría comprobar qué correos están registrados probándolos uno a uno.
/// </para>
/// <para>
/// Una cuenta suspendida sí recibe un mensaje propio: ahí la persona ya ha demostrado ser quien
/// dice, y dejarla sin explicación no protege nada y sí genera un problema de soporte.
/// </para>
/// </remarks>
public sealed class IniciarSesionManejador(
    IServicioDeCredenciales credenciales,
    IRepositorioDeUsuarios usuarios,
    IEmisorDeTokens emisorDeTokens,
    ILogger<IniciarSesionManejador> registro) : IManejadorDeComando<IniciarSesion, SesionIniciada>
{
    public async Task<SesionIniciada> ManejarAsync(IniciarSesion comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        CorreoElectronico correo;

        try
        {
            correo = new CorreoElectronico(comando.Correo);
        }
        catch (Domain.Common.ExcepcionDeDominio)
        {
            // Un correo mal formado se trata como credenciales incorrectas, no como un error de
            // validación: si no, el formulario delataría qué direcciones tienen formato aceptado.
            throw CredencialesIncorrectas();
        }

        var usuarioId = await credenciales.ValidarAsync(correo, comando.Contrasena, cancelacion);

        if (usuarioId is null)
        {
            registro.InicioDeSesionFallido();
            throw CredencialesIncorrectas();
        }

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId.Value, cancelacion)
            ?? throw CredencialesIncorrectas();

        if (usuario.Estado == EstadoCuenta.Eliminada)
        {
            throw CredencialesIncorrectas();
        }

        if (usuario.Estado == EstadoCuenta.Suspendida)
        {
            throw new AccesoDenegadoException("Tu cuenta está suspendida. Escríbenos si crees que se trata de un error.");
        }

        var tokens = await emisorDeTokens.EmitirAsync(usuario, comando.Dispositivo, cancelacion);

        return new SesionIniciada(tokens, ConstruirPerfil(usuario));
    }

    internal static PerfilDeSesion ConstruirPerfil(Usuario usuario) => new(
        usuario.Id.Valor,
        usuario.NombreVisible.Valor,
        usuario.Correo.Valor,
        usuario.Ciudad,
        [.. usuario.Roles.Select(r => r.ToString())],
        usuario.Verificacion.Estado.ToString(),
        usuario.PuedeOrganizar);

    private static AccesoDenegadoException CredencialesIncorrectas() =>
        new("Correo o contraseña incorrectos.");
}
