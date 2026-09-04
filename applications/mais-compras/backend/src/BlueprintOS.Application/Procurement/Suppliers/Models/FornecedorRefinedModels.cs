using BlueprintOS.Domain.Integrations.Occurrences;

namespace BlueprintOS.Application.Procurement.Suppliers.Models;

/// <summary>Uma ocorrência estruturada detectada pelo REFINED — nunca só uma string solta. Vira uma
/// <see cref="IntegrationOccurrence"/> persistida pelo orquestrador; o projetor em si não tem acesso a
/// banco/ExecutionId (computação pura), então só monta os campos que já conhece.</summary>
public sealed record RefinedOcorrencia(
    IntegrationOccurrenceSeverity Severidade,
    string Code,
    string Mensagem,
    string? OriginRecordKey);

public enum RefinedAction
{
    Insert = 1,
    Update = 2,
    NoChange = 3,
}

/// <summary>Estado atual de UM vínculo Linx já persistido no domínio, projetado para a decisão do REFINED
/// sem carregar a entidade de domínio inteira (leitura em lote, AsNoTracking).</summary>
public sealed record VinculoExistente(
    Guid Id,
    string CodigoErp,
    string NomeClifor,
    bool InativoFornecedores,
    bool InativoCadastroCliFor,
    DateTimeOffset? DataParaTransferencia,
    bool Principal)
{
    public bool Ativo => !InativoFornecedores && !InativoCadastroCliFor;
}

/// <summary>Estado atual de UM Fornecedor corporativo já persistido, com todos os seus vínculos Linx
/// conhecidos (ativos e inativos/históricos) — REFINED precisa do conjunto completo para decidir Principal e
/// fonte cadastral corretamente, nunca só dos vínculos presentes no RAW desta execução.</summary>
public sealed record FornecedorExistente(
    Guid Id, string Cnpj, string Status, string RazaoSocial, string? NomeFantasia, string? TipoPessoa,
    IReadOnlyList<VinculoExistente> Vinculos);

public sealed record VinculoRefinedDecision(
    string CodigoErp,
    RefinedAction Action,
    string NomeClifor,
    bool InativoFornecedores,
    bool InativoCadastroCliFor,
    DateTimeOffset? UltimaAlteracao,
    bool Ativo,
    Guid? VinculoExistenteId,
    bool AtribuirPrincipal,
    bool RemoverPrincipal);

public sealed record FornecedorRefinedDecision(
    string Cnpj,
    RefinedAction Action,
    Guid? FornecedorExistenteId,
    string RazaoSocial,
    string? NomeFantasia,
    string TipoPessoa,
    bool Ativo,
    bool AtivoAntes,
    IReadOnlyList<VinculoRefinedDecision> Vinculos);

public sealed record RefinedPlan(
    IReadOnlyList<FornecedorRefinedDecision> Fornecedores,
    IReadOnlyList<RefinedOcorrencia> ConflitosPrincipal,
    IReadOnlyList<RefinedOcorrencia> Erros)
{
    public RefinedPlanResumo Resumir(int fornecedoresAtivosAntes)
    {
        var fornecedoresValidos = Fornecedores;

        // Fornecedores existentes NÃO tocados por esta execução (nenhuma linha RAW válida para o CNPJ)
        // preservam seu estado Ativo/Inativo atual e não aparecem em `Fornecedores` — por isso a projeção
        // "depois" soma o estado real de antes (`fornecedoresAtivosAntes`) com as transições líquidas que
        // ESTA execução causaria, em vez de recalcular do zero a partir só do que está no plano.
        var passaramAAtivo = fornecedoresValidos.Count(f => f.Ativo && (f.Action == RefinedAction.Insert || (f.Action == RefinedAction.Update && !f.AtivoAntes)));
        var passaramAInativo = fornecedoresValidos.Count(f => !f.Ativo && f.Action == RefinedAction.Update && f.AtivoAntes);
        var ativosDepoisEstimado = fornecedoresAtivosAntes - passaramAInativo + passaramAAtivo;

        var vinculos = fornecedoresValidos.SelectMany(f => f.Vinculos).ToList();

        return new RefinedPlanResumo(
            FornecedoresCorporativosEsperados: fornecedoresValidos.Count,
            VinculosEsperados: vinculos.Count,
            FornecedoresInsert: fornecedoresValidos.Count(f => f.Action == RefinedAction.Insert),
            FornecedoresUpdate: fornecedoresValidos.Count(f => f.Action == RefinedAction.Update),
            FornecedoresSemAlteracao: fornecedoresValidos.Count(f => f.Action == RefinedAction.NoChange),
            VinculosInsert: vinculos.Count(v => v.Action == RefinedAction.Insert),
            VinculosUpdate: vinculos.Count(v => v.Action == RefinedAction.Update),
            VinculosSemAlteracao: vinculos.Count(v => v.Action == RefinedAction.NoChange),
            Conflitos: ConflitosPrincipal.Count,
            // Cada conflito de empate (item 5 do PO: "nunca inventar desempate... deixar sem Principal
            // operacional") corresponde a exatamente um Fornecedor ativo sem Principal ativo ao final desta
            // execução — o projetor só registra um conflito por CNPJ, então a contagem é direta.
            SemPrincipal: ConflitosPrincipal.Count,
            PrincipaisNovosCriados: vinculos.Count(v => v.AtribuirPrincipal),
            VinculosAtivos: vinculos.Count(v => v.Ativo),
            VinculosInativos: vinculos.Count(v => !v.Ativo),
            FornecedoresAtivosAntes: fornecedoresAtivosAntes,
            FornecedoresAtivosDepoisEstimado: ativosDepoisEstimado,
            PercentualInativacao: fornecedoresAtivosAntes == 0 ? 0m : (decimal)passaramAInativo / fornecedoresAtivosAntes,
            Erros: Erros.Count);
    }
}

public sealed record RefinedPlanResumo(
    int FornecedoresCorporativosEsperados,
    int VinculosEsperados,
    int FornecedoresInsert,
    int FornecedoresUpdate,
    int FornecedoresSemAlteracao,
    int VinculosInsert,
    int VinculosUpdate,
    int VinculosSemAlteracao,
    int Conflitos,
    int SemPrincipal,
    int PrincipaisNovosCriados,
    int VinculosAtivos,
    int VinculosInativos,
    int FornecedoresAtivosAntes,
    int FornecedoresAtivosDepoisEstimado,
    decimal PercentualInativacao,
    int Erros);
