using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// B3 — Bloco 5A.9, Gate RAW→REFINED→DOMÍNIO: CLI para <see cref="ProcessarFornecedoresRawParaDominioUseCase"/>.
/// Escreve exclusivamente no MAISCOMPRAS (nunca no Linx — este comando nem abre conexão com o ERP, só lê a
/// tabela RAW já carregada por <c>linx-liveread</c>). Modo <c>dry-run</c> nunca chama SaveChanges.
///
/// Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <c>--business-unit</c> é OBRIGATÓRIO — mesmo padrão fail-closed
/// já aplicado a <c>ItensFiscaisRefinedCliHandler</c>. Resolvido contra o cadastro real de UnidadeNegocio
/// (nunca Guid hardcoded); slug ausente/inexistente/inativo falha fechado antes de qualquer domínio.
/// </summary>
public static class RefinedFornecedorCliHandler
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter() },
    };

    public static async Task<int> RunAsync(string[] args, TextWriter output, IConfiguration configuration, CancellationToken cancellationToken = default)
    {
        if (args.Length < 2 || args[1] != "run")
        {
            return await WriteErrorAsync(output, "UNKNOWN_VERB", "Uso: linx-refined run --dataset <nome> --mode dry-run|apply --business-unit <slug>");
        }

        var datasetName = ReadOption(args, "--dataset") ?? LinxReadDatasetCatalog.FornecedoresSnapshot;
        var modeText = ReadOption(args, "--mode") ?? "dry-run";
        bool dryRun;
        switch (modeText)
        {
            case "dry-run": dryRun = true; break;
            case "apply": dryRun = false; break;
            default: return await WriteErrorAsync(output, "INVALID_MODE", "--mode deve ser exatamente 'dry-run' ou 'apply'.");
        }

        var businessUnitSlug = ReadOption(args, "--business-unit");
        if (string.IsNullOrWhiteSpace(businessUnitSlug))
        {
            return await WriteErrorAsync(output, "BUSINESS_UNIT_REQUIRED", "--business-unit é obrigatório (slug de uma Unidade de Negócio real) — pipeline headless nunca infere a Business Unit da execução.");
        }

        var services = new ServiceCollection();
        services.AddInfrastructure(configuration);
        services.AddLogging(b => b.AddConsole());
#pragma warning disable ASP0000 // Isolated CLI composition root; no ASP.NET host is created.
        await using var provider = services.BuildServiceProvider();
#pragma warning restore ASP0000
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<BlueprintOSDbContext>();
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<ProcessarFornecedoresRawParaDominioUseCase>>();

        var unidadesNegocio = new UnidadeNegocioRepository(db);
        var unidadeNegocio = await unidadesNegocio.ObterPorSlugAsync(businessUnitSlug.Trim(), cancellationToken);
        if (unidadeNegocio is null)
        {
            return await WriteErrorAsync(output, "BUSINESS_UNIT_NOT_FOUND", $"Nenhuma Unidade de Negócio encontrada com slug '{businessUnitSlug}'. Fail closed — nenhum domínio foi lido ou escrito.");
        }
        if (!unidadeNegocio.Ativa)
        {
            return await WriteErrorAsync(output, "BUSINESS_UNIT_INATIVA", $"A Unidade de Negócio '{businessUnitSlug}' existe mas está inativa. Fail closed — nenhum domínio foi lido ou escrito.");
        }

        var occurrenceRepository = new BlueprintOS.Infrastructure.Persistence.Repositories.IntegrationOccurrenceRepository(db);
        var useCase = new ProcessarFornecedoresRawParaDominioUseCase(db, occurrenceRepository, logger);
        var cronometro = System.Diagnostics.Stopwatch.StartNew();
        var resultado = await useCase.ExecutarAsync(datasetName, unidadeNegocio.Id, dryRun, TimeProvider.System, cancellationToken);
        cronometro.Stop();

        await WriteAsync(output, new
        {
            execucaoRawId = resultado.ExecucaoRawId,
            dryRun = resultado.DryRun,
            aplicado = resultado.Aplicado,
            limiarInativacaoExcedido = resultado.LimiarInativacaoExcedido,
            resumo = resultado.Resumo,
            conflitos = resultado.Conflitos,
            erros = resultado.Erros,
            reconciliacao = resultado.Reconciliacao,
            duracaoRefinedMs = resultado.DuracaoRefined.TotalMilliseconds,
            duracaoAplicacaoMs = resultado.DuracaoAplicacao.TotalMilliseconds,
            duracaoReconciliacaoMs = resultado.DuracaoReconciliacao.TotalMilliseconds,
            duracaoTotalMs = cronometro.Elapsed.TotalMilliseconds,
            batchesAplicados = resultado.BatchesAplicados,
            ocorrenciasPersistidas = resultado.OcorrenciasPersistidas,
            baselineHomologada = resultado.BaselineHomologada,
            cargaFullInicialValidada = resultado.CargaFullInicialValidada,
            incrementalLiberado = resultado.IncrementalLiberado,
            watermarkInicial = resultado.WatermarkInicial,
            watermarkAvancado = resultado.WatermarkAvancado,
        });

        return resultado.LimiarInativacaoExcedido ? 1 : 0;
    }

    private static string? ReadOption(string[] args, string name)
    {
        for (var i = 0; i < args.Length - 1; i++)
        {
            if (string.Equals(args[i], name, StringComparison.Ordinal)) return args[i + 1];
        }

        return null;
    }

    private static Task WriteAsync(TextWriter output, object value) =>
        output.WriteLineAsync(JsonSerializer.Serialize(value, JsonOptions));

    private static async Task<int> WriteErrorAsync(TextWriter output, string error, string? message = null)
    {
        await WriteAsync(output, new { error, message });
        return 1;
    }
}
