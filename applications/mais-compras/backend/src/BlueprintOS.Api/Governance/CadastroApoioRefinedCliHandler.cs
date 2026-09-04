using System.Text.Json;
using System.Text.Json.Serialization;
using BlueprintOS.Application.Identity;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Domain.Identity.Raw;
using BlueprintOS.Domain.Integrations.Occurrences;
using BlueprintOS.Infrastructure.DependencyInjection;
using BlueprintOS.Infrastructure.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Soma;
using BlueprintOS.Infrastructure.Persistence;
using BlueprintOS.Infrastructure.Persistence.Repositories;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Api.Governance;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): CLI para <see cref="ProcessarCadastroApoioRawParaDominioUseCase{TRaw,TMetadado}"/>,
/// compartilhado pelos 3 cadastros de apoio (Conta Contábil, Unidade de Medida, Centro de Custo) — mesma
/// isolação arquitetural do <see cref="RefinedFornecedorCliHandler"/>: escreve exclusivamente no MAISCOMPRAS,
/// nunca abre conexão com o Linx.
///
/// Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): <c>--business-unit</c> é OBRIGATÓRIO, mesmo padrão fail-closed
/// dos demais CLI handlers governados desta rodada.
/// </summary>
public static class CadastroApoioRefinedCliHandler
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
            return await WriteErrorAsync(output, "UNKNOWN_VERB", "Uso: linx-cadastro-apoio run --dataset <nome> --mode dry-run|apply --business-unit <slug>");
        }

        var datasetName = ReadOption(args, "--dataset");
        var modeText = ReadOption(args, "--mode") ?? "dry-run";
        bool dryRun;
        switch (modeText)
        {
            case "dry-run": dryRun = true; break;
            case "apply": dryRun = false; break;
            default: return await WriteErrorAsync(output, "INVALID_MODE", "--mode deve ser exatamente 'dry-run' ou 'apply'.");
        }

        if (string.IsNullOrWhiteSpace(datasetName))
        {
            return await WriteErrorAsync(output, "DATASET_REQUIRED", "--dataset é obrigatório.");
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

        var occurrenceRepository = new IntegrationOccurrenceRepository(db);

        object resultado;
        switch (datasetName)
        {
            case LinxReadDatasetCatalog.ContasContabeisSnapshot:
                resultado = await new ProcessarCadastroApoioRawParaDominioUseCase<RawLinxContaContabilRegistro, ContaContabilMetadado>(
                    db, occurrenceRepository,
                    scope.ServiceProvider.GetRequiredService<ILogger<ProcessarCadastroApoioRawParaDominioUseCase<RawLinxContaContabilRegistro, ContaContabilMetadado>>>(),
                    datasetName, IntegrationStage.Refined, suportaIncremental: true,
                    r => new CadastroApoioRefinedItem(r.CodigoErp, r.DescricaoErp, r.InativoErp, r.UltimaAlteracao, r.Id))
                    .ExecutarAsync(dryRun, unidadeNegocio.Id, TimeProvider.System, cancellationToken);
                break;
            case LinxReadDatasetCatalog.UnidadesMedidaSnapshot:
                resultado = await new ProcessarCadastroApoioRawParaDominioUseCase<RawLinxUnidadeMedidaRegistro, UnidadeMedidaMetadado>(
                    db, occurrenceRepository,
                    scope.ServiceProvider.GetRequiredService<ILogger<ProcessarCadastroApoioRawParaDominioUseCase<RawLinxUnidadeMedidaRegistro, UnidadeMedidaMetadado>>>(),
                    datasetName, IntegrationStage.Refined, suportaIncremental: false,
                    r => new CadastroApoioRefinedItem(r.CodigoErp, r.DescricaoErp, r.InativoErp, r.UltimaAlteracao, r.Id))
                    .ExecutarAsync(dryRun, unidadeNegocio.Id, TimeProvider.System, cancellationToken);
                break;
            case LinxReadDatasetCatalog.CentrosCustoSnapshot:
                resultado = await new ProcessarCadastroApoioRawParaDominioUseCase<RawLinxCentroCustoRegistro, CentroCustoMetadado>(
                    db, occurrenceRepository,
                    scope.ServiceProvider.GetRequiredService<ILogger<ProcessarCadastroApoioRawParaDominioUseCase<RawLinxCentroCustoRegistro, CentroCustoMetadado>>>(),
                    datasetName, IntegrationStage.Refined, suportaIncremental: true,
                    r => new CadastroApoioRefinedItem(r.CodigoErp, r.DescricaoErp, r.InativoErp, r.UltimaAlteracao, r.Id))
                    .ExecutarAsync(dryRun, unidadeNegocio.Id, TimeProvider.System, cancellationToken);
                break;
            case LinxReadDatasetCatalog.FiliaisSnapshot:
                resultado = await new ProcessarCadastroApoioRawParaDominioUseCase<RawLinxFilialRegistro, FilialMetadado>(
                    db, occurrenceRepository,
                    scope.ServiceProvider.GetRequiredService<ILogger<ProcessarCadastroApoioRawParaDominioUseCase<RawLinxFilialRegistro, FilialMetadado>>>(),
                    datasetName, IntegrationStage.Refined, suportaIncremental: true,
                    r => new CadastroApoioRefinedItem(r.CodigoErp, r.DescricaoErp, r.InativoErp, r.UltimaAlteracao, r.Id))
                    .ExecutarAsync(dryRun, unidadeNegocio.Id, TimeProvider.System, cancellationToken);
                break;
            default:
                return await WriteErrorAsync(output, "DATASET_UNKNOWN", $"Dataset '{datasetName}' não é um cadastro de apoio suportado por este handler.");
        }

        await WriteAsync(output, resultado);
        return 0;
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
