using FluentValidation;
using Microsoft.Extensions.Logging;
using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;
using PlanVibe.Domain.Categorias;
using PlanVibe.Domain.Quedadas;
using PlanVibe.Domain.Quedadas.ObjetosDeValor;

namespace PlanVibe.Application.Quedadas.Comandos;

/// <summary>Publica una quedada nueva (RF-09, RF-10).</summary>
public sealed record CrearQuedada(
    string Titulo,
    string Descripcion,
    Guid CategoriaId,
    DateTimeOffset Inicio,
    int DuracionEnMinutos,
    string Lugar,
    string? Referencia,
    string? DireccionExacta,
    double Latitud,
    double Longitud,
    bool ConfirmaQueEsLugarPublico,
    int Capacidad,
    IReadOnlyList<string> Normas) : IComando<Guid>;

/// <summary>
/// Validación de forma: comprueba que los datos son coherentes antes de tocar la base de datos.
/// </summary>
/// <remarks>
/// No duplica las reglas de negocio, que viven en el agregado y son la última palabra. Su papel es
/// devolver todos los errores del formulario juntos, en lugar de uno por uno según los va
/// encontrando el dominio.
/// </remarks>
public sealed class CrearQuedadaValidador : AbstractValidator<CrearQuedada>
{
    public CrearQuedadaValidador()
    {
        RuleFor(c => c.Titulo)
            .NotEmpty().WithMessage("Ponle un título al plan.")
            .Length(Quedada.LongitudMinimaTitulo, Quedada.LongitudMaximaTitulo)
            .WithMessage($"El título debe tener entre {Quedada.LongitudMinimaTitulo} y {Quedada.LongitudMaximaTitulo} caracteres.");

        RuleFor(c => c.Descripcion)
            .MaximumLength(Quedada.LongitudMaximaDescripcion);

        RuleFor(c => c.CategoriaId)
            .NotEmpty().WithMessage("Elige una categoría.");

        RuleFor(c => c.DuracionEnMinutos)
            .InclusiveBetween((int)FranjaTemporal.DuracionMinima.TotalMinutes, (int)FranjaTemporal.DuracionMaxima.TotalMinutes)
            .WithMessage("Indica cuánto va a durar el plan.");

        RuleFor(c => c.Lugar)
            .NotEmpty().WithMessage("Indica dónde quedáis.")
            .MaximumLength(PuntoEncuentro.LongitudMaximaLugar);

        RuleFor(c => c.Latitud).InclusiveBetween(-90, 90);
        RuleFor(c => c.Longitud).InclusiveBetween(-180, 180);

        RuleFor(c => c.ConfirmaQueEsLugarPublico)
            .Equal(true)
            .WithMessage("El punto de encuentro debe ser un lugar público. No se permiten domicilios particulares.");

        RuleFor(c => c.Capacidad)
            .InclusiveBetween(Capacidad.Minima, Capacidad.Maxima)
            .WithMessage($"La capacidad debe estar entre {Capacidad.Minima} y {Capacidad.Maxima} personas.");

        RuleFor(c => c.Normas)
            .Must(n => n is null || n.Count <= NormasDelPlan.MaximoNormas)
            .WithMessage($"Puedes indicar como máximo {NormasDelPlan.MaximoNormas} normas.");
    }
}

/// <summary>
/// Orquesta la creación: comprueba quién pide la acción, delega la decisión en el dominio,
/// persiste y deja constancia en la auditoría.
/// </summary>
/// <remarks>
/// El manejador no decide si la persona puede organizar: se lo pregunta al agregado
/// <c>Usuario</c> a través de <c>PuedeOrganizar</c>. Repetir aquí las tres condiciones sería
/// duplicar la regla y arriesgarse a que las dos copias se separen con el tiempo.
/// </remarks>
public sealed class CrearQuedadaManejador(
    IRepositorioDeUsuarios usuarios,
    IRepositorioDeQuedadas quedadas,
    IUnidadDeTrabajo unidadDeTrabajo,
    IContextoDeUsuarioActual contexto,
    IRegistroDeAuditoria auditoria,
    TimeProvider reloj,
    ILogger<CrearQuedadaManejador> registro) : IManejadorDeComando<CrearQuedada, Guid>
{
    public async Task<Guid> ManejarAsync(CrearQuedada comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión para publicar un plan.");

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId.ToString());

        if (!usuario.PuedeOrganizar)
        {
            // No se detalla cuál de las condiciones falla: la interfaz ya guía a la persona
            // según el estado de su verificación, y el mensaje de error no es el sitio para eso.
            throw new AccesoDenegadoException("Para publicar un plan necesitas completar la verificación de organizador.");
        }

        var ahora = reloj.GetUtcNow();

        var quedada = Quedada.Crear(
            QuedadaId.Nuevo(),
            new Organizador(usuario.Id, usuario.Verificacion.Estado == Domain.Usuarios.EstadoVerificacion.Verificada, usuario.Verificacion.MayoriaDeEdadConfirmada),
            comando.Titulo,
            comando.Descripcion,
            new CategoriaId(comando.CategoriaId),
            new FranjaTemporal(comando.Inicio, TimeSpan.FromMinutes(comando.DuracionEnMinutos)),
            PuntoEncuentro.Crear(
                comando.Lugar,
                comando.Referencia,
                comando.DireccionExacta,
                new Coordenadas(comando.Latitud, comando.Longitud),
                comando.ConfirmaQueEsLugarPublico),
            new Capacidad(comando.Capacidad),
            new NormasDelPlan(comando.Normas ?? []),
            ahora);

        await quedadas.AgregarAsync(quedada, cancelacion);
        await unidadDeTrabajo.GuardarCambiosAsync(cancelacion);

        await auditoria.RegistrarAsync(
            usuario.Id,
            "quedada.publicada",
            "Quedada",
            quedada.Id.ToString(),
            new Dictionary<string, string> { ["categoria"] = comando.CategoriaId.ToString() },
            cancelacion);

        registro.QuedadaPublicada(quedada.Id, usuario.Id);

        return quedada.Id.Valor;
    }
}
