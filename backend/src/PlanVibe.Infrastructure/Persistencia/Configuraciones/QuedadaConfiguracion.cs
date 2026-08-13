using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using NetTopologySuite.Geometries;
using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Infrastructure.Persistencia.Configuraciones;

/// <summary>Mapeo del agregado <see cref="Quedada"/> y de sus asistencias.</summary>
public sealed class QuedadaConfiguracion : IEntityTypeConfiguration<Quedada>
{
    /// <summary>
    /// Nombre de la columna geográfica calculada por PostgreSQL a partir de la latitud y la longitud.
    /// </summary>
    /// <remarks>
    /// Se declara como propiedad sombra porque el dominio no debe conocer NetTopologySuite:
    /// <see cref="Coordenadas"/> es un objeto de valor propio, sin dependencias de infraestructura.
    /// La columna existe solo para que PostGIS pueda resolver «qué hay a menos de X metros»
    /// apoyándose en un índice espacial, en lugar de calcular la distancia fila a fila.
    /// </remarks>
    public const string ColumnaUbicacion = "ubicacion";

    /// <summary>Sistema de referencia WGS 84, el de GPS y OpenStreetMap.</summary>
    public const int Srid = 4326;

    public void Configure(EntityTypeBuilder<Quedada> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("quedadas");

        builder.HasKey(q => q.Id);

        builder.Property(q => q.Id)
            .HasColumnName("id")
            .HasConversion(id => id.Valor, valor => new QuedadaId(valor))
            .ValueGeneratedNever();

        builder.Property(q => q.OrganizadorId)
            .HasColumnName("organizador_id")
            .HasConversion(id => id.Valor, valor => new UsuarioId(valor))
            .IsRequired();

        builder.Property(q => q.CategoriaId)
            .HasColumnName("categoria_id")
            .HasConversion(id => id.Valor, valor => new CategoriaId(valor))
            .IsRequired();

        builder.Property(q => q.Titulo)
            .HasColumnName("titulo")
            .HasMaxLength(Quedada.LongitudMaximaTitulo)
            .IsRequired();

        builder.Property(q => q.Descripcion)
            .HasColumnName("descripcion")
            .HasMaxLength(Quedada.LongitudMaximaDescripcion);

        builder.Property(q => q.Estado)
            .HasColumnName("estado")
            .HasConversion<int>()
            .IsRequired();

        builder.Property(q => q.MotivoDeCancelacion)
            .HasColumnName("motivo_de_cancelacion")
            .HasMaxLength(Quedada.LongitudMaximaMotivo);

        builder.Property(q => q.CreadaEn).HasColumnName("creada_en").IsRequired();
        builder.Property(q => q.ActualizadaEn).HasColumnName("actualizada_en").IsRequired();
        builder.Property(q => q.UltimoOrdenDeLlegada).HasColumnName("ultimo_orden_de_llegada").IsRequired();

        // Concurrencia optimista: la columna de sistema xmin de PostgreSQL cambia en cada UPDATE.
        // Es la última barrera contra que dos inscripciones simultáneas sobrepasen la capacidad.
        builder.Property(q => q.VersionFila)
            .HasColumnName("xmin")
            .HasColumnType("xid")
            .ValueGeneratedOnAddOrUpdate()
            .IsConcurrencyToken();

        ConfigurarCuando(builder);
        ConfigurarDonde(builder);
        ConfigurarNormas(builder);
        ConfigurarAsistencias(builder);
        ConfigurarUbicacionYIndices(builder);

        builder.Ignore(q => q.EventosDeDominio);
    }

    /// <summary>
    /// Mapea la franja temporal y la capacidad como propiedades complejas.
    /// </summary>
    /// <remarks>
    /// Se usan propiedades complejas y no conversores de valor porque una propiedad compleja
    /// conserva el acceso a sus miembros dentro de una consulta: <c>q.Capacidad.Maximo</c> se
    /// traduce a la columna <c>capacidad</c>. Con un conversor, esa misma expresión no se podría
    /// traducir y el filtro «solo con plazas» acabaría evaluándose en memoria.
    /// </remarks>
    private static void ConfigurarCuando(EntityTypeBuilder<Quedada> builder)
    {
        builder.ComplexProperty(q => q.Cuando, franja =>
        {
            franja.Property(f => f.Inicio).HasColumnName("inicio").IsRequired();
            franja.Property(f => f.Duracion).HasColumnName("duracion").IsRequired();

            // Fin se calcula sumando la duración al inicio: no es una columna.
            franja.Ignore(f => f.Fin);
        });

        builder.ComplexProperty(q => q.Capacidad, capacidad =>
            capacidad.Property(c => c.Maximo).HasColumnName("capacidad").IsRequired());
    }

