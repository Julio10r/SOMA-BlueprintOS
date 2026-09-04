namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>B3 — Bloco 5A.9: migração dos Fornecedores existentes para o modelo de vínculos Linx. Operação
/// puramente LOCAL (nunca lê o Linx) — para cada <c>Fornecedor</c> que já possui
/// <c>ErpFornecedorId</c>/<c>ErpSistema</c> (identidade legada) e ainda não tem nenhum
/// <c>FornecedorLinxVinculo</c> correspondente, cria um vínculo Principal com essa identidade,
/// preservando-a explicitamente ANTES de qualquer sincronização real subsequente processar o Linx —
/// garante que a atribuição automática de Principal por "sem nenhum Principal definido"
/// (<c>SincronizarFornecedoresErpUseCase</c>) nunca tenha a chance de eleger um vínculo-irmão só por
/// ordem de leitura/paginação, mesmo que ele tenha `DATA_PARA_TRANSFERENCIA` mais recente (decisão do
/// Product Owner: a migração nunca substitui automaticamente o Principal já em uso).
///
/// Os metadados do vínculo criado (NomeClifor/situação/timestamp) nascem com os melhores valores locais já
/// conhecidos (nome atual do Fornecedor, situação cadastral atual) — serão corrigidos pela sincronização
/// real subsequente, que sempre encontra este vínculo já existente e apenas o atualiza (nunca recria nem
/// toca em Principal).</summary>
public interface IBackfillFornecedorLinxVinculosUseCase
{
    Task<BackfillFornecedorLinxVinculosResumo> ExecuteAsync(BackfillFornecedorLinxVinculosDto dto, CancellationToken cancellationToken = default);
}

public sealed record BackfillFornecedorLinxVinculosDto(bool DryRun = false);

public sealed record BackfillFornecedorLinxVinculosResumo(
    string Status, DateTimeOffset Inicio, DateTimeOffset Fim,
    int FornecedoresComIdentidadeErpLegada, int VinculosCriados, int VinculosJaExistentes, long DuracaoMs);
