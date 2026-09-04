using System.Data;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>Leitura real de `ITEM_FISCAL_REF_FORNECEDOR` com resolução de identidade de Fornecedor embutida
/// na consulta (B3 — Bloco 5A). Colunas fixas e já exaustivamente comprovadas por evidência real
/// (`docs/audits/B3-Bloco5A-ValidacaoIdentidadeFornecedor.md`): `ITEM_FISCAL_REF_FORNECEDOR` tem
/// exatamente 3 colunas (`FORNECEDOR`, `CODIGO_ITEM`, `CODIGO_ITEM_FORNECEDOR`, todas `varchar NOT NULL`) —
/// sem introspecção dinâmica de coluna aqui, diferente dos demais readers, porque não há ambiguidade de
/// nome a tolerar nesta tabela específica.
///
/// Resolução (mesma cadeia validada com dado real): `FORNECEDOR` (que é `NOME_CLIFOR`, nunca código) é
/// comparado por igualdade EXATA com `LTRIM(RTRIM(...))` — trim só para padding acidental, nunca
/// normalização de caixa/acentos — contra `CADASTRO_CLI_FOR.NOME_CLIFOR`, encadeando `CLIFOR` até
/// `FORNECEDORES.COD_FORNECEDOR`. `FornecedoresResolvidos` conta quantos `CLIFOR` distintos batem — o
/// chamador NUNCA deve confiar em `ErpFornecedorId` quando esse número for diferente de 1.</summary>
public sealed class SomaItemFiscalReferenciaFornecedorReader(IConfiguration configuration, ILogger<SomaItemFiscalReferenciaFornecedorReader> logger) : IItemFiscalReferenciaFornecedorErpReader
{
    public async Task<IReadOnlyList<ItemFiscalReferenciaFornecedorErpDto>> BuscarReferenciasAsync(int skip, int take, CancellationToken cancellationToken = default)
    {
        var offset = Math.Max(0, skip);
        var pageSize = Math.Clamp(take <= 0 ? 500 : take, 1, 5000);
        await using var connection = await OpenAsync(cancellationToken);

        await using var command = connection.CreateCommand();
        command.CommandTimeout = TimeoutSeconds;
        command.CommandText = @"
            SELECT
                r.CODIGO_ITEM AS CodigoItem,
                r.CODIGO_ITEM_FORNECEDOR AS CodigoItemFornecedor,
                (SELECT COUNT(DISTINCT c.CLIFOR)
                 FROM CADASTRO_CLI_FOR c
                 WHERE LTRIM(RTRIM(c.NOME_CLIFOR)) = LTRIM(RTRIM(r.FORNECEDOR))) AS FornecedoresResolvidos,
                (SELECT TOP (1) f.COD_FORNECEDOR
                 FROM CADASTRO_CLI_FOR c
                 JOIN FORNECEDORES f ON f.CLIFOR = c.CLIFOR
                 WHERE LTRIM(RTRIM(c.NOME_CLIFOR)) = LTRIM(RTRIM(r.FORNECEDOR))) AS ErpFornecedorId
            FROM ITEM_FISCAL_REF_FORNECEDOR r
            ORDER BY r.CODIGO_ITEM, r.FORNECEDOR, r.CODIGO_ITEM_FORNECEDOR
            OFFSET @skip ROWS FETCH NEXT @take ROWS ONLY";
        command.Parameters.Add(new SqlParameter("@skip", SqlDbType.Int) { Value = offset });
        command.Parameters.Add(new SqlParameter("@take", SqlDbType.Int) { Value = pageSize });

        logger.LogInformation("Leitura operacional de referencias de item fiscal por fornecedor SOMA iniciada. Skip {Skip}. Take {Take}", offset, pageSize);
        var referencias = new List<ItemFiscalReferenciaFornecedorErpDto>();
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);
        while (await reader.ReadAsync(cancellationToken)) referencias.Add(Map(reader));
        return referencias;
    }

    private async Task<SqlConnection> OpenAsync(CancellationToken ct)
    {
        var connectionString = LinxConnectionStringResolver.Resolve(configuration, LinxConnectionProfiles.Development);
        var connection = new SqlConnection(connectionString);
        await connection.OpenAsync(ct);
        return connection;
    }

    private static ItemFiscalReferenciaFornecedorErpDto Map(IDataRecord reader) => new(
        Convert.ToString(reader["CodigoItem"])!.Trim(),
        Convert.ToString(reader["CodigoItemFornecedor"])!.Trim(),
        reader["ErpFornecedorId"] is DBNull ? null : Convert.ToString(reader["ErpFornecedorId"])?.Trim(),
        Convert.ToInt32(reader["FornecedoresResolvidos"]));

    private int TimeoutSeconds => int.TryParse(configuration["ErpIntegration:TimeoutSeconds"], out var value) ? Math.Clamp(value, 1, 120) : 30;
}
