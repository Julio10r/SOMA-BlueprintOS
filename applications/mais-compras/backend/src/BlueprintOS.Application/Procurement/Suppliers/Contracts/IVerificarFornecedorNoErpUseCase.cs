namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Gate de homologação de Fornecedores (2026-09-01): antes de criar um novo Fornecedor no
/// +Compras, verifica por CNPJ/CPF se ele já existe no Linx — distinguindo os 3 estados reais já
/// documentados (decisão validada do Product Owner, `agents/knowledge/linx-fornecedor-cnpj/*`,
/// unidade `linx-idempotencia-convergencia-create-update-fornecedor`): não existe; existe em
/// CADASTRO_CLI_FOR sem o papel Fornecedor (INDICA_FORNECEDOR=0/sem linha em FORNECEDORES); existe
/// com o papel Fornecedor. Reaproveita a leitura já existente e testada
/// (<c>ISnapshotCapableAdapter.CaptureSnapshotAsync</c>, mesma usada pelo framework de governança de
/// escrita) — nenhuma nova query SQL é introduzida.</summary>
public enum EstadoFornecedorErp
{
    NaoExiste,
    ExisteSemPapelFornecedor,
    ExisteComPapelFornecedor
}

public sealed record VerificacaoFornecedorErpResultado(EstadoFornecedorErp Estado, string? CodigoClifor);

public interface IVerificarFornecedorNoErpUseCase
{
    Task<VerificacaoFornecedorErpResultado> ExecuteAsync(string businessUnit, string documentoFiscal, CancellationToken cancellationToken = default);
}
