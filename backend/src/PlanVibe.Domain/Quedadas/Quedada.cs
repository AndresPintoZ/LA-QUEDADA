using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Common;
using PlanVibe.Domain.Quedadas.Eventos;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;
using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Domain.Quedadas;

/// <summary>
/// Una quedada: un plan concreto al que otras personas pueden apuntarse.
/// Es la raíz del agregado y la única forma de tocar las asistencias.
/// </summary>
/// <remarks>
/// <para>
/// Toda la aritmética de plazas vive aquí dentro. Ningún caso de uso puede contar asistentes por su
/// cuenta y decidir si cabe una persona más: si esa lógica se escapara del agregado, tarde o
/// temprano dos caminos distintos calcularían la capacidad de forma distinta.
/// </para>
/// <para>
/// El instante actual se recibe como parámetro (<c>ahora</c>) en lugar de leerse de un reloj
/// estático. Así el dominio no depende de nada externo y las pruebas pueden situarse en cualquier
/// momento sin trucos.
/// </para>
/// </remarks>
public sealed class Quedada : RaizDeAgregado<QuedadaId>
{
    public const int LongitudMinimaTitulo = 3;
    public const int LongitudMaximaTitulo = 120;
    public const int LongitudMaximaDescripcion = 2_000;
    public const int LongitudMaximaMotivo = 300;

    private readonly List<Asistencia> _asistencias = [];

    private Quedada(
        QuedadaId id,
        UsuarioId organizadorId,
        string titulo,
        string descripcion,
        CategoriaId categoriaId,
        FranjaTemporal cuando,
        PuntoEncuentro donde,
        Capacidad capacidad,
        NormasDelPlan normas,
        DateTimeOffset ahora)
        : base(id)
    {
        OrganizadorId = organizadorId;
        Titulo = titulo;
        Descripcion = descripcion;
        CategoriaId = categoriaId;
        Cuando = cuando;
        Donde = donde;
        Capacidad = capacidad;
        Normas = normas;
        Estado = EstadoQuedada.Publicada;
        CreadaEn = ahora;
        ActualizadaEn = ahora;
    }

    /// <summary>Constructor para EF Core.</summary>
    private Quedada()
    {
        Titulo = string.Empty;
        Descripcion = string.Empty;
        Normas = NormasDelPlan.Ninguna;
        Donde = null!;
    }

    public UsuarioId OrganizadorId { get; private set; }

    public string Titulo { get; private set; }

    public string Descripcion { get; private set; }

    public CategoriaId CategoriaId { get; private set; }

    public FranjaTemporal Cuando { get; private set; }

    public PuntoEncuentro Donde { get; private set; }

    public Capacidad Capacidad { get; private set; }

    public NormasDelPlan Normas { get; private set; }

    public EstadoQuedada Estado { get; private set; }

    public string? MotivoDeCancelacion { get; private set; }

    public DateTimeOffset CreadaEn { get; private set; }

    public DateTimeOffset ActualizadaEn { get; private set; }

    /// <summary>Contador de turnos ya repartidos. Nunca decrece, ni siquiera cuando alguien se retira.</summary>
    public long UltimoOrdenDeLlegada { get; private set; }

    public IReadOnlyCollection<Asistencia> Asistencias => _asistencias.AsReadOnly();

    public int PlazasOcupadas => _asistencias.Count(a => a.Estado == EstadoAsistencia.Confirmada);

    public int PlazasLibres => Math.Max(0, Capacidad.Maximo - PlazasOcupadas);

    public int EnListaDeEspera => _asistencias.Count(a => a.Estado == EstadoAsistencia.EnListaDeEspera);

    /// <summary>Admite inscripciones: está publicada y todavía no ha empezado.</summary>
    public bool AdmiteInscripcionesEn(DateTimeOffset ahora) =>
        Estado == EstadoQuedada.Publicada && !Cuando.YaComenzoEn(ahora);

    // -----------------------------------------------------------------------
    // Creación
    // -----------------------------------------------------------------------

