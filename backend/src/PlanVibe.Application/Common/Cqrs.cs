namespace PlanVibe.Application.Common;

/// <summary>
/// Intención de cambiar el estado del sistema: crear una quedada, apuntarse, cancelar.
/// </summary>
/// <typeparam name="TResultado">Lo que devuelve la operación al terminar.</typeparam>
/// <remarks>
/// Comandos y consultas se separan porque tienen necesidades distintas: los comandos protegen
/// invariantes y pasan por el dominio; las consultas solo leen y pueden ir directas a la base de
/// datos con la forma que la pantalla necesita, sin materializar agregados completos.
/// </remarks>
public interface IComando<TResultado>;

/// <summary>Ejecuta un comando concreto. Un manejador por caso de uso, sin excepciones.</summary>
public interface IManejadorDeComando<in TComando, TResultado>
    where TComando : IComando<TResultado>
{
    public Task<TResultado> ManejarAsync(TComando comando, CancellationToken cancelacion);
}

/// <summary>Petición de datos que no modifica nada.</summary>
public interface IConsulta<TResultado>;

/// <summary>Resuelve una consulta concreta.</summary>
public interface IManejadorDeConsulta<in TConsulta, TResultado>
    where TConsulta : IConsulta<TResultado>
{
    public Task<TResultado> ManejarAsync(TConsulta consulta, CancellationToken cancelacion);
}
