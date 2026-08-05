using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Engineering;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Engineering;

/// <summary>
/// Implementação de <see cref="IApiGenerator"/>, refletindo os endpoints reais mapeados
/// em <c>BlueprintOS.Api/Program.cs</c>.
/// </summary>
public sealed class ApiGenerator : IApiGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## API — documentação técnica");
        builder.AppendLine();
        builder.AppendLine("`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de");
        builder.AppendLine("infraestrutura via `AddInfrastructure` e expõe saúde, recomendação consultiva de");
        builder.AppendLine("negociação e o vertical slice de Fornecedores (cadastro, descoberta, consulta/");
        builder.AppendLine("enriquecimento de CNPJ e sincronização com o ERP):");
        builder.AppendLine();
        builder.AppendLine("```");
        builder.AppendLine("GET /health");
        builder.AppendLine("  -> 200 OK { Status, Application, Environment, Version }");
        builder.AppendLine();
        builder.AppendLine("POST /api/v1/negociacoes/recomendacoes");
        builder.AppendLine("  -> 200 OK { RequestId, Strategy, Justifications, Alerts, SuccessProbability, HumanDecisionRequired }");
        builder.AppendLine();
        builder.AppendLine("GET  /fornecedores                          -> busca/listagem de fornecedores");
        builder.AppendLine("POST /fornecedores                          -> cadastro de fornecedor");
        builder.AppendLine("GET  /fornecedores/{id}                     -> detalhe do fornecedor");
        builder.AppendLine("PUT  /fornecedores/{id}                     -> atualização do fornecedor");
        builder.AppendLine("POST /fornecedores/consulta-cnpj            -> consulta de CNPJ (BrasilAPI)");
        builder.AppendLine("POST /fornecedores/{id}/enriquecimento-cnpj -> análise de divergências de CNPJ");
        builder.AppendLine("POST /fornecedores/{id}/enriquecimento-cnpj/aprovar -> aprova o enriquecimento");
        builder.AppendLine("POST /fornecedores/{id}/enriquecimento-cnpj/rejeitar -> rejeita o enriquecimento");
        builder.AppendLine();
        builder.AppendLine("POST /api/fornecedores/descobrir            -> descoberta de fornecedores no ERP");
        builder.AppendLine("GET  /api/fornecedores/descobertas          -> lista descobertas registradas");
        builder.AppendLine();
        builder.AppendLine("POST /api/fornecedores/sincronizar          -> sincroniza um fornecedor com o ERP");
        builder.AppendLine("POST /api/fornecedores/sincronizar/lote     -> sincroniza um lote de fornecedores");
        builder.AppendLine("GET  /api/fornecedores/sincronizar-erp      -> executa e audita a sincronização ERP → +Compras");
        builder.AppendLine("```");
        builder.AppendLine();
        builder.AppendLine("OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.");
        builder.AppendLine("Os controllers delegam a casos de uso Application, reutilizando contratos de domínio,");
        builder.AppendLine("memória e estratégia; a API permanece fina, sem regra de negócio própria.");

        return Task.FromResult(builder.ToString());
    }
}
