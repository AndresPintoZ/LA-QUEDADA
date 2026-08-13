using PlanVibe.Domain.Common;

namespace PlanVibe.Domain.Categorias;

/// <summary>
/// Categoría del catálogo (senderismo, música, cultura…), gestionada por administración.
/// </summary>
/// <remarks>
/// Es un agregado propio y muy pequeño. Está separado de <c>Quedada</c> a propósito: las
/// categorías cambian por decisiones de producto, no por lo que ocurra con un plan concreto,
/// y una quedada solo necesita referenciarla por identidad.
/// </remarks>
public sealed class Categoria : RaizDeAgregado<CategoriaId>
{
    public const int LongitudMaximaNombre = 60;

    private Categoria(CategoriaId id, string nombre, string clave, string colorHex, int orden)
        : base(id)
    {
        Nombre = nombre;
        Clave = clave;
        ColorHex = colorHex;
        Orden = orden;
        Activa = true;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Categoria()
    {
        Nombre = string.Empty;
        Clave = string.Empty;
        ColorHex = string.Empty;
    }

    /// <summary>Nombre que se muestra, p. ej. «Bici y deporte».</summary>
    public string Nombre { get; private set; }

    /// <summary>
    /// Identificador estable y legible, p. ej. «bici-y-deporte». Se usa en las URL y en los
    /// filtros para que un enlace compartido siga funcionando aunque se renombre la categoría.
    /// </summary>
    public string Clave { get; private set; }

    /// <summary>Color de la categoría en la interfaz. Nunca es el único indicador: siempre va con texto.</summary>
    public string ColorHex { get; private set; }

    public int Orden { get; private set; }

    /// <summary>Las categorías no se borran: se desactivan, para no romper las quedadas que ya las usan.</summary>
    public bool Activa { get; private set; }

    public static Categoria Crear(CategoriaId id, string nombre, string clave, string colorHex, int orden)
    {
        var nombreLimpio = nombre?.Trim() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            nombreLimpio.Length is > 0 and <= LongitudMaximaNombre,
            "categoria.nombre_invalido",
            $"El nombre de la categoría debe tener entre 1 y {LongitudMaximaNombre} caracteres.");

        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(clave),
            "categoria.clave_invalida",
            "La categoría necesita una clave.");

        return new Categoria(id, nombreLimpio, clave.Trim().ToLowerInvariant(), colorHex, orden);
    }

    public void Desactivar() => Activa = false;

    public void Reactivar() => Activa = true;
}
