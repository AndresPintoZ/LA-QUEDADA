using Microsoft.EntityFrameworkCore;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Common;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Infrastructure.Persistencia;

/// <inheritdoc cref="IRepositorioDeUsuarios"/>
public sealed class RepositorioDeUsuarios(PlanVibeDbContext contexto) : IRepositorioDeUsuarios
{
    public Task<Usuario?> ObtenerPorIdAsync(UsuarioId id, CancellationToken cancelacion) =>
        contexto.Usuarios.FirstOrDefaultAsync(u => u.Id == id, cancelacion);

    public Task<Usuario?> ObtenerPorCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion) =>
        contexto.Usuarios.FirstOrDefaultAsync(u => u.Correo == correo, cancelacion);

    public Task<bool> ExisteCorreoAsync(CorreoElectronico correo, CancellationToken cancelacion) =>
        contexto.Usuarios.AnyAsync(u => u.Correo == correo, cancelacion);

    public async Task AgregarAsync(Usuario usuario, CancellationToken cancelacion) =>
        await contexto.Usuarios.AddAsync(usuario, cancelacion);
}

/// <inheritdoc cref="IRepositorioDeQuedadas"/>
public sealed class RepositorioDeQuedadas(PlanVibeDbContext contexto) : IRepositorioDeQuedadas
{
    /// <summary>
    /// Carga la quedada con todas sus asistencias.
    /// </summary>
    /// <remarks>
    /// El <c>Include</c> no es optimizable: un agregado se lee entero o no se lee. Sin las
    /// asistencias, el agregado no podría contar las plazas ocupadas y decidiría mal.
    /// </remarks>
    public Task<Quedada?> ObtenerPorIdAsync(QuedadaId id, CancellationToken cancelacion) =>
        contexto.Quedadas
            .Include(q => q.Asistencias)
            .FirstOrDefaultAsync(q => q.Id == id, cancelacion);

    public async Task AgregarAsync(Quedada quedada, CancellationToken cancelacion) =>
        await contexto.Quedadas.AddAsync(quedada, cancelacion);
}

/// <summary>
/// Confirma la transacción y, solo después, publica los eventos de dominio acumulados.
/// </summary>
/// <remarks>
/// <para>
/// El orden es lo importante. Publicar los eventos antes de confirmar significaría enviar el
/// correo de «te has apuntado» de una inscripción que después falla al escribirse.
/// </para>
/// <para>
/// Los eventos se recogen y se limpian <em>antes</em> de guardar porque, si se hiciera después,
/// cualquier nueva instancia cargada durante el guardado podría alterar la lista mientras se recorre.
/// </para>
/// </remarks>
public sealed class UnidadDeTrabajo(
    PlanVibeDbContext contexto,
    IPublicadorDeEventos publicador) : IUnidadDeTrabajo
{
    public async Task<int> GuardarCambiosAsync(CancellationToken cancelacion)
    {
        var eventos = RecogerYLimpiarEventos();

        int filasAfectadas;

        try
        {
            filasAfectadas = await contexto.SaveChangesAsync(cancelacion);
        }
        catch (DbUpdateConcurrencyException excepcion)
        {
            // Dos personas han modificado la misma quedada a la vez. Se traduce a un conflicto
            // que la API convierte en 409 para que el cliente pueda reintentar con datos frescos.
            throw new ConflictoException(
                "concurrencia.conflicto",
                "Alguien ha modificado este plan mientras lo hacías tú. Vuelve a intentarlo.")
            {
                Source = excepcion.Source,
            };
        }

        await publicador.PublicarAsync(eventos, cancelacion);

        return filasAfectadas;
    }

    private List<IEventoDeDominio> RecogerYLimpiarEventos()
    {
        var raices = contexto.ChangeTracker
            .Entries()
            .Select(e => e.Entity)
            .OfType<IPortadorDeEventos>()
            .Where(r => r.EventosDeDominio.Count > 0)
            .ToList();

        var eventos = raices.SelectMany(r => r.EventosDeDominio).ToList();

        foreach (var raiz in raices)
        {
            raiz.LimpiarEventos();
        }

        return eventos;
    }
}