    /// <summary>
    /// Publica una quedada nueva. El organizador queda apuntado con plaza confirmada:
    /// no tendría sentido un plan cuyo creador no asiste, y así ocupa plaza en el recuento.
    /// </summary>
    /// <exception cref="ExcepcionDeDominio">
    /// Si el organizador no está verificado (RF-09, RF-20), es menor de edad (RF-24),
    /// la fecha ya pasó, o el título o la descripción no son válidos.
    /// </exception>
    public static Quedada Crear(
        QuedadaId id,
        Organizador organizador,
        string titulo,
        string descripcion,
        CategoriaId categoria,
        FranjaTemporal cuando,
        PuntoEncuentro donde,
        Capacidad capacidad,
        NormasDelPlan normas,
        DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(donde);
        ArgumentNullException.ThrowIfNull(normas);

        ExcepcionDeDominio.SiNo(
            organizador.EstaVerificado,
            "quedada.organizador_no_verificado",
            "Para publicar una quedada necesitas completar la verificación de organizador.");

        ExcepcionDeDominio.SiNo(
            organizador.EsMayorDeEdad,
            "quedada.organizador_menor_de_edad",
            "Durante el piloto solo pueden organizar quedadas las personas mayores de edad.");

        ExcepcionDeDominio.SiNo(
            !cuando.YaComenzoEn(ahora),
            "quedada.fecha_en_pasado",
            "La quedada debe empezar en el futuro.");

        var quedada = new Quedada(
            id,
            organizador.Id,
            ValidarTitulo(titulo),
            ValidarDescripcion(descripcion),
            categoria,
            cuando,
            donde,
            capacidad,
            normas,
            ahora);

        // El organizador ocupa la primera plaza.
        quedada.UltimoOrdenDeLlegada = 1;
        quedada._asistencias.Add(Asistencia.Crear(organizador.Id, EstadoAsistencia.Confirmada, ordenDeLlegada: 1, ahora));

        quedada.RegistrarEvento(new QuedadaPublicada(id, organizador.Id, ahora));

        return quedada;
    }

    // -----------------------------------------------------------------------
    // Inscripciones
    // -----------------------------------------------------------------------

    /// <summary>
    /// Apunta a una persona. Si quedan plazas obtiene la suya; si no, entra en lista de espera
    /// por orden de llegada (RF-14, RF-15).
    /// </summary>
    public ResultadoDeApuntarse Apuntar(UsuarioId usuarioId, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            Estado == EstadoQuedada.Publicada,
            "quedada.no_admite_inscripciones",
            "Esta quedada ya no admite inscripciones.");

        ExcepcionDeDominio.SiNo(
            !Cuando.YaComenzoEn(ahora),
            "quedada.ya_comenzada",
            "Esta quedada ya ha empezado.");

        var existente = BuscarAsistencia(usuarioId);

        ExcepcionDeDominio.SiNo(
            existente is null || !existente.EstaActiva,
            "quedada.ya_apuntado",
            "Ya estás apuntado a esta quedada.");

        var hayPlaza = PlazasLibres > 0;
        var estado = hayPlaza ? EstadoAsistencia.Confirmada : EstadoAsistencia.EnListaDeEspera;
        var orden = ++UltimoOrdenDeLlegada;

        if (existente is null)
        {
            _asistencias.Add(Asistencia.Crear(usuarioId, estado, orden, ahora));
        }
        else
        {
            existente.Reactivar(estado, orden, ahora);
        }

        ActualizadaEn = ahora;
        RegistrarEvento(new UsuarioApuntado(Id, usuarioId, hayPlaza, ahora));

