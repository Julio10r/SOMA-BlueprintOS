using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>Resolve a Unidade de Negócio (BU) real usada na integração ERP a partir do contexto de
/// sessão autenticado — nunca do frontend (B2.9, decisão do PO, seção 4). Cai em "DEFAULT" apenas
/// quando a claim de BU não está disponível (ex.: autenticação de Development), reaproveitando a
/// entrada já existente em appsettings — não é um valor de marca inventado, é o único ERP configurado
/// hoje. Reavaliar quando existir mais de uma BU/ERP real.</summary>
public sealed class ResolvedorBusinessUnit(IUnidadeNegocioRepository unidadesNegocio)
{
    public async Task<string> ResolverAsync(Guid? unidadeNegocioId, CancellationToken cancellationToken)
    {
        if (unidadeNegocioId is { } id)
        {
            var unidade = await unidadesNegocio.ObterPorIdAsync(id, cancellationToken);
            if (unidade is not null) return unidade.Slug.ToUpperInvariant();
        }

        return "DEFAULT";
    }
}

/// <summary>Orquestra "Cadastrar Fornecedor" como um único comando funcional (B2.9, decisão do PO, seção
/// 3): persiste em +Compras e, na mesma operação, garante o vínculo no ERP via
/// <see cref="IGarantirFornecedorNoErpUseCase"/>. Não há transação distribuída entre as duas fronteiras
/// de persistência (seção 7) — se +Compras falha, o ERP nunca é chamado; se o ERP falha após +Compras já
/// ter persistido, o Fornecedor permanece salvo localmente com <c>StatusSincronizacao="Pendente"</c> (o
/// mesmo estado que uma reconsulta/nova tentativa converge naturalmente, pela idempotência já provada do
/// Adapter) e a falha não é propagada como erro do cadastro.</summary>
public sealed class CadastrarFornecedorUseCase(
    IFornecedorRepository repository,
    ICurrentIdentity identity,
    IGarantirFornecedorNoErpUseCase garantirNoErp,
    ResolvedorBusinessUnit resolvedorBu,
    ILogger<CadastrarFornecedorUseCase> logger) : ICadastrarFornecedorUseCase
{
    public async Task<FornecedorDto> ExecuteAsync(CadastrarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var requestIdentity = identity.GetRequired();
        var user = requestIdentity.UserId;
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        var documento = DocumentoFiscal.Create(dto.Cnpj_Cpf ?? Cnpj.Create(dto.Cnpj).Value);
        if (await repository.ExisteAsync(documento.Value, cancellationToken)) throw new InvalidOperationException("Documento fiscal already exists.");
        var fornecedor = new Fornecedor(Guid.NewGuid(), dto.RazaoSocial ?? dto.Nome, documento, dto.TipoPessoa, dto.Categoria, dto.Email, dto.Telefone,
            dto.Website, dto.Cidade, dto.Estado, dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, user, DateTimeOffset.UtcNow,
            nomeFantasia: dto.NomeFantasia, cep: dto.Cep, logradouro: dto.Logradouro, numero: dto.Numero, complemento: dto.Complemento, bairro: dto.Bairro);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        // CNAE principal só é persistido nesta operação explícita de cadastro (B2.8) — nunca a
        // partir de uma consulta isolada. Ambos os campos são opcionais/complementares.
        if (dto.CnaePrincipalCodigo is not null || dto.CnaePrincipalDescricao is not null)
            fornecedor.DefinirCnaePrincipal(dto.CnaePrincipalCodigo, dto.CnaePrincipalDescricao, DateTimeOffset.UtcNow);
        await repository.AdicionarAsync(fornecedor, cancellationToken);

        await GarantirNoErpSemFalharCadastroAsync(fornecedor, requestIdentity.UnidadeNegocioId, cancellationToken);

        return FornecedorMapper.ToDto(fornecedor);
    }

    internal async Task GarantirNoErpSemFalharCadastroAsync(Fornecedor fornecedor, Guid? unidadeNegocioId, CancellationToken cancellationToken)
    {
        try
        {
            var businessUnit = await resolvedorBu.ResolverAsync(unidadeNegocioId, cancellationToken);
            await garantirNoErp.ExecuteAsync(fornecedor.Id, businessUnit, new GarantirFornecedorNoErpDto(null), cancellationToken);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            logger.LogWarning(ex, "Falha ao garantir Fornecedor {FornecedorId} no ERP durante o cadastro; permanece Pendente para nova tentativa.", fornecedor.Id);
            fornecedor.RegistrarSincronizacao("Pendente", DateTimeOffset.UtcNow, ex.Message);
            await repository.AtualizarAsync(fornecedor, cancellationToken);
        }
    }
}

