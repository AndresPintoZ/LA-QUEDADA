namespace PlanVibe.Domain.Common;

/// <summary>
/// Se lanza cuando se intenta llevar un agregado a un estado que el negocio no admite:
/// apuntarse a una quedada cancelada, capacidad negativa, correo con formato imposible.
/// </summary>
/// <remarks>
/// La API traduce esta excepción a un 400/409 con un cuerpo <c>ProblemDetails</c> que expone
/// únicamente <see cref="Codigo"/> y <see cref="Exception.Message"/>: ambos están redactados para
/// ser legibles por la persona usuaria y no revelan detalles internos del sistema.
/// </remarks>
public class ExcepcionDeDominio : Exception
{
    public ExcepcionDeDominio(string codigo, string mensaje) : base(mensaje) => Codigo = codigo;

    public ExcepcionDeDominio(string codigo, string mensaje, Exception interna)
        : base(mensaje, interna) => Codigo = codigo;

    /// <summary>Código estable en formato <c>area.motivo</c>, apto para que el cliente decida qué mostrar.</summary>
    public string Codigo { get; } = "dominio.error";

    /// <summary>Atajo para validar invariantes sin llenar el dominio de <c>if</c> repetidos.</summary>
    public static void SiNo(bool condicion, string codigo, string mensaje)
    {
        if (!condicion)
        {
            throw new ExcepcionDeDominio(codigo, mensaje);
        }
    }
}
