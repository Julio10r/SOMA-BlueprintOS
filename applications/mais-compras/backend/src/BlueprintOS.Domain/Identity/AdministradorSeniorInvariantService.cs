namespace BlueprintOS.Domain.Identity;

/// <summary>Regra estrutural do último Administrador Sênior ativo (ADR-0020, item 12; Work Order O1.4.3,
/// seção 3/14): o sistema nunca pode ficar sem pelo menos um Administrador Sênior ativo. Método de domínio
/// reutilizável — nenhum fluxo que possa violar essa invariante (inativação, remoção de vínculo, inativação
/// do próprio Perfil) deve duplicar esta lógica; deve chamar este método antes de persistir. Nesta etapa
/// (O1.4.3.2), é invocado trivialmente pela conclusão do Bootstrap, onde a invariante é satisfeita por
/// construção (é a primeira criação) — os fluxos futuros de inativação/remoção (seção 14 da Work Order) não
/// são implementados aqui.</summary>
public static class AdministradorSeniorInvariantService
{
    /// <param name="quantidadeAtivaAposOperacao">Quantidade de vínculos Ativos do Perfil "Administrador
    /// Sênior" na Unidade de Negócio, já considerando o efeito da operação proposta (ex.: descontando o
    /// vínculo que seria removido/inativado, ou somando o que está sendo criado).</param>
    public static void GarantirQueRestaAoMenosUmAdministradorSeniorAtivo(int quantidadeAtivaAposOperacao)
    {
        if (quantidadeAtivaAposOperacao < 1)
        {
            throw new UltimoAdministradorSeniorAtivoException(
                "A operação deixaria a Unidade de Negócio sem nenhum Administrador Sênior ativo.");
        }
    }
}

/// <summary>Violação da invariante do último Administrador Sênior ativo (seção 3/14 da Work Order O1.4.3).</summary>
public sealed class UltimoAdministradorSeniorAtivoException : Exception
{
    public UltimoAdministradorSeniorAtivoException(string message) : base(message) { }
}
