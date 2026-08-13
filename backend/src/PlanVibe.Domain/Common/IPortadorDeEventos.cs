namespace PlanVibe.Domain.Common;

/// <summary>
/// Algo que acumula eventos de dominio pendientes de publicar.
/// </summary>
/// <remarks>
/// Existe para que la unidad de trabajo pueda recorrer las entidades que está rastreando y
/// recoger sus eventos sin conocer el tipo concreto de cada agregado ni el tipo de su
/// identificador. Sin esta interfaz habría que comprobar contra
/// <c>RaizDeAgregado&lt;TId&gt;</c>, que es genérica y por tanto no sirve para ese recorrido.
/// </remarks>
public interface IPortadorDeEventos
{
    public IReadOnlyCollection<IEventoDeDominio> EventosDeDominio { get; }

    public void LimpiarEventos();
}
