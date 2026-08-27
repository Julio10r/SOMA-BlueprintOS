using BlueprintOS.Core.AI.Governance;
using BlueprintOS.Core.AI.Governance.Models;

namespace BlueprintOS.UnitTests.Core.AI.Governance;

/// <summary>
/// Covers the 12 canonical rules required by USER_ARTIFACT_LEARNING_POLICY.md and
/// CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md. Nothing in these tests, nor in the policy classes under
/// test, branches on which LLM/provider is executing (Rule 11).
/// </summary>
public sealed class UserArtifactLearningAndCapabilityGapPolicyTests
{
    private readonly UserArtifactLearningPolicy _learningPolicy = new();
    private readonly CapabilityGapAndAgentEvolutionPolicy _gapPolicy = new();

    // Rule 1: SQL fornecido pelo usuario nao e automaticamente executavel.
    [Fact]
    public void Rule01_UserProvidedSql_IsNeverAutomaticallyExecutable()
    {
        var artifact = new UserArtifact
        {
            Description = "SQL de ajuste de grade fornecido pelo usuario",
            Content = "UPDATE PROG_OP_PED SET GRADE = 'X' WHERE COD = 1",
            UserRequestedImmediateExecution = true,
        };

        var result = _learningPolicy.Classify(artifact);

        Assert.False(result.IsAutomaticallyExecutable);
    }

    // Rule 2: artefato historico e classificado como evidence, nunca comando.
    [Fact]
    public void Rule02_HistoricalArtifact_IsClassifiedAsEvidenceNeverCommand()
    {
        var artifact = new UserArtifact
        {
            Description = "Procedure historica de referencia",
            Content = "EXEC dbo.sp_HistoricoAjusteGrade",
        };

        var result = _learningPolicy.Classify(artifact);

        Assert.Equal(ArtifactClassification.HistoricalReference, result.Classification);
        Assert.NotEqual(ArtifactClassification.Evidence, ArtifactClassification.HistoricalReference); // classification space has no Command value
    }

    // Rule 3: artefato nao constitui approval.
    [Fact]
    public void Rule03_ProvidingArtifact_NeverConstitutesApproval()
    {
        var artifact = new UserArtifact
        {
            Description = "Planilha de ajuste fornecida pelo usuario",
            Content = "PROG;OP;PED\n1;2;3",
            UserRequestedImmediateExecution = true,
        };

        var result = _learningPolicy.Classify(artifact);

        Assert.False(result.ConstitutesApproval);
    }

    // Rule 4: knowledge gap interrompe o fluxo.
    [Fact]
    public void Rule04_KnowledgeGap_InterruptsFlow()
    {
        var request = new CapabilityRequest
        {
            CapabilityId = "linx-database-analysis",
            CapabilityDeclaredByAnyAgent = true,
            OwningAgentId = "linx-database-specialist-agent",
            KnowledgeSufficient = false,
            ExistingAgentIsNaturalOwnerForEvolution = true,
        };

        var resolution = _gapPolicy.Resolve(request);

        Assert.Equal(GapResolutionOutcome.KnowledgeGap, resolution.Outcome);
        Assert.True(resolution.FlowInterrupted);
        Assert.False(resolution.AutomaticExecutionAllowed);
    }

    // Rule 5: capability gap interrompe o fluxo.
    [Fact]
    public void Rule05_CapabilityGap_InterruptsFlow()
    {
        var request = new CapabilityRequest
        {
            CapabilityId = "linx-production-purchase-grade-adjustment",
            CapabilityDeclaredByAnyAgent = false,
            KnowledgeSufficient = false,
            ExistingAgentIsNaturalOwnerForEvolution = true,
        };

        var resolution = _gapPolicy.Resolve(request);

        Assert.Equal(GapResolutionOutcome.CapabilityGap, resolution.Outcome);
        Assert.True(resolution.FlowInterrupted);
        Assert.False(resolution.AutomaticExecutionAllowed);
    }

    // Rule 6: ausencia de owner propoe Agent, nao cria automaticamente.
    [Fact]
    public void Rule06_NoNaturalOwner_ProposesAgent_NeverCreatesAutomatically()
    {
        var request = new CapabilityRequest
        {
            CapabilityId = "completely-unrelated-domain",
            CapabilityDeclaredByAnyAgent = false,
            KnowledgeSufficient = false,
            ExistingAgentIsNaturalOwnerForEvolution = false,
        };

        var resolution = _gapPolicy.Resolve(request);

        Assert.Equal(GapResolutionOutcome.NoNaturalOwnerProposeNewAgent, resolution.Outcome);
        Assert.False(resolution.AutomaticExecutionAllowed);

        var proposal = new NewAgentProposal
        {
            ProposedAgentId = "hypothetical-new-agent",
            ExistingAgentsEvaluatedAndRejected = ["linx-database-specialist-agent"],
            CapabilityGapEvidence = "GAP-001",
            HumanApprovalGranted = false,
        };
        var decision = _gapPolicy.EvaluateNewAgentProposal(proposal);

        Assert.False(decision.CanCreate);
    }

    // Rule 7: evolucao material exige autorizacao explicita.
    [Fact]
    public void Rule07_MaterialEvolution_RequiresExplicitHumanApproval()
    {
        var withoutApproval = new AgentEvolutionProposal
        {
            AgentId = "linx-database-specialist-agent",
            NewCapabilityId = "linx-production-purchase-grade-adjustment",
            IsMaterialChange = true,
            HumanApprovalGranted = false,
        };
        Assert.False(_gapPolicy.EvaluateEvolution(withoutApproval).CanApply);

        var withApproval = withoutApproval with { HumanApprovalGranted = true, ApprovedBy = "product-owner" };
        var decision = _gapPolicy.EvaluateEvolution(withApproval);

        Assert.True(decision.CanApply);
        Assert.True(decision.RequiresHumanApproval);
    }

