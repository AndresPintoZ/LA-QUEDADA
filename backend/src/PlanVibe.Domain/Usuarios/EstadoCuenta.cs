namespace PlanVibe.Domain.Usuarios;

/// <summary>Ciclo de vida de una cuenta.</summary>
public enum EstadoCuenta
{
    /// <summary>Uso normal de la plataforma.</summary>
    Activa = 1,

    /// <summary>Suspendida por moderación: puede iniciar sesión pero no publicar ni participar (RF-18).</summary>
    Suspendida = 2,

    /// <summary>
    /// Eliminada a petición de la persona (RF-03). Se conserva la fila anonimizada porque las
    /// quedadas y comentarios ya publicados la referencian; borrarla dejaría huérfano ese histórico.
    /// </summary>
    Eliminada = 3,
}
