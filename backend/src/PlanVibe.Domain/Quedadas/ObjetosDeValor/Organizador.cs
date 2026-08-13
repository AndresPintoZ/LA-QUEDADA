using PlanVibe.Domain.Usuarios;

namespace PlanVibe.Domain.Quedadas.ObjetosDeValor;

/// <summary>
/// Instantánea de quien organiza, tomada en el momento de crear la quedada.
/// </summary>
/// <remarks>
/// El agregado <see cref="Quedada"/> no carga el agregado <c>Usuario</c> completo: recibe solo los
/// tres datos que necesita para decidir si puede publicar. Así se respeta la regla de que un
/// agregado referencia a otro por identidad, y la comprobación de permisos queda explícita en la
/// firma del método en lugar de escondida en una consulta.
/// </remarks>
/// <param name="Id">Identificador del usuario organizador.</param>
/// <param name="EstaVerificado">Resultado vigente de la verificación de identidad (RF-09, RF-20).</param>
/// <param name="EsMayorDeEdad">Si tiene 18 años cumplidos; en el piloto es obligatorio para organizar (RF-24).</param>
public readonly record struct Organizador(UsuarioId Id, bool EstaVerificado, bool EsMayorDeEdad);
