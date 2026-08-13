namespace PlanVibe.Domain.Usuarios.ObjetosDeValor;

/// <summary>
/// Todo lo que PlanVibe conserva sobre la verificación de identidad de una persona.
/// </summary>
/// <remarks>
/// <para>
/// Esta lista es corta a propósito y es la traducción literal de la decisión de producto
/// recogida en <c>docs/04-seguridad-privacidad-moderacion.md</c>: la plataforma comprueba
/// la identidad <em>sin</em> almacenar fotografía, número ni copia de ningún documento oficial.
/// </para>
/// <para>
/// Ese trabajo lo hace el proveedor externo, que devuelve solo un resultado y una referencia
/// técnica con la que reclamar trazabilidad si algún día hiciera falta. Añadir aquí un campo
/// con datos documentales rompería la prueba
/// <c>La_verificacion_no_guarda_ningun_dato_del_documento</c>, que existe justo para eso.
/// </para>
/// </remarks>
/// <param name="Estado">Situación actual del proceso.</param>
/// <param name="Proveedor">Identificador del proveedor que realizó la comprobación.</param>
/// <param name="ReferenciaExterna">Referencia técnica de la transacción en el proveedor.</param>
/// <param name="MayoriaDeEdadConfirmada">
/// Si el proveedor confirmó que la persona tiene 18 años cumplidos. Se guarda como un sí/no
/// en lugar de la fecha de nacimiento: es el dato mínimo que resuelve la necesidad (RF-24).
/// </param>
/// <param name="ActualizadaEn">Fecha y hora del último cambio de estado.</param>
/// <param name="Observacion">Motivo de un rechazo o de una revocación, para poder explicarlo.</param>
public sealed record DatosDeVerificacion(
    EstadoVerificacion Estado,
    string? Proveedor,
    string? ReferenciaExterna,
    bool MayoriaDeEdadConfirmada,
    DateTimeOffset? ActualizadaEn,
    string? Observacion)
{
    /// <summary>Estado de partida de toda cuenta recién creada.</summary>
    public static DatosDeVerificacion SinIniciar { get; } =
        new(EstadoVerificacion.NoIniciada, null, null, false, null, null);
}
