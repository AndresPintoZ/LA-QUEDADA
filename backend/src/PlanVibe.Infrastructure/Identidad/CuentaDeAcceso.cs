using Microsoft.AspNetCore.Identity;

namespace PlanVibe.Infrastructure.Identidad;

/// <summary>
/// Credenciales de acceso gestionadas por ASP.NET Core Identity.
/// </summary>
/// <remarks>
/// <para>
/// Comparte identificador con el agregado <c>Usuario</c> del dominio, pero vive en tablas
/// distintas. Esta separación es la que permite que el dominio no sepa qué es un hash de
/// contraseña, y que cambiar el mecanismo de acceso no obligue a tocar las reglas de negocio.
/// </para>
/// <para>
/// Aquí no se añade ningún dato de perfil. Si alguien necesita el nombre visible o la ciudad,
/// el sitio es el agregado <c>Usuario</c>: duplicarlos aquí garantizaría que las dos copias
/// acabarían discrepando.
/// </para>
/// </remarks>
public sealed class CuentaDeAcceso : IdentityUser<Guid>
{
    /// <summary>Fecha de alta de las credenciales, para poder auditar cuentas creadas en masa.</summary>
    public DateTimeOffset CreadaEn { get; set; }
}

/// <summary>
/// Token de renovación de sesión, guardado siempre como hash.
/// </summary>
/// <remarks>
/// <para>
/// Nunca se almacena el valor en claro: si alguien accediera a la base de datos, no podría
/// suplantar sesiones activas. La comprobación se hace calculando el hash del token recibido.
/// </para>
/// <para>
/// <see cref="Familia"/> agrupa todos los tokens nacidos de un mismo inicio de sesión. Cuando se
/// detecta que se reutiliza un token ya rotado —señal clara de robo—, se revoca la familia entera
/// y no solo el token concreto, porque no se puede saber si quien tiene el token válido es la
/// persona legítima o quien lo copió.
/// </para>
/// </remarks>
public sealed class TokenDeRenovacion
{
    public Guid Id { get; set; }

    public Guid UsuarioId { get; set; }

    /// <summary>Hash SHA-256 del token. El valor en claro solo existe en el navegador de la persona.</summary>
    public required string HashDelToken { get; set; }

    public Guid Familia { get; set; }

    public DateTimeOffset CreadoEn { get; set; }

    public DateTimeOffset ExpiraEn { get; set; }

    public DateTimeOffset? UsadoEn { get; set; }

    public DateTimeOffset? RevocadoEn { get; set; }

    /// <summary>Descripción corta del dispositivo, para que la persona reconozca sus sesiones.</summary>
    public string? Dispositivo { get; set; }

    public bool EstaVigenteEn(DateTimeOffset instante) =>
        RevocadoEn is null && UsadoEn is null && instante < ExpiraEn;
}
