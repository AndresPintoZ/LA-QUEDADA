using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using PlanVibe.Application.Common;
using PlanVibe.Application.Identidad.Comandos;
using PlanVibe.Application.Quedadas;
using PlanVibe.Application.Quedadas.Comandos;
using PlanVibe.Application.Quedadas.Consultas;

namespace PlanVibe.Application;

/// <summary>Registro en el contenedor de todo lo que aporta la capa de aplicación.</summary>
public static class InyeccionDeDependencias
{
    /// <summary>
    /// Registra los casos de uso, sus validadores y el decorador de validación.
    /// </summary>
    /// <remarks>
    /// Los manejadores se registran uno a uno, sin escaneo automático del ensamblado. Es algo más
    /// verboso, pero significa que este archivo es la lista completa de lo que la aplicación sabe
    /// hacer: se lee de un vistazo y no depende de convenciones de nombres invisibles.
    /// </remarks>
    public static IServiceCollection AgregarCapaDeAplicacion(this IServiceCollection servicios)
    {
        ArgumentNullException.ThrowIfNull(servicios);

        servicios.AddValidatorsFromAssemblyContaining<RegistrarUsuarioValidador>(includeInternalTypes: false);

        // --- Identidad ---
        servicios.AgregarComando<RegistrarUsuario, Guid, RegistrarUsuarioManejador>();
        servicios.AgregarComando<IniciarSesion, SesionIniciada, IniciarSesionManejador>();
        servicios.AgregarComando<RenovarSesion, Abstracciones.ParDeTokens, RenovarSesionManejador>();
        servicios.AgregarComando<CerrarSesion, bool, CerrarSesionManejador>();
        servicios.AgregarComando<IniciarVerificacion, Abstracciones.SesionDeVerificacion, IniciarVerificacionManejador>();
        servicios.AgregarComando<CompletarVerificacion, string, CompletarVerificacionManejador>();
        servicios.AgregarConsulta<ObtenerMiPerfil, PerfilDeSesion, ObtenerMiPerfilManejador>();

        // --- Quedadas ---
        servicios.AgregarComando<CrearQuedada, Guid, CrearQuedadaManejador>();
        servicios.AgregarComando<ApuntarseAQuedada, ResultadoDeInscripcion, ApuntarseAQuedadaManejador>();
        servicios.AgregarComando<AbandonarQuedada, bool, AbandonarQuedadaManejador>();
        servicios.AgregarComando<CancelarQuedada, bool, CancelarQuedadaManejador>();
        servicios.AgregarConsulta<BuscarPlanes, PaginaDe<ResumenDePlan>, BuscarPlanesManejador>();
        servicios.AgregarConsulta<ObtenerPlan, DetalleDePlan, ObtenerPlanManejador>();
        servicios.AgregarConsulta<ObtenerMisPlanes, IReadOnlyList<ResumenDePlan>, ObtenerMisPlanesManejador>();

        return servicios;
    }

    /// <summary>
    /// Registra un manejador de comando envuelto siempre en el decorador de validación.
    /// </summary>
    private static IServiceCollection AgregarComando<TComando, TResultado, TManejador>(this IServiceCollection servicios)
        where TComando : IComando<TResultado>
        where TManejador : class, IManejadorDeComando<TComando, TResultado>
    {
        servicios.TryAddScoped<TManejador>();

        servicios.AddScoped<IManejadorDeComando<TComando, TResultado>>(proveedor =>
            new ManejadorConValidacion<TComando, TResultado>(
                proveedor.GetRequiredService<TManejador>(),
                proveedor.GetServices<IValidator<TComando>>()));

        return servicios;
    }

    /// <summary>Las consultas no se validan: no modifican nada y sus topes los aplica el manejador.</summary>
    private static IServiceCollection AgregarConsulta<TConsulta, TResultado, TManejador>(this IServiceCollection servicios)
        where TConsulta : IConsulta<TResultado>
        where TManejador : class, IManejadorDeConsulta<TConsulta, TResultado>
    {
        servicios.AddScoped<IManejadorDeConsulta<TConsulta, TResultado>, TManejador>();

        return servicios;
    }
}
