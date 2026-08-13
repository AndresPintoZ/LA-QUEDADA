namespace PlanVibe.Infrastructure.Persistencia;

/// <summary>
/// Registro inmutable de una acción sensible: verificaciones, publicaciones, reportes y
/// decisiones de moderación (RNF-04).
/// </summary>
/// <remarks>
/// <para>
/// Es un registro de <em>hechos</em>, no una copia de seguridad del contenido. Guarda quién hizo
/// qué, sobre qué y cuándo. En <see cref="Metadatos"/> solo deben ir datos mínimos y no
/// identificativos: una categoría, un estado, una referencia técnica. Nunca el texto de un
/// comentario ni datos personales.
/// </para>
/// <para>
/// Las filas no se modifican ni se borran desde la aplicación. Su retención se define en la
/// política de datos y se aplica con un proceso aparte, para que borrar una cuenta no destruya
/// la traza de las decisiones de moderación que la afectaron.
/// </para>
/// </remarks>
public sealed class EntradaDeAuditoria
{
    public Guid Id { get; set; }

    /// <summary>Quién hizo la acción, o <c>null</c> si la ejecutó el sistema.</summary>
    public Guid? ActorId { get; set; }

    /// <summary>Acción en formato <c>area.accion</c>, p. ej. <c>quedada.publicada</c>.</summary>
    public required string Accion { get; set; }

    public required string TipoDeObjeto { get; set; }

    public required string ObjetoId { get; set; }

    /// <summary>Datos adicionales en JSON. Mínimos y sin información personal.</summary>
    public string? Metadatos { get; set; }

    public DateTimeOffset OcurridoEn { get; set; }
}