        return new ResultadoDeApuntarse(hayPlaza, hayPlaza ? null : PosicionEnListaDeEspera(usuarioId));
    }

    /// <summary>
    /// Retira a una persona. Si tenía plaza confirmada, la primera de la lista de espera
    /// pasa a tenerla automáticamente (RF-15).
    /// </summary>
    public void Abandonar(UsuarioId usuarioId, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            usuarioId != OrganizadorId,
            "quedada.organizador_no_puede_abandonar",
            "Como organizador no puedes abandonar tu propia quedada; cancélala si no puedes ir.");

        var asistencia = BuscarAsistencia(usuarioId);

        ExcepcionDeDominio.SiNo(
            asistencia is not null && asistencia.EstaActiva,
            "quedada.no_apuntado",
            "No estás apuntado a esta quedada.");

        var teniaPlaza = asistencia!.Estado == EstadoAsistencia.Confirmada;
        asistencia.CambiarEstado(EstadoAsistencia.Retirada, ahora);

        ActualizadaEn = ahora;
        RegistrarEvento(new UsuarioRetirado(Id, usuarioId, teniaPlaza, ahora));

        if (teniaPlaza)
        {
            PromoverDesdeListaDeEspera(ahora);
        }
    }

    // -----------------------------------------------------------------------
    // Gestión del organizador
    // -----------------------------------------------------------------------

    /// <summary>
    /// Cancela la quedada y deja preparado el aviso a los asistentes (RF-11).
    /// No se borra el registro: la moderación y la auditoría necesitan el histórico (RNF-04).
    /// </summary>
    public void Cancelar(UsuarioId solicitanteId, string motivo, DateTimeOffset ahora)
    {
        ExigirQueSeaElOrganizador(solicitanteId);
        ExigirQueSeaModificable();

        var motivoLimpio = motivo?.Trim() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            motivoLimpio.Length > 0,
            "quedada.motivo_requerido",
            "Indica el motivo de la cancelación; quien se había apuntado merece saberlo.");

        ExcepcionDeDominio.SiNo(
            motivoLimpio.Length <= LongitudMaximaMotivo,
            "quedada.motivo_demasiado_largo",
            $"El motivo no puede superar {LongitudMaximaMotivo} caracteres.");

        Estado = EstadoQuedada.Cancelada;
        MotivoDeCancelacion = motivoLimpio;
        ActualizadaEn = ahora;

        RegistrarEvento(new QuedadaCancelada(Id, solicitanteId, motivoLimpio, UsuariosAAvisar(), ahora));
    }

    /// <summary>
    /// Cambia la capacidad. Ampliarla promueve automáticamente a quien esperaba;
    /// reducirla por debajo de las plazas ya ocupadas no se permite: nadie pierde una plaza concedida.
    /// </summary>
    public void CambiarCapacidad(UsuarioId solicitanteId, Capacidad nuevaCapacidad, DateTimeOffset ahora)
    {
        ExigirQueSeaElOrganizador(solicitanteId);
        ExigirQueSeaModificable();

        ExcepcionDeDominio.SiNo(
            nuevaCapacidad.Maximo >= PlazasOcupadas,
            "quedada.capacidad_menor_que_asistentes",
            $"Ya hay {PlazasOcupadas} personas con plaza; no puedes bajar la capacidad por debajo de esa cifra.");

        if (nuevaCapacidad.Maximo == Capacidad.Maximo)
        {
            return;
        }

        Capacidad = nuevaCapacidad;
        ActualizadaEn = ahora;

        PromoverDesdeListaDeEspera(ahora);
        RegistrarEvento(new QuedadaModificada(Id, solicitanteId, ["capacidad"], UsuariosAAvisar(), ahora));
    }

    /// <summary>Cambia los datos descriptivos que no afectan a la logística del encuentro.</summary>
    public void CambiarDetalles(UsuarioId solicitanteId, string titulo, string descripcion, NormasDelPlan normas, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(normas);
        ExigirQueSeaElOrganizador(solicitanteId);
        ExigirQueSeaModificable();

        Titulo = ValidarTitulo(titulo);
        Descripcion = ValidarDescripcion(descripcion);
        Normas = normas;
        ActualizadaEn = ahora;

        RegistrarEvento(new QuedadaModificada(Id, solicitanteId, ["titulo", "descripcion", "normas"], UsuariosAAvisar(), ahora));
    }

    /// <summary>
    /// Cambia cuándo o dónde es la quedada. Se avisa a todos los apuntados porque puede
    /// hacer que ya no les encaje (RF-11, RF-19).
    /// </summary>
    public void CambiarCuandoYDonde(UsuarioId solicitanteId, FranjaTemporal cuando, PuntoEncuentro donde, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(donde);
        ExigirQueSeaElOrganizador(solicitanteId);
        ExigirQueSeaModificable();

        ExcepcionDeDominio.SiNo(
            !cuando.YaComenzoEn(ahora),
            "quedada.fecha_en_pasado",
            "La nueva fecha debe estar en el futuro.");

        Cuando = cuando;
        Donde = donde;
        ActualizadaEn = ahora;

        RegistrarEvento(new QuedadaModificada(Id, solicitanteId, ["cuando", "donde"], UsuariosAAvisar(), ahora));
    }

    /// <summary>Retira la quedada de la vista pública por decisión de moderación (RF-18).</summary>
    public void OcultarPorModeracion(string motivo, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(motivo),
            "quedada.motivo_requerido",
            "Toda decisión de moderación debe registrar su motivo.");

        Estado = EstadoQuedada.OcultaPorModeracion;
        MotivoDeCancelacion = motivo.Trim();
        ActualizadaEn = ahora;
    }

    /// <summary>Marca como finalizada una quedada cuya hora de fin ya pasó. La ejecuta un proceso programado.</summary>
    public void FinalizarSiProcede(DateTimeOffset ahora)
    {
        if (Estado == EstadoQuedada.Publicada && Cuando.YaTerminoEn(ahora))
        {
            Estado = EstadoQuedada.Finalizada;
            ActualizadaEn = ahora;
        }
    }

    // -----------------------------------------------------------------------
    // Consultas
    // -----------------------------------------------------------------------

    public bool EstaApuntado(UsuarioId usuarioId) => BuscarAsistencia(usuarioId)?.EstaActiva == true;

    public EstadoAsistencia? EstadoDe(UsuarioId usuarioId) => BuscarAsistencia(usuarioId)?.Estado;

    /// <summary>Posición en la lista de espera empezando por 1, o <c>null</c> si no está esperando.</summary>
    public int? PosicionEnListaDeEspera(UsuarioId usuarioId)
    {
        var enEspera = _asistencias
            .Where(a => a.Estado == EstadoAsistencia.EnListaDeEspera)
            .OrderBy(a => a.OrdenDeLlegada)
            .ToList();

        var indice = enEspera.FindIndex(a => a.UsuarioId == usuarioId);

        return indice >= 0 ? indice + 1 : null;
    }

    /// <summary>
    /// Devuelve la dirección exacta solo si la persona tiene plaza confirmada; en cualquier otro
    /// caso, <c>null</c>. Es la regla de privacidad del punto de encuentro y se resuelve aquí,
    /// en el dominio, para que ninguna vista pueda saltársela por olvido.
    /// </summary>
    public string? DireccionExactaVisiblePara(UsuarioId usuarioId) =>
        EstadoDe(usuarioId) == EstadoAsistencia.Confirmada ? Donde.DireccionExacta : null;

    // -----------------------------------------------------------------------
    // Detalles internos
    // -----------------------------------------------------------------------

    private Asistencia? BuscarAsistencia(UsuarioId usuarioId) =>
        _asistencias.Find(a => a.UsuarioId == usuarioId);

    /// <summary>Personas a las que hay que avisar de un cambio: todas las activas menos el organizador.</summary>
    private List<UsuarioId> UsuariosAAvisar() =>
        _asistencias
            .Where(a => a.EstaActiva && a.UsuarioId != OrganizadorId)
            .Select(a => a.UsuarioId)
            .ToList();

    /// <summary>
    /// Rellena las plazas libres con quienes esperan, respetando el orden de llegada.
    /// Se ejecuta tanto al liberarse una plaza como al ampliar la capacidad.
    /// </summary>
    private void PromoverDesdeListaDeEspera(DateTimeOffset ahora)
    {
        var candidatos = _asistencias
            .Where(a => a.Estado == EstadoAsistencia.EnListaDeEspera)
            .OrderBy(a => a.OrdenDeLlegada)
            .Take(PlazasLibres)
            .ToList();

        foreach (var candidato in candidatos)
        {
            candidato.CambiarEstado(EstadoAsistencia.Confirmada, ahora);
            RegistrarEvento(new AsistentePromovido(Id, candidato.UsuarioId, ahora));
        }
    }

    private void ExigirQueSeaElOrganizador(UsuarioId solicitanteId) =>
        ExcepcionDeDominio.SiNo(
            solicitanteId == OrganizadorId,
            "quedada.solo_el_organizador",
            "Solo quien organiza la quedada puede realizar esta acción.");

    private void ExigirQueSeaModificable() =>
        ExcepcionDeDominio.SiNo(
            Estado == EstadoQuedada.Publicada,
            "quedada.no_modificable",
            "Esta quedada ya no se puede modificar.");

    private static string ValidarTitulo(string titulo)
    {
        var limpio = titulo?.Trim() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            limpio.Length is >= LongitudMinimaTitulo and <= LongitudMaximaTitulo,
            "quedada.titulo_invalido",
            $"El título debe tener entre {LongitudMinimaTitulo} y {LongitudMaximaTitulo} caracteres.");

        return limpio;
    }

    private static string ValidarDescripcion(string descripcion)
    {
        var limpia = descripcion?.Trim() ?? string.Empty;

        ExcepcionDeDominio.SiNo(
            limpia.Length <= LongitudMaximaDescripcion,
            "quedada.descripcion_demasiado_larga",
            $"La descripción no puede superar {LongitudMaximaDescripcion} caracteres.");

        return limpia;
    }
}

/// <summary>
/// Qué ha pasado al apuntarse: plaza confirmada, o puesto en la lista de espera.
/// Se devuelve como resultado explícito para que la interfaz pueda dar el mensaje correcto
/// sin volver a consultar el estado de la quedada.
/// </summary>
/// <param name="Confirmada">Cierto si ha obtenido plaza.</param>
/// <param name="PosicionEnListaDeEspera">Puesto en la cola, empezando por 1, o <c>null</c> si tiene plaza.</param>
public readonly record struct ResultadoDeApuntarse(bool Confirmada, int? PosicionEnListaDeEspera);
