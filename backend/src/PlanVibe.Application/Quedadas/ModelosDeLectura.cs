using PlanVibe.Domain.Quedadas;

namespace PlanVibe.Application.Quedadas;

/// <summary>
/// Lo que necesita una tarjeta de la lista de explorar. Nada más.
/// </summary>
/// <remarks>
/// Es un modelo de lectura, no el agregado: la infraestructura lo proyecta directamente desde SQL
/// sin materializar quedadas ni asistencias. Devolver el agregado en un listado obligaría a cargar
/// todas las asistencias de todas las quedadas para acabar mostrando un contador.
/// </remarks>
/// <param name="DistanciaEnMetros">Distancia al punto de referencia de la búsqueda, si se indicó.</param>
public sealed record ResumenDePlan(
    Guid Id,
    string Titulo,
    string Categoria,
    DateTimeOffset Inicio,
    string Lugar,
    double Latitud,
    double Longitud,
    double? DistanciaEnMetros,
    int Capacidad,
    int PlazasOcupadas,
    EstadoQuedada Estado,
    string OrganizadorNombre,
    bool OrganizadorVerificado);

/// <summary>
/// Detalle completo de un plan.
/// </summary>
/// <remarks>
/// <see cref="DireccionExacta"/> llega con valor solo si quien consulta tiene plaza confirmada.
/// La decisión la toma el agregado (<see cref="Quedada.DireccionExactaVisiblePara"/>), no la vista.
/// </remarks>
public sealed record DetalleDePlan(
    Guid Id,
    string Titulo,
    string Descripcion,
    string Categoria,
    DateTimeOffset Inicio,
    DateTimeOffset Fin,
    string Lugar,
    string? Referencia,
    string? DireccionExacta,
    double Latitud,
    double Longitud,
    int Capacidad,
    int PlazasOcupadas,
    int EnListaDeEspera,
    EstadoQuedada Estado,
    string? MotivoDeCancelacion,
    IReadOnlyList<string> Normas,
    OrganizadorDeLectura Organizador,
    EstadoAsistencia? MiAsistencia,
    int? MiPosicionEnListaDeEspera);

/// <param name="Iniciales">Se calculan en servidor para no enviar el nombre completo cuando solo se pinta el avatar.</param>
public sealed record OrganizadorDeLectura(Guid Id, string Nombre, string Iniciales, bool Verificado, int QuedadasOrganizadas);

/// <summary>Página de resultados con el total, para que la interfaz pueda paginar.</summary>
public sealed record PaginaDe<T>(IReadOnlyList<T> Elementos, int Total, int Pagina, int TamanoDePagina)
{
    public bool HayMas => Pagina * TamanoDePagina < Total;
}
