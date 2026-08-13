using FluentValidation;

namespace PlanVibe.Application.Common;

/// <summary>
/// Envuelve un manejador de comando y valida la entrada antes de ejecutarlo.
/// </summary>
/// <remarks>
/// <para>
/// Se resuelve con el patrón decorador en lugar de con una llamada a validar dentro de cada
/// manejador. La diferencia importa: aquí es imposible olvidarse de validar un comando nuevo,
/// porque la validación la aplica el contenedor de dependencias, no la disciplina de quien programa.
/// </para>
/// <para>
/// Si no hay validador registrado para un comando, sencillamente no se valida. Es intencionado:
/// muchos comandos no tienen datos de entrada que comprobar.
/// </para>
/// </remarks>
public sealed class ManejadorConValidacion<TComando, TResultado>(
    IManejadorDeComando<TComando, TResultado> interno,
    IEnumerable<IValidator<TComando>> validadores) : IManejadorDeComando<TComando, TResultado>
    where TComando : IComando<TResultado>
{
    public async Task<TResultado> ManejarAsync(TComando comando, CancellationToken cancelacion)
    {
        var errores = new Dictionary<string, List<string>>(StringComparer.Ordinal);

        foreach (var validador in validadores)
        {
            var resultado = await validador.ValidateAsync(comando, cancelacion);

            foreach (var fallo in resultado.Errors)
            {
                if (!errores.TryGetValue(fallo.PropertyName, out var lista))
                {
                    lista = [];
                    errores[fallo.PropertyName] = lista;
                }

                lista.Add(fallo.ErrorMessage);
            }
        }

        if (errores.Count > 0)
        {
            // Se devuelven todos los errores juntos: rellenar un formulario campo a campo,
            // recargando en cada intento, es una mala experiencia y no aporta nada.
            throw new ValidacionException(errores.ToDictionary(e => e.Key, e => e.Value.ToArray(), StringComparer.Ordinal));
        }

        return await interno.ManejarAsync(comando, cancelacion);
    }
}