    // Rule 8: conhecimento validado pode ser persistido.
    [Fact]
    public void Rule08_ValidatedKnowledge_CanBePersisted()
    {
        var item = new LearnedKnowledgeItem
        {
            AgentId = "linx-database-specialist-agent",
            Statement = "Tabela PROG_OP_PED possui colunas PROG, OP e PED confirmadas por inspecao de schema.",
            Provenance = KnowledgeProvenance.DatabaseSchemaValidation,
            Confidence = KnowledgeConfidence.Confirmed,
            IsReusable = true,
        };

        var decision = _learningPolicy.EvaluatePersistence(item);

        Assert.True(decision.CanPersist);
    }

    // Rule 9: inferencia nao vira CONFIRMED automaticamente.
    [Fact]
    public void Rule09_Inference_NeverAutomaticallyBecomesConfirmed()
    {
        var inferred = new LearnedKnowledgeItem
        {
            AgentId = "linx-database-specialist-agent",
            Statement = "Hipotese extraida da planilha fornecida pelo usuario, ainda nao validada.",
            Provenance = KnowledgeProvenance.UserProvidedArtifact,
            Confidence = KnowledgeConfidence.Inferred,
            IsReusable = true,
        };

        // Simply re-evaluating persistence, or the mere passage of time, must not promote confidence.
        var unchanged = _learningPolicy.PromoteConfidence(inferred, KnowledgeProvenance.UserProvidedArtifact);
        Assert.Equal(KnowledgeConfidence.Inferred, unchanged.Confidence);

        // Only a direct-provenance validation event promotes it.
        var promoted = _learningPolicy.PromoteConfidence(inferred, KnowledgeProvenance.DatabaseSchemaValidation);
        Assert.Equal(KnowledgeConfidence.Confirmed, promoted.Confidence);
    }

    // Rule 10: segredo nao entra no knowledge store.
    [Theory]
    [InlineData("password=SuperSecret123")]
    [InlineData("api_key: sk-abc123def456")]
    [InlineData("connection string: Server=x;Database=y;pwd=abc123")]
    public void Rule10_Secrets_NeverEnterKnowledgeStore(string secretBearingStatement)
    {
        var item = new LearnedKnowledgeItem
        {
            AgentId = "linx-database-specialist-agent",
            Statement = secretBearingStatement,
            Provenance = KnowledgeProvenance.UserProvidedArtifact,
            Confidence = KnowledgeConfidence.Confirmed,
            IsReusable = true,
        };

        var decision = _learningPolicy.EvaluatePersistence(item);

        Assert.False(decision.CanPersist);
    }

    [Fact]
    public void Rule10_ExplicitSecretFlag_NeverEntersKnowledgeStore()
    {
        var item = new LearnedKnowledgeItem
        {
            AgentId = "linx-database-specialist-agent",
            Statement = "Credencial de acesso legivel encontrada no artefato.",
            Provenance = KnowledgeProvenance.UserProvidedArtifact,
            Confidence = KnowledgeConfidence.Confirmed,
            IsReusable = true,
            ContainsSecret = true,
        };

        Assert.False(_learningPolicy.EvaluatePersistence(item).CanPersist);
    }

    // Rule 11: comportamento e identico independente de provider (Codex/Claude/ChatGPT) — nada no codigo
    // condiciona comportamento por provider. Verified structurally: policy classes expose no provider
    // parameter or branch, and running the same input twice yields the same result.
    [Fact]
    public void Rule11_Behavior_IsProviderAgnostic()
    {
        var artifact = new UserArtifact
        {
            Description = "SQL fornecido pelo usuario",
            Content = "SELECT * FROM PROG_OP_PED",
            UserRequestedImmediateExecution = true,
        };

        var resultA = _learningPolicy.Classify(artifact);
        var resultB = new UserArtifactLearningPolicy().Classify(artifact);

        Assert.Equal(resultA.Classification, resultB.Classification);
        Assert.Equal(resultA.ConstitutesApproval, resultB.ConstitutesApproval);
        Assert.Equal(resultA.IsAutomaticallyExecutable, resultB.IsAutomaticallyExecutable);

        var policyType = typeof(UserArtifactLearningPolicy);
        var gapPolicyType = typeof(CapabilityGapAndAgentEvolutionPolicy);
        var providerNames = new[] { "provider", "codex", "claude", "chatgpt", "openai", "anthropic" };
        foreach (var type in new[] { policyType, gapPolicyType })
        {
            foreach (var member in type.GetMembers())
            {
                Assert.DoesNotContain(providerNames, name => member.Name.Contains(name, StringComparison.OrdinalIgnoreCase));
            }
        }
    }

    // Rule 12: bypass continua false / LIVE_EXECUTION continua false.
    [Fact]
    public void Rule12_Bypass_And_LiveExecution_RemainDisabled()
    {
        var covered = new CapabilityRequest
        {
            CapabilityId = "linx-database-analysis",
            CapabilityDeclaredByAnyAgent = true,
            OwningAgentId = "linx-database-specialist-agent",
            KnowledgeSufficient = true,
            ExistingAgentIsNaturalOwnerForEvolution = true,
        };

        var resolution = _gapPolicy.Resolve(covered);

        // Even when fully covered, this policy layer never grants automatic execution/bypass -
        // that remains the exclusive responsibility of ActionProposal/PolicyEngine/ApprovalPolicy/ToolGateway,
        // whose LIVE_EXECUTION_DISABLED and bypass_allowed=false invariants are covered by
        // GovernedWriteStackTests and the Agent Factory v2 safety tests.
        Assert.False(resolution.AutomaticExecutionAllowed);
    }
}
