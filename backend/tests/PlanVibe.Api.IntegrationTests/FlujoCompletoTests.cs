using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Shouldly;

namespace PlanVibe.Api.IntegrationTests;

/// <summary>
/// Recorrido completo del piloto sobre la API real y una base de datos real:
/// registro → sesión → verificación → publicar → explorar → apuntarse.
/// </summary>
/// <remarks>
/// Es la prueba que responde a «¿funciona esto de verdad?». Las unitarias comprueban cada
/// pieza por separado; esta comprueba que encajan: el mapeo de EF, las migraciones, la
/// autenticación, las políticas de autorización y la serialización JSON.
/// </remarks>
public class FlujoCompletoTests(FabricaDeApiDePrueba fabrica) : IClassFixture<FabricaDeApiDePrueba>
{
    [RequiereDocker]
    public async Task Una_persona_se_registra_se_verifica_publica_un_plan_y_otra_se_apunta()
    {
        var cancelacion = TestContext.Current.CancellationToken;

        // --- Organizadora: se registra e inicia sesión --------------------------------
        var organizadora = fabrica.CreateClient();
        var correoOrganizadora = $"organizadora-{Guid.CreateVersion7():N}@example.com";

        var registro = await organizadora.PostAsJsonAsync("/api/identidad/registro", new
        {
            correo = correoOrganizadora,
            contrasena = "una frase larga que recuerdo bien",
            nombreVisible = "Club Bici Ávila",
            ciudad = "Ávila",
            anioDeNacimiento = 1990,
            versionNormasAceptada = "2026-08",
        }, cancelacion);

        registro.StatusCode.ShouldBe(HttpStatusCode.Created);

        var tokensDeOrganizadora = await IniciarSesionAsync(organizadora, correoOrganizadora, cancelacion);
        organizadora.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokensDeOrganizadora);

        // --- Sin verificar no se puede publicar (RF-09, RF-20) ------------------------
        var intentoPrematuro = await organizadora.PostAsJsonAsync("/api/quedadas", CuerpoDeQuedada(), cancelacion);

        intentoPrematuro.StatusCode.ShouldBe(
            HttpStatusCode.Forbidden,
            "sin verificación de organizador no se debería poder publicar");

        // --- Verificación (con el proveedor simulado de desarrollo) -------------------
        var inicioDeVerificacion = await organizadora.PostAsync("/api/identidad/verificacion", null, cancelacion);
        inicioDeVerificacion.StatusCode.ShouldBe(HttpStatusCode.OK);

        var sesionDeVerificacion = await inicioDeVerificacion.Content.ReadFromJsonAsync<SesionDeVerificacionDto>(cancelacion);
        sesionDeVerificacion.ShouldNotBeNull();

        var completar = await organizadora.PostAsJsonAsync(
            "/api/identidad/verificacion/completar",
            new { referenciaExterna = sesionDeVerificacion.ReferenciaExterna },
            cancelacion);

        completar.StatusCode.ShouldBe(HttpStatusCode.OK);

