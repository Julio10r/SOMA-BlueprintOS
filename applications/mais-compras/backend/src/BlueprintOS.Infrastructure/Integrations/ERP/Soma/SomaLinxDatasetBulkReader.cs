using System.Data;
using System.Diagnostics;
using BlueprintOS.Core.AI.Governance.Contracts;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Infrastructure.Integrations.ERP.Soma;

/// <summary>
/// B3 — Bloco 5A.9, Gate A: o serviço plano, determinístico, que executa a leitura real de um
/// <see cref="ReadDatasetDefinition"/> — deliberadamente FORA de qualquer infraestrutura de Agent/LLM
/// (princípio "Agent ≠ LLM"). Streaming verdadeiro: <see cref="SqlDataReader"/> alimentando
/// <see cref="SqlBulkCopy"/> diretamente, sem materializar o resultado completo em memória.
///
/// Estratégia de isolamento (item 10 da autorização do PO): READ UNCOMMITTED é fixado apenas nesta conexão
/// de leitura à origem — nunca na configuração do servidor/banco Linx, que nunca é alterada por este código.
/// Risco aceito e documentado: possível leitura suja/fantasma durante o snapshot; mitigado porque (a) o RAW
/// carrega identidade de execução e completude — uma carga incompleta nunca é promovida a REFINED/DOMÍNIO —
/// e (b) o próprio propósito do RAW é uma fotografia point-in-time re-executável, com REFINED resolvendo
/// conflitos (LWW) a jusante. READ COMMITTED puro foi descartado por maior risco de bloquear o ERP, que é a
/// prioridade máxima desta autorização (item 9).
/// </summary>
public sealed class SomaLinxDatasetBulkReader(IConfiguration configuration, ILogger<SomaLinxDatasetBulkReader> logger) : ISomaLinxDatasetBulkReader
{
    private const string IsolationLevel = "READ UNCOMMITTED";

    public async Task<ReadExecutionResult> StreamAsync(ReadDatasetDefinition dataset, Guid executionId, DatasetLoadKind modo, DateTimeOffset? watermark, CancellationToken cancellationToken = default)
    {
        if (modo == DatasetLoadKind.Incremental && watermark is null)
            throw new ArgumentNullException(nameof(watermark), $"Dataset '{dataset.Name}': execução Incremental exige um watermark resolvido — nunca inferido aqui.");

        var stopwatch = Stopwatch.StartNew();
        logger.LogInformation("LiveRead iniciado. ExecucaoId {ExecucaoId} Dataset {Dataset} Modo {Modo}", executionId, dataset.Name, modo);

        try
        {
            var sourceConnectionString = LinxConnectionStringResolver.Resolve(configuration, ResolveProfile(dataset.SourceConnectionProfileKey));
            var destinationConnectionString = LinxConnectionStringResolver.Resolve(configuration, ResolveProfile(dataset.DestinationConnectionProfileKey));

            await using var source = new SqlConnection(sourceConnectionString);
            await source.OpenAsync(cancellationToken);

            await using (var setIsolation = source.CreateCommand())
            {
                setIsolation.CommandText = $"SET TRANSACTION ISOLATION LEVEL {IsolationLevel}";
                await setIsolation.ExecuteNonQueryAsync(cancellationToken);
            }

            await using var command = source.CreateCommand();
            command.CommandText = dataset.ResolveCommandText(modo);
            command.CommandTimeout = dataset.CommandTimeoutSeconds;
            if (modo == DatasetLoadKind.Incremental)
            {
                var watermarkNoFusoDoServidorOrigem = await ResolveWatermarkNoFusoDoServidorOrigemAsync(source, watermark!.Value, cancellationToken);
                command.Parameters.Add(new SqlParameter("@watermark", SqlDbType.DateTime) { Value = watermarkNoFusoDoServidorOrigem });
            }

            // Deliberadamente SEM CommandBehavior.SequentialAccess: SqlBulkCopy lê as colunas do reader na
            // ordem das colunas FÍSICAS da tabela de destino (não na ordem do SELECT) quando ColumnMappings
            // são fornecidos por nome — uma coluna adicionada depois via ALTER TABLE fica no final da tabela
            // fisicamente, o que quebraria o acesso sequencial estrito. O conjunto de resultados aqui é
            // estreito (poucas strings/bits/um datetime por linha), então o custo de não usar acesso
            // sequencial é desprezível.
            await using var reader = await command.ExecuteReaderAsync(cancellationToken);

            await using var destination = new SqlConnection(destinationConnectionString);
            await destination.OpenAsync(cancellationToken);

            if (modo == DatasetLoadKind.Full)
            {
                // Full e sempre a fotografia completa e substitui qualquer carga anterior — Incremental
                // nunca trunca, apenas acrescenta as linhas alteradas desde o ultimo watermark valido.
                await using var truncate = destination.CreateCommand();
                truncate.CommandText = $"TRUNCATE TABLE {dataset.DestinationTable}";
                await truncate.ExecuteNonQueryAsync(cancellationToken);
            }

            using var bulkCopy = new SqlBulkCopy(destination)
            {
                DestinationTableName = dataset.DestinationTable,
                BulkCopyTimeout = dataset.CommandTimeoutSeconds,
            };
            foreach (var column in dataset.Columns)
            {
                bulkCopy.ColumnMappings.Add(column, column);
            }

            await bulkCopy.WriteToServerAsync(reader, cancellationToken);
            stopwatch.Stop();

            var rows = bulkCopy.RowsCopied;
            logger.LogInformation("LiveRead concluído. ExecucaoId {ExecucaoId} Dataset {Dataset} Linhas {Linhas} DuracaoMs {DuracaoMs}",
                executionId, dataset.Name, rows, stopwatch.ElapsedMilliseconds);
            return new ReadExecutionResult(true, rows, rows, IsolationLevel, stopwatch.Elapsed, []);
        }
        catch (OperationCanceledException)
        {
            stopwatch.Stop();
            logger.LogWarning("LiveRead cancelado. ExecucaoId {ExecucaoId} Dataset {Dataset}", executionId, dataset.Name);
            return new ReadExecutionResult(false, 0, 0, IsolationLevel, stopwatch.Elapsed, ["CANCELLED"], "A leitura foi cancelada.");
        }
    }

