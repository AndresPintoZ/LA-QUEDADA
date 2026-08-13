using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Application.Quedadas.Comandos;
using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;
using Shouldly;

namespace PlanVibe.Application.UnitTests.Quedadas;

/// <summary>
/// Pruebas del caso de uso de publicación.
/// </summary>
/// <remarks>
/// Aquí se comprueba la <em>orquestación</em>, no las reglas de negocio: que se compruebe
/// quién pide la acción, que se guarde y que quede auditado. Las reglas del agregado ya
/// tienen sus propias pruebas en el proyecto de dominio y no se repiten.
/// </remarks>
public class CrearQuedadaManejadorTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    private readonly IRepositorioDeUsuarios _usuarios = Substitute.For<IRepositorioDeUsuarios>();
    private readonly IRepositorioDeQuedadas _quedadas = Substitute.For<IRepositorioDeQuedadas>();
    private readonly IUnidadDeTrabajo _unidadDeTrabajo = Substitute.For<IUnidadDeTrabajo>();
    private readonly IContextoDeUsuarioActual _contexto = Substitute.For<IContextoDeUsuarioActual>();
    private readonly IRegistroDeAuditoria _auditoria = Substitute.For<IRegistroDeAuditoria>();

    private readonly CrearQuedadaManejador _manejador;

    public CrearQuedadaManejadorTests()
    {
        var reloj = new RelojDePrueba(Ahora);

        _manejador = new CrearQuedadaManejador(
            _usuarios,
            _quedadas,
            _unidadDeTrabajo,
            _contexto,
            _auditoria,
            reloj,
            NullLogger<CrearQuedadaManejador>.Instance);
    }

    [Fact]
    public async Task Una_persona_sin_sesion_no_puede_publicar()
    {
        _contexto.UsuarioId.Returns((UsuarioId?)null);

        var error = await Should.ThrowAsync<AccesoDenegadoException>(
            () => _manejador.ManejarAsync(ComandoValido(), TestContext.Current.CancellationToken));

        error.Message.ShouldContain("iniciar sesión");

        // Nada debe haberse guardado ni auditado.
        await _quedadas.DidNotReceive().AgregarAsync(Arg.Any<Quedada>(), Arg.Any<CancellationToken>());
        await _unidadDeTrabajo.DidNotReceive().GuardarCambiosAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Una_persona_sin_verificar_no_puede_publicar()  // RF-09, RF-20
    {
        var usuario = UsuarioDePrueba(verificado: false);
        PrepararUsuario(usuario);

        var error = await Should.ThrowAsync<AccesoDenegadoException>(
            () => _manejador.ManejarAsync(ComandoValido(), TestContext.Current.CancellationToken));

        error.Message.ShouldContain("verificación");
        await _quedadas.DidNotReceive().AgregarAsync(Arg.Any<Quedada>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Una_persona_verificada_pero_menor_de_edad_no_puede_publicar()  // RF-24
    {
        var usuario = UsuarioDePrueba(verificado: true, mayorDeEdad: false);
        PrepararUsuario(usuario);

        await Should.ThrowAsync<AccesoDenegadoException>(
            () => _manejador.ManejarAsync(ComandoValido(), TestContext.Current.CancellationToken));

        await _quedadas.DidNotReceive().AgregarAsync(Arg.Any<Quedada>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Una_organizadora_verificada_publica_y_queda_auditado()
    {
        var usuario = UsuarioDePrueba(verificado: true);
        PrepararUsuario(usuario);

        Quedada? guardada = null;
        await _quedadas.AgregarAsync(Arg.Do<Quedada>(q => guardada = q), Arg.Any<CancellationToken>());

        var id = await _manejador.ManejarAsync(ComandoValido(), TestContext.Current.CancellationToken);

        id.ShouldNotBe(Guid.Empty);
        guardada.ShouldNotBeNull();
        guardada.Titulo.ShouldBe("Ruta en bici por el Valle Amblés");
        guardada.OrganizadorId.ShouldBe(usuario.Id);

        await _unidadDeTrabajo.Received(1).GuardarCambiosAsync(Arg.Any<CancellationToken>());

        // La auditoría es un requisito, no un extra (RNF-04).
        await _auditoria.Received(1).RegistrarAsync(
            usuario.Id,
            "quedada.publicada",
            "Quedada",
            Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Se_guarda_antes_de_auditar_para_no_registrar_algo_que_no_ocurrio()
    {
        var usuario = UsuarioDePrueba(verificado: true);
        PrepararUsuario(usuario);

        var orden = new List<string>();

        _unidadDeTrabajo.GuardarCambiosAsync(Arg.Any<CancellationToken>())
            .Returns(_ => { orden.Add("guardar"); return 1; });

        _auditoria.RegistrarAsync(
            Arg.Any<UsuarioId?>(), Arg.Any<string>(), Arg.Any<string>(), Arg.Any<string>(),
            Arg.Any<IReadOnlyDictionary<string, string>>(), Arg.Any<CancellationToken>())
            .Returns(_ => { orden.Add("auditar"); return Task.CompletedTask; });

        await _manejador.ManejarAsync(ComandoValido(), TestContext.Current.CancellationToken);

        orden.ShouldBe(["guardar", "auditar"]);
    }

    private void PrepararUsuario(Usuario usuario)
    {
        _contexto.UsuarioId.Returns(usuario.Id);
        _usuarios.ObtenerPorIdAsync(usuario.Id, Arg.Any<CancellationToken>()).Returns(usuario);
    }

    private static Usuario UsuarioDePrueba(bool verificado, bool mayorDeEdad = true)
    {
        var usuario = Usuario.Registrar(
            UsuarioId.Nuevo(),
            new CorreoElectronico("organizadora@example.com"),
            new NombreVisible("Club Bici Ávila"),
            "Ávila",
            anioDeNacimiento: 1990,
            versionNormasAceptada: "2026-08",
            Ahora);

        if (verificado)
        {
            usuario.IniciarVerificacion("proveedor-de-prueba", "ref-1", Ahora);
            usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: mayorDeEdad, Ahora);
        }

        return usuario;
    }

    private static CrearQuedada ComandoValido() => new(
        Titulo: "Ruta en bici por el Valle Amblés",
        Descripcion: "Pedaleamos 35 km sin prisa.",
        CategoriaId: CategoriaId.BiciYDeporte.Valor,
        Inicio: Ahora.AddDays(4),
        DuracionEnMinutos: 180,
        Lugar: "Puente Adaja",
        Referencia: "Junto al quiosco",
        DireccionExacta: "Av. de Juan Carlos I, 12",
        Latitud: 40.6565,
        Longitud: -4.7009,
        ConfirmaQueEsLugarPublico: true,
        Capacidad: 15,
        Normas: ["Casco obligatorio"]);
}

/// <summary>Reloj fijo para que las pruebas no dependan de la hora a la que se ejecuten.</summary>
internal sealed class RelojDePrueba(DateTimeOffset ahora) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => ahora;
}
