using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class AnalisarEnriquecimentoFornecedorUseCase(IFornecedorRepository fornecedores)
    : IAnalisarEnriquecimentoFornecedorUseCase
{
    public async Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, AnalisarEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, cancellationToken);
        return fornecedor is null ? null : FornecedorEnriquecimentoComparer.Comparar(fornecedor, dto.Consulta, dto.ConsultaId, ResolveCorrelationId(dto.CorrelationId));
    }

    internal static string ResolveCorrelationId(string? correlationId) =>
        string.IsNullOrWhiteSpace(correlationId) ? Guid.NewGuid().ToString("N") : correlationId.Trim();
}

public sealed class AprovarEnriquecimentoFornecedorUseCase(
    IFornecedorRepository fornecedores,
    IFornecedorEnriquecimentoAnaliseRepository analises,
    ICurrentIdentity identity) : IAprovarEnriquecimentoFornecedorUseCase
{
    public async Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, DecidirEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var requestIdentity = identity.GetRequired();
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, cancellationToken);
        if (fornecedor is null) return null;
        var correlationId = AnalisarEnriquecimentoFornecedorUseCase.ResolveCorrelationId(dto.CorrelationId);
        var divergencias = FornecedorEnriquecimentoComparer.Comparar(fornecedor, dto.Consulta, dto.ConsultaId, correlationId).Divergencias;
        var campos = ResolveCampos(dto.Campos, divergencias);
        var alteracoes = new Dictionary<string, string?>(StringComparer.OrdinalIgnoreCase);
        foreach (var divergencia in divergencias.Where(x => campos.Contains(x.Campo)))
        {
            if (FornecedorEnriquecimentoComparer.CampoPodeAtualizar(diferenciaCampo: divergencia.Campo))
            {
                alteracoes[divergencia.Campo] = divergencia.ValorSugerido;
            }
            await RegistrarAsync(analises, fornecedor, dto, divergencia, "Aceito", requestIdentity.UserId, correlationId, cancellationToken);
        }

        if (alteracoes.Count > 0)
        {
            fornecedor.AplicarEnriquecimentoCnpj(alteracoes, DateTimeOffset.UtcNow);
            await fornecedores.AtualizarAsync(fornecedor, cancellationToken);
        }

        return FornecedorEnriquecimentoComparer.Comparar(fornecedor, dto.Consulta, dto.ConsultaId, correlationId) with
        {
            Divergencias = divergencias.Select(x => x with { StatusDecisao = campos.Contains(x.Campo) ? FornecedorCampoDecisao.Aceito : x.StatusDecisao }).ToArray()
        };
    }

    internal static HashSet<string> ResolveCampos(IReadOnlyList<string> campos, IReadOnlyList<FornecedorCampoDivergencia> divergencias) =>
        campos.Count == 0
            ? divergencias.Select(x => x.Campo).ToHashSet(StringComparer.OrdinalIgnoreCase)
            : campos.Where(x => !string.IsNullOrWhiteSpace(x)).Select(x => x.Trim()).ToHashSet(StringComparer.OrdinalIgnoreCase);

    internal static Task RegistrarAsync(IFornecedorEnriquecimentoAnaliseRepository analises, Fornecedor fornecedor,
        DecidirEnriquecimentoFornecedorDto dto, FornecedorCampoDivergencia divergencia, string decisao, Guid usuario,
        string correlationId, CancellationToken cancellationToken) => analises.AdicionarAsync(
        new FornecedorEnriquecimentoAnalise(Guid.NewGuid(), fornecedor.Id, fornecedor.Cnpj_Cpf, dto.ConsultaId, divergencia.Campo,
            divergencia.ValorAtual, divergencia.ValorSugerido, decisao, usuario, DateTimeOffset.UtcNow, correlationId,
            dto.BusinessUnit, dto.ErpSistema, dto.Consulta.FonteConsulta), cancellationToken);
}

