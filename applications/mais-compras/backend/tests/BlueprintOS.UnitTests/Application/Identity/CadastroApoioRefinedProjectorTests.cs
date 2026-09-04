using BlueprintOS.Application.Identity;

namespace BlueprintOS.UnitTests.Application.Identity;

/// <summary>
/// B3 — Bloco 5A (preparação de certificação final): prova, como computação pura (sem banco), a única regra
/// compartilhada pelos cadastros de apoio (Conta Contábil, Unidade de Medida, Centro de Custo, Filial) —
/// Linx só pode FORÇAR inativação local (ADR-0024), nunca reativar, e um código sem metadado local nunca é
/// criado aqui. PO (revisão B3/Bloco 5A pós-certificação): essa ausência é estado normal/lazy — nunca gera
/// <c>IntegrationOccurrence</c>; só situações realmente excepcionais (hoje: código Linx ambíguo) geram.
/// </summary>
public sealed class CadastroApoioRefinedProjectorTests
{
    [Fact]
    public void Linx_Inativo_Com_Metadado_Local_Ativo_Gera_Decisao_De_Inativar()
    {
        var id = Guid.NewGuid();
        var raw = new[] { new CadastroApoioRefinedItem("001", "CONTA X", InativoErp: true, UltimaAlteracao: new DateTime(2026, 1, 1)) };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(id, AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("001", decisao.CodigoErp);
        Assert.Equal(id, decisao.MetadadoId);
        Assert.Equal(CadastroApoioRefinedAction.Inativar, decisao.Acao);
        Assert.Empty(plano.Ocorrencias);
        Assert.Empty(plano.CodigosSemMetadadoLocal);
    }

    [Fact]
    public void Linx_Inativo_Com_Metadado_Local_Ja_Inativo_Nao_Gera_Decisao()
    {
        var raw = new[] { new CadastroApoioRefinedItem("001", "CONTA X", InativoErp: true, UltimaAlteracao: new DateTime(2026, 1, 1)) };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(Guid.NewGuid(), AtivoNoMaisCompras: false) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Decisoes);
    }

