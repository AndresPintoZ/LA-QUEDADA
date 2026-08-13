using FluentValidation;
using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Application.Identidad.Comandos;

/// <summary>Alta de una cuenta nueva (RF-01, RF-04).</summary>
/// <param name="AnioDeNacimiento">Solo el año: es el dato mínimo que permite comprobar la edad de acceso.</param>
/// <param name="VersionNormasAceptada">Versión concreta de las normas que se mostró y se aceptó.</param>
public sealed record RegistrarUsuario(
    string Correo,
    string Contrasena,
    string NombreVisible,
    string? Ciudad,
    int AnioDeNacimiento,
    string VersionNormasAceptada) : IComando<Guid>;

public sealed class RegistrarUsuarioValidador : AbstractValidator<RegistrarUsuario>
{
    /// <summary>
    /// Longitud mínima de contraseña. Se prefiere exigir longitud a exigir símbolos raros:
    /// las reglas de composición empujan a la gente hacia patrones predecibles y anotados en
    /// un papel, mientras que una frase larga es fácil de recordar y difícil de adivinar.
    /// </summary>
    public const int LongitudMinimaContrasena = 12;

    public const int LongitudMaximaContrasena = 256;

    public RegistrarUsuarioValidador()
    {
        RuleFor(c => c.Correo)
            .NotEmpty().WithMessage("Escribe tu correo electrónico.")
            .MaximumLength(CorreoElectronico.LongitudMaxima);

        RuleFor(c => c.Contrasena)
            .NotEmpty().WithMessage("Elige una contraseña.")
            .MinimumLength(LongitudMinimaContrasena)
            .WithMessage($"La contraseña debe tener al menos {LongitudMinimaContrasena} caracteres. Una frase que recuerdes vale perfectamente.")
            .MaximumLength(LongitudMaximaContrasena)
            .WithMessage("La contraseña es demasiado larga.");

        RuleFor(c => c.NombreVisible)
            .NotEmpty().WithMessage("Escribe el nombre con el que quieres que te vean.")
            .Length(NombreVisible.LongitudMinima, NombreVisible.LongitudMaxima);

        RuleFor(c => c.AnioDeNacimiento)
            .InclusiveBetween(1900, DateTime.UtcNow.Year)
            .WithMessage("Indica tu año de nacimiento.");

        RuleFor(c => c.VersionNormasAceptada)
            .NotEmpty().WithMessage("Hay que aceptar las normas de la comunidad.");
    }
}

/// <summary>
/// Crea la cuenta: primero el agregado de dominio y después las credenciales en Identity.
/// </summary>
/// <remarks>
/// <para>
/// Ante un correo ya registrado se devuelve el mismo mensaje genérico que en el resto de casos
/// y no se revela que la dirección existe. Filtrar esa información convierte el formulario de
/// registro en una herramienta para averiguar quién tiene cuenta en la plataforma.
/// </para>
/// <para>
/// Las dos escrituras (dominio e Identity) van en la misma transacción de base de datos, así que
/// no puede quedar un usuario sin credenciales ni unas credenciales sin usuario.
/// </para>
/// </remarks>
public sealed class RegistrarUsuarioManejador(
    IRepositorioDeUsuarios usuarios,
    IServicioDeCredenciales credenciales,
    IUnidadDeTrabajo unidadDeTrabajo,
    IRegistroDeAuditoria auditoria,
    TimeProvider reloj,
    ILogger<RegistrarUsuarioManejador> registro) : IManejadorDeComando<RegistrarUsuario, Guid>
{
    public async Task<Guid> ManejarAsync(RegistrarUsuario comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var correo = new CorreoElectronico(comando.Correo);

        if (await usuarios.ExisteCorreoAsync(correo, cancelacion))
        {
            // El registro se queda anotado para poder detectar intentos masivos de enumeración,
            // pero a quien lo intenta se le responde siempre lo mismo.
            registro.RegistroConCorreoExistente();
            throw new ConflictoException("registro.no_disponible", "No se ha podido completar el registro con esos datos.");
        }

        var ahora = reloj.GetUtcNow();

        var usuario = Usuario.Registrar(
            UsuarioId.Nuevo(),
            correo,
            new NombreVisible(comando.NombreVisible),
            comando.Ciudad,
            comando.AnioDeNacimiento,
            comando.VersionNormasAceptada,
            ahora);

        await usuarios.AgregarAsync(usuario, cancelacion);

        var resultado = await credenciales.CrearAsync(usuario.Id, correo, comando.Contrasena, cancelacion);

        if (!resultado.Correcto)
        {
            throw new ValidacionException(new Dictionary<string, string[]> { ["contrasena"] = [.. resultado.Errores] });
        }

        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await auditoria.RegistrarAsync(usuario.Id, "usuario.registrado", "Usuario", usuario.Id.ToString(), null, cancelacion);

        return usuario.Id.Valor;
    }
}