public sealed class RejeitarEnriquecimentoFornecedorUseCase(
    IFornecedorRepository fornecedores,
    IFornecedorEnriquecimentoAnaliseRepository analises,
    ICurrentIdentity identity) : IRejeitarEnriquecimentoFornecedorUseCase
{
    public async Task<FornecedorEnriquecimentoAnaliseDto?> ExecuteAsync(Guid fornecedorId, DecidirEnriquecimentoFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var requestIdentity = identity.GetRequired();
        var fornecedor = await fornecedores.ObterPorIdAsync(fornecedorId, cancellationToken);
        if (fornecedor is null) return null;
        var correlationId = AnalisarEnriquecimentoFornecedorUseCase.ResolveCorrelationId(dto.CorrelationId);
        var analise = FornecedorEnriquecimentoComparer.Comparar(fornecedor, dto.Consulta, dto.ConsultaId, correlationId);
        var campos = AprovarEnriquecimentoFornecedorUseCase.ResolveCampos(dto.Campos, analise.Divergencias);
        foreach (var divergencia in analise.Divergencias.Where(x => campos.Contains(x.Campo)))
        {
            await AprovarEnriquecimentoFornecedorUseCase.RegistrarAsync(analises, fornecedor, dto, divergencia, "Rejeitado",
                requestIdentity.UserId, correlationId, cancellationToken);
        }

        return analise with
        {
            Divergencias = analise.Divergencias.Select(x => x with { StatusDecisao = campos.Contains(x.Campo) ? FornecedorCampoDecisao.Rejeitado : x.StatusDecisao }).ToArray()
        };
    }
}

internal static class FornecedorEnriquecimentoComparer
{
    private const string OrigemConsultaCnpj = "ConsultaCnpj";

    public static FornecedorEnriquecimentoAnaliseDto Comparar(Fornecedor fornecedor, ConsultaCnpjResultado consulta, Guid? consultaId, string correlationId)
    {
        var divergencias = new List<FornecedorCampoDivergencia>();
        Add(divergencias, nameof(Fornecedor.RazaoSocial), fornecedor.RazaoSocial, consulta.RazaoSocial);
        Add(divergencias, nameof(Fornecedor.NomeFantasia), fornecedor.NomeFantasia, consulta.NomeFantasia);
        Add(divergencias, nameof(Fornecedor.Cep), fornecedor.Cep, consulta.Cep);
        Add(divergencias, nameof(Fornecedor.Logradouro), fornecedor.Logradouro, consulta.Logradouro);
        Add(divergencias, nameof(Fornecedor.Numero), fornecedor.Numero, consulta.Numero);
        Add(divergencias, nameof(Fornecedor.Complemento), fornecedor.Complemento, consulta.Complemento);
        Add(divergencias, nameof(Fornecedor.Bairro), fornecedor.Bairro, consulta.Bairro);
        Add(divergencias, nameof(Fornecedor.Cidade), fornecedor.Cidade, consulta.Cidade);
        Add(divergencias, nameof(Fornecedor.Estado), fornecedor.Estado, consulta.Estado);
        Add(divergencias, nameof(Fornecedor.Email), fornecedor.Email, consulta.Email);
        Add(divergencias, nameof(Fornecedor.Telefone), fornecedor.Telefone, consulta.Telefone);
        // CNAE principal participa da mesma comparação campo a campo (B2.8) — fornecedor existente
        // não é atualizado silenciosamente; divergência exige aprovação explícita como qualquer outro campo.
        Add(divergencias, nameof(Fornecedor.CnaePrincipalCodigo), fornecedor.CnaePrincipalCodigo, consulta.CnaePrincipalCodigo);
        Add(divergencias, nameof(Fornecedor.CnaePrincipalDescricao), fornecedor.CnaePrincipalDescricao, consulta.CnaePrincipalDescricao);

        var alertas = new List<string>();
        if (!DocumentoIgual(fornecedor.Cnpj_Cpf, consulta.Cnpj_Cpf)) alertas.Add("Cnpj_Cpf retornado pela consulta externa diverge do fornecedor.");
        if (consulta.SituacaoCadastral is { } situacao && situacao != SituacaoCadastralCnpj.Ativa)
        {
            alertas.Add($"Fornecedor possui situação cadastral {situacao.ToString().ToLowerInvariant()}.");
        }

        return new(fornecedor.Id, fornecedor.Cnpj_Cpf, consultaId, consulta.FonteConsulta, correlationId, divergencias, alertas);
    }

    public static bool CampoPodeAtualizar(string diferenciaCampo) =>
        !string.Equals(diferenciaCampo, nameof(Fornecedor.NomeFantasia), StringComparison.OrdinalIgnoreCase) &&
        !string.Equals(diferenciaCampo, nameof(Fornecedor.Cnpj_Cpf), StringComparison.OrdinalIgnoreCase);

    private static void Add(List<FornecedorCampoDivergencia> divergencias, string campo, string? atual, string? sugerido)
    {
        if (string.IsNullOrWhiteSpace(sugerido)) return;
        if (Normalizar(atual) == Normalizar(sugerido)) return;
        divergencias.Add(new(campo, atual, sugerido.Trim(), OrigemConsultaCnpj, FornecedorCampoDecisao.Pendente));
    }

    private static bool DocumentoIgual(string atual, string sugerido) => Normalizar(atual) == Normalizar(sugerido);
    private static string? Normalizar(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim().ToUpperInvariant();
}
