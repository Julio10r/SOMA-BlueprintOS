using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class DescobrirFornecedoresUseCase(
    IErpFornecedorDiscoveryRepository erpRepository,
    IFornecedorDescobertoRepository descobertaRepository) : IDescobrirFornecedoresUseCase
{
    public async Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(DescobrirFornecedoresDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.CodigoItem)) throw new ArgumentException("Código do item é obrigatório.", nameof(dto.CodigoItem));
        if (string.IsNullOrWhiteSpace(dto.Descricao) && string.IsNullOrWhiteSpace(dto.Categoria))
            throw new ArgumentException("Descrição ou categoria deve ser informada.", nameof(dto));

        var candidates = await erpRepository.DescobrirAsync(new FornecedorDiscoveryQuery(dto.CodigoItem, dto.Descricao, dto.Categoria), cancellationToken);
        var result = new List<FornecedorDescobertoDto>(candidates.Count);
        foreach (var candidate in candidates)
        {
            var score = ScoreFornecedor.Calcular(candidate.ItemExato, candidate.Familia, candidate.Categoria, candidate.Historico);
            var descoberta = new FornecedorDescoberto(Guid.NewGuid(), dto.CodigoItem, dto.Descricao, dto.Categoria,
                candidate.Nome, candidate.Cnpj, candidate.CodigoFornecedor, score,
                ScoreFornecedor.DeterminarCriterio(candidate.ItemExato, candidate.Familia, candidate.Categoria, candidate.Historico),
                DateTimeOffset.UtcNow);
            await descobertaRepository.AdicionarAsync(descoberta, cancellationToken);
            result.Add(ToDto(descoberta));
        }
        return result;
    }

    internal static FornecedorDescobertoDto ToDto(FornecedorDescoberto value) => new(value.Id, value.CodigoItem, value.Descricao,
        value.Categoria, value.Nome, value.Cnpj, value.CodigoFornecedor, value.Score, value.Criterio, value.DescobertoEm);
}

public sealed class ListarDescobertasUseCase(IFornecedorDescobertoRepository repository) : IListarDescobertasUseCase
{
    public async Task<IReadOnlyList<FornecedorDescobertoDto>> ExecuteAsync(CancellationToken cancellationToken = default) =>
        (await repository.ListarAsync(cancellationToken)).Select(DescobrirFornecedoresUseCase.ToDto).ToArray();

    public async Task<FornecedorDescobertoDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default) =>
        (await repository.ObterPorIdAsync(id, cancellationToken)) is { } value ? DescobrirFornecedoresUseCase.ToDto(value) : null;
}
