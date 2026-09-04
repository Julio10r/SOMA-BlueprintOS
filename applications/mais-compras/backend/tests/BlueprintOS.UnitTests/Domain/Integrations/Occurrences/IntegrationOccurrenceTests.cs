using BlueprintOS.Domain.Integrations.Occurrences;

namespace BlueprintOS.UnitTests.Domain.Integrations.Occurrences;

/// <summary>B3 — Bloco 5A.9, complemento "PERSISTÊNCIA DE OCORRÊNCIAS/ERROS DE INTEGRAÇÃO": toda ocorrência
/// nasce Pendente e pode transicionar para Resolvido/IgnoradoAceito — nunca automaticamente, sempre uma ação
/// futura explícita (não implementada nesta rodada, só o modelo que a suporta).</summary>
public sealed class IntegrationOccurrenceTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 20, 0, 0, TimeSpan.Zero);
    private static readonly Guid Bu = Guid.NewGuid();

    [Fact]
    public void Registrar_Cria_Ocorrencia_Pendente()
    {
        var ocorrencia = IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Bu, "linx.fornecedores.snapshot", IntegrationStage.Refined, IntegrationOccurrenceSeverity.Error,
            "CNPJ_CPF_INVALIDO", "CNPJ/CPF inválido.", "000001", Now);

        Assert.Equal(IntegrationOccurrenceStatus.Pendente, ocorrencia.Status);
        Assert.Equal(IntegrationOccurrenceSeverity.Error, ocorrencia.Severity);
        Assert.Equal("000001", ocorrencia.OriginRecordKey);
        Assert.Equal(Bu, ocorrencia.UnidadeNegocioId);
    }

    [Fact]
    public void Registrar_Exige_Code_E_Message()
    {
        Assert.Throws<ArgumentException>(() => IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Bu, "ds", IntegrationStage.Raw, IntegrationOccurrenceSeverity.Warning, "", "msg", null, Now));
        Assert.Throws<ArgumentException>(() => IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Bu, "ds", IntegrationStage.Raw, IntegrationOccurrenceSeverity.Warning, "CODE", "", null, Now));
    }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): toda ocorrência
    /// identifica sua Unidade de Negócio — nunca criada sem uma BU explícita e válida (fail closed).</summary>
    [Fact]
    public void Registrar_Exige_UnidadeNegocioId()
    {
        Assert.Throws<ArgumentException>(() => IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Guid.Empty, "ds", IntegrationStage.Raw, IntegrationOccurrenceSeverity.Warning, "CODE", "msg", null, Now));
    }

    [Fact]
    public void MarcarResolvido_Transiciona_Status()
    {
        var ocorrencia = IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Bu, "ds", IntegrationStage.Domain, IntegrationOccurrenceSeverity.Conflict, "PRINCIPAL_EMPATE", "msg", "CNPJ", Now);

        ocorrencia.MarcarResolvido(Now.AddDays(1));

        Assert.Equal(IntegrationOccurrenceStatus.Resolvido, ocorrencia.Status);
    }

    [Fact]
    public void MarcarIgnoradoAceito_Transiciona_Status()
    {
        var ocorrencia = IntegrationOccurrence.Registrar(
            Guid.NewGuid(), Bu, "ds", IntegrationStage.Domain, IntegrationOccurrenceSeverity.Conflict, "PRINCIPAL_EMPATE", "msg", "CNPJ", Now);

        ocorrencia.MarcarIgnoradoAceito(Now.AddDays(1));

        Assert.Equal(IntegrationOccurrenceStatus.IgnoradoAceito, ocorrencia.Status);
    }

    /// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026): mesma chave de dedupe (ExecutionId/Dataset/Stage/
    /// Code/OriginRecordKey) em Unidades de Negócio diferentes nunca colide — são ocorrências distintas.</summary>
    [Fact]
    public void Mesma_Chave_De_Dedupe_Em_BUs_Diferentes_Nao_Colide()
    {
        var execucaoId = Guid.NewGuid();
        var outraBu = Guid.NewGuid();

        var ocorrenciaBu1 = IntegrationOccurrence.Registrar(
            execucaoId, Bu, "ds", IntegrationStage.Refined, IntegrationOccurrenceSeverity.Error, "CODE", "msg", "chave-1", Now);
        var ocorrenciaBu2 = IntegrationOccurrence.Registrar(
            execucaoId, outraBu, "ds", IntegrationStage.Refined, IntegrationOccurrenceSeverity.Error, "CODE", "msg", "chave-1", Now);

        Assert.NotEqual(ocorrenciaBu1.Id, ocorrenciaBu2.Id);
        Assert.Equal(Bu, ocorrenciaBu1.UnidadeNegocioId);
        Assert.Equal(outraBu, ocorrenciaBu2.UnidadeNegocioId);
    }
}
