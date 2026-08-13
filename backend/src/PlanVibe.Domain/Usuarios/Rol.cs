namespace PlanVibe.Domain.Usuarios;

/// <summary>
/// Roles de la plataforma (<c>docs/01-requisitos-funcionales.md</c>). Son acumulativos:
/// una persona moderadora sigue siendo usuaria registrada y puede apuntarse a planes.
/// </summary>
/// <remarks>
/// «Visitante» no aparece aquí: no es un rol de una cuenta, sino la ausencia de cuenta.
/// </remarks>
public enum Rol
{
    /// <summary>Descubre planes, se apunta, comenta y reporta.</summary>
    Registrado = 1,

    /// <summary>Ha superado la verificación y puede crear quedadas y eventos.</summary>
    OrganizadorVerificado = 2,

    /// <summary>Revisa la cola de reportes y modera contenido.</summary>
    Moderador = 3,

    /// <summary>Gestiona usuarios, categorías y reglas.</summary>
    Administrador = 4,
}
