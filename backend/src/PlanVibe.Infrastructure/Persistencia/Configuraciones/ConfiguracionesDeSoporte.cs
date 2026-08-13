using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanVibe.Domain.Categorias;
using PlanVibe.Infrastructure.Identidad;

namespace PlanVibe.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo del catálogo de categorías.</summary>
public sealed class CategoriaConfiguracion : IEntityTypeConfiguration<Categoria>
{
    public void Configure(EntityTypeBuilder<Categoria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("categorias");

        builder.HasKey(c => c.Id);

        builder.Property(c => c.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new CategoriaId(valor))
            .ValueGeneratedNever();

        builder.Property(c => c.Nombre).HasColumnName("nombre").HasMaxLength(Categoria.LongitudMaximaNombre).IsRequired();
        builder.Property(c => c.Clave).HasColumnName("clave").HasMaxLength(60).IsRequired();
        builder.Property(c => c.ColorHex).HasColumnName("color_hex").HasMaxLength(7).IsRequired();
        builder.Property(c => c.Orden).HasColumnName("orden").IsRequired();
        builder.Property(c => c.Activa).HasColumnName("activa").IsRequired();

        builder.Property(c => c.VersionFila)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        // La clave aparece en las URL de los filtros compartidos, así que debe ser única.
        builder.HasIndex(c => c.Clave).IsUnique().HasDatabaseName("ix_categorias_clave");

        builder.Ignore(c => c.EventosDeDominio);
    }
}

/// <summary>Mapeo de los tokens de renovación de sesión.</summary>
public sealed class TokenDeRenovacionConfiguracion : IEntityTypeConfiguration<TokenDeRenovacion>
{
    public void Configure(EntityTypeBuilder<TokenDeRenovacion> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("tokens_de_renovacion", PlanVibeDbContext.EsquemaIdentidad);

        builder.HasKey(t => t.Id);

        builder.Property(t => t.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(t => t.UsuarioId).HasColumnName("usuario_id").IsRequired();

        // Se guarda el hash en hexadecimal, longitud fija de SHA-256.
        builder.Property(t => t.HashDelToken).HasColumnName("hash_del_token").HasMaxLength(64).IsRequired();

        builder.Property(t => t.Familia).HasColumnName("familia").IsRequired();
        builder.Property(t => t.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(t => t.ExpiraEn).HasColumnName("expira_en").IsRequired();
        builder.Property(t => t.UsadoEn).HasColumnName("usado_en");
        builder.Property(t => t.RevocadoEn).HasColumnName("revocado_en");
        builder.Property(t => t.Dispositivo).HasColumnName("dispositivo").HasMaxLength(200);

        // La búsqueda por hash ocurre en cada renovación de sesión: debe resolverse por índice.
        builder.HasIndex(t => t.HashDelToken).IsUnique().HasDatabaseName("ix_tokens_hash");

        // La revocación por reutilización necesita alcanzar de golpe toda la familia.
        builder.HasIndex(t => t.Familia).HasDatabaseName("ix_tokens_familia");

        builder.HasIndex(t => t.ExpiraEn).HasDatabaseName("ix_tokens_expiracion");
    }
}

/// <summary>Mapeo del registro de auditoría (RNF-04).</summary>
public sealed class EntradaDeAuditoriaConfiguracion : IEntityTypeConfiguration<EntradaDeAuditoria>
{
    public void Configure(EntityTypeBuilder<EntradaDeAuditoria> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("entradas", PlanVibeDbContext.EsquemaAuditoria);

        builder.HasKey(e => e.Id);

        builder.Property(e => e.Id).HasColumnName("id").ValueGeneratedNever();
        builder.Property(e => e.ActorId).HasColumnName("actor_id");
        builder.Property(e => e.Accion).HasColumnName("accion").HasMaxLength(80).IsRequired();
        builder.Property(e => e.TipoDeObjeto).HasColumnName("tipo_de_objeto").HasMaxLength(60).IsRequired();
        builder.Property(e => e.ObjetoId).HasColumnName("objeto_id").HasMaxLength(100).IsRequired();

        // jsonb en lugar de json: permite indexar y consultar los metadatos si hace falta
        // investigar un incidente, sin tener que analizar el texto en la aplicación.
        builder.Property(e => e.Metadatos).HasColumnName("metadatos").HasColumnType("jsonb");

        builder.Property(e => e.OcurridoEn).HasColumnName("ocurrido_en").IsRequired();

        // Las dos consultas reales de una investigación: «qué hizo esta persona» y
        // «qué le ha pasado a este objeto».
        builder.HasIndex(e => new { e.ActorId, e.OcurridoEn }).HasDatabaseName("ix_auditoria_actor_fecha");
        builder.HasIndex(e => new { e.TipoDeObjeto, e.ObjetoId }).HasDatabaseName("ix_auditoria_objeto");
    }
}
