using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;
using BlueprintOS.Infrastructure.Persistence.Governance;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

public sealed class PostWriteValidationRuleCatalogTests
{
    private static readonly DateTimeOffset Now = new(2026, 8, 28, 12, 0, 0, TimeSpan.Zero);

    [Theory]
    [InlineData("CADASTRO_CLI_FOR", ActionOperation.Insert)]
    [InlineData("CADASTRO_CLI_FOR", ActionOperation.Update)]
    [InlineData("FORNECEDORES", ActionOperation.Insert)]
    [InlineData("FORNECEDORES", ActionOperation.Update)]
    public void Seeded_Fornecedor_Pairs_Resolve_To_A_Rule(string resource, ActionOperation operation)
    {
        var rule = new PostWriteValidationRuleCatalog().Resolve(operation, resource);
        Assert.NotNull(rule);
        Assert.Equal(resource, rule!.Resource);
        Assert.NotEmpty(rule.BusinessKeyFields);
        Assert.NotEmpty(rule.FieldsToCompare);
    }

    [Fact]
    public void Unseeded_Resource_Resolves_To_Null_So_The_Write_Can_Be_Blocked()
    {
        var catalog = new PostWriteValidationRuleCatalog();
        Assert.Null(catalog.Resolve(ActionOperation.Update, "PRODUTOS"));
        Assert.Null(catalog.Resolve(ActionOperation.Delete, "FORNECEDORES"));
        Assert.Null(catalog.Resolve(ActionOperation.Update, ""));
    }

    [Fact]
    public void Resource_Matching_Is_Case_Insensitive_But_Operation_Matching_Is_Exact()
    {
        var catalog = new PostWriteValidationRuleCatalog();
        Assert.NotNull(catalog.Resolve(ActionOperation.Update, "fornecedores"));
        Assert.Null(catalog.Resolve(ActionOperation.Merge, "FORNECEDORES"));
    }

    [Fact]
    public async Task Knowledge_Gap_Is_Recorded_With_The_Unknown_Rule_Reason_Code()
    {
        var store = new InMemoryWriteValidationKnowledgeGapStore();
        await store.RecordAsync(new WriteValidationKnowledgeGap(
            Guid.NewGuid(), "REQ-GAP", "linx-database-specialist-agent", "linx-development",
            "PRODUTOS", ActionOperation.Update, WriteValidationKnowledgeGap.ReasonCode, null, Now));

        var gap = Assert.Single(await store.ListAsync());
        Assert.Equal("WRITE_VALIDATION_RULE_UNKNOWN", gap.Reason);
        Assert.Equal("PRODUTOS", gap.Resource);
    }

    [Fact]
    public async Task File_Knowledge_Gap_Store_Persists_And_Lists_Gaps()
    {
        var root = Path.Combine(Path.GetTempPath(), "blueprintos-governance-tests", Guid.NewGuid().ToString("N"));
        try
        {
            var store = new FileWriteValidationKnowledgeGapStore(root);

            await store.RecordAsync(new WriteValidationKnowledgeGap(
                Guid.NewGuid(), "REQ-GAP", "linx-database-specialist-agent", "linx-development",
                "PEDIDOS", ActionOperation.Insert, WriteValidationKnowledgeGap.ReasonCode, Guid.NewGuid(), Now));

            var gap = Assert.Single(await store.ListAsync());
            Assert.Equal("PEDIDOS", gap.Resource);
            Assert.Equal(ActionOperation.Insert, gap.Operation);
            Assert.NotNull(gap.ActionProposalId);
        }
        finally
        {
            if (Directory.Exists(root)) Directory.Delete(root, recursive: true);
        }
    }
}
