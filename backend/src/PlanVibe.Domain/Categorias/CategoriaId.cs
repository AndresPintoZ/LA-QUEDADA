namespace PlanVibe.Domain.Categorias;

/// <summary>
/// Identificador de categoría. Las categorías son un catálogo que administra el rol Administrador,
/// no un enumerado del código: añadir una categoría no debe exigir un despliegue.
/// </summary>
/// <remarks>
/// Las constantes de abajo son las categorías iniciales del piloto descritas en
/// <c>docs/00-vision-y-mvp.md</c>. Existen como identificadores estables para la carga inicial
/// de datos y para las pruebas; la aplicación siempre las lee de la base de datos.
/// </remarks>
public readonly record struct CategoriaId(Guid Valor)
{
    public static CategoriaId Nuevo() => new(Guid.CreateVersion7());

    public static CategoriaId SenderismoYNaturaleza { get; } = new(Guid.Parse("0195c1a0-0001-7000-8000-000000000001"));

    public static CategoriaId BiciYDeporte { get; } = new(Guid.Parse("0195c1a0-0002-7000-8000-000000000002"));

    public static CategoriaId Musica { get; } = new(Guid.Parse("0195c1a0-0003-7000-8000-000000000003"));

    public static CategoriaId CulturaYOcio { get; } = new(Guid.Parse("0195c1a0-0004-7000-8000-000000000004"));

    public static CategoriaId JuegosYCreatividad { get; } = new(Guid.Parse("0195c1a0-0005-7000-8000-000000000005"));

    public override string ToString() => Valor.ToString();
}
