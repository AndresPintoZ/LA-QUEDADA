using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Infrastructure.Identidad;

namespace PlanVibe.Infrastructure.Persistencia;

/// <summary>
/// Contexto de datos de PlanVibe. Reúne el modelo de dominio, las tablas de Identity y las
/// tablas de soporte (auditoría y tokens de sesión) en una sola base de datos.
/// </summary>
/// <remarks>
/// <para>
/// Están en la misma base de datos, pero en esquemas separados: <c>app</c> para el dominio,
/// <c>identidad</c> para credenciales y sesiones, y <c>auditoria</c> para la traza. Esa división
/// aplica el principio de separar datos públicos, datos de cuenta y datos de moderación descrito
/// en <c>docs/02-arquitectura.md</c>, y permite conceder permisos distintos por esquema a cada
/// usuario de base de datos cuando llegue el momento de desplegar en producción.
/// </para>
/// <para>
/// El contexto no arrastra reglas de negocio: solo describe cómo se guardan los agregados.
/// </para>
/// </remarks>
public sealed class PlanVibeDbContext(DbContextOptions<PlanVibeDbContext> opciones)
    : IdentityDbContext<CuentaDeAcceso, IdentityRole<Guid>, Guid>(opciones)
{
    public const string EsquemaApp = "app";
    public const string EsquemaIdentidad = "identidad";
    public const string EsquemaAuditoria = "auditoria";

    public DbSet<Usuario> Usuarios => Set<Usuario>();

    public DbSet<Quedada> Quedadas => Set<Quedada>();

    public DbSet<Categoria> Categorias => Set<Categoria>();

    public DbSet<TokenDeRenovacion> TokensDeRenovacion => Set<TokenDeRenovacion>();

    public DbSet<EntradaDeAuditoria> Auditoria => Set<EntradaDeAuditoria>();

    protected override void OnModelCreating(ModelBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        base.OnModelCreating(builder);

        // PostGIS: necesaria para el tipo geography y los índices espaciales de la búsqueda por cercanía.
        builder.HasPostgresExtension("postgis");

        // Las tablas de Identity van a su propio esquema para no mezclarlas con el dominio.
        builder.HasDefaultSchema(EsquemaApp);
        MoverTablasDeIdentityASuEsquema(builder);

        builder.ApplyConfigurationsFromAssembly(typeof(PlanVibeDbContext).Assembly);
    }

    private static void MoverTablasDeIdentityASuEsquema(ModelBuilder builder)
    {
        builder.Entity<CuentaDeAcceso>().ToTable("cuentas_de_acceso", EsquemaIdentidad);
        builder.Entity<IdentityRole<Guid>>().ToTable("roles", EsquemaIdentidad);
        builder.Entity<IdentityUserRole<Guid>>().ToTable("cuentas_roles", EsquemaIdentidad);
        builder.Entity<IdentityUserClaim<Guid>>().ToTable("cuentas_reclamaciones", EsquemaIdentidad);
        builder.Entity<IdentityUserLogin<Guid>>().ToTable("cuentas_accesos_externos", EsquemaIdentidad);
        builder.Entity<IdentityUserToken<Guid>>().ToTable("cuentas_tokens", EsquemaIdentidad);
        builder.Entity<IdentityRoleClaim<Guid>>().ToTable("roles_reclamaciones", EsquemaIdentidad);
    }
}
