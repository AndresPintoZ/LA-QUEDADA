using PlanVibe.Domain.Common;
using PlanVibe.Domain.Usuarios.Eventos;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;

namespace PlanVibe.Domain.Usuarios;

/// <summary>
/// Persona con cuenta en PlanVibe: su perfil público, sus roles y el estado de su verificación.
/// </summary>
/// <remarks>
/// <para>
/// Este agregado <em>no</em> guarda la contraseña ni ningún dato de credenciales. De eso se ocupa
/// ASP.NET Core Identity en tablas separadas, enlazadas por el mismo identificador. La separación
/// es deliberada: mantiene el dominio libre de detalles de autenticación y permite cambiar el
/// mecanismo de acceso (contraseña hoy, proveedor de identidad mañana) sin tocar estas reglas.
/// </para>
/// <para>
/// De la edad solo se guarda el año de nacimiento, no la fecha completa, siguiendo el criterio de
/// minimización de <c>docs/04-seguridad-privacidad-moderacion.md</c>. Basta para comprobar la edad
/// mínima de acceso, y la mayoría de edad para organizar la confirma el proveedor de verificación.
/// </para>
/// </remarks>
public sealed class Usuario : RaizDeAgregado<UsuarioId>
{
    /// <summary>Edad mínima para tener cuenta; el público del piloto empieza en 16 años.</summary>
    public const int EdadMinima = 16;

    /// <summary>Edad mínima para organizar encuentros públicos durante el piloto (RF-24).</summary>
    public const int EdadMinimaParaOrganizar = 18;

    public const int MaximoIntereses = 12;
    public const int LongitudMaximaBiografia = 300;
    public const int LongitudMaximaCiudad = 80;

    private readonly HashSet<Rol> _roles = [];
    private readonly List<string> _intereses = [];

    private Usuario(
        UsuarioId id,
        CorreoElectronico correo,
        NombreVisible nombreVisible,
        string? ciudad,
        int anioDeNacimiento,
        string versionNormasAceptada,
        DateTimeOffset ahora)
        : base(id)
    {
        Correo = correo;
        NombreVisible = nombreVisible;
        Ciudad = ciudad;
        AnioDeNacimiento = anioDeNacimiento;
        VersionNormasAceptada = versionNormasAceptada;
        NormasAceptadasEn = ahora;
        Estado = EstadoCuenta.Activa;
        Verificacion = DatosDeVerificacion.SinIniciar;
        CreadoEn = ahora;
        ActualizadoEn = ahora;
        _roles.Add(Rol.Registrado);
    }

    /// <summary>Constructor para EF Core.</summary>
    private Usuario()
    {
        Correo = null!;
        NombreVisible = null!;
        Verificacion = DatosDeVerificacion.SinIniciar;
        VersionNormasAceptada = string.Empty;
    }

    public CorreoElectronico Correo { get; private set; }

    public NombreVisible NombreVisible { get; private set; }

    public string? Ciudad { get; private set; }

    public string? Biografia { get; private set; }

    /// <summary>Solo el año, nunca la fecha completa. Ver las notas del tipo.</summary>
    public int AnioDeNacimiento { get; private set; }

    public IReadOnlyCollection<string> Intereses => _intereses.AsReadOnly();

    public IReadOnlyCollection<Rol> Roles => _roles;

    public EstadoCuenta Estado { get; private set; }

    public DatosDeVerificacion Verificacion { get; private set; }

    /// <summary>Versión de las normas de comunidad que la persona aceptó, para poder demostrarlo.</summary>
    public string VersionNormasAceptada { get; private set; }

    public DateTimeOffset NormasAceptadasEn { get; private set; }

    public DateTimeOffset CreadoEn { get; private set; }

    public DateTimeOffset ActualizadoEn { get; private set; }

    /// <summary>
    /// Única fuente de verdad sobre si esta persona puede publicar planes. Reúne las tres
    /// condiciones (cuenta activa, verificación vigente y mayoría de edad) para que ningún
    /// caso de uso tenga que recordarlas por separado.
    /// </summary>
    public bool PuedeOrganizar =>
        Estado == EstadoCuenta.Activa
        && Verificacion.Estado == EstadoVerificacion.Verificada
        && Verificacion.MayoriaDeEdadConfirmada;

    // -----------------------------------------------------------------------
    // Alta y perfil
    // -----------------------------------------------------------------------

    /// <summary>Da de alta una cuenta nueva (RF-01).</summary>
    public static Usuario Registrar(
        UsuarioId id,
        CorreoElectronico correo,
        NombreVisible nombreVisible,
        string? ciudad,
        int anioDeNacimiento,
        string versionNormasAceptada,
        DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(correo);
        ArgumentNullException.ThrowIfNull(nombreVisible);

        // Con el año basta para descartar a quien claramente no alcanza la edad mínima.
        // Se toma la interpretación más favorable a la persona: se le supone ya cumplido el año.
        var edadMaximaPosible = ahora.Year - anioDeNacimiento;

        ExcepcionDeDominio.SiNo(
            edadMaximaPosible >= EdadMinima,
            "usuario.edad_minima_no_alcanzada",
            $"Para usar PlanVibe hay que tener al menos {EdadMinima} años.");

        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(versionNormasAceptada),
            "usuario.normas_no_aceptadas",
            "Hay que aceptar las normas de la comunidad para crear una cuenta.");

