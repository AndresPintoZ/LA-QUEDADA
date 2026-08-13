using PlanVibe.Application.Abstracciones;
using PlanVibe.Application.Common;

namespace PlanVibe.Application.Identidad.Comandos;

/// <summary>Renueva la sesión a partir de un token de renovación válido.</summary>
public sealed record RenovarSesion(string TokenDeRenovacion) : IComando<ParDeTokens>;

/// <summary>Cierra la sesión revocando el token de renovación.</summary>
public sealed record CerrarSesion(string TokenDeRenovacion) : IComando<bool>;

/// <summary>Datos de la persona autenticada, para la pantalla de perfil y la cabecera.</summary>
public sealed record ObtenerMiPerfil : IConsulta<PerfilDeSesion>;

public sealed class RenovarSesionManejador(IEmisorDeTokens emisorDeTokens) : IManejadorDeComando<RenovarSesion, ParDeTokens>
{
    public async Task<ParDeTokens> ManejarAsync(RenovarSesion comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        // Un token inválido, caducado o ya usado da siempre el mismo error, sin distinguir el motivo.
        return await emisorDeTokens.RenovarAsync(comando.TokenDeRenovacion, cancelacion)
            ?? throw new AccesoDenegadoException("La sesión ha caducado. Vuelve a iniciar sesión.");
    }
}

public sealed class CerrarSesionManejador(IEmisorDeTokens emisorDeTokens) : IManejadorDeComando<CerrarSesion, bool>
{
    public async Task<bool> ManejarAsync(CerrarSesion comando, CancellationToken cancelacion)
    {
        ArgumentNullException.ThrowIfNull(comando);

        await emisorDeTokens.RevocarAsync(comando.TokenDeRenovacion, cancelacion);

        return true;
    }
}

public sealed class ObtenerMiPerfilManejador(
    IRepositorioDeUsuarios usuarios,
    IContextoDeUsuarioActual contexto) : IManejadorDeConsulta<ObtenerMiPerfil, PerfilDeSesion>
{
    public async Task<PerfilDeSesion> ManejarAsync(ObtenerMiPerfil consulta, CancellationToken cancelacion)
    {
        var usuarioId = contexto.UsuarioId
            ?? throw new AccesoDenegadoException("Necesitas iniciar sesión.");

        var usuario = await usuarios.ObtenerPorIdAsync(usuarioId, cancelacion)
            ?? throw new NoEncontradoException("el usuario", usuarioId.ToString());

        return IniciarSesionManejador.ConstruirPerfil(usuario);
    }
}
