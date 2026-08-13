using PlanVibe.Domain.Common;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Domain.Quedadas;

/// <summary>
/// Relación entre una persona y una quedada. Es una entidad hija dentro del agregado
/// <see cref="Quedada"/>: solo se crea y se modifica a través de la quedada, nunca por su cuenta.
/// Esa restricción es la que garantiza que jamás se supere la capacidad.
/// </summary>
public sealed class Asistencia : Entidad<Guid>
{
    private Asistencia(Guid id, UsuarioId usuarioId, EstadoAsistencia estado, long ordenDeLlegada, DateTimeOffset solicitadaEn)
        : base(id)
    {
        UsuarioId = usuarioId;
        Estado = estado;
        OrdenDeLlegada = ordenDeLlegada;
        SolicitadaEn = solicitadaEn;
        ActualizadaEn = solicitadaEn;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Asistencia()
    {
    }

    public UsuarioId UsuarioId { get; private set; }

    public EstadoAsistencia Estado { get; private set; }

    /// <summary>
    /// Turno de llegada dentro de la quedada. Ordena la lista de espera de forma estable
    /// aunque dos personas se apunten en el mismo segundo, cosa que una marca de tiempo no garantiza.
    /// </summary>
    public long OrdenDeLlegada { get; private set; }

    public DateTimeOffset SolicitadaEn { get; private set; }

    public DateTimeOffset ActualizadaEn { get; private set; }

    /// <summary>Está participando, ya sea con plaza o esperando.</summary>
    public bool EstaActiva => Estado is EstadoAsistencia.Confirmada or EstadoAsistencia.EnListaDeEspera;

    internal static Asistencia Crear(UsuarioId usuarioId, EstadoAsistencia estado, long ordenDeLlegada, DateTimeOffset ahora) =>
        new(Guid.CreateVersion7(), usuarioId, estado, ordenDeLlegada, ahora);

    internal void CambiarEstado(EstadoAsistencia nuevoEstado, DateTimeOffset ahora)
    {
        Estado = nuevoEstado;
        ActualizadaEn = ahora;
    }

    /// <summary>
    /// Reactiva una asistencia retirada en lugar de crear una fila nueva. Mantener una sola fila
    /// por persona y quedada permite un índice único en base de datos, que es la última barrera
    /// contra una doble inscripción por dos peticiones simultáneas.
    /// </summary>
    internal void Reactivar(EstadoAsistencia estado, long ordenDeLlegada, DateTimeOffset ahora)
    {
        Estado = estado;
        OrdenDeLlegada = ordenDeLlegada;
        ActualizadaEn = ahora;
    }
}
