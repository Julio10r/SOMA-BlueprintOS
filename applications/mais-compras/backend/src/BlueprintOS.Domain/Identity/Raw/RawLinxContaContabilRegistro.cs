namespace BlueprintOS.Domain.Identity.Raw;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): linha crua (staging) do snapshot Linx de Contas
/// Contábeis (<c>CTB_CONTA_PLANO</c>), sem nenhuma interpretação de regra de negócio — mesma forma de
/// staging já homologada para Fornecedor (ver <c>RawLinxFornecedorSnapshotRegistro</c>): truncate-and-reload
/// em carga Full, sem FK para a execução (identidade/completude vivem em
/// <c>BlueprintOS.Domain.Procurement.Suppliers.Raw.RawLinxFornecedorSnapshotExecucao</c>, reutilizada como
/// cabeçalho de execução genérico entre datasets — seu campo <c>Dataset</c> já discrimina por dataset desde
/// o Gate A, nada ali é específico de Fornecedor apesar do nome da classe).
/// </summary>
public sealed class RawLinxContaContabilRegistro
{
    public int Id { get; private set; }
    public string CodigoErp { get; private set; } = string.Empty;
    public string? DescricaoErp { get; private set; }
    public bool InativoErp { get; private set; }
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxContaContabilRegistro()
    {
    }

    /// <summary>Linhas reais nascem exclusivamente via <c>SqlBulkCopy</c> — fábrica só para testes.</summary>
    public static RawLinxContaContabilRegistro ParaTeste(string codigoErp, string? descricaoErp, bool inativoErp, DateTime? ultimaAlteracao) => new()
    {
        CodigoErp = codigoErp,
        DescricaoErp = descricaoErp,
        InativoErp = inativoErp,
        UltimaAlteracao = ultimaAlteracao,
    };
}
