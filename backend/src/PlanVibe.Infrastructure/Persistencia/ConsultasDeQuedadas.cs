using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Quedadas;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Infrastructure.Persistencia.Configuraciones;

namespace PlanVibe.Infrastructure.Persistencia;

/// <summary>
/// Lado de lectura: proyecta directamente a los modelos que consume la interfaz.
/// </summary>
/// <remarks>
/// <para>
/// Todas las consultas usan <c>AsNoTracking</c>: no van a modificar nada, así que no tiene
/// sentido que EF guarde copias para detectar cambios.
/// </para>
/// <para>
/// El filtro por cercanía se apoya en la columna geográfica calculada y en su índice GIST.
/// Calcular la distancia en memoria obligaría a traerse todas las quedadas de la base de datos
/// para descartar la mayoría.
/// </para>
/// </remarks>
public sealed class ConsultasDeQuedadas(PlanVibeDbContext contexto) : IConsultasDeQuedadas
{
    public async Task<PaginaDe<ResumenDePlan>> BuscarAsync(FiltroDeBusqueda filtro, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(filtro);

        var consulta = contexto.Quedadas.AsNoTracking().AsQueryable();

        // Solo se listan planes publicados: los cancelados y los ocultos por moderación
        // no aparecen en explorar, aunque sigan siendo accesibles por enlace directo
        // para quien ya estaba apuntado.
        consulta = consulta.Where(q => q.Estado == EstadoQuedada.Publicada);

        if (filtro.Desde is { } desde)
        {
            consulta = consulta.Where(q => q.Cuando.Inicio >= desde);
        }

        if (filtro.Hasta is { } hasta)
        {
            consulta = consulta.Where(q => q.Cuando.Inicio <= hasta);
        }

        if (filtro.CategoriaIds is { Count: > 0 } categorias)
        {
            consulta = consulta.Where(q => categorias.Contains(q.CategoriaId.Valor));
        }

        if (!string.IsNullOrWhiteSpace(filtro.Texto))
        {
            // ILike de PostgreSQL: comparación sin distinguir mayúsculas ni acentos según la
            // intercalación de la base de datos. El texto va parametrizado, no concatenado.
            var patron = $"%{filtro.Texto.Trim()}%";
            consulta = consulta.Where(q => EF.Functions.ILike(q.Titulo, patron) || EF.Functions.ILike(q.Descripcion, patron));
        }

        if (filtro.SoloConPlazas)
        {
            consulta = consulta.Where(q =>
                q.Asistencias.Count(a => a.Estado == EstadoAsistencia.Confirmada) < q.Capacidad.Maximo);
        }

        Point? centro = null;

        if (filtro is { Latitud: { } latitud, Longitud: { } longitud })
        {
            centro = new Point(longitud, latitud) { SRID = QuedadaConfiguracion.Srid };

            if (filtro.RadioEnMetros is { } radio)
            {
                consulta = consulta.Where(q =>
                    EF.Property<Point>(q, QuedadaConfiguracion.ColumnaUbicacion).Distance(centro) <= radio);
            }
        }

        var total = await consulta.CountAsync(cancelacion);

        // Se ordena por cercanía si hay un centro de búsqueda; si no, por lo que ocurre antes.
        var ordenada = centro is null
            ? consulta.OrderBy(q => q.Cuando.Inicio)
            : consulta.OrderBy(q => EF.Property<Point>(q, QuedadaConfiguracion.ColumnaUbicacion).Distance(centro));

        var elementos = await ordenada
            .Skip((filtro.Pagina - 1) * filtro.TamanoDePagina)
            .Take(filtro.TamanoDePagina)
            .Select(q => new ResumenDePlan(
                q.Id.Valor,
                q.Titulo,
                contexto.Categorias.Where(c => c.Id == q.CategoriaId).Select(c => c.Nombre).FirstOrDefault() ?? string.Empty,
                q.Cuando.Inicio,
                q.Donde.Lugar,
                q.Donde.Latitud,
                q.Donde.Longitud,
                centro == null ? null : EF.Property<Point>(q, QuedadaConfiguracion.ColumnaUbicacion).Distance(centro),
                q.Capacidad.Maximo,
                q.Asistencias.Count(a => a.Estado == EstadoAsistencia.Confirmada),
                q.Estado,
                contexto.Usuarios.Where(u => u.Id == q.OrganizadorId).Select(u => u.NombreVisible.Valor).FirstOrDefault() ?? string.Empty,
                contexto.Usuarios.Any(u => u.Id == q.OrganizadorId && u.Verificacion.Estado == EstadoVerificacion.Verificada)))
            .ToListAsync(cancelacion);

        return new PaginaDe<ResumenDePlan>(elementos, total, filtro.Pagina, filtro.TamanoDePagina);
    }