    [Fact]
    public void Linx_Ativo_Nunca_Reativa_Metadado_Local_Inativo()
    {
        var raw = new[] { new CadastroApoioRefinedItem("001", "CONTA X", InativoErp: false, UltimaAlteracao: new DateTime(2026, 1, 1)) };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(Guid.NewGuid(), AtivoNoMaisCompras: false) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Decisoes);
    }

    [Fact]
    public void Dataset_Sem_Conceito_De_Status_Nunca_Inativa_Mesmo_Com_Metadado_Ativo()
    {
        // Unidades de Medida: InativoErp sempre null (Linx não tem coluna de status para esta tabela).
        var raw = new[] { new CadastroApoioRefinedItem("UN", "UNIDADE", InativoErp: null, UltimaAlteracao: new DateTime(2026, 1, 1)) };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["UN"] = new(Guid.NewGuid(), AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Decisoes);
    }

    /// <summary>PO (revisão B3/Bloco 5A pós-certificação): ausência de metadado local é BY DESIGN — nunca
    /// gera IntegrationOccurrence e nunca cria o metadado. O código fica só em
    /// <see cref="CadastroApoioRefinedPlan.CodigosSemMetadadoLocal"/> (estatística informativa), nunca em
    /// <see cref="CadastroApoioRefinedPlan.Ocorrencias"/>.</summary>
    [Fact]
    public void Codigo_Sem_Metadado_Local_Nao_Gera_Ocorrencia_E_Nunca_Cria_Metadado()
    {
        var raw = new[] { new CadastroApoioRefinedItem("999", "CONTA NOVA", InativoErp: false, UltimaAlteracao: new DateTime(2026, 1, 1)) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, new Dictionary<string, CadastroApoioExistente>());

        Assert.Empty(plano.Decisoes);
        Assert.Empty(plano.Ocorrencias);
        var codigo = Assert.Single(plano.CodigosSemMetadadoLocal);
        Assert.Equal("999", codigo);
    }

    /// <summary>Mesmo quando o Linx sinaliza o código como inativo, sem metadado local não há o que
    /// inativar — nenhuma decisão, nenhuma ocorrência, e o código segue disponível normalmente (ver
    /// Listar*UseCase de cada cadastro, que trata ausência de metadado como "ativo por padrão").</summary>
    [Fact]
    public void Codigo_Sem_Metadado_Local_Inativo_No_Linx_Tambem_Nao_Gera_Ocorrencia()
    {
        var raw = new[] { new CadastroApoioRefinedItem("999", "CONTA NOVA", InativoErp: true, UltimaAlteracao: null) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, new Dictionary<string, CadastroApoioExistente>());

        Assert.Empty(plano.Decisoes);
        Assert.Empty(plano.Ocorrencias);
        Assert.Equal(["999"], plano.CodigosSemMetadadoLocal);
    }

    [Fact]
    public void Codigo_Erp_E_Comparado_Aparado_De_Espacos()
    {
        var id = Guid.NewGuid();
        var raw = new[] { new CadastroApoioRefinedItem("001   ", "CONTA X", InativoErp: true, UltimaAlteracao: null) };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(id, AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal(id, decisao.MetadadoId);
    }

    /// <summary>Fail-closed continua valendo para exceções reais: código ambíguo nunca é resolvido por
    /// suposição, e continua gerando ocorrência (diferente do caso "sem metadado local", que é lazy/normal).</summary>
    [Fact]
    public void Codigos_Que_Convergem_Apos_Trim_Geram_Ocorrencia_De_Ambiguidade_Sem_Decisao()
    {
        // Regressão: CTB_CENTRO_CUSTO real tinha "    1.000310   " e "1.000310       " — dois registros
        // Linx fisicamente distintos que colidem no mesmo código após Trim(). Nunca escolher um dos dois.
        var raw = new[]
        {
            new CadastroApoioRefinedItem("    1.000310   ", "CC A", InativoErp: true, UltimaAlteracao: null),
            new CadastroApoioRefinedItem("1.000310       ", "CC B", InativoErp: true, UltimaAlteracao: null),
        };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["1.000310"] = new(Guid.NewGuid(), AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Decisoes);
        Assert.Empty(plano.CodigosSemMetadadoLocal);
        var ocorrencia = Assert.Single(plano.Ocorrencias);
        Assert.Equal("1.000310", ocorrencia.CodigoErp);
        Assert.Equal("CADASTRO_APOIO_CODIGO_LINX_AMBIGUO", ocorrencia.Code);
    }

    /// <summary>Onda 2 — auditoria RAW determinística (04/09/2026): sob Incremental, RAW é append-only e o
    /// MESMO código Linx (idêntico pré-trim, não apenas colidindo após trim) pode aparecer 2x — a linha
    /// antiga e a recém-anexada. Diferente da ambiguidade de formatação (códigos pré-trim DISTINTOS), aqui
    /// os valores são IDÊNTICOS: deve vencer a versão mais recente por UltimaAlteracao, nunca gerar
    /// ocorrência de ambiguidade, e o resultado deve ser o mesmo independente da ordem de entrada.</summary>
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Mesmo_Codigo_Exato_Duplicado_Resolve_Pela_Versao_Mais_Recente_Independente_Da_Ordem(bool inverterOrdem)
    {
        var id = Guid.NewGuid();
        var antiga = new CadastroApoioRefinedItem("001", "CONTA ANTIGA", InativoErp: false, UltimaAlteracao: new DateTime(2026, 1, 1), Id: 10);
        var recente = new CadastroApoioRefinedItem("001", "CONTA RECENTE", InativoErp: true, UltimaAlteracao: new DateTime(2026, 2, 1), Id: 11);
        var raw = inverterOrdem ? new[] { recente, antiga } : new[] { antiga, recente };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(id, AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Ocorrencias);
        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("001", decisao.CodigoErp);
        Assert.Equal(CadastroApoioRefinedAction.Inativar, decisao.Acao); // decidido pela linha "recente" (InativoErp=true)
    }

    /// <summary>Empate exato de UltimaAlteracao entre duas versões do MESMO código: desempate estável por
    /// maior Id (RAW só cresce sob Incremental — Id mais alto é sempre a linha fisicamente mais recente).</summary>
    [Fact]
    public void Mesmo_Codigo_Exato_Com_Empate_De_UltimaAlteracao_Desempata_Por_Maior_Id()
    {
        var id = Guid.NewGuid();
        var mesmaData = new DateTime(2026, 1, 1);
        var raw = new[]
        {
            new CadastroApoioRefinedItem("001", "A", InativoErp: false, UltimaAlteracao: mesmaData, Id: 5),
            new CadastroApoioRefinedItem("001", "B", InativoErp: true, UltimaAlteracao: mesmaData, Id: 9),
        };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["001"] = new(id, AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Ocorrencias);
        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal(CadastroApoioRefinedAction.Inativar, decisao.Acao); // Id=9 (maior) venceu, InativoErp=true
    }

    /// <summary>Regressão explícita: a ambiguidade de formatação (2 códigos Linx DISTINTOS pré-trim) continua
    /// gerando ocorrência mesmo após a deduplicação por código exato — não deve ser confundida com o caso de
    /// duplicata exata acima.</summary>
    [Fact]
    public void Ambiguidade_De_Formatacao_Continua_Gerando_Ocorrencia_Mesmo_Apos_Deduplicacao_Exata()
    {
        var raw = new[]
        {
            new CadastroApoioRefinedItem("    1.000310   ", "CC A", InativoErp: true, UltimaAlteracao: null, Id: 1),
            new CadastroApoioRefinedItem("1.000310       ", "CC B", InativoErp: true, UltimaAlteracao: null, Id: 2),
        };
        var existentes = new Dictionary<string, CadastroApoioExistente> { ["1.000310"] = new(Guid.NewGuid(), AtivoNoMaisCompras: true) };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        Assert.Empty(plano.Decisoes);
        var ocorrencia = Assert.Single(plano.Ocorrencias);
        Assert.Equal("CADASTRO_APOIO_CODIGO_LINX_AMBIGUO", ocorrencia.Code);
    }

    [Fact]
    public void Multiplos_Itens_Sao_Processados_Independentemente()
    {
        var idAtivo = Guid.NewGuid();
        var idJaInativo = Guid.NewGuid();
        var raw = new[]
        {
            new CadastroApoioRefinedItem("A", null, InativoErp: true, UltimaAlteracao: null),
            new CadastroApoioRefinedItem("B", null, InativoErp: true, UltimaAlteracao: null),
            new CadastroApoioRefinedItem("C", null, InativoErp: false, UltimaAlteracao: null),
            new CadastroApoioRefinedItem("D", null, InativoErp: true, UltimaAlteracao: null),
        };
        var existentes = new Dictionary<string, CadastroApoioExistente>
        {
            ["A"] = new(idAtivo, AtivoNoMaisCompras: true),
            ["B"] = new(idJaInativo, AtivoNoMaisCompras: false),
            ["C"] = new(Guid.NewGuid(), AtivoNoMaisCompras: true),
        };

        var plano = CadastroApoioRefinedProjector.Projetar(raw, existentes);

        var decisao = Assert.Single(plano.Decisoes);
        Assert.Equal("A", decisao.CodigoErp);
        Assert.Empty(plano.Ocorrencias);
        var codigo = Assert.Single(plano.CodigosSemMetadadoLocal);
        Assert.Equal("D", codigo);
    }
}