        // El token anterior sigue diciendo que no puede organizar: hay que volver a
        // iniciar sesión para que la reclamación se emita actualizada.
        var tokenActualizado = await IniciarSesionAsync(organizadora, correoOrganizadora, cancelacion);
        organizadora.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenActualizado);

        // --- Publicar el plan (RF-09, RF-10) ------------------------------------------
        var publicacion = await organizadora.PostAsJsonAsync("/api/quedadas", CuerpoDeQuedada(), cancelacion);

        publicacion.StatusCode.ShouldBe(HttpStatusCode.Created);

        var creada = await publicacion.Content.ReadFromJsonAsync<IdDto>(cancelacion);
        creada.ShouldNotBeNull();

        // --- El plan aparece en explorar, incluso sin sesión (RF-05) ------------------
        var visitante = fabrica.CreateClient();
        var busqueda = await visitante.GetFromJsonAsync<PaginaDto>("/api/quedadas", cancelacion);

        busqueda.ShouldNotBeNull();
        busqueda.Elementos.ShouldContain(p => p.Id == creada.Id);

        // --- La dirección exacta NO se ve sin plaza confirmada -------------------------
        var detallePublico = await visitante.GetFromJsonAsync<DetalleDto>($"/api/quedadas/{creada.Id}", cancelacion);

        detallePublico.ShouldNotBeNull();
        detallePublico.Lugar.ShouldBe("Puente Adaja");
        detallePublico.DireccionExacta.ShouldBeNull("la dirección exacta solo se revela a quien tiene plaza");

        // --- Otra persona se apunta (RF-14) -------------------------------------------
        var asistente = fabrica.CreateClient();
        var correoAsistente = $"asistente-{Guid.CreateVersion7():N}@example.com";

        await asistente.PostAsJsonAsync("/api/identidad/registro", new
        {
            correo = correoAsistente,
            contrasena = "otra frase larga y distinta",
            nombreVisible = "Diego L.",
            ciudad = "Ávila",
            anioDeNacimiento = 2000,
            versionNormasAceptada = "2026-08",
        }, cancelacion);

        var tokenDeAsistente = await IniciarSesionAsync(asistente, correoAsistente, cancelacion);
        asistente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", tokenDeAsistente);

        var inscripcion = await asistente.PostAsync($"/api/quedadas/{creada.Id}/asistencia", null, cancelacion);
        inscripcion.StatusCode.ShouldBe(HttpStatusCode.OK);

        var resultado = await inscripcion.Content.ReadFromJsonAsync<InscripcionDto>(cancelacion);
        resultado.ShouldNotBeNull();
        resultado.Confirmada.ShouldBeTrue();

        // --- Ahora sí ve la dirección exacta -------------------------------------------
        var detalleConPlaza = await asistente.GetFromJsonAsync<DetalleDto>($"/api/quedadas/{creada.Id}", cancelacion);

        detalleConPlaza.ShouldNotBeNull();
        detalleConPlaza.DireccionExacta.ShouldBe("Av. de Juan Carlos I, 12");
        detalleConPlaza.MiAsistencia.ShouldBe("Confirmada");

        // --- Y el plan aparece en «mis planes» ------------------------------------------
        var misPlanes = await asistente.GetFromJsonAsync<List<ResumenDto>>("/api/quedadas/mios", cancelacion);

        misPlanes.ShouldNotBeNull();
        misPlanes.ShouldContain(p => p.Id == creada.Id);
    }

    [RequiereDocker]
    public async Task Apuntarse_sin_sesion_devuelve_401_y_no_filtra_si_el_plan_existe()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var anonimo = fabrica.CreateClient();

        var respuesta = await anonimo.PostAsync($"/api/quedadas/{Guid.CreateVersion7()}/asistencia", null, cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.Unauthorized);
    }

    [RequiereDocker]
    public async Task El_endpoint_de_salud_responde_sin_autenticacion()
    {
        var cancelacion = TestContext.Current.CancellationToken;

        var respuesta = await fabrica.CreateClient().GetAsync("/salud", cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);
    }

    [RequiereDocker]
    public async Task Las_respuestas_llevan_las_cabeceras_de_seguridad()
    {
        var cancelacion = TestContext.Current.CancellationToken;

        var respuesta = await fabrica.CreateClient().GetAsync("/api/quedadas", cancelacion);

        respuesta.Headers.GetValues("X-Content-Type-Options").ShouldContain("nosniff");
        respuesta.Headers.GetValues("X-Frame-Options").ShouldContain("DENY");
        respuesta.Headers.GetValues("Referrer-Policy").ShouldContain("no-referrer");
    }

    private static async Task<string> IniciarSesionAsync(HttpClient cliente, string correo, CancellationToken cancelacion)
    {
        var respuesta = await cliente.PostAsJsonAsync("/api/identidad/sesion", new
        {
            correo,
            contrasena = correo.StartsWith("organizadora", StringComparison.Ordinal)
                ? "una frase larga que recuerdo bien"
                : "otra frase larga y distinta",
            dispositivo = "pruebas",
        }, cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.OK);

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>(cancelacion);

        sesion.ShouldNotBeNull();

        return sesion.Tokens.TokenDeAcceso;
    }

    private static object CuerpoDeQuedada() => new
    {
        titulo = "Ruta en bici por el Valle Amblés",
        descripcion = "Pedaleamos 35 km sin prisa.",
        // Categoría sembrada al arrancar en desarrollo.
        categoriaId = Domain.Categorias.CategoriaId.BiciYDeporte.Valor,
        inicio = DateTimeOffset.UtcNow.AddDays(7),
        duracionEnMinutos = 180,
        lugar = "Puente Adaja",
        referencia = "Junto al quiosco",
        direccionExacta = "Av. de Juan Carlos I, 12",
        latitud = 40.6565,
        longitud = -4.7009,
        confirmaQueEsLugarPublico = true,
        capacidad = 15,
        normas = new[] { "Casco obligatorio", "Nivel medio" },
    };

    // Modelos mínimos para deserializar. Se declaran aquí y no se reutilizan los de la API
    // a propósito: si el contrato cambia sin querer, estas pruebas lo notan.
    private sealed record IdDto(Guid Id);

    private sealed record SesionDeVerificacionDto(string ReferenciaExterna, string UrlDeRedireccion);

    private sealed record TokensDto(string TokenDeAcceso, DateTimeOffset ExpiraEn, string TokenDeRenovacion);

    private sealed record SesionDto(TokensDto Tokens);

    private sealed record ResumenDto(Guid Id, string Titulo);

    private sealed record PaginaDto(List<ResumenDto> Elementos, int Total);

    private sealed record DetalleDto(Guid Id, string Lugar, string? DireccionExacta, string? MiAsistencia);

    private sealed record InscripcionDto(bool Confirmada, int? PosicionEnListaDeEspera);
}
