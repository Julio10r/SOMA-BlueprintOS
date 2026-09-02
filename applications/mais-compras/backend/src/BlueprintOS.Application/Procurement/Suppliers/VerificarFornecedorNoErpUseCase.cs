using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Core.AI.Governance.Contracts;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class VerificarFornecedorNoErpUseCase(
    IGarantirFornecedorErpAdapterResolver resolver,
    ILogger<VerificarFornecedorNoErpUseCase> logger) : IVerificarFornecedorNoErpUseCase
{
    public async Task<VerificacaoFornecedorErpResultado> ExecuteAsync(string businessUnit, string documentoFiscal, CancellationToken cancellationToken = default)
    {
        var adapter = resolver.Resolver(businessUnit);
        if (adapter is not ISnapshotCapableAdapter snapshotAdapter)
        {
            // Capability gap: sem leitura read-only disponível para esta BU/ERP, não bloqueia o
            // cadastro local (mesmo comportamento de antes desta verificação existir) — apenas não
            // detecta duplicidade prévia no Linx. Nunca falha o cadastro por causa disso.
            logger.LogWarning("Adapter Linx para BU {BusinessUnit} não suporta verificação prévia de existência (ISnapshotCapableAdapter). Prosseguindo sem checagem.", businessUnit);
            return new(EstadoFornecedorErp.NaoExiste, null);
        }

        // CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026):
        // CGC_CPF no Linx é varchar(19) (docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md:244),
        // sem constraint numérica — Where(char.IsDigit) descartava as letras das 12 primeiras
        // posições do CNPJ antes mesmo de consultar o Linx, nunca encontrando o fornecedor real.
        var chars = documentoFiscal.ToUpperInvariant().Where(c => (c >= '0' && c <= '9') || (c >= 'A' && c <= 'Z')).ToArray();
        var documentoNormalizado = new string(chars);
        if (string.IsNullOrWhiteSpace(documentoNormalizado)) return new(EstadoFornecedorErp.NaoExiste, null);

        var snapshot = await snapshotAdapter.CaptureSnapshotAsync([$"CGC_CPF={documentoNormalizado}"], cancellationToken);
        var cadastro = snapshot.FirstOrDefault(s => s.Resource == "CADASTRO_CLI_FOR")?.Records ?? [];
        var fornecedores = snapshot.FirstOrDefault(s => s.Resource == "FORNECEDORES")?.Records ?? [];

        if (cadastro.Count == 0) return new(EstadoFornecedorErp.NaoExiste, null);
        if (fornecedores.Count == 0) return new(EstadoFornecedorErp.ExisteSemPapelFornecedor, cadastro[0].GetValueOrDefault("COD_CLIFOR"));
        return new(EstadoFornecedorErp.ExisteComPapelFornecedor, fornecedores[0].GetValueOrDefault("COD_FORNECEDOR"));
    }
}