        var usuario = new Usuario(id, correo, nombreVisible, ValidarCiudad(ciudad), anioDeNacimiento, versionNormasAceptada, ahora);
        usuario.RegistrarEvento(new UsuarioRegistrado(id, ahora));

        return usuario;
    }

    /// <summary>Actualiza los datos públicos del perfil (RF-02, RF-03).</summary>
    public void ActualizarPerfil(NombreVisible nombreVisible, string? ciudad, string? biografia, IEnumerable<string> intereses, DateTimeOffset ahora)
    {
        ArgumentNullException.ThrowIfNull(nombreVisible);
        ArgumentNullException.ThrowIfNull(intereses);
        ExigirCuentaUtilizable();

        var limpios = intereses
            .Select(i => i?.Trim() ?? string.Empty)
            .Where(i => i.Length > 0)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        ExcepcionDeDominio.SiNo(
            limpios.Count <= MaximoIntereses,
            "perfil.demasiados_intereses",
            $"Puedes elegir como máximo {MaximoIntereses} intereses.");

        var biografiaLimpia = string.IsNullOrWhiteSpace(biografia) ? null : biografia.Trim();

        ExcepcionDeDominio.SiNo(
            biografiaLimpia is null || biografiaLimpia.Length <= LongitudMaximaBiografia,
            "perfil.biografia_demasiado_larga",
            $"La biografía no puede superar {LongitudMaximaBiografia} caracteres.");

        NombreVisible = nombreVisible;
        Ciudad = ValidarCiudad(ciudad);
        Biografia = biografiaLimpia;
        _intereses.Clear();
        _intereses.AddRange(limpios);
        ActualizadoEn = ahora;
    }

    // -----------------------------------------------------------------------
    // Verificación de identidad
    // -----------------------------------------------------------------------

    /// <summary>
    /// Registra que se ha enviado una solicitud al proveedor de verificación y queda a la espera
    /// de su respuesta (RF-20, RF-21).
    /// </summary>
    public void IniciarVerificacion(string proveedor, string referenciaExterna, DateTimeOffset ahora)
    {
        ExigirCuentaUtilizable();

        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(proveedor) && !string.IsNullOrWhiteSpace(referenciaExterna),
            "verificacion.datos_incompletos",
            "Falta el proveedor o la referencia de la verificación.");

        ExcepcionDeDominio.SiNo(
            Verificacion.Estado != EstadoVerificacion.Verificada,
            "verificacion.ya_verificado",
            "Tu cuenta ya está verificada.");

        Verificacion = new DatosDeVerificacion(
            EstadoVerificacion.Pendiente,
            proveedor.Trim(),
            referenciaExterna.Trim(),
            MayoriaDeEdadConfirmada: false,
            ahora,
            Observacion: null);

        ActualizadoEn = ahora;
        RegistrarEvento(new VerificacionActualizada(Id, EstadoVerificacion.Pendiente, Verificacion.Proveedor, Verificacion.ReferenciaExterna, ahora));
    }

    /// <summary>
    /// Anota el resultado favorable devuelto por el proveedor. Solo concede el rol de organizador
    /// si además se confirmó la mayoría de edad: identidad probada y edad suficiente son dos
    /// requisitos distintos (RF-24).
    /// </summary>
    public void ConfirmarVerificacion(bool mayoriaDeEdadConfirmada, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            Verificacion.Estado == EstadoVerificacion.Pendiente,
            "verificacion.no_iniciada",
            "No hay ninguna verificación pendiente que confirmar.");

        Verificacion = Verificacion with
        {
            Estado = EstadoVerificacion.Verificada,
            MayoriaDeEdadConfirmada = mayoriaDeEdadConfirmada,
            ActualizadaEn = ahora,
            Observacion = null,
        };

        if (mayoriaDeEdadConfirmada)
        {
            _roles.Add(Rol.OrganizadorVerificado);
        }

        ActualizadoEn = ahora;
        RegistrarEvento(new VerificacionActualizada(Id, EstadoVerificacion.Verificada, Verificacion.Proveedor, Verificacion.ReferenciaExterna, ahora));
    }

    /// <summary>Anota un resultado desfavorable. Se puede volver a intentar.</summary>
    public void RechazarVerificacion(string motivo, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            Verificacion.Estado == EstadoVerificacion.Pendiente,
            "verificacion.no_iniciada",
            "No hay ninguna verificación pendiente que rechazar.");

        Verificacion = Verificacion with
        {
            Estado = EstadoVerificacion.Rechazada,
            MayoriaDeEdadConfirmada = false,
            ActualizadaEn = ahora,
            Observacion = string.IsNullOrWhiteSpace(motivo) ? null : motivo.Trim(),
        };

        _roles.Remove(Rol.OrganizadorVerificado);
        ActualizadoEn = ahora;
        RegistrarEvento(new VerificacionActualizada(Id, EstadoVerificacion.Rechazada, Verificacion.Proveedor, Verificacion.ReferenciaExterna, ahora));
    }

    /// <summary>Retira la condición de organizador tras una revisión de seguridad o moderación (RF-23).</summary>
    public void RevocarVerificacion(string motivo, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(motivo),
            "verificacion.motivo_requerido",
            "Toda revocación debe registrar su motivo.");

        Verificacion = Verificacion with
        {
            Estado = EstadoVerificacion.Revocada,
            MayoriaDeEdadConfirmada = false,
            ActualizadaEn = ahora,
            Observacion = motivo.Trim(),
        };

        _roles.Remove(Rol.OrganizadorVerificado);
        ActualizadoEn = ahora;
        RegistrarEvento(new VerificacionActualizada(Id, EstadoVerificacion.Revocada, Verificacion.Proveedor, Verificacion.ReferenciaExterna, ahora));
    }

    // -----------------------------------------------------------------------
    // Roles, moderación y baja
    // -----------------------------------------------------------------------

    /// <summary>Concede un rol. Reservado a administración; la comprobación de permisos la hace el caso de uso.</summary>
    public void AsignarRol(Rol rol)
    {
        _roles.Add(rol);
        _roles.Add(Rol.Registrado);  // ningún rol sustituye al básico
    }

    public void RetirarRol(Rol rol)
    {
        ExcepcionDeDominio.SiNo(
            rol != Rol.Registrado,
            "usuario.rol_basico_no_retirable",
            "El rol de usuario registrado no se puede retirar.");

        _roles.Remove(rol);
    }

    /// <summary>Suspende la cuenta por decisión de moderación (RF-18).</summary>
    public void Suspender(string motivo, DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            !string.IsNullOrWhiteSpace(motivo),
            "usuario.motivo_requerido",
            "Toda suspensión debe registrar su motivo.");

        ExcepcionDeDominio.SiNo(
            Estado != EstadoCuenta.Eliminada,
            "usuario.cuenta_eliminada",
            "Esta cuenta ya no existe.");

        Estado = EstadoCuenta.Suspendida;
        ActualizadoEn = ahora;
        RegistrarEvento(new CuentaSuspendida(Id, motivo.Trim(), ahora));
    }

    /// <summary>Levanta una suspensión.</summary>
    public void Reactivar(DateTimeOffset ahora)
    {
        ExcepcionDeDominio.SiNo(
            Estado == EstadoCuenta.Suspendida,
            "usuario.no_suspendida",
            "Esta cuenta no está suspendida.");

        Estado = EstadoCuenta.Activa;
        ActualizadoEn = ahora;
    }

    /// <summary>
    /// Ejercicio del derecho de supresión (RF-03). No borra la fila: la anonimiza.
    /// </summary>
    /// <remarks>
    /// Se conserva el identificador porque las quedadas, asistencias y comentarios ya publicados
    /// lo referencian; eliminarlo dejaría ese histórico sin sentido y rompería la auditoría, que
    /// tiene su propia base legal. Lo que desaparece es todo lo que identifica a la persona.
    /// El correo se sustituye por uno irreversible y único para no chocar con el índice único.
    /// </remarks>
    public void Eliminar(DateTimeOffset ahora)
    {
        Estado = EstadoCuenta.Eliminada;
        Correo = new CorreoElectronico($"eliminada-{Id.Valor:N}@cuentas.invalid");
        NombreVisible = new NombreVisible("Cuenta eliminada");
        Ciudad = null;
        Biografia = null;
        _intereses.Clear();
        _roles.Clear();
        _roles.Add(Rol.Registrado);
        Verificacion = DatosDeVerificacion.SinIniciar;
        ActualizadoEn = ahora;

        RegistrarEvento(new CuentaEliminada(Id, ahora));
    }

    private void ExigirCuentaUtilizable() =>
        ExcepcionDeDominio.SiNo(
            Estado == EstadoCuenta.Activa,
            "usuario.cuenta_no_activa",
            "Tu cuenta no está activa. Si crees que es un error, contacta con nosotros.");

    private static string? ValidarCiudad(string? ciudad)
    {
        if (string.IsNullOrWhiteSpace(ciudad))
        {
            return null;
        }

        var limpia = ciudad.Trim();

        ExcepcionDeDominio.SiNo(
            limpia.Length <= LongitudMaximaCiudad,
            "perfil.ciudad_invalida",
            $"El nombre de la ciudad no puede superar {LongitudMaximaCiudad} caracteres.");

        return limpia;
    }
}
