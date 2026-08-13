using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Common;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Quedadas.Eventos;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;
using PlanVibe.Domain.Usuarios;
using Shouldly;

namespace PlanVibe.Domain.UnitTests.Quedadas;

/// <summary>
/// Reglas de negocio del agregado <see cref="Quedada"/>.
/// Cada prueba nombra la regla y, cuando procede, el requisito funcional que la origina.
/// </summary>
public class QuedadaTests
{
    // ---------------------------------------------------------------------
    // Creación
    // ---------------------------------------------------------------------

    [Fact]
    public void Crear_deja_la_quedada_publicada_con_el_organizador_ya_apuntado()
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 15);

        quedada.Estado.ShouldBe(EstadoQuedada.Publicada);
        quedada.PlazasOcupadas.ShouldBe(1, "el organizador ocupa una plaza desde el principio");
        quedada.PlazasLibres.ShouldBe(14);
        quedada.EstaApuntado(Escenario.Organizadora.Id).ShouldBeTrue();
    }

    [Fact]
    public void Crear_falla_si_el_organizador_no_esta_verificado()  // RF-09, RF-20
    {
        var sinVerificar = Escenario.Organizadora with { EstaVerificado = false };

        var error = Should.Throw<ExcepcionDeDominio>(() => Escenario.Quedada(organizador: sinVerificar));

        error.Codigo.ShouldBe("quedada.organizador_no_verificado");
    }

    [Fact]
    public void Crear_falla_si_el_organizador_es_menor_de_edad()  // RF-24
    {
        var menor = Escenario.Organizadora with { EsMayorDeEdad = false };

        var error = Should.Throw<ExcepcionDeDominio>(() => Escenario.Quedada(organizador: menor));

        error.Codigo.ShouldBe("quedada.organizador_menor_de_edad");
    }

    [Fact]
    public void Crear_falla_si_la_fecha_de_inicio_ya_ha_pasado()
    {
        var error = Should.Throw<ExcepcionDeDominio>(() =>
            Escenario.Quedada(inicio: Escenario.Ahora.AddHours(-1)));

        error.Codigo.ShouldBe("quedada.fecha_en_pasado");
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ab")]
    public void Crear_falla_si_el_titulo_no_es_utilizable(string titulo)
    {
        var error = Should.Throw<ExcepcionDeDominio>(() => Escenario.Quedada(titulo: titulo));

        error.Codigo.ShouldBe("quedada.titulo_invalido");
    }

    [Fact]
    public void Crear_registra_el_evento_de_quedada_publicada()
    {
        var quedada = Escenario.Quedada();

        quedada.EventosDeDominio.OfType<QuedadaPublicada>().ShouldHaveSingleItem()
            .QuedadaId.ShouldBe(quedada.Id);
    }

    // ---------------------------------------------------------------------
    // Apuntarse
    // ---------------------------------------------------------------------

    [Fact]
    public void Apuntarse_confirma_la_plaza_mientras_queden_libres()  // RF-14
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 3);

        var resultado = quedada.Apuntar(Escenario.Ana, Escenario.Ahora);

        resultado.Confirmada.ShouldBeTrue();
        resultado.PosicionEnListaDeEspera.ShouldBeNull();
        quedada.PlazasLibres.ShouldBe(1);
    }

    [Fact]
    public void Apuntarse_a_una_quedada_llena_entra_en_lista_de_espera_por_orden_de_llegada()  // RF-15
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 2);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);   // llena la quedada

        var deBruno = quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);
        var deCarla = quedada.Apuntar(Escenario.Carla, Escenario.Ahora);

        deBruno.Confirmada.ShouldBeFalse();
        deBruno.PosicionEnListaDeEspera.ShouldBe(1);
        deCarla.PosicionEnListaDeEspera.ShouldBe(2);
        quedada.PlazasLibres.ShouldBe(0);
    }

    [Fact]
    public void Apuntarse_dos_veces_no_esta_permitido()
    {
        var quedada = Escenario.Quedada();
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);

        var error = Should.Throw<ExcepcionDeDominio>(() => quedada.Apuntar(Escenario.Ana, Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.ya_apuntado");
    }

    [Fact]
    public void Apuntarse_a_una_quedada_cancelada_no_esta_permitido()
    {
        var quedada = Escenario.Quedada();
        quedada.Cancelar(Escenario.Organizadora.Id, "Llueve", Escenario.Ahora);

        var error = Should.Throw<ExcepcionDeDominio>(() => quedada.Apuntar(Escenario.Ana, Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.no_admite_inscripciones");
    }

    [Fact]
    public void Apuntarse_cuando_la_quedada_ya_ha_empezado_no_esta_permitido()
    {
        var quedada = Escenario.Quedada(inicio: Escenario.Ahora.AddHours(2));

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.Apuntar(Escenario.Ana, Escenario.Ahora.AddHours(3)));

        error.Codigo.ShouldBe("quedada.ya_comenzada");
    }

    [Fact]
    public void Apuntarse_registra_un_evento_de_dominio_con_la_persona_apuntada()
    {
        var quedada = Escenario.Quedada();
        quedada.LimpiarEventos();

        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);

        quedada.EventosDeDominio.OfType<UsuarioApuntado>().ShouldHaveSingleItem()
            .UsuarioId.ShouldBe(Escenario.Ana);
    }

    // ---------------------------------------------------------------------
    // Abandonar
    // ---------------------------------------------------------------------

    [Fact]
    public void Abandonar_libera_la_plaza()  // RF-14
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 3);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);

        quedada.Abandonar(Escenario.Ana, Escenario.Ahora);

        quedada.PlazasLibres.ShouldBe(2);
        quedada.EstaApuntado(Escenario.Ana).ShouldBeFalse();
    }

    [Fact]
    public void Abandonar_promueve_a_la_primera_persona_de_la_lista_de_espera()  // RF-15
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 2);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);  // queda en espera, posición 1
        quedada.Apuntar(Escenario.Carla, Escenario.Ahora);  // queda en espera, posición 2
        quedada.LimpiarEventos();

        quedada.Abandonar(Escenario.Ana, Escenario.Ahora);

        quedada.EstadoDe(Escenario.Bruno).ShouldBe(EstadoAsistencia.Confirmada);
        quedada.PosicionEnListaDeEspera(Escenario.Carla).ShouldBe(1, "Carla avanza al liberarse Bruno");
        quedada.PlazasLibres.ShouldBe(0);
        quedada.EventosDeDominio.OfType<AsistentePromovido>().ShouldHaveSingleItem()
            .UsuarioId.ShouldBe(Escenario.Bruno);
    }

    [Fact]
    public void Abandonar_desde_la_lista_de_espera_no_promueve_a_nadie()
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 2);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);
        quedada.Apuntar(Escenario.Carla, Escenario.Ahora);
        quedada.LimpiarEventos();

        quedada.Abandonar(Escenario.Bruno, Escenario.Ahora);

        quedada.EventosDeDominio.OfType<AsistentePromovido>().ShouldBeEmpty();
        quedada.PosicionEnListaDeEspera(Escenario.Carla).ShouldBe(1);
    }

    [Fact]
    public void El_organizador_no_puede_abandonar_su_propia_quedada()
    {
        var quedada = Escenario.Quedada();

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.Abandonar(Escenario.Organizadora.Id, Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.organizador_no_puede_abandonar");
    }

    [Fact]
    public void Abandonar_sin_estar_apuntado_no_esta_permitido()
    {
        var quedada = Escenario.Quedada();

        var error = Should.Throw<ExcepcionDeDominio>(() => quedada.Abandonar(Escenario.Ana, Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.no_apuntado");
    }

    [Fact]
    public void Se_puede_volver_a_apuntar_despues_de_haber_abandonado()
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 3);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Abandonar(Escenario.Ana, Escenario.Ahora);

        var resultado = quedada.Apuntar(Escenario.Ana, Escenario.Ahora);

        resultado.Confirmada.ShouldBeTrue();
        quedada.PlazasOcupadas.ShouldBe(2);
    }

    // ---------------------------------------------------------------------
    // Cancelación
    // ---------------------------------------------------------------------

    [Fact]
    public void El_organizador_puede_cancelar_y_se_avisa_a_los_asistentes()  // RF-11
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 3);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.LimpiarEventos();

        quedada.Cancelar(Escenario.Organizadora.Id, "Aviso de tormenta", Escenario.Ahora);

        quedada.Estado.ShouldBe(EstadoQuedada.Cancelada);
        quedada.MotivoDeCancelacion.ShouldBe("Aviso de tormenta");

        var evento = quedada.EventosDeDominio.OfType<QuedadaCancelada>().ShouldHaveSingleItem();
        evento.UsuariosAAvisar.ShouldContain(Escenario.Ana);
        evento.UsuariosAAvisar.ShouldNotContain(Escenario.Organizadora.Id, "quien cancela no necesita su propio aviso");
    }

    [Fact]
    public void Solo_el_organizador_puede_cancelar_su_quedada()
    {
        var quedada = Escenario.Quedada();

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.Cancelar(Escenario.Ana, "Porque sí", Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.solo_el_organizador");
    }

    [Fact]
    public void Cancelar_una_quedada_ya_cancelada_no_esta_permitido()
    {
        var quedada = Escenario.Quedada();
        quedada.Cancelar(Escenario.Organizadora.Id, "Llueve", Escenario.Ahora);

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.Cancelar(Escenario.Organizadora.Id, "Llueve otra vez", Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.no_modificable");
    }

    [Fact]
    public void Cancelar_exige_un_motivo()  // el motivo queda en la auditoría (RNF-04)
    {
        var quedada = Escenario.Quedada();

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.Cancelar(Escenario.Organizadora.Id, "  ", Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.motivo_requerido");
    }

    // ---------------------------------------------------------------------
    // Privacidad del punto de encuentro
    // ---------------------------------------------------------------------

    [Fact]
    public void La_direccion_exacta_solo_se_revela_a_quien_tiene_plaza_confirmada()
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 2);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);  // se queda en lista de espera

        quedada.DireccionExactaVisiblePara(Escenario.Ana).ShouldNotBeNull();
        quedada.DireccionExactaVisiblePara(Escenario.Organizadora.Id).ShouldNotBeNull();
        quedada.DireccionExactaVisiblePara(Escenario.Bruno).ShouldBeNull();
        quedada.DireccionExactaVisiblePara(Escenario.Carla).ShouldBeNull();
    }

    // ---------------------------------------------------------------------
    // Edición
    // ---------------------------------------------------------------------

    [Fact]
    public void Reducir_la_capacidad_por_debajo_de_las_plazas_ya_concedidas_no_esta_permitido()  // RF-11
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 10);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);  // 3 plazas ocupadas con la organizadora

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            quedada.CambiarCapacidad(Escenario.Organizadora.Id, new Capacidad(2), Escenario.Ahora));

        error.Codigo.ShouldBe("quedada.capacidad_menor_que_asistentes");
    }

    [Fact]
    public void Reducir_la_capacidad_hasta_las_plazas_ya_concedidas_si_esta_permitido()  // RF-11
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 10);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);

        quedada.CambiarCapacidad(Escenario.Organizadora.Id, new Capacidad(3), Escenario.Ahora);

        quedada.Capacidad.Maximo.ShouldBe(3);
        quedada.PlazasLibres.ShouldBe(0, "la quedada queda cerrada, pero nadie pierde su plaza");
    }

    [Fact]
    public void Ampliar_la_capacidad_promueve_a_quienes_esperan()  // RF-15
    {
        var quedada = Escenario.Quedada(capacidadMaxima: 2);
        quedada.Apuntar(Escenario.Ana, Escenario.Ahora);
        quedada.Apuntar(Escenario.Bruno, Escenario.Ahora);
        quedada.Apuntar(Escenario.Carla, Escenario.Ahora);
        quedada.LimpiarEventos();

        quedada.CambiarCapacidad(Escenario.Organizadora.Id, new Capacidad(4), Escenario.Ahora);

        quedada.EstadoDe(Escenario.Bruno).ShouldBe(EstadoAsistencia.Confirmada);
        quedada.EstadoDe(Escenario.Carla).ShouldBe(EstadoAsistencia.Confirmada);
        quedada.EventosDeDominio.OfType<AsistentePromovido>().Count().ShouldBe(2);
    }
}

