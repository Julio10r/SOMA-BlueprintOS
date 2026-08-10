using BlueprintOS.Domain.Identity;

namespace BlueprintOS.UnitTests.Domain.Identity;

/// <summary>Item 15 do plano de testes da Work Order O1.4.3 (seção 18): lógica pura da invariante do último
/// Administrador Sênior ativo, isolada de qualquer fluxo de inativação/remoção (ainda não implementados).</summary>
public sealed class AdministradorSeniorInvariantServiceTests
{
    [Fact]
    public void Should_Throw_When_Operation_Would_Leave_Zero_Active_Administradores_Senior()
    {
        Assert.Throws<UltimoAdministradorSeniorAtivoException>(
            () => AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo(0));
    }

    [Fact]
    public void Should_Not_Throw_When_At_Least_One_Active_Administrador_Senior_Remains()
    {
        AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo(1);
    }
}
