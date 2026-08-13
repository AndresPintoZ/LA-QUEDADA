using Microsoft.EntityFrameworkCore;
using PlanVibe.Domain.Categorias;
using PlanVibe.Infrastructure;
using PlanVibe.Infrastructure.Persistencia;

namespace PlanVibe.Api;

/// <summary>
/// Aplica las migraciones y siembra el catálogo mínimo al arrancar en desarrollo.
/// </summary>
/// <remarks>
/// Solo se invoca en desarrollo. En producción, migrar desde el arranque de la aplicación es
/// peligroso: si escalan varias instancias, todas intentan modificar el esquema a la vez, y un
/// despliegue fallido puede dejar la base de datos a medio migrar sin nadie mirando. Allí las
/// migraciones son un paso explícito y observable del despliegue.
/// </remarks>
public static class PreparacionDeBaseDeDatos
{
    public static async Task PrepararBaseDeDatosAsync(this WebApplication aplicacion)
    {
        ArgumentNullException.ThrowIfNull(aplicacion);

        using var ambito = aplicacion.Services.CreateScope();

        var contexto = ambito.ServiceProvider.GetRequiredService<PlanVibeDbContext>();

        await contexto.Database.MigrateAsync();
        await SembrarCategoriasAsync(contexto);

        aplicacion.Logger.BaseDeDatosPreparada();
    }

    /// <summary>
    /// Inserta las categorías iniciales del piloto si no existen.
    /// </summary>
    /// <remarks>
    /// Son las cinco descritas en <c>docs/00-vision-y-mvp.md</c>. Se comprueba antes de insertar
    /// para que arrancar la aplicación dos veces no duplique el catálogo ni pise los cambios que
    /// haya hecho administración.
    /// </remarks>
    private static async Task SembrarCategoriasAsync(PlanVibeDbContext contexto)
    {
        if (await contexto.Categorias.AnyAsync())
        {
            return;
        }

        // Los colores salen del sistema visual de los mockups (frontend/tailwind.config.ts).
        // Nunca son el único indicador de nada: siempre acompañan al nombre.
        Categoria[] iniciales =
        [
            Categoria.Crear(CategoriaId.SenderismoYNaturaleza, "Senderismo y naturaleza", "senderismo", "#1E8A5F", 1),
            Categoria.Crear(CategoriaId.BiciYDeporte, "Bici y deporte", "bici-y-deporte", "#0B7C9B", 2),
            Categoria.Crear(CategoriaId.Musica, "Música", "musica", "#8A5A0B", 3),
            Categoria.Crear(CategoriaId.CulturaYOcio, "Cultura y ocio", "cultura", "#075E77", 4),
            Categoria.Crear(CategoriaId.JuegosYCreatividad, "Juegos y creatividad", "juegos-y-creativo", "#6E827D", 5),
        ];

        contexto.Categorias.AddRange(iniciales);

        await contexto.SaveChangesAsync();
    }
}
