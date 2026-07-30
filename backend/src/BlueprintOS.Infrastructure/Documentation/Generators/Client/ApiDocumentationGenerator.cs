using System.Text;
using BlueprintOS.Core.Documentation.Contracts.Client;

namespace BlueprintOS.Infrastructure.Documentation.Generators.Client;

/// <summary>
/// Implementação de <see cref="IApiDocumentationGenerator"/> voltada a clientes/integradores,
/// refletindo a superfície real, hoje mínima, de endpoints expostos por <c>BlueprintOS.Api</c>.
/// </summary>
public sealed class ApiDocumentationGenerator : IApiDocumentationGenerator
{
    /// <inheritdoc />
    public Task<string> GenerateAsync(CancellationToken cancellationToken = default)
    {
        var builder = new StringBuilder();
        builder.AppendLine("## API para clientes e integradores");
        builder.AppendLine();
        builder.AppendLine("A API pública do BlueprintOS ainda está em estágio inicial. Além da verificação");
        builder.AppendLine("de saúde, há um primeiro fluxo consultivo de negociação do +COMPRAS:");
        builder.AppendLine();
        builder.AppendLine("| Método | Rota | Descrição |");
        builder.AppendLine("|--------|------|-----------|");
        builder.AppendLine("| GET | `/health` | Retorna o status de saúde da aplicação. |");
        builder.AppendLine("| POST | `/api/v1/negotiations/history` | Registra uma negociação concluída no histórico transitório. |");
        builder.AppendLine("| GET | `/api/v1/negotiations/suppliers/{supplierId}` | Consulta o histórico consolidado de um fornecedor. |");
        builder.AppendLine("| POST | `/api/v1/negotiations/recommendations` | Produz recomendação explicável; exige decisão humana. |");
        builder.AppendLine();
        builder.AppendLine("O histórico é perdido ao reiniciar a aplicação. Os endpoints não executam compras,");
        builder.AppendLine("não integram ERP e ainda não possuem autenticação ou autorização.");

        return Task.FromResult(builder.ToString());
    }
}
