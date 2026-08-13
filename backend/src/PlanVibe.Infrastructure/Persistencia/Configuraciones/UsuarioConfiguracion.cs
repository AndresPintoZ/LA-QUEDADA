using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo del agregado <see cref="Usuario"/>.</summary>
public sealed class UsuarioConfiguracion : IEntityTypeConfiguration<Usuario>
{
    public void Configure(EntityTypeBuilder<Usuario> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("usuarios");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new UsuarioId(valor))
            .ValueGeneratedNever();

        builder.Property(u => u.Correo)
            .HasColumnName("correo")
            .HasMaxLength(CorreoElectronico.LongitudMaxima)
            .HasConversion(correo => correo.Valor, valor => new CorreoElectronico(valor))
            .IsRequired();

        // El correo ya se guarda normalizado en minúsculas por el objeto de valor, así que
        // el índice único basta para impedir cuentas duplicadas por diferencias de mayúsculas.
        builder.HasIndex(u => u.Correo).IsUnique().HasDatabaseName("ix_usuarios_correo");

        builder.Property(u => u.NombreVisible)
            .HasColumnName("nombre_visible")
            .HasMaxLength(NombreVisible.LongitudMaxima)
            .HasConversion(nombre => nombre.Valor, valor => new NombreVisible(valor))
            .IsRequired();

        builder.Property(u => u.Ciudad).HasColumnName("ciudad").HasMaxLength(Usuario.LongitudMaximaCiudad);
        builder.Property(u => u.Biografia).HasColumnName("biografia").HasMaxLength(Usuario.LongitudMaximaBiografia);

        // Solo el año de nacimiento, nunca la fecha completa: es el dato mínimo que resuelve
        // la comprobación de edad mínima (docs/04-seguridad-privacidad-moderacion.md).
        builder.Property(u => u.AnioDeNacimiento).HasColumnName("anio_de_nacimiento").IsRequired();

        builder.Property(u => u.Estado).HasColumnName("estado").HasConversion<int>().IsRequired();

        builder.Property(u => u.VersionNormasAceptada)
            .HasColumnName("version_normas_aceptada")
            .HasMaxLength(40)
            .IsRequired();

        builder.Property(u => u.NormasAceptadasEn).HasColumnName("normas_aceptadas_en").IsRequired();
        builder.Property(u => u.CreadoEn).HasColumnName("creado_en").IsRequired();
        builder.Property(u => u.ActualizadoEn).HasColumnName("actualizado_en").IsRequired();

        builder.Property(u => u.VersionFila)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        ConfigurarIntereses(builder);
        ConfigurarRoles(builder);
        ConfigurarVerificacion(builder);

        builder.Ignore(u => u.EventosDeDominio);
        builder.Ignore(u => u.PuedeOrganizar);
    }

    private static void ConfigurarIntereses(EntityTypeBuilder<Usuario> builder) =>
        builder.Property<List<string>>("_intereses")
            .HasColumnName("intereses")
            .HasColumnType("text[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

    private static void ConfigurarRoles(EntityTypeBuilder<Usuario> builder) =>
        // Los roles del dominio se guardan como array de enteros en la propia fila del usuario.
        // Son pocos, se leen siempre junto al usuario y no tienen datos propios: una tabla de
        // relación solo añadiría una unión a cada consulta sin aportar nada.
        builder.Property<HashSet<Rol>>("_roles")
            .HasColumnName("roles")
            .HasConversion(
                roles => roles.Select(r => (int)r).ToArray(),
                valores => valores.Select(v => (Rol)v).ToHashSet(),
                // Sin comparador, conceder o retirar un rol no se detectaría como cambio y no
                // llegaría a guardarse. En un conjunto de roles eso es un fallo de seguridad:
                // una revocación que parece aplicada pero no lo está.
                new ValueComparer<HashSet<Rol>>(
                    (a, b) => a!.SetEquals(b!),
                    roles => roles.Aggregate(0, (acumulado, rol) => acumulado ^ rol.GetHashCode()),
                    roles => new HashSet<Rol>(roles)))
            .HasColumnType("integer[]")
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .IsRequired();

    private static void ConfigurarVerificacion(EntityTypeBuilder<Usuario> builder) =>
        builder.OwnsOne(u => u.Verificacion, verificacion =>
        {
            verificacion.Property(v => v.Estado)
                .HasColumnName("verificacion_estado")
                .HasConversion<int>()
                .IsRequired();

            verificacion.Property(v => v.Proveedor).HasColumnName("verificacion_proveedor").HasMaxLength(60);
            verificacion.Property(v => v.ReferenciaExterna).HasColumnName("verificacion_referencia").HasMaxLength(200);
            verificacion.Property(v => v.MayoriaDeEdadConfirmada).HasColumnName("verificacion_mayoria_de_edad").IsRequired();
            verificacion.Property(v => v.ActualizadaEn).HasColumnName("verificacion_actualizada_en");
            verificacion.Property(v => v.Observacion).HasColumnName("verificacion_observacion").HasMaxLength(300);

            // No hay ninguna columna para la imagen ni el número del documento, y no debe haberla:
            // esos datos no llegan nunca al sistema (RF-22).
        });
}
