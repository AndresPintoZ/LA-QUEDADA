using System.Net;
using System.Text;
using Shouldly;

namespace PlanVibe.Api.IntegrationTests;

/// <summary>
/// Cómo responde la API ante peticiones mal formadas.
/// </summary>
/// <remarks>
/// Estas pruebas nacieron de un fallo real: al enviar un cuerpo con codificación
/// inválida, la API devolvía 500. Un 500 significa «el fallo es nuestro» y acaba en
/// las alertas del equipo; un cuerpo mal codificado que envía un cliente es un 400.
/// </remarks>
public class ManejoDeErroresTests(FabricaDeApiDePrueba fabrica) : IClassFixture<FabricaDeApiDePrueba>
{
    [RequiereDocker]
    public async Task Un_cuerpo_con_codificacion_invalida_devuelve_400_y_no_500()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var cliente = fabrica.CreateClient();

        // 0xC1 suelto es la "Á" en ISO-8859-1, que no es UTF-8 válido. Es exactamente lo
        // que envía un cliente mal configurado al mandar un nombre con tilde.
        var bytes = Encoding.UTF8.GetBytes("""{"correo":"a@b.es","nombreVisible":"PLACEHOLDER"}""");
        var invalidos = bytes.Select(b => b == (byte)'P' ? (byte)0xC1 : b).ToArray();

        var contenido = new ByteArrayContent(invalidos);
        contenido.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

        var respuesta = await cliente.PostAsync("/api/identidad/registro", contenido, cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [RequiereDocker]
    public async Task Un_json_sintacticamente_roto_devuelve_400()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var cliente = fabrica.CreateClient();

        var contenido = new StringContent("{esto no es json", Encoding.UTF8, "application/json");

        var respuesta = await cliente.PostAsync("/api/identidad/registro", contenido, cancelacion);

        respuesta.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
    }

    [RequiereDocker]
    public async Task Un_error_no_revela_detalles_internos()
    {
        var cancelacion = TestContext.Current.CancellationToken;
        var cliente = fabrica.CreateClient();

        var contenido = new StringContent("{roto", Encoding.UTF8, "application/json");
        var respuesta = await cliente.PostAsync("/api/identidad/registro", contenido, cancelacion);
        var cuerpo = await respuesta.Content.ReadAsStringAsync(cancelacion);

        // Ni trazas de pila, ni nombres de tipos internos, ni rutas del servidor.
        cuerpo.ShouldNotContain("System.");
        cuerpo.ShouldNotContain("PlanVibe.");
        cuerpo.ShouldNotContain("at ");
    }
}
