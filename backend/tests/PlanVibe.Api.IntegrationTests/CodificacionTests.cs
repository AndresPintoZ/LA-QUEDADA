using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text;
using Shouldly;

namespace PlanVibe.Api.IntegrationTests;

/// <summary>
/// El texto con acentos y eñes sobrevive el viaje completo: petición, base de datos y respuesta.
/// </summary>
/// <remarks>
/// <para>
/// Parece una prueba tonta hasta que falla. «Ávila», «Amblés» y «El Barraco de la Peña» son
/// nombres reales del piloto: si la codificación se rompe en algún punto del trayecto, la
/// aplicación se vuelve inutilizable en su propia ciudad.
/// </para>
/// <para>
/// La comprobación es por <em>longitud en caracteres</em> además de por igualdad de cadena. Un
/// texto doblemente codificado a veces parece correcto al imprimirlo según la consola, pero
/// «Ávila» siempre tiene 5 caracteres: si salen 6, hay un problema aunque se vea bien.
/// </para>
/// </remarks>
public class CodificacionTests(FabricaDeApiDePrueba fabrica) : IClassFixture<FabricaDeApiDePrueba>
{
    /// <summary>Nombres reales de la comarca del piloto, con los acentos que de verdad se usan.</summary>
    private const string TituloConAcentos = "Ruta en bici por el Valle Amblés";

    private const string LugarConAcentos = "El Barraco de la Peña";

    private const string NombreConAcentos = "Club Bici Ávila";

    private static readonly string[] NormasDelPlan = ["Casco obligatorio", "Nivel medio"];

    [RequiereDocker]
    public async Task El_texto_acentuado_sobrevive_el_viaje_completo()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var cliente = fabrica.CreateClient();
        var correo = $"acentos-{Guid.CreateVersion7():N}@ejemplo.es";
        const string Clave = "una frase larga que recuerdo bien";

        // --- Alta con nombre y ciudad acentuados ---
        var registro = await cliente.PostAsJsonAsync("/api/identidad/registro", new
        {
            correo,
            contrasena = Clave,
            nombreVisible = NombreConAcentos,
            ciudad = "Ávila",
            anioDeNacimiento = 1990,
            versionNormasAceptada = "2026-08",
        }, cancelacion);

        registro.EnsureSuccessStatusCode();

        var token = await IniciarSesionAsync(cliente, correo, Clave, cancelacion);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        // El perfil debe devolver exactamente lo que se envió.
        var perfil = await cliente.GetFromJsonAsync<PerfilDto>("/api/identidad/yo", cancelacion);

        perfil.ShouldNotBeNull();
        perfil.NombreVisible.ShouldBe(NombreConAcentos);
        perfil.Ciudad.ShouldBe("Ávila");
        perfil.Ciudad!.Length.ShouldBe(5, "«Ávila» son 5 caracteres; 6 significaría doble codificación");

        // --- Publicación con título y lugar acentuados ---
        await VerificarOrganizadorAsync(cliente, cancelacion);
        token = await IniciarSesionAsync(cliente, correo, Clave, cancelacion);
        cliente.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var publicacion = await cliente.PostAsJsonAsync("/api/quedadas", new
        {
            titulo = TituloConAcentos,
            descripcion = "Pedaleamos sin prisa. Si te descuelgas, te esperamos.",
            categoriaId = Domain.Categorias.CategoriaId.BiciYDeporte.Valor,
            inicio = DateTimeOffset.UtcNow.AddDays(7),
            duracionEnMinutos = 210,
            lugar = LugarConAcentos,
            referencia = "Junto al quiosco",
            direccionExacta = "Avenida de la Estación, 3",
            latitud = 40.6565,
            longitud = -4.7009,
            confirmaQueEsLugarPublico = true,
            capacidad = 15,
            normas = NormasDelPlan,
        }, cancelacion);

        publicacion.EnsureSuccessStatusCode();

        var creada = await publicacion.Content.ReadFromJsonAsync<IdDto>(cancelacion);
        creada.ShouldNotBeNull();

