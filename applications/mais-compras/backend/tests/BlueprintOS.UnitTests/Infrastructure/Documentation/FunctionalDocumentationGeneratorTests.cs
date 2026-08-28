using BlueprintOS.Core.Documentation.Models;
using BlueprintOS.Infrastructure.Documentation;

namespace BlueprintOS.UnitTests.Infrastructure.Documentation;

public class FunctionalDocumentationGeneratorTests
{
    [Fact]
    public void Generate_Should_Include_UseCase_Steps_And_Expected_Result()
    {
        var generator = new FunctionalDocumentationGenerator();
        var input = new FunctionalDocumentationInput(
            "Procurement",
            new[]
            {
                new UseCaseDescription(
                    "Criar Pedido de Compra",
                    "Permite ao comprador criar um novo pedido.",
                    new[] { "Comprador" },
                    new[] { "Selecionar fornecedor", "Adicionar itens", "Confirmar pedido" },
                    "Pedido criado com status Pendente."),
            });

        var markdown = generator.Generate(input);

        Assert.Contains("Módulo Procurement", markdown);
        Assert.Contains("Criar Pedido de Compra", markdown);
        Assert.Contains("1. Selecionar fornecedor", markdown);
        Assert.Contains("Pedido criado com status Pendente.", markdown);
    }
}
