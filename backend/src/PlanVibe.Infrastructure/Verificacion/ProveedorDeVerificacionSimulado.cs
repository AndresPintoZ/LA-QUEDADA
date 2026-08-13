using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Infrastructure.Verificacion;

/// <summary>Configuración del proveedor de verificación simulado.</summary>
public sealed class OpcionesDeVerificacionSimulada
{
    public const string Seccion = "VerificacionSimulada";

    /// <summary>
    /// Si está activo. Debe ser <c>false</c> en cualquier entorno que no sea de desarrollo.
    /// </summary>
    /// <remarks>
    /// La comprobación no se deja a la configuración: el registro de dependencias se niega a
    /// activar este proveedor fuera de desarrollo aunque alguien ponga aquí <c>true</c>.
    /// </remarks>
    public bool Activo { get; init; }

    /// <summary>Dónde vuelve la persona tras «completar» la verificación simulada.</summary>
    public string UrlDeRetorno { get; init; } = "http://localhost:3000/verificacion/resultado";
}

/// <summary>
/// Proveedor de verificación de mentira para desarrollo y pruebas.
/// </summary>
/// <remarks>
/// <para>
/// Aprueba todas las solicitudes. Existe para que el flujo completo —solicitar, redirigir, volver
/// y obtener el rol de organizador— se pueda recorrer en local sin contratar un proveedor real
/// ni manejar documentos de identidad de nadie.
/// </para>
/// <para>
/// Antes de abrir el registro a personas reales hay que sustituirlo por un proveedor de verdad.
/// La interfaz <see cref="IProveedorDeVerificacion"/> está pensada para que ese cambio afecte
/// solo a esta carpeta: ni el dominio ni los casos de uso saben quién verifica.
/// Ver el paso 1 de «Próximos pasos» en <c>docs/04-seguridad-privacidad-moderacion.md</c>.
/// </para>
/// </remarks>
public sealed class ProveedorDeVerificacionSimulado(
    IOptions<OpcionesDeVerificacionSimulada> opciones,
    ILogger<ProveedorDeVerificacionSimulado> registro) : IProveedorDeVerificacion
{
    private readonly OpcionesDeVerificacionSimulada _opciones = opciones.Value;

    public string Nombre => "simulado-desarrollo";

    public Task<SesionDeVerificacion> IniciarAsync(UsuarioId usuarioId, CancellationToken cancelacion)
    {
        var referencia = $"sim-{Guid.CreateVersion7():N}";

        registro.VerificacionSimuladaIniciada(usuarioId);

        return Task.FromResult(new SesionDeVerificacion(
            referencia,
            $"{_opciones.UrlDeRetorno}?referencia={Uri.EscapeDataString(referencia)}"));
    }

    public Task<ResultadoDeVerificacion> ConsultarResultadoAsync(string referenciaExterna, CancellationToken cancelacion) =>
        Task.FromResult(new ResultadoDeVerificacion(
            EstadoVerificacion.Verificada,
            MayoriaDeEdadConfirmada: true,
            Observacion: "Verificación simulada de desarrollo. No comprueba ninguna identidad real."));
}