    /// <summary>
    /// Achado real (Onda 2, bateria final de certificação B3, 04/09/2026): as colunas de watermark
    /// (<c>DATA_PARA_TRANSFERENCIA</c>) são estampadas pelas triggers Linx via <c>GETDATE()</c> — hora LOCAL
    /// do servidor Linx (SRV-SOMA-DEV = UTC-3, sem DST, confirmado por <c>GETDATE()</c>/<c>GETUTCDATE()</c>
    /// real), sem qualquer informação de fuso. Comparar esse valor diretamente contra
    /// <paramref name="watermark"/>.UtcDateTime (como o código fazia antes) subestima mudanças: uma alteração
    /// feita há poucas horas aparenta ser "mais antiga" que o watermark sempre que o relógio LOCAL ainda não
    /// alcançou numericamente o valor UTC do watermark — reproduzido neste mesmo teste (alteração controlada
    /// e reversível em 101 Fornecedores não detectada por um INCREMENTAL executado poucos minutos depois).
    /// Resolve o offset em runtime, direto do servidor de origem (nunca hardcoded como "-3h", que quebraria
    /// silenciosamente se o servidor Linx um dia mudar de fuso) e converte o watermark para o mesmo referencial
    /// LOCAL sem-fuso da coluna, uma única vez por execução incremental.
    /// </summary>
    private static async Task<DateTime> ResolveWatermarkNoFusoDoServidorOrigemAsync(SqlConnection source, DateTimeOffset watermark, CancellationToken cancellationToken)
    {
        await using var offsetCommand = source.CreateCommand();
        offsetCommand.CommandText = "SELECT DATEDIFF(SECOND, GETDATE(), GETUTCDATE())";
        var utcMenosLocalEmSegundos = (int)(await offsetCommand.ExecuteScalarAsync(cancellationToken))!;
        return watermark.UtcDateTime.AddSeconds(-utcMenosLocalEmSegundos);
    }

    private static LinxConnectionProfile ResolveProfile(string profileKey) => profileKey switch
    {
        "linx-development" => LinxConnectionProfiles.Development,
        "linx-production" => LinxConnectionProfiles.Production,
        "mais-compras-development" => LinxConnectionProfiles.MaisComprasDevelopment,
        _ => throw new InvalidOperationException($"Connection profile key '{profileKey}' não é reconhecido. Datasets só podem referenciar profiles fixos e revisados."),
    };
}
