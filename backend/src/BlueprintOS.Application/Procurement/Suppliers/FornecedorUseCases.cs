using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class CadastrarFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : ICadastrarFornecedorUseCase
{
    public async Task<FornecedorDto> ExecuteAsync(CadastrarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var user = identity.GetRequired().UserId;
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        var documento = DocumentoFiscal.Create(dto.Cnpj_Cpf ?? Cnpj.Create(dto.Cnpj).Value);
        if (await repository.ExisteAsync(documento.Value, cancellationToken)) throw new InvalidOperationException("Documento fiscal already exists.");
        var fornecedor = new Fornecedor(Guid.NewGuid(), dto.RazaoSocial ?? dto.Nome, documento, dto.TipoPessoa, dto.Categoria, dto.Email, dto.Telefone,
            dto.Website, dto.Cidade, dto.Estado, dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, user, DateTimeOffset.UtcNow);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        await repository.AdicionarAsync(fornecedor, cancellationToken);
        return FornecedorMapper.ToDto(fornecedor);
    }
}

public sealed class AtualizarFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IAtualizarFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, AtualizarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        var fornecedor = await repository.ObterPorIdAsync(id, identity.GetRequired().UserId, cancellationToken);
        if (fornecedor is null) return null;
        fornecedor.Atualizar(dto.RazaoSocial ?? dto.Nome, dto.Categoria, dto.Email, dto.Telefone, dto.Website, dto.Cidade, dto.Estado,
            dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, DateTimeOffset.UtcNow);
        var documento = dto.Cnpj_Cpf ?? dto.Cnpj;
        if (!string.IsNullOrWhiteSpace(documento) && documento != fornecedor.Cnpj_Cpf) fornecedor.AtualizarDocumento(documento, dto.TipoPessoa, DateTimeOffset.UtcNow);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        await repository.AtualizarAsync(fornecedor, cancellationToken);
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
        value.TipoFornecedorDominioId, value.SubtipoFornecedorDominioId);
}
