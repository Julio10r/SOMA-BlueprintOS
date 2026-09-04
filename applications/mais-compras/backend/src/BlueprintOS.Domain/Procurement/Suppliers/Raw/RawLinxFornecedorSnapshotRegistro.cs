namespace BlueprintOS.Domain.Procurement.Suppliers.Raw;

/// <summary>
/// B3 — Bloco 5A.9, Gate A: uma linha crua (staging) do snapshot Linx de Fornecedores, sem nenhuma
/// interpretação de regra de negócio — isso é responsabilidade exclusiva do REFINED (ainda não implementado
/// neste Gate). A tabela é truncate-and-reload a cada execução do LiveRead: identidade e completude do
/// carregamento vivem em <see cref="RawLinxFornecedorSnapshotExecucao"/>, não linha a linha — por isso não há
/// FK para a execução aqui (ver decisão de design do Gate A).
/// </summary>
public sealed class RawLinxFornecedorSnapshotRegistro
{
    public int Id { get; private set; }
    public string CodigoFornecedor { get; private set; } = string.Empty;
    public string? Clifor { get; private set; }
    public string? CnpjCpf { get; private set; }
    public string? RazaoSocial { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string? TipoPessoa { get; private set; }
    public bool InativoFornecedores { get; private set; }
    public bool InativoCadastroCliFor { get; private set; }

    /// <summary>COALESCE(CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA, FORNECEDORES.DATA_PARA_TRANSFERENCIA) —
    /// mesma regra homologada já usada por SomaFornecedorReader/SincronizarFornecedoresErpUseCase para fonte
    /// cadastral (LWW) e seleção de Principal. Tipo <see cref="DateTime"/> (não <see cref="DateTimeOffset"/>)
    /// deliberado: espelha o tipo SQL Server <c>datetime</c> de origem exatamente, para que o SqlBulkCopy
    /// nunca precise de conversão implícita entre tipos de data incompatíveis.</summary>
    public DateTime? UltimaAlteracao { get; private set; }

    private RawLinxFornecedorSnapshotRegistro()
    {
    }

    /// <summary>Linhas reais nascem exclusivamente via <c>SqlBulkCopy</c> (nunca por este construtor, que o
    /// bulk copy ignora ao materializar diretamente nas colunas físicas) — esta fábrica existe só para
    /// testes do REFINED conseguirem montar um lote RAW em memória sem depender de reflexão sobre setters
    /// privados. Sem invariantes de negócio para proteger aqui: é staging, não domínio.</summary>
    public static RawLinxFornecedorSnapshotRegistro ParaTeste(
        string codigoFornecedor, string? clifor, string? cnpjCpf, string? razaoSocial, string? nomeFantasia,
        string? tipoPessoa, bool inativoFornecedores, bool inativoCadastroCliFor, DateTime? ultimaAlteracao) => new()
    {
        CodigoFornecedor = codigoFornecedor,
        Clifor = clifor,
        CnpjCpf = cnpjCpf,
        RazaoSocial = razaoSocial,
        NomeFantasia = nomeFantasia,
        TipoPessoa = tipoPessoa,
        InativoFornecedores = inativoFornecedores,
        InativoCadastroCliFor = inativoCadastroCliFor,
        UltimaAlteracao = ultimaAlteracao,
    };
}