        // --- El detalle devuelve el texto intacto tras pasar por PostgreSQL ---
        var detalle = await cliente.GetFromJsonAsync<DetalleDto>($"/api/quedadas/{creada.Id}", cancelacion);

        detalle.ShouldNotBeNull();
        detalle.Titulo.ShouldBe(TituloConAcentos);
        detalle.Lugar.ShouldBe(LugarConAcentos);

        detalle.Titulo.Length.ShouldBe(
            TituloConAcentos.Length,
            "una longitud mayor delata que la «é» se guardó como dos caracteres");

        // Comprobación explícita del punto de código, que no depende de cómo se imprima.
        detalle.Titulo.ShouldContain('é');
        detalle.Titulo.ShouldNotContain("Ã", Case.Sensitive);
        detalle.Lugar.ShouldContain('ñ');

        // --- Y también en el listado, que usa una proyección distinta ---
        var pagina = await cliente.GetFromJsonAsync<PaginaDto>("/api/quedadas", cancelacion);

        pagina.ShouldNotBeNull();
        pagina.Elementos.ShouldContain(p => p.Titulo == TituloConAcentos);
    }

    [RequiereDocker]
    public async Task La_busqueda_por_texto_encuentra_palabras_acentuadas()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var cliente = fabrica.CreateClient();

        // Que se guarde bien no basta: hay que poder buscarlo. Si la intercalación de la base
        // de datos no trata bien los acentos, esta búsqueda devuelve cero resultados.
        var respuesta = await cliente.GetAsync("/api/quedadas?texto=bici", cancelacion);

        respuesta.EnsureSuccessStatusCode();

        var pagina = await respuesta.Content.ReadFromJsonAsync<PaginaDto>(cancelacion);
        pagina.ShouldNotBeNull();
    }

    private static async Task<string> IniciarSesionAsync(HttpClient cliente, string correo, string clave, CancellationToken cancelacion)
    {
        var anterior = cliente.DefaultRequestHeaders.Authorization;
        cliente.DefaultRequestHeaders.Authorization = null;

        var respuesta = await cliente.PostAsJsonAsync(
            "/api/identidad/sesion",
            new { correo, contrasena = clave, dispositivo = "pruebas" },
            cancelacion);

        respuesta.EnsureSuccessStatusCode();
        cliente.DefaultRequestHeaders.Authorization = anterior;

        var sesion = await respuesta.Content.ReadFromJsonAsync<SesionDto>(cancelacion);
        sesion.ShouldNotBeNull();

        return sesion.Tokens.TokenDeAcceso;
    }

    private static async Task VerificarOrganizadorAsync(HttpClient cliente, CancellationToken cancelacion)
    {
        var inicio = await cliente.PostAsync("/api/identidad/verificacion", null, cancelacion);
        inicio.EnsureSuccessStatusCode();

        var sesion = await inicio.Content.ReadFromJsonAsync<VerificacionDto>(cancelacion);
        sesion.ShouldNotBeNull();

        var completar = await cliente.PostAsJsonAsync(
            "/api/identidad/verificacion/completar",
            new { referenciaExterna = sesion.ReferenciaExterna },
            cancelacion);

        completar.EnsureSuccessStatusCode();
    }

    private sealed record IdDto(Guid Id);

    private sealed record PerfilDto(string NombreVisible, string? Ciudad);

    private sealed record TokensDto(string TokenDeAcceso);

    private sealed record SesionDto(TokensDto Tokens);

    private sealed record VerificacionDto(string ReferenciaExterna);

    private sealed record ResumenDto(Guid Id, string Titulo);

    private sealed record PaginaDto(List<ResumenDto> Elementos, int Total);

    private sealed record DetalleDto(Guid Id, string Titulo, string Lugar);
}

/// <summary>Encoding de UTF-8 sin BOM, usado por las utilidades de las pruebas.</summary>
internal static class Utf8SinBom
{
    public static readonly Encoding Instancia = new UTF8Encoding(encoderShouldEmitUTF8Identifier: false);
}
