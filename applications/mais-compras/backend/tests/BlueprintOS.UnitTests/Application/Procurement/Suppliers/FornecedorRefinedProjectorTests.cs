using BlueprintOS.Application.Procurement.Suppliers;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers.Raw;

namespace BlueprintOS.UnitTests.Application.Procurement.Suppliers;

/// <summary>
/// B3 — Bloco 5A.9, Gate REFINED: prova, como computação pura (sem banco), que o processamento em lote
/// reproduz exatamente as mesmas regras já homologadas por <c>SincronizarFornecedoresErpUseCase</c> — Ativo,
/// fonte cadastral (LWW), Caso A/B unificados de Principal, tie-break nunca inventado, e o caso-limite
/// defensivo de reativação.
/// </summary>
public sealed class FornecedorRefinedProjectorTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 3, 18, 0, 0, TimeSpan.Zero);
    private const string ValidCnpj1 = "11222333000181"; // dígitos verificadores válidos
    private const string ValidCnpj2 = "11444777000161";

    [Fact]
    public void Novo_Fornecedor_Com_Um_Unico_Vinculo_Ativo_Recebe_Principal_Automaticamente()
    {
        var raw = new[] { Row("000001", ValidCnpj1, "EMPRESA UM", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)) };

        var plano = FornecedorRefinedProjector.Projetar(raw, new Dictionary<string, FornecedorExistente>(), Now);

        var decisao = Assert.Single(plano.Fornecedores);
        Assert.Equal(RefinedAction.Insert, decisao.Action);
        Assert.True(decisao.Ativo);
        var vinculo = Assert.Single(decisao.Vinculos);
        Assert.True(vinculo.AtribuirPrincipal);
        Assert.Empty(plano.ConflitosPrincipal);
        Assert.Empty(plano.Erros);
    }

    [Fact]
    public void Novo_Fornecedor_Com_Dois_Vinculos_Ativos_Empatados_Nunca_Inventa_Desempate()
    {
        var mesmaData = new DateTime(2026, 1, 1);
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "EMPRESA A", inativoF: false, inativoC: false, data: mesmaData),
            Row("000002", ValidCnpj1, "EMPRESA A FILIAL", inativoF: false, inativoC: false, data: mesmaData),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, new Dictionary<string, FornecedorExistente>(), Now);

        var decisao = Assert.Single(plano.Fornecedores);
        Assert.All(decisao.Vinculos, v => Assert.False(v.AtribuirPrincipal));
        Assert.Single(plano.ConflitosPrincipal);
        Assert.Equal("PRINCIPAL_EMPATE", plano.ConflitosPrincipal[0].Code);
    }

    [Fact]
    public void Existente_Sem_Nenhum_Principal_Historico_Recebe_Principal_Pelo_Vinculo_Ativo_Mais_Recente()
    {
        var existenteVinculoAntigo = new VinculoExistente(Guid.NewGuid(), "000001", "EMPRESA ANTIGA", false, false, Now.AddYears(-2), Principal: false);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "EMPRESA ANTIGA", null, "PJ", [existenteVinculoAntigo]),
        };
        var raw = new[] { Row("000002", ValidCnpj1, "EMPRESA NOVA", inativoF: false, inativoC: false, data: new DateTime(2026, 2, 1)) };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        var novoVinculo = Assert.Single(decisao.Vinculos);
        Assert.True(novoVinculo.AtribuirPrincipal); // o vínculo NOVO (mais recente) recebe Principal, já que o existente nunca foi Principal
    }

    [Fact]
    public void Principal_Existente_Ativo_Nunca_E_Trocado_Por_Vinculo_Mais_Recente()
    {
        var principalAtual = new VinculoExistente(Guid.NewGuid(), "000001", "PRINCIPAL ATUAL", false, false, Now.AddDays(-30), Principal: true);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "PRINCIPAL ATUAL", null, "PJ", [principalAtual]),
        };
        // Linha RAW para o vínculo existente (sem mudança) + uma linha nova, mais recente, ativa.
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "PRINCIPAL ATUAL", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)),
            Row("000002", ValidCnpj1, "VINCULO MAIS RECENTE", inativoF: false, inativoC: false, data: new DateTime(2026, 9, 1)),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        Assert.All(decisao.Vinculos, v => Assert.False(v.AtribuirPrincipal));
        Assert.Empty(plano.ConflitosPrincipal);
    }

    [Fact]
    public void Reativacao_De_Principal_Historico_Nao_Reassume_Quando_Outro_Ja_E_Principal_Ativo()
    {
        var outroPrincipalAtivoId = Guid.NewGuid();
        var principalHistoricoId = Guid.NewGuid();
        var outroPrincipalAtivo = new VinculoExistente(outroPrincipalAtivoId, "000002", "OUTRO PRINCIPAL", false, false, Now.AddDays(-5), Principal: true);
        var principalHistoricoInativo = new VinculoExistente(principalHistoricoId, "000001", "PRINCIPAL HISTORICO", true, false, Now.AddDays(-40), Principal: true);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "OUTRO PRINCIPAL", null, "PJ", [outroPrincipalAtivo, principalHistoricoInativo]),
        };
        // O vínculo historicamente Principal volta a ficar ativo nesta leitura.
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "PRINCIPAL HISTORICO", inativoF: false, inativoC: false, data: new DateTime(2026, 9, 1)),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        var reativado = Assert.Single(decisao.Vinculos);
        Assert.True(reativado.RemoverPrincipal);
        Assert.False(reativado.AtribuirPrincipal);
        Assert.Single(plano.ConflitosPrincipal);
    }

    [Fact]
    public void Fonte_Cadastral_LWW_Usa_Vinculo_Ativo_Mais_Recente_Independente_Do_Principal()
    {
        var principalAntigo = new VinculoExistente(Guid.NewGuid(), "000001", "PRINCIPAL ANTIGO", false, false, Now.AddDays(-100), Principal: true);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "RAZAO ANTIGA", null, "PJ", [principalAntigo]),
        };
        // Vínculo NÃO-Principal, mas mais recente — deve fornecer os dados cadastrais mesmo sem ser Principal.
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "PRINCIPAL ANTIGO", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1), razaoSocial: "RAZAO ANTIGA"),
            Row("000002", ValidCnpj1, "MAIS RECENTE", inativoF: false, inativoC: false, data: new DateTime(2026, 9, 1), razaoSocial: "RAZAO NOVA LTDA"),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        Assert.Equal("RAZAO NOVA LTDA", decisao.RazaoSocial);
    }

    [Fact]
    public void Sem_Nenhum_Vinculo_Ativo_Cadastro_Permanece_Inalterado_E_Fornecedor_Fica_Inativo()
    {
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "RAZAO PRESERVADA", "FANTASIA PRESERVADA", "PJ",
                [new VinculoExistente(Guid.NewGuid(), "000001", "X", false, false, Now.AddDays(-10), Principal: true)]),
        };
        var raw = new[] { Row("000001", ValidCnpj1, "X", inativoF: true, inativoC: false, data: new DateTime(2026, 9, 1), razaoSocial: "NOME QUE NAO DEVERIA SER USADO") };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        Assert.False(decisao.Ativo);
        Assert.Equal("RAZAO PRESERVADA", decisao.RazaoSocial);
        Assert.Equal(RefinedAction.Update, decisao.Action); // vínculo mudou de ativo->inativo mesmo sem tocar cadastro
    }

    [Fact]
    public void Vinculo_Ativo_Somente_Quando_Nenhuma_Das_Duas_Tabelas_Marca_Inativo()
    {
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "A", inativoF: false, inativoC: true, data: new DateTime(2026, 1, 1)),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, new Dictionary<string, FornecedorExistente>(), Now);

        var decisao = Assert.Single(plano.Fornecedores);
        var vinculo = Assert.Single(decisao.Vinculos);
        Assert.False(vinculo.Ativo);
        Assert.False(decisao.Ativo);
    }

    [Fact]
    public void Linha_Com_Cnpj_Invalido_Vira_Erro_E_Nao_Bloqueia_As_Demais()
    {
        var raw = new[]
        {
            Row("000001", "00000000000000", "INVALIDO", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)),
            Row("000002", ValidCnpj1, "VALIDO", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, new Dictionary<string, FornecedorExistente>(), Now);

        Assert.Single(plano.Fornecedores);
        Assert.Single(plano.Erros);
        Assert.Equal("000001", plano.Erros[0].OriginRecordKey);
        Assert.Equal("CNPJ_CPF_INVALIDO", plano.Erros[0].Code);
    }

    [Fact]
    public void Vinculo_Sem_Mudanca_E_Marcado_Como_NoChange()
    {
        // O RAW carrega DateTime "naive" (wall-clock de Brasília, sem fuso — espelha o tipo SQL Server
        // datetime de origem); o projetor converte para DateTimeOffset UTC usando America/Sao_Paulo. Para o
        // teste comparar corretamente "sem mudança", a data crua precisa ser a mesma wall-clock que,
        // convertida, produz o DataParaTransferencia já existente — não o valor UTC bruto.
        var dataBrasilia = new DateTime(2026, 8, 29, 10, 0, 0, DateTimeKind.Unspecified);
        var zone = TimeZoneInfo.FindSystemTimeZoneById(OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");
        var dataUtc = new DateTimeOffset(TimeZoneInfo.ConvertTimeToUtc(dataBrasilia, zone));
        var existente = new VinculoExistente(Guid.NewGuid(), "000001", "SEM MUDANCA", false, false, dataUtc, Principal: true);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "SEM MUDANCA", null, "PJ", [existente]),
        };
        var raw = new[] { Row("000001", ValidCnpj1, "SEM MUDANCA", inativoF: false, inativoC: false, data: dataBrasilia) };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        var vinculo = Assert.Single(decisao.Vinculos);
        Assert.Equal(RefinedAction.NoChange, vinculo.Action);
        Assert.Equal(RefinedAction.NoChange, decisao.Action);
    }

    /// <summary>
    /// Regressão real encontrada na aplicação ao MAISCOMPRAS Development (03/09/2026): COD_FORNECEDOR/CLIFOR
    /// são <c>char(6)</c> no Linx — chegam ao RAW com espaço à direita quando o código real tem menos de 6
    /// caracteres (ex.: "2660" chega como "2660  "), mas <c>FornecedorLinxVinculo</c> normaliza (Trim()) ao
    /// persistir. Sem normalizar a leitura do RAW, uma reexecução classificava um vínculo já existente como
    /// "novo" e a tentativa de INSERT colidia com o índice único (ErpSistema, CodigoErp) — quebra real de
    /// idempotência, não hipotética.
    /// </summary>
    [Fact]
    public void Vinculo_Existente_E_Reconhecido_Mesmo_Quando_Raw_Traz_CodigoErp_Com_Espaco_A_Direita_De_Char6()
    {
        var existente = new VinculoExistente(Guid.NewGuid(), "2660", "JA EXISTENTE", false, false, Now.AddDays(-5), Principal: true);
        var existentes = new Dictionary<string, FornecedorExistente>
        {
            [ValidCnpj1] = new(Guid.NewGuid(), ValidCnpj1, "Ativo", "JA EXISTENTE", null, "PJ", [existente]),
        };
        // RAW traz o código com padding de char(6) — "2660" com dois espaços à direita.
        var raw = new[] { Row("2660  ", ValidCnpj1, "JA EXISTENTE", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)) };

        var plano = FornecedorRefinedProjector.Projetar(raw, existentes, Now);

        var decisao = Assert.Single(plano.Fornecedores);
        var vinculo = Assert.Single(decisao.Vinculos);
        Assert.NotEqual(RefinedAction.Insert, vinculo.Action); // nunca tenta inserir um vínculo que já existe
        Assert.Equal(existente.Id, vinculo.VinculoExistenteId);
        Assert.Equal("2660", vinculo.CodigoErp); // decisão sempre carrega o valor já normalizado (trimado)
    }

    [Fact]
    public void Duas_Cnpjs_Distintos_Produzem_Duas_Decisoes_Independentes()
    {
        var raw = new[]
        {
            Row("000001", ValidCnpj1, "A", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)),
            Row("000002", ValidCnpj2, "B", inativoF: false, inativoC: false, data: new DateTime(2026, 1, 1)),
        };

        var plano = FornecedorRefinedProjector.Projetar(raw, new Dictionary<string, FornecedorExistente>(), Now);

        Assert.Equal(2, plano.Fornecedores.Count);
    }

    private static RawLinxFornecedorSnapshotRegistro Row(
        string codigoFornecedor, string cnpj, string nomeFantasia, bool inativoF, bool inativoC, DateTime data, string? razaoSocial = null) =>
        RawLinxFornecedorSnapshotRegistro.ParaTeste(
            codigoFornecedor, clifor: codigoFornecedor, cnpjCpf: cnpj, razaoSocial: razaoSocial ?? nomeFantasia,
            nomeFantasia: nomeFantasia, tipoPessoa: "PJ", inativoFornecedores: inativoF, inativoCadastroCliFor: inativoC,
            ultimaAlteracao: data);
}