public sealed class AtualizarFornecedorUseCase(
    IFornecedorRepository repository,
    ICurrentIdentity identity,
    IGarantirFornecedorNoErpUseCase garantirNoErp,
    ResolvedorBusinessUnit resolvedorBu,
    ILogger<AtualizarFornecedorUseCase> logger) : IAtualizarFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, AtualizarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        var requestIdentity = identity.GetRequired();
        var fornecedor = await repository.ObterPorIdAsync(id, requestIdentity.UserId, cancellationToken);
        if (fornecedor is null) return null;
        fornecedor.Atualizar(dto.RazaoSocial ?? dto.Nome, dto.Categoria, dto.Email, dto.Telefone, dto.Website, dto.Cidade, dto.Estado,
            dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, DateTimeOffset.UtcNow,
            nomeFantasia: dto.NomeFantasia, cep: dto.Cep, logradouro: dto.Logradouro, numero: dto.Numero, complemento: dto.Complemento, bairro: dto.Bairro);
        var documento = dto.Cnpj_Cpf ?? dto.Cnpj;
        if (!string.IsNullOrWhiteSpace(documento) && documento != fornecedor.Cnpj_Cpf) fornecedor.AtualizarDocumento(documento, dto.TipoPessoa, DateTimeOffset.UtcNow);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        if (dto.CnaePrincipalCodigo is not null || dto.CnaePrincipalDescricao is not null)
            fornecedor.DefinirCnaePrincipal(dto.CnaePrincipalCodigo, dto.CnaePrincipalDescricao, DateTimeOffset.UtcNow);
        await repository.AtualizarAsync(fornecedor, cancellationToken);

        try
        {
            var businessUnit = await resolvedorBu.ResolverAsync(requestIdentity.UnidadeNegocioId, cancellationToken);
            await garantirNoErp.ExecuteAsync(fornecedor.Id, businessUnit, new GarantirFornecedorNoErpDto(null), cancellationToken);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            logger.LogWarning(ex, "Falha ao garantir Fornecedor {FornecedorId} no ERP durante a atualização; permanece Pendente para nova tentativa.", fornecedor.Id);
            fornecedor.RegistrarSincronizacao("Pendente", DateTimeOffset.UtcNow, ex.Message);
            await repository.AtualizarAsync(fornecedor, cancellationToken);
        }

        return FornecedorMapper.ToDto(fornecedor);
    }
}

public sealed class ExcluirFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IExcluirFornecedorUseCase
{
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var fornecedor = await repository.ObterPorIdAsync(id, identity.GetRequired().UserId, cancellationToken);
        if (fornecedor is null) return false;
        await repository.ExcluirAsync(fornecedor, cancellationToken);
        return true;
    }
}

public sealed class ObterFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IObterFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await repository.ObterPorIdAsync(id, identity.GetRequired().UserId, cancellationToken)) is { } fornecedor ? FornecedorMapper.ToDto(fornecedor) : null;
}

public sealed class PesquisarFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IPesquisarFornecedorUseCase
{
    public async Task<IReadOnlyList<FornecedorDto>> ExecuteAsync(string? termo, CancellationToken cancellationToken = default)
    {
        var user = identity.GetRequired().UserId;
        var fornecedores = string.IsNullOrWhiteSpace(termo)
            ? await repository.ListarAsync(user, cancellationToken)
            : await repository.PesquisarAsync(termo, user, cancellationToken);
        return fornecedores.Select(FornecedorMapper.ToDto).ToArray();
    }
}

internal static class FornecedorMapper
{
    public static FornecedorDto ToDto(Fornecedor value) => new(value.Id, value.Nome, value.Cnpj, value.Categoria, value.Email,
        value.Telefone, value.Website, value.Cidade, value.Estado, value.Pais, value.Status, value.ScoreIA,
        value.TemporaryUserId, value.CreatedAt, value.UpdatedAt, value.NomeFantasia, value.TipoPessoa, value.InscricaoEstadual,
        value.InscricaoMunicipal, value.Cep, value.Logradouro, value.Numero, value.Complemento, value.Bairro, value.CodigoMunicipio,
        value.Ddd, value.EmailFiscal, value.Banco, value.Agencia, value.Conta, value.DigitosConta, value.CondicaoPagamento,
        value.TipoFornecedor, value.SubtipoFornecedor, value.ContaContabil, value.RegimeFiscal, value.SimplesNacional,
        value.CategoriasFornecimento, value.ForneceMateriais, value.ForneceConsumo, value.ForneceServicos, value.ForneceProdutos,
        value.BusinessUnit, value.ErpSistema, value.ErpFornecedorId, value.Versao, value.HashDadosSincronizaveis,
        value.Cnpj_Cpf, value.RazaoSocial, value.Beneficiador, value.Licenciado, value.CondicaoPagamentoDominioId,
        value.TipoFornecedorDominioId, value.SubtipoFornecedorDominioId, value.CnaePrincipalCodigo, value.CnaePrincipalDescricao);
}
