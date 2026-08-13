using PlanVibe.Domain.Common;
using PlanVibe.Domain.Usuarios;
using PlanVibe.Domain.Usuarios.ObjetosDeValor;
using Shouldly;

namespace PlanVibe.Domain.UnitTests.Usuarios;

/// <summary>
/// Reglas del agregado <see cref="Usuario"/>: alta, perfil, verificación de organizador y moderación.
/// </summary>
public class UsuarioTests
{
    private static readonly DateTimeOffset Ahora = new(2026, 9, 12, 10, 0, 0, TimeSpan.Zero);

    private static Usuario Registrar(int anioDeNacimiento = 1996) => Usuario.Registrar(
        UsuarioId.Nuevo(),
        new CorreoElectronico("Lucia.Ramos@Example.COM"),
        new NombreVisible("Lucía R."),
        ciudad: "Ávila",
        anioDeNacimiento: anioDeNacimiento,
        versionNormasAceptada: "2026-08",
        ahora: Ahora);

    // ---------------------------------------------------------------------
    // Alta
    // ---------------------------------------------------------------------

    [Fact]
    public void Registrarse_deja_la_cuenta_activa_y_sin_verificar()  // RF-01
    {
        var usuario = Registrar();

        usuario.Estado.ShouldBe(EstadoCuenta.Activa);
        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.NoIniciada);
        usuario.Roles.ShouldBe([Rol.Registrado]);
    }

    [Fact]
    public void El_correo_se_normaliza_para_que_no_haya_cuentas_duplicadas_por_mayusculas()
    {
        var usuario = Registrar();

        usuario.Correo.Valor.ShouldBe("lucia.ramos@example.com");
    }

    [Theory]
    [InlineData("sin-arroba")]
    [InlineData("dos@@arrobas.com")]
    [InlineData("")]
    [InlineData("   ")]
    public void Un_correo_con_formato_imposible_se_rechaza(string correo)
    {
        var error = Should.Throw<ExcepcionDeDominio>(() => new CorreoElectronico(correo));

        error.Codigo.ShouldBe("correo.formato_invalido");
    }

    [Fact]
    public void No_se_admiten_altas_por_debajo_de_la_edad_minima()
    {
        // El público del piloto empieza en 16 años (docs/00-vision-y-mvp.md).
        var error = Should.Throw<ExcepcionDeDominio>(() => Registrar(anioDeNacimiento: Ahora.Year - 12));

        error.Codigo.ShouldBe("usuario.edad_minima_no_alcanzada");
    }

    [Fact]
    public void El_alta_guarda_que_se_aceptaron_las_normas()  // RF-04, trazabilidad
    {
        var usuario = Registrar();

        usuario.VersionNormasAceptada.ShouldBe("2026-08");
        usuario.NormasAceptadasEn.ShouldBe(Ahora);
    }

    // ---------------------------------------------------------------------
    // Verificación de organizador
    // ---------------------------------------------------------------------

    [Fact]
    public void Iniciar_la_verificacion_la_deja_pendiente_de_respuesta_del_proveedor()  // RF-21
    {
        var usuario = Registrar();

        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Pendiente);
        usuario.Verificacion.Proveedor.ShouldBe("proveedor-simulado");
        usuario.Verificacion.ReferenciaExterna.ShouldBe("ref-externa-123");
        usuario.PuedeOrganizar.ShouldBeFalse();
    }

    [Fact]
    public void Una_verificacion_correcta_convierte_al_usuario_en_organizador_verificado()  // RF-09, RF-20
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);

        usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: true, Ahora);

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Verificada);
        usuario.Roles.ShouldContain(Rol.OrganizadorVerificado);
        usuario.PuedeOrganizar.ShouldBeTrue();
    }

    [Fact]
    public void Una_verificacion_de_alguien_menor_de_edad_no_habilita_para_organizar()  // RF-24
    {
        var usuario = Registrar(anioDeNacimiento: Ahora.Year - 17);
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);

        usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: false, Ahora);

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Verificada);
        usuario.PuedeOrganizar.ShouldBeFalse("la identidad está probada, pero el piloto exige ser mayor de edad");
        usuario.Roles.ShouldNotContain(Rol.OrganizadorVerificado);
    }

    [Fact]
    public void No_se_puede_confirmar_una_verificacion_que_no_se_habia_iniciado()
    {
        var usuario = Registrar();

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: true, Ahora));

        error.Codigo.ShouldBe("verificacion.no_iniciada");
    }

    [Fact]
    public void Una_verificacion_rechazada_guarda_el_motivo_y_no_habilita_para_organizar()
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);

        usuario.RechazarVerificacion("El documento no se pudo leer", Ahora);

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Rechazada);
        usuario.Verificacion.Observacion.ShouldBe("El documento no se pudo leer");
        usuario.PuedeOrganizar.ShouldBeFalse();
    }

    [Fact]
    public void Se_puede_reintentar_la_verificacion_despues_de_un_rechazo()
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "intento-1", Ahora);
        usuario.RechazarVerificacion("Documento ilegible", Ahora);

        usuario.IniciarVerificacion("proveedor-simulado", "intento-2", Ahora.AddMinutes(5));

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Pendiente);
        usuario.Verificacion.ReferenciaExterna.ShouldBe("intento-2");
    }

    [Fact]
    public void Revocar_la_verificacion_retira_la_condicion_de_organizador()  // RF-23
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);
        usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: true, Ahora);

        usuario.RevocarVerificacion("Suplantación detectada en revisión de seguridad", Ahora.AddDays(3));

        usuario.Verificacion.Estado.ShouldBe(EstadoVerificacion.Revocada);
        usuario.PuedeOrganizar.ShouldBeFalse();
        usuario.Roles.ShouldNotContain(Rol.OrganizadorVerificado);
    }

    [Fact]
    public void La_verificacion_no_guarda_ningun_dato_del_documento()  // RF-22 y minimización de datos
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);
        usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: true, Ahora);

        // El objeto de valor solo expone estado, proveedor, referencia técnica, fecha y mayoría de edad.
        // Esta prueba falla en cuanto alguien añada un campo con datos documentales.
        var propiedades = typeof(DatosDeVerificacion)
            .GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            .Select(p => p.Name)
            .ToArray();

        propiedades.ShouldBe(
            [
                nameof(DatosDeVerificacion.Estado),
                nameof(DatosDeVerificacion.Proveedor),
                nameof(DatosDeVerificacion.ReferenciaExterna),
                nameof(DatosDeVerificacion.MayoriaDeEdadConfirmada),
                nameof(DatosDeVerificacion.ActualizadaEn),
                nameof(DatosDeVerificacion.Observacion),
            ],
            ignoreOrder: true);
    }

    // ---------------------------------------------------------------------
    // Moderación y ciclo de vida de la cuenta
    // ---------------------------------------------------------------------

    [Fact]
    public void Una_cuenta_suspendida_no_puede_organizar()  // RF-18
    {
        var usuario = Registrar();
        usuario.IniciarVerificacion("proveedor-simulado", "ref-externa-123", Ahora);
        usuario.ConfirmarVerificacion(mayoriaDeEdadConfirmada: true, Ahora);

        usuario.Suspender("Reiteradas normas de convivencia incumplidas", Ahora);

        usuario.Estado.ShouldBe(EstadoCuenta.Suspendida);
        usuario.PuedeOrganizar.ShouldBeFalse();
    }

    [Fact]
    public void Eliminar_la_cuenta_borra_los_datos_personales_y_conserva_el_identificador()  // RF-03 y derecho de supresión
    {
        var usuario = Registrar();
        usuario.ActualizarPerfil(new NombreVisible("Lucía R."), "Ávila", "Me gusta la montaña", ["Senderismo"], Ahora);

        usuario.Eliminar(Ahora);

        usuario.Estado.ShouldBe(EstadoCuenta.Eliminada);
        usuario.NombreVisible.Valor.ShouldBe("Cuenta eliminada");
        usuario.Correo.Valor.ShouldNotContain("lucia", Case.Insensitive);
        usuario.Biografia.ShouldBeNull();
        usuario.Intereses.ShouldBeEmpty();
        usuario.Ciudad.ShouldBeNull();
    }

    [Fact]
    public void Un_moderador_tiene_su_rol_ademas_del_de_usuario_registrado()
    {
        var usuario = Registrar();

        usuario.AsignarRol(Rol.Moderador);

        usuario.Roles.ShouldBe([Rol.Registrado, Rol.Moderador], ignoreOrder: true);
    }

    // ---------------------------------------------------------------------
    // Perfil
    // ---------------------------------------------------------------------

    [Fact]
    public void El_perfil_admite_hasta_un_numero_razonable_de_intereses()  // RF-02
    {
        var usuario = Registrar();
        var demasiados = Enumerable.Range(1, Usuario.MaximoIntereses + 1).Select(n => $"Interés {n}").ToArray();

        var error = Should.Throw<ExcepcionDeDominio>(() =>
            usuario.ActualizarPerfil(new NombreVisible("Lucía R."), "Ávila", null, demasiados, Ahora));

        error.Codigo.ShouldBe("perfil.demasiados_intereses");
    }

    [Theory]
    [InlineData("a")]
    [InlineData("")]
    public void El_nombre_visible_debe_ser_utilizable(string nombre)
    {
        var error = Should.Throw<ExcepcionDeDominio>(() => new NombreVisible(nombre));

        error.Codigo.ShouldBe("nombre_visible.invalido");
    }
}
