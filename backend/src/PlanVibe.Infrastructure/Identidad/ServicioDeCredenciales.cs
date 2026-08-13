using System.Diagnostics;
using Microsoft.AspNetCore.Identity;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Infrastructure.Identidad;

/// <inheritdoc cref="IServicioDeCredenciales"/>
public sealed class ServicioDeCredenciales(
    UserManager<CuentaDeAcceso> gestorDeUsuarios,
    TimeProvider reloj) : IServicioDeCredenciales
{
    /// <summary>
    /// Tiempo mínimo que tarda una validación, exista o no la cuenta.
    /// </summary>
    /// <remarks>
    /// Sin esto, un correo inexistente respondería casi al instante mientras que uno existente
    /// tardaría lo que cuesta comprobar el hash. Esa diferencia de milisegundos es suficiente
    /// para averiguar qué direcciones están registradas probándolas en masa.
    /// </remarks>
    private static readonly TimeSpan DuracionMinimaDeValidacion = TimeSpan.FromMilliseconds(250);

    public async Task<ResultadoDeCredenciales> CrearAsync(
        UsuarioId usuarioId,
        CorreoElectronico correo,
        string contrasena,
        CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(correo);

        var cuenta = new CuentaDeAcceso
        {
            // Comparte identificador con el agregado de dominio: es lo que los mantiene unidos
            // sin necesidad de una tabla de correspondencia.
            Id = usuarioId.Valor,
            UserName = correo.Valor,
            Email = correo.Valor,
            CreadaEn = reloj.GetUtcNow(),
        };

        var resultado = await gestorDeUsuarios.CreateAsync(cuenta, contrasena);

        return resultado.Succeeded
            ? ResultadoDeCredenciales.Exito
            : ResultadoDeCredenciales.Fallo([.. resultado.Errors.Select(e => TraducirError(e.Code))]);
    }

    public async Task<UsuarioId?> ValidarAsync(CorreoElectronico correo, string contrasena, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(correo);

        var cronometro = Stopwatch.StartNew();

        try
        {
            var cuenta = await gestorDeUsuarios.FindByEmailAsync(correo.Valor);

            if (cuenta is null)
            {
                // Se calcula un hash igualmente para que el trabajo de CPU sea comparable
                // al del camino en que la cuenta sí existe.
                _ = gestorDeUsuarios.PasswordHasher.HashPassword(new CuentaDeAcceso(), contrasena);
                return null;
            }

            if (await gestorDeUsuarios.IsLockedOutAsync(cuenta))
            {
                return null;
            }

            var correcta = await gestorDeUsuarios.CheckPasswordAsync(cuenta, contrasena);

            // El contador de intentos fallidos alimenta el bloqueo temporal de la cuenta,
            // que es lo que frena los ataques de fuerza bruta contra una dirección concreta.
            if (!correcta)
            {
                await gestorDeUsuarios.AccessFailedAsync(cuenta);
                return null;
            }

            await gestorDeUsuarios.ResetAccessFailedCountAsync(cuenta);

            return new UsuarioId(cuenta.Id);
        }
        finally
        {
            var restante = DuracionMinimaDeValidacion - cronometro.Elapsed;

            if (restante > TimeSpan.Zero)
            {
                await Task.Delay(restante, cancelacion);
            }
        }
    }

    public async Task<ResultadoDeCredenciales> CambiarContrasenaAsync(
        UsuarioId usuarioId,
        string contrasenaActual,
        string contrasenaNueva,
        CancellationToken cancelacion)
    {
        var cuenta = await gestorDeUsuarios.FindByIdAsync(usuarioId.Valor.ToString());

        if (cuenta is null)
        {
            return ResultadoDeCredenciales.Fallo("No se ha podido cambiar la contraseña.");
        }

        var resultado = await gestorDeUsuarios.ChangePasswordAsync(cuenta, contrasenaActual, contrasenaNueva);

        return resultado.Succeeded
            ? ResultadoDeCredenciales.Exito
            : ResultadoDeCredenciales.Fallo([.. resultado.Errors.Select(e => TraducirError(e.Code))]);
    }

    /// <summary>
    /// Invalida las sesiones cambiando la marca de seguridad de la cuenta.
    /// </summary>
    /// <remarks>
    /// Se usa al cambiar la contraseña y al detectar la reutilización de un token robado:
    /// en ambos casos hay que expulsar a quien pueda estar dentro con credenciales antiguas.
    /// </remarks>
    public async Task CerrarTodasLasSesionesAsync(UsuarioId usuarioId, CancellationToken cancelacion)
    {
        var cuenta = await gestorDeUsuarios.FindByIdAsync(usuarioId.Valor.ToString());

        if (cuenta is not null)
        {
            await gestorDeUsuarios.UpdateSecurityStampAsync(cuenta);
        }
    }

    /// <summary>
    /// Traduce los códigos de Identity a mensajes en español y sin detalles innecesarios.
    /// </summary>
    /// <remarks>
    /// «Duplicate email» se traduce a un mensaje genérico a propósito: confirmar que una
    /// dirección ya está registrada convierte el formulario en un comprobador de cuentas.
    /// </remarks>
    private static string TraducirError(string codigo) => codigo switch
    {
        "PasswordTooShort" => "La contraseña es demasiado corta.",
        "PasswordRequiresDigit" => "La contraseña debe incluir algún número.",
        "PasswordRequiresLower" => "La contraseña debe incluir alguna letra minúscula.",
        "PasswordRequiresUpper" => "La contraseña debe incluir alguna letra mayúscula.",
        "PasswordRequiresNonAlphanumeric" => "La contraseña debe incluir algún símbolo.",
        "PasswordRequiresUniqueChars" => "La contraseña repite demasiado los mismos caracteres.",
        "DuplicateUserName" or "DuplicateEmail" => "No se ha podido completar el registro con esos datos.",
        _ => "No se ha podido completar la operación.",
    };
}