    /// <summary>
    /// Detalle de un plan. La dirección exacta se resuelve preguntando al agregado, no aquí.
    /// </summary>
    public async Task<DetalleDePlan?> ObtenerDetalleAsync(QuedadaId id, UsuarioId? solicitanteId, CancellationToken cancelacion)
    {
        var quedada = await contexto.Quedadas
            .AsNoTracking()
            .Include(q => q.Asistencias)
            .FirstOrDefaultAsync(q => q.Id == id, cancelacion);

        if (quedada is null)
        {
            return null;
        }

        var categoria = await contexto.Categorias
            .AsNoTracking()
            .Where(c => c.Id == quedada.CategoriaId)
            .Select(c => c.Nombre)
            .FirstOrDefaultAsync(cancelacion) ?? string.Empty;

        var organizador = await contexto.Usuarios
            .AsNoTracking()
            .Where(u => u.Id == quedada.OrganizadorId)
            .Select(u => new
            {
                u.Id,
                Nombre = u.NombreVisible.Valor,
                Verificado = u.Verificacion.Estado == EstadoVerificacion.Verificada,
            })
            .FirstOrDefaultAsync(cancelacion);

        var quedadasOrganizadas = await contexto.Quedadas
            .AsNoTracking()
            .CountAsync(q => q.OrganizadorId == quedada.OrganizadorId, cancelacion);

        // Es el agregado quien decide si esta persona puede ver la dirección exacta.
        var direccionExacta = solicitanteId is { } quien ? quedada.DireccionExactaVisiblePara(quien) : null;
        var miAsistencia = solicitanteId is { } yo ? quedada.EstadoDe(yo) : null;

        return new DetalleDePlan(
            quedada.Id.Valor,
            quedada.Titulo,
            quedada.Descripcion,
            categoria,
            quedada.Cuando.Inicio,
            quedada.Cuando.Fin,
            quedada.Donde.Lugar,
            quedada.Donde.Referencia,
            direccionExacta,
            quedada.Donde.Latitud,
            quedada.Donde.Longitud,
            quedada.Capacidad.Maximo,
            quedada.PlazasOcupadas,
            quedada.EnListaDeEspera,
            quedada.Estado,
            quedada.MotivoDeCancelacion,
            [.. quedada.Normas],
            new OrganizadorDeLectura(
                organizador?.Id.Valor ?? Guid.Empty,
                organizador?.Nombre ?? "Organizador",
                CalcularIniciales(organizador?.Nombre),
                organizador?.Verificado ?? false,
                quedadasOrganizadas),
            miAsistencia,
            solicitanteId is { } persona ? quedada.PosicionEnListaDeEspera(persona) : null);
    }

    public async Task<IReadOnlyList<ResumenDePlan>> ObtenerMisPlanesAsync(UsuarioId usuarioId, CancellationToken cancelacion) =>
        await contexto.Quedadas
            .AsNoTracking()
            // Se incluyen los cancelados: quien se había apuntado necesita ver que su plan
            // se canceló, no que desapareció sin explicación (docs/03-diseno-visual.md).
            .Where(q => q.Asistencias.Any(a => a.UsuarioId == usuarioId && a.Estado != EstadoAsistencia.Retirada))
            .OrderBy(q => q.Cuando.Inicio)
            .Select(q => new ResumenDePlan(
                q.Id.Valor,
                q.Titulo,
                contexto.Categorias.Where(c => c.Id == q.CategoriaId).Select(c => c.Nombre).FirstOrDefault() ?? string.Empty,
                q.Cuando.Inicio,
                q.Donde.Lugar,
                q.Donde.Latitud,
                q.Donde.Longitud,
                null,
                q.Capacidad.Maximo,
                q.Asistencias.Count(a => a.Estado == EstadoAsistencia.Confirmada),
                q.Estado,
                contexto.Usuarios.Where(u => u.Id == q.OrganizadorId).Select(u => u.NombreVisible.Valor).FirstOrDefault() ?? string.Empty,
                contexto.Usuarios.Any(u => u.Id == q.OrganizadorId && u.Verificacion.Estado == EstadoVerificacion.Verificada)))
            .ToListAsync(cancelacion);

    /// <summary>
    /// Iniciales del avatar. Se calculan en servidor para que la interfaz no necesite
    /// el nombre completo cuando lo único que va a pintar es un círculo con dos letras.
    /// </summary>
    private static string CalcularIniciales(string? nombre)
    {
        if (string.IsNullOrWhiteSpace(nombre))
        {
            return "??";
        }

        var partes = nombre.Split(' ', StringSplitOptions.RemoveEmptyEntries);

        return partes.Length == 1
            ? partes[0][..Math.Min(2, partes[0].Length)].ToUpperInvariant()
            : $"{partes[0][0]}{partes[1][0]}".ToUpperInvariant();
    }
}