/// <summary>
/// Constructor de escenarios de prueba. Concentra los datos por defecto para que cada prueba
/// solo tenga que expresar lo que la diferencia de las demás.
/// </summary>
internal static class Escenario
{
    public static readonly DateTimeOffset Ahora = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    public static readonly Organizador Organizadora = new(
        Id: new UsuarioId(Guid.Parse("11111111-1111-1111-1111-111111111111")),
        EstaVerificado: true,
        EsMayorDeEdad: true);

    public static readonly UsuarioId Ana = new(Guid.Parse("22222222-2222-2222-2222-222222222222"));
    public static readonly UsuarioId Bruno = new(Guid.Parse("33333333-3333-3333-3333-333333333333"));
    public static readonly UsuarioId Carla = new(Guid.Parse("44444444-4444-4444-4444-444444444444"));

    public static Quedada Quedada(
        Organizador? organizador = null,
        string titulo = "Ruta en bici por el Valle Amblés",
        int capacidadMaxima = 15,
        DateTimeOffset? inicio = null)
        => Domain.Quedadas.Quedada.Crear(
            id: QuedadaId.Nuevo(),
            organizador: organizador ?? Organizadora,
            titulo: titulo,
            descripcion: "Pedaleamos 35 km sin prisa. Si te descuelgas, te esperamos.",
            categoria: CategoriaId.BiciYDeporte,
            cuando: new FranjaTemporal(inicio ?? Ahora.AddDays(4), TimeSpan.FromHours(3)),
            donde: PuntoEncuentro(),
            capacidad: new Capacidad(capacidadMaxima),
            normas: new NormasDelPlan(["Casco obligatorio", "Nivel medio"]),
            ahora: Ahora);

    public static PuntoEncuentro PuntoEncuentro() => Domain.Quedadas.ObjetosDeValor.PuntoEncuentro.Crear(
        lugar: "Puente Adaja",
        referencia: "Junto al quiosco",
        direccionExacta: "Av. de Juan Carlos I, 12",
        coordenadas: new Coordenadas(40.6565, -4.7009),
        esLugarPublico: true);
}
