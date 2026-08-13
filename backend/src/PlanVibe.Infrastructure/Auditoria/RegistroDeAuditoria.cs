using System.Text.Json;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Infrastructure.Persistencia;

namespace PlanVibe.Infrastructure.Auditoria;

/// <summary>
/// Escribe en la tabla de auditoría (RNF-04).
/// </summary>
/// <remarks>
/// La entrada se añade al contexto sin guardar: se confirma con la misma transacción que la
/// acción que la origina. Así no puede quedar registrada una publicación que después falló,
/// ni perderse el rastro de una que sí ocurrió.
/// </remarks>
public sealed class RegistroDeAuditoria(PlanVibeDbContext contexto, TimeProvider reloj) : IRegistroDeAuditoria
{
    private static readonly JsonSerializerOptions OpcionesDeJson = new(JsonSerializerDefaults.Web);

    public async Task RegistrarAsync(
        UsuarioId? actorId,
        string accion,
        string tipoDeObjeto,
        string objetoId,
        IReadOnlyDictionary<string, string>? metadatos,
        CancellationToken cancelacion)
    {
        var entrada = new EntradaDeAuditoria
        {
            Id = Guid.CreateVersion7(),
            ActorId = actorId?.Valor,
            Accion = accion,
            TipoDeObjeto = tipoDeObjeto,
            ObjetoId = objetoId,
            Metadatos = metadatos is { Count: > 0 } ? JsonSerializer.Serialize(metadatos, OpcionesDeJson) : null,
            OcurridoEn = reloj.GetUtcNow(),
        };

        await contexto.Auditoria.AddAsync(entrada, cancelacion);
        await contexto.SaveChangesAsync(cancelacion);
    }
}
