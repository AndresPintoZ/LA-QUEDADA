namespace PlanVibe.Domain.Common;

/// <summary>
/// Raíz de agregado: única puerta de entrada para modificar el agregado y frontera de consistencia.
/// Todo lo que se guarda en una transacción debe pertenecer a un solo agregado.
/// </summary>
/// <remarks>
/// Los eventos de dominio se acumulan aquí y los publica la unidad de trabajo <em>después</em>
/// de confirmar la transacción. Así nunca se notifica algo que luego se revierte.
/// </remarks>
public abstract class RaizDeAgregado<TId> : Entidad<TId>, IPortadorDeEventos
    where TId : struct
{
    private readonly List<IEventoDeDominio> _eventos = [];

    protected RaizDeAgregado(TId id) : base(id)
    {
    }

    protected RaizDeAgregado()
    {
    }

    /// <summary>Eventos pendientes de publicar. Solo lectura para quien está fuera del agregado.</summary>
    public IReadOnlyCollection<IEventoDeDominio> EventosDeDominio => _eventos.AsReadOnly();

    /// <summary>
    /// Marca de concurrencia optimista. Si dos personas se apuntan al último hueco a la vez,
    /// la segunda transacción falla en lugar de sobrepasar la capacidad.
    /// </summary>
    public uint VersionFila { get; protected set; }

    protected void RegistrarEvento(IEventoDeDominio evento) => _eventos.Add(evento);

    public void LimpiarEventos() => _eventos.Clear();
}
