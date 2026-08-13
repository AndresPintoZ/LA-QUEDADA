namespace PlanVibe.Application.Common;

/// <summary>
/// Lo que se pedía no existe, o quien lo pide no tiene derecho a saber que existe.
/// </summary>
/// <remarks>
/// Se usa deliberadamente también en el segundo caso. Distinguir «no existe» de «existe pero no
/// puedes verlo» permitiría a un atacante enumerar identificadores válidos observando los códigos
/// de respuesta. Ante la duda, 404.
/// </remarks>
public sealed class NoEncontradoException(string recurso, string identificador)
    : Exception($"No se ha encontrado {recurso} con identificador {identificador}.")
{
    public string Recurso { get; } = recurso;

    public string Identificador { get; } = identificador;
}

/// <summary>La persona está identificada, pero su rol o su estado no le permiten esta acción.</summary>
public sealed class AccesoDenegadoException(string mensaje) : Exception(mensaje);

/// <summary>Los datos de entrada no superan la validación. Agrupa todos los errores de una vez.</summary>
public sealed class ValidacionException(IReadOnlyDictionary<string, string[]> errores)
    : Exception("Los datos enviados no son válidos.")
{
    /// <summary>Errores agrupados por campo, en el formato que espera <c>ProblemDetails</c>.</summary>
    public IReadOnlyDictionary<string, string[]> Errores { get; } = errores;
}

/// <summary>
/// La operación choca con el estado actual del sistema: correo ya registrado, o dos personas
/// tomando la última plaza a la vez.
/// </summary>
public sealed class ConflictoException(string codigo, string mensaje) : Exception(mensaje)
{
    public string Codigo { get; } = codigo;
}
