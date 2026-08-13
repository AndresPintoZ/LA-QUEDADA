using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Usuarios.ObjetosDeValor;

/// <summary>
/// Nombre con el que a la persona se la ve en la aplicación. No tiene por qué ser su nombre legal:
/// la identidad real la comprueba el proveedor de verificación y no se muestra a nadie (RF-22).
/// </summary>
public sealed record NombreVisible
{
    public const int LongitudMinima = 2;
    public const int LongitudMaxima = 60;

    public NombreVisible(string valor)
    {
        // Se colapsan los espacios repetidos para evitar nombres que simulan estar vacíos
        // o que usan espaciado para saltarse filtros de contenido.
        var limpio = string.Join(' ', (valor ?? string.Empty).Split(' ', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries));

        ExcepcionDeDominio.SiNo(
            limpio.Length is >= LongitudMinima and <= LongitudMaxima,
            "nombre_visible.invalido",
            $"El nombre visible debe tener entre {LongitudMinima} y {LongitudMaxima} caracteres.");

        Valor = limpio;
    }

    public string Valor { get; }

    public override string ToString() => Valor;
}
