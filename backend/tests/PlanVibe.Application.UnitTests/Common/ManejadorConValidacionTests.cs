using FluentValidation;
using PlanVibe.Application.Common;
using Shouldly;

namespace PlanVibe.Application.UnitTests.Common;

/// <summary>
/// El decorador de validación es la garantía de que ningún comando llega al dominio sin
/// comprobarse. Estas pruebas protegen justo eso.
/// </summary>
public class ManejadorConValidacionTests
{
    [Fact]
    public async Task Un_comando_valido_llega_al_manejador_interno()
    {
        var interno = new ManejadorDePrueba();
        var decorado = new ManejadorConValidacion<ComandoDePrueba, string>(interno, [new ValidadorDePrueba()]);

        var resultado = await decorado.ManejarAsync(new ComandoDePrueba("un valor correcto"), TestContext.Current.CancellationToken);

        resultado.ShouldBe("procesado: un valor correcto");
        interno.VecesLlamado.ShouldBe(1);
    }

    [Fact]
    public async Task Un_comando_invalido_no_llega_al_manejador_interno()
    {
        var interno = new ManejadorDePrueba();
        var decorado = new ManejadorConValidacion<ComandoDePrueba, string>(interno, [new ValidadorDePrueba()]);

        await Should.ThrowAsync<ValidacionException>(
            () => decorado.ManejarAsync(new ComandoDePrueba(""), TestContext.Current.CancellationToken));

        interno.VecesLlamado.ShouldBe(0, "un comando inválido no debe tocar el dominio");
    }

    [Fact]
    public async Task Los_errores_se_devuelven_todos_juntos_y_agrupados_por_campo()
    {
        var decorado = new ManejadorConValidacion<ComandoDePrueba, string>(
            new ManejadorDePrueba(),
            [new ValidadorDePrueba(), new SegundoValidadorDePrueba()]);

        var error = await Should.ThrowAsync<ValidacionException>(
            () => decorado.ManejarAsync(new ComandoDePrueba(""), TestContext.Current.CancellationToken));

        // Rellenar un formulario campo a campo, con una recarga por error, es una mala
        // experiencia: se devuelven todos de una vez.
        error.Errores.ShouldContainKey(nameof(ComandoDePrueba.Valor));
        error.Errores[nameof(ComandoDePrueba.Valor)].Length.ShouldBe(2);
    }

    [Fact]
    public async Task Sin_validadores_registrados_el_comando_pasa_sin_validarse()
    {
        // Muchos comandos no tienen datos de entrada que comprobar; no es un error.
        var interno = new ManejadorDePrueba();
        var decorado = new ManejadorConValidacion<ComandoDePrueba, string>(interno, []);

        await decorado.ManejarAsync(new ComandoDePrueba(""), TestContext.Current.CancellationToken);

        interno.VecesLlamado.ShouldBe(1);
    }

    private sealed record ComandoDePrueba(string Valor) : IComando<string>;

    private sealed class ManejadorDePrueba : IManejadorDeComando<ComandoDePrueba, string>
    {
        public int VecesLlamado { get; private set; }

        public Task<string> ManejarAsync(ComandoDePrueba comando, CancellationToken cancelacion)
        {
            VecesLlamado++;

            return Task.FromResult($"procesado: {comando.Valor}");
        }
    }

    private sealed class ValidadorDePrueba : AbstractValidator<ComandoDePrueba>
    {
        public ValidadorDePrueba() => RuleFor(c => c.Valor).NotEmpty().WithMessage("El valor es obligatorio.");
    }

    private sealed class SegundoValidadorDePrueba : AbstractValidator<ComandoDePrueba>
    {
        public SegundoValidadorDePrueba() => RuleFor(c => c.Valor).MinimumLength(3).WithMessage("Demasiado corto.");
    }
}
