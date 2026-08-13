using PlanVibe.Application.Quedadas;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Application.Abstracciones;

/// <summary>
/// Lado de lectura de las quedadas: proyecciones a medida de cada pantalla.
/// </summary>
/// <remarks>
/// Está separado de <see cref="IRepositorioDeQuedadas"/> porque leer para mostrar y leer para
/// modificar son problemas distintos. Aquí no hay agregados ni invariantes que proteger: hay
/// consultas que deben ser rápidas y devolver justo lo que la pantalla pinta.
/// </remarks>
public interface IConsultasDeQuedadas
{
    /// <summary>Busca planes aplicando los filtros de explorar (RF-05, RF-06).</summary>
    public Task<PaginaDe<ResumenDePlan>> BuscarAsync(FiltroDeBusqueda filtro, CancellationToken cancelacion);

    /// <summary>
    /// Detalle de un plan (RF-07). <paramref name="solicitanteId"/> decide qué se revela:
    /// la dirección exacta y el estado de la propia asistencia solo se incluyen si procede.
    /// </summary>
    public Task<DetalleDePlan?> ObtenerDetalleAsync(QuedadaId id, UsuarioId? solicitanteId, CancellationToken cancelacion);

    /// <summary>Planes a los que una persona se ha apuntado o que ha organizado («Mis planes»).</summary>
    public Task<IReadOnlyList<ResumenDePlan>> ObtenerMisPlanesAsync(UsuarioId usuarioId, CancellationToken cancelacion);
}

/// <summary>
/// Criterios de la pantalla de explorar.
/// </summary>
/// <remarks>
/// Los valores por defecto están pensados para el piloto de Ávila: quien abre la aplicación
/// quiere ver qué hay cerca y pronto, no todo el catálogo.
/// </remarks>
/// <param name="Texto">Búsqueda libre sobre título y descripción.</param>
/// <param name="CategoriaIds">Categorías seleccionadas; vacío significa todas.</param>
/// <param name="Desde">Límite inferior de fecha. Si no se indica, desde ahora.</param>
/// <param name="Hasta">Límite superior de fecha.</param>
/// <param name="Latitud">Centro de la búsqueda por cercanía.</param>
/// <param name="Longitud">Centro de la búsqueda por cercanía.</param>
/// <param name="RadioEnMetros">Radio máximo desde el centro.</param>
/// <param name="SoloConPlazas">Excluye los planes completos.</param>
/// <param name="Pagina">Número de página, empezando en 1.</param>
/// <param name="TamanoDePagina">Elementos por página; la implementación aplica un tope.</param>
public sealed record FiltroDeBusqueda(
    string? Texto = null,
    IReadOnlyList<Guid>? CategoriaIds = null,
    DateTimeOffset? Desde = null,
    DateTimeOffset? Hasta = null,
    double? Latitud = null,
    double? Longitud = null,
    int? RadioEnMetros = null,
    bool SoloConPlazas = false,
    int Pagina = 1,
    int TamanoDePagina = 20)
{
    /// <summary>
    /// Tope de resultados por página. Existe para que nadie pueda pedir cien mil filas de una vez
    /// y convertir una consulta pública en una denegación de servicio.
    /// </summary>
    public const int TamanoMaximoDePagina = 50;

    public const int RadioMaximoEnMetros = 100_000;
}
