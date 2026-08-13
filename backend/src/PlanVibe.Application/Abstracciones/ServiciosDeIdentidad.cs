using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Application.Abstracciones;

/// <summary>
/// Gestión de credenciales de acceso, delegada en ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// El agregado <see cref="Usuario"/> no sabe nada de contraseñas: esa responsabilidad vive aquí.
/// La interfaz nunca devuelve ni expone el hash, y las contraseñas se pasan siempre como texto
/// que se consume de inmediato, nunca se almacena ni se registra.
/// </remarks>
public interface IServicioDeCredenciales
{
    /// <summary>Crea las credenciales de una cuenta recién registrada.</summary>
    public Task<ResultadoDeCredenciales> CrearAsync(UsuarioId usuarioId, CorreoElectronico correo, string contrasena, CancellationToken cancelacion);

    /// <summary>
    /// Comprueba unas credenciales.
    /// </summary>
    /// <remarks>
    /// La implementación debe tardar lo mismo tanto si el correo existe como si no, para no
    /// filtrar qué direcciones están registradas midiendo tiempos de respuesta.
    /// </remarks>
    public Task<UsuarioId?> ValidarAsync(CorreoElectronico correo, string contrasena, CancellationToken cancelacion);

    /// <summary>Cambia la contraseña comprobando antes la actual.</summary>
    public Task<ResultadoDeCredenciales> CambiarContrasenaAsync(UsuarioId usuarioId, string contrasenaActual, string contrasenaNueva, CancellationToken cancelacion);

    /// <summary>Invalida todas las sesiones abiertas de la cuenta.</summary>
    public Task CerrarTodasLasSesionesAsync(UsuarioId usuarioId, CancellationToken cancelacion);
}

/// <param name="Correcto">Si la operación salió bien.</param>
/// <param name="Errores">Motivos de fallo, ya redactados para mostrar a la persona.</param>
public sealed record ResultadoDeCredenciales(bool Correcto, IReadOnlyList<string> Errores)
{
    public static ResultadoDeCredenciales Exito { get; } = new(true, []);

    public static ResultadoDeCredenciales Fallo(params string[] errores) => new(false, errores);
}

/// <summary>Emisión y renovación de los tokens de sesión.</summary>
public interface IEmisorDeTokens
{
    /// <summary>
    /// Emite un token de acceso de vida corta y un token de renovación de vida larga.
    /// </summary>
    /// <remarks>
    /// El de renovación es rotativo: cada uso emite uno nuevo e invalida el anterior. Si un token
    /// robado se reutiliza después de haber sido rotado, se detecta la reutilización y se cierra
    /// toda la familia de sesiones de esa cuenta.
    /// </remarks>
    public Task<ParDeTokens> EmitirAsync(Usuario usuario, string? dispositivo, CancellationToken cancelacion);

    /// <summary>Rota un token de renovación válido y devuelve un par nuevo.</summary>
    public Task<ParDeTokens?> RenovarAsync(string tokenDeRenovacion, CancellationToken cancelacion);

    /// <summary>Revoca un token de renovación concreto (cierre de sesión).</summary>
    public Task RevocarAsync(string tokenDeRenovacion, CancellationToken cancelacion);
}

/// <param name="TokenDeAcceso">JWT firmado que acompaña a cada petición.</param>
/// <param name="ExpiraEn">Caducidad del token de acceso.</param>
/// <param name="TokenDeRenovacion">Valor opaco de un solo uso para obtener un token de acceso nuevo.</param>
/// <param name="RenovacionExpiraEn">Caducidad del token de renovación.</param>
public sealed record ParDeTokens(
    string TokenDeAcceso,
    DateTimeOffset ExpiraEn,
    string TokenDeRenovacion,
    DateTimeOffset RenovacionExpiraEn);
