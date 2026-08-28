using BlueprintOS.Application.Knowledge.Linx;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Core.AI.Contracts;
using BlueprintOS.Core.AI.Models;
using BlueprintOS.Core.Agents.Models;
using BlueprintOS.Domain.Knowledge.Linx;
using BlueprintOS.Infrastructure.Knowledge.Linx;
using BlueprintOS.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BlueprintOS.UnitTests.Application.Knowledge.Linx;

/// <summary>O1.13.5, seção 28 — validação funcional dos dois especialistas com componentes REAIS
/// (persistência EF Core, use cases reais, Agents reais), não apenas fakes de unidade. Demonstra o ciclo
/// completo exigido pela Work Order: pergunta → recuperação → resposta fundamentada com proveniência/fonte;
/// e descoberta controlada → persistência → consulta posterior → reutilização (nunca "aprendizado" apenas
/// via contexto temporário de conversa).
///
/// O conteúdo semeado aqui não é fabricado: descreve o comportamento real e já validado de
/// <c>SomaFornecedorReader</c>/<c>SomaFilialReader</c> (introspecção dinâmica de
/// <c>INFORMATION_SCHEMA.COLUMNS</c> sobre `CADASTRO_CLI_FOR` no `SOMA_DESENV`, B2.1/O1.7) — conhecimento
/// já comprovado no próprio código do repositório, usado aqui como seed mínimo DESCOBERTO/VALIDADO (Work
/// Order, seção 16).</summary>
public sealed class LinxSpecialistsFunctionalValidationTests
{
    private sealed class CapturingAIRuntime : IAIRuntime
    {
        public string LastPrompt { get; private set; } = string.Empty;

        public Task<AIResponse> ExecuteAsync(AIRequest request, CancellationToken cancellationToken = default)
        {
            LastPrompt = request.Messages[0].Content;
            var message = new ChatMessage(ChatRole.Assistant, "resposta fundamentada");
            var usage = new TokenUsage(PromptTokens: 1, CompletionTokens: 1);
            var metrics = new AIExecutionMetrics("fake-provider", request.Model.Id, TimeSpan.Zero, usage);
            return Task.FromResult(new AIResponse(message, usage, metrics, FinishReason: "stop"));
        }
    }

    private static BlueprintOSDbContext CreateContext(string dbName) =>
        new(new DbContextOptionsBuilder<BlueprintOSDbContext>().UseInMemoryDatabase(dbName).Options);

    [Fact]
    public async Task LinxDatabaseSpecialist_Should_Discover_Persist_Retrieve_And_Reuse_Real_Schema_Knowledge()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateContext(dbName);
        var repositorio = new LinxKnowledgeRepository(db);
        var registrar = new RegistrarConhecimentoUseCase(repositorio, TimeProvider.System);
        var promover = new PromoverConhecimentoUseCase(repositorio, TimeProvider.System);
        var buscar = new BuscarConhecimentoUseCase(repositorio);

        // 1) DESCOBERTA — Database Specialist registra o que já é comprovado no código de SomaFornecedorReader.
        var registro = await registrar.ExecuteAsync(
            new RegistrarConhecimentoInput(
                LinxEspecialista.LinxDatabaseSpecialist, LinxConhecimentoCategoria.SchemaTabelaColuna,
                "Estrutura de Fornecedor no SOMA_DESENV",
                "A tabela dbo.CADASTRO_CLI_FOR contém as colunas de código e nome do fornecedor, descobertas por introspecção dinâmica de INFORMATION_SCHEMA.COLUMNS (SomaFornecedorReader).",
                LinxConhecimentoProveniencia.Descoberto,
                "SomaFornecedorReader.LoadShapeAsync (B2.1/B2.1.2)",
                UnidadeNegocioId: null, Tags: ["fornecedor", "schema", "CADASTRO_CLI_FOR"]),
            "linx-database-specialist-agent", CancellationToken.None);
        Assert.True(registro.Sucesso);

        // 2) VALIDAÇÃO — confirmado por evidência técnica (revisão humana do código real do reader).
        var validado = await promover.ExecuteAsync(registro.Valor!.Id, LinxConhecimentoProveniencia.Validado, "julio.cesar@somagrupo.com.br", CancellationToken.None);
        Assert.True(validado.Sucesso);

        // 3) RECUPERAÇÃO POSTERIOR — uma nova consulta (simula uma nova pergunta em outro momento) encontra
        // o conhecimento já persistido, não depende do contexto da conversa que o descobriu.
        var encontrados = await buscar.ExecuteAsync(new LinxKnowledgeFiltro("CADASTRO_CLI_FOR"), CancellationToken.None);
        Assert.Single(encontrados);
        Assert.Equal(LinxConhecimentoProveniencia.Validado, encontrados[0].Proveniencia);

        // 4) REUTILIZAÇÃO — o Agent responde a uma pergunta nova usando o conhecimento já persistido, com
        // proveniência e fonte explícitas na resposta fundamentada.
        var runtime = new CapturingAIRuntime();
        var agent = new LinxDatabaseSpecialistAgent(runtime, buscar);
        var resultado = await agent.ExecuteAsync(new AgentContext { Input = "Qual estrutura do Linx representa um fornecedor no CADASTRO_CLI_FOR?" });

        Assert.Equal("resposta fundamentada", resultado.Output);
        Assert.Contains("Validado", runtime.LastPrompt);
        Assert.Contains("SomaFornecedorReader.LoadShapeAsync", runtime.LastPrompt);
        Assert.Contains("CADASTRO_CLI_FOR", runtime.LastPrompt);
    }

    [Fact]
    public async Task LinxErpSpecialist_Should_Answer_From_Already_Documented_Functional_Rule_With_Provenance()
    {
        var dbName = Guid.NewGuid().ToString();
        await using var db = CreateContext(dbName);
        var repositorio = new LinxKnowledgeRepository(db);
        var registrar = new RegistrarConhecimentoUseCase(repositorio, TimeProvider.System);
        var buscar = new BuscarConhecimentoUseCase(repositorio);

        // Regra funcional já documentada no projeto (ADR-0020, item 3): Filial/Centro de Custo são dados
        // mestres do ERP, nunca criados/editados/excluídos no +Compras — apenas ativados/inativados
        // localmente. Registrada aqui como Descoberto (fonte documental), sem fabricar conteúdo novo.
        var registro = await registrar.ExecuteAsync(
            new RegistrarConhecimentoInput(
                LinxEspecialista.LinxErpSpecialist, LinxConhecimentoCategoria.RegraFuncional,
                "Filial e Centro de Custo como dados mestres do ERP",
                "Filial e Centro de Custo são dados mestres do Visual Linx e nunca são criados, editados ou excluídos pelo +Compras — apenas ativados/inativados localmente, com metadados locais opcionais.",
                LinxConhecimentoProveniencia.Descoberto,
                "ADR-0020, item 3 (docs/architecture)",
                UnidadeNegocioId: null, Tags: ["filial", "centro-de-custo", "regra-funcional"]),
            "linx-erp-specialist-agent", CancellationToken.None);
        Assert.True(registro.Sucesso);

        var runtime = new CapturingAIRuntime();
        var agent = new LinxErpSpecialistAgent(runtime, buscar);
        var resultado = await agent.ExecuteAsync(new AgentContext { Input = "O +Compras pode criar uma Filial diretamente?" });

        Assert.Equal("resposta fundamentada", resultado.Output);
        Assert.Contains("Descoberto", runtime.LastPrompt);
        Assert.Contains("ADR-0020, item 3", runtime.LastPrompt);
        Assert.Contains("nunca são criados", runtime.LastPrompt);
    }
}
