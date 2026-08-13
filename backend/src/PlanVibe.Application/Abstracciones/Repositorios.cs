using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Application.Abstracciones;

/// <summary>
/// Acceso a los agregados <see cref="Usuario"/>.
/// </summary>
/// <remarks>
/// Un repositorio por agregado y solo métodos que devuelven agregados completos. Las consultas de
/// listados y pantallas no pasan por aquí: van por <see cref="IConsultasDeQuedadas"/>, que devuelve
/// proyecciones a medida. Mezclar ambas cosas es lo que convierte un repositorio en un cajón de sastre.
/// </remarks>
public interface IRepositorioDeUsuarios
{
    public Task<Usuario?> ObtenerPorIdAsync(UsuarioId id, CancellationToken cancelacion);

    public Task<Usuario?> ObtenerPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion);

    public Task<bool> ExisteCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion);

    public Task AgregarAsync(Usuario usuario, CancellationToken cancelacion);
}

/// <summary>Acceso a los agregados <see cref="Quedada"/>.</summary>
public interface IRepositorioDeQuedadas
{
    /// <summary>Carga la quedada con todas sus asistencias: el agregado se lee entero o no se lee.</summary>
    public Task<Quedada?> ObtenerPorIdAsync(QuedadaId id, CancellationToken cancelacion);

    public Task AgregarAsync(Quedada quedada, CancellationToken cancelacion);
}

/// <summary>
/// Confirma la transacción y publica los eventos de dominio acumulados.
/// </summary>
/// <remarks>
/// El orden importa: primero se confirma en base de datos y después se publican los eventos.
/// Publicar antes podría enviar el correo de «te has apuntado» de una inscripción que luego
/// se revierte por un fallo de escritura.
/// </remarks>
public interface IUnidadDeTrabajo
{
    public Task<int> GuardarCambiosAsync(CancellationToken cancelacion);
}