    /// <summary>
    /// Mapea el punto de encuentro y sus coordenadas como columnas de la propia tabla de quedadas.
    /// </summary>
    /// <remarks>
    /// Se usan propiedades complejas y no una entidad propia porque el punto de encuentro es un
    /// objeto de valor: no tiene identidad ni vida independiente de la quedada. Al aplanarlo en
    /// la misma tabla, la columna geográfica calculada puede leer <c>latitud</c> y <c>longitud</c>
    /// directamente, sin ninguna unión.
    /// </remarks>
    private static void ConfigurarDonde(EntityTypeBuilder<Quedada> builder) =>
        builder.ComplexProperty(q => q.Donde, punto =>
        {
            punto.Property(p => p.Lugar)
                .HasColumnName("lugar")
                .HasMaxLength(PuntoEncuentro.LongitudMaximaLugar)
                .IsRequired();

            punto.Property(p => p.Referencia)
                .HasColumnName("referencia")
                .HasMaxLength(PuntoEncuentro.LongitudMaximaReferencia);

            // La dirección exacta se guarda, pero la API solo la devuelve a quien tiene plaza
            // confirmada. La decisión la toma el agregado, no la consulta ni la vista.
            punto.Property(p => p.DireccionExacta)
                .HasColumnName("direccion_exacta")
                .HasMaxLength(PuntoEncuentro.LongitudMaximaDireccion);

            punto.Property(p => p.EsLugarPublico)
                .HasColumnName("es_lugar_publico")
                .IsRequired();

            punto.Property(p => p.Latitud).HasColumnName("latitud").IsRequired();
            punto.Property(p => p.Longitud).HasColumnName("longitud").IsRequired();

            // Coordenadas es una propiedad calculada a partir de las dos anteriores: no es una
            // columna, se reconstruye al leer.
            punto.Ignore(p => p.Coordenadas);
        });

    private static void ConfigurarNormas(EntityTypeBuilder<Quedada> builder) =>
        // Las normas son una lista corta de frases sin identidad propia: no merecen una tabla.
        // Se guardan como array nativo de PostgreSQL, que sí admite consultas si algún día hacen falta.
        builder.Property(q => q.Normas)
            .HasColumnName("normas")
            .HasConversion(
                normas => normas.ToArray(),
                valores => new NormasDelPlan(valores),
                // Sin comparador, EF compara las normas por referencia y no se entera de que han
                // cambiado: editarlas no generaría ningún UPDATE y el cambio se perdería en silencio.
                new ValueComparer<NormasDelPlan>(
                    (a, b) => a!.SequenceEqual(b!, StringComparer.Ordinal),
                    normas => normas.Aggregate(0, (acumulado, norma) => HashCode.Combine(acumulado, norma.GetHashCode(StringComparison.Ordinal))),
                    normas => new NormasDelPlan(normas)))
            .HasColumnType("text[]")
            .IsRequired();

    private static void ConfigurarAsistencias(EntityTypeBuilder<Quedada> builder) =>
        builder.OwnsMany(q => q.Asistencias, asistencia =>
        {
            asistencia.ToTable("asistencias");

            asistencia.HasKey(a => a.Id);
            asistencia.Property(a => a.Id).HasColumnName("id").ValueGeneratedNever();

            // La clave ajena es una propiedad sombra: el dominio no necesita que cada asistencia
            // sepa a qué quedada pertenece, porque solo se llega a ella a través de su quedada.
            // Se declara con el mismo tipo fuertemente tipado que la clave primaria de Quedada;
            // si fuera un Guid suelto, EF la rechazaría por incompatible.
            asistencia.Property<QuedadaId>("quedada_id")
                .HasColumnName("quedada_id")
                .HasConversion(id => id.Valor, valor => new QuedadaId(valor));

            asistencia.WithOwner().HasForeignKey("quedada_id");

            asistencia.Property(a => a.UsuarioId)
                .HasColumnName("usuario_id")
                .HasConversion(id => id.Valor, valor => new UsuarioId(valor))
                .IsRequired();

            asistencia.Property(a => a.Estado).HasColumnName("estado").HasConversion<int>().IsRequired();
            asistencia.Property(a => a.OrdenDeLlegada).HasColumnName("orden_de_llegada").IsRequired();
            asistencia.Property(a => a.SolicitadaEn).HasColumnName("solicitada_en").IsRequired();
            asistencia.Property(a => a.ActualizadaEn).HasColumnName("actualizada_en").IsRequired();

            // Una sola fila por persona y quedada. Es lo que hace imposible una doble inscripción
            // aunque dos peticiones lleguen exactamente a la vez: la segunda choca con el índice.
            asistencia.HasIndex("quedada_id", nameof(Asistencia.UsuarioId))
                .IsUnique()
                .HasDatabaseName("ix_asistencias_quedada_usuario");

            asistencia.HasIndex(a => a.UsuarioId).HasDatabaseName("ix_asistencias_usuario");
        });

    private static void ConfigurarUbicacionYIndices(EntityTypeBuilder<Quedada> builder)
    {
        // Columna calculada por PostgreSQL a partir de latitud y longitud. Al ser generada,
        // no puede quedar desincronizada con las coordenadas: no hay forma de escribirla a mano.
        builder.Property<Point>(ColumnaUbicacion)
            .HasColumnName(ColumnaUbicacion)
            .HasColumnType($"geography(Point,{Srid})")
            .HasComputedColumnSql(
                $"ST_SetSRID(ST_MakePoint(longitud, latitud), {Srid})::geography",
                stored: true);

        // Índice espacial: convierte «qué hay a menos de 5 km» en una búsqueda por índice
        // en lugar de un recorrido completo de la tabla calculando distancias.
        builder.HasIndex(ColumnaUbicacion)
            .HasMethod("gist")
            .HasDatabaseName("ix_quedadas_ubicacion");

        // Índice del caso de uso principal de explorar: planes publicados ordenados por fecha.
        builder.HasIndex(q => new { q.Estado })
            .HasDatabaseName("ix_quedadas_estado");

        builder.HasIndex(q => q.OrganizadorId).HasDatabaseName("ix_quedadas_organizador");
        builder.HasIndex(q => q.CategoriaId).HasDatabaseName("ix_quedadas_categoria");
    }
}
