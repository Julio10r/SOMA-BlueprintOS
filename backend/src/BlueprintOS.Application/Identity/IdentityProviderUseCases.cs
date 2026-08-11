using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

internal static class IdentityProviderProjection
{
    public static IdentityProviderDto Projetar(IdentityProvider provider) => new(
        provider.Id,
        provider.UnidadeNegocioId,
        provider.Tipo,
        provider.DominiosAutorizados,
        provider.ParametrosConfigurados,
        provider.EstaAtivo(),
        provider.CriadoEm,
        provider.AtualizadoEm);
}

/// <summary>O1.11 — Identity Providers por Unidade de Negócio. Operação administrativa corporativa sobre
/// uma UN explícita do path (não a da sessão de quem administra) — protegida por
/// <c>Sistema.Gerenciar</c>. Segredos nunca em claro no log; apenas o Id/Tipo são logados.</summary>
public sealed class ListarIdentityProvidersUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IIdentityProviderRepository providers) : IListarIdentityProvidersUseCase
{
    public async Task<RbacResultado<IReadOnlyList<IdentityProviderDto>>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<IReadOnlyList<IdentityProviderDto>>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var encontrados = await providers.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return RbacResultado<IReadOnlyList<IdentityProviderDto>>.Ok(encontrados.Select(IdentityProviderProjection.Projetar).ToArray());
    }
}

public sealed class CriarIdentityProviderUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IIdentityProviderRepository providers, ISegredoProtector protector,
    TimeProvider clock, ILogger<CriarIdentityProviderUseCase> logger) : ICriarIdentityProviderUseCase
{
    public async Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, IdentityProviderInput input, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var tipo = (input.Tipo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.TipoObrigatorio, "Tipo do Identity Provider é obrigatório.");
        }

        var parametrosProtegidos = string.IsNullOrEmpty(input.Parametros) ? null : protector.Proteger(input.Parametros);
        var agora = clock.GetUtcNow();
        var provider = new IdentityProvider(unidadeNegocioId, tipo, input.DominiosAutorizados, parametrosProtegidos, agora);

        await providers.AdicionarAsync(provider, ct);
        await providers.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Identity Provider criado. UnidadeNegocioId={UnidadeNegocioId} IdentityProviderId={IdentityProviderId} Tipo={Tipo}",
            unidadeNegocioId, provider.Id, provider.Tipo);

        return RbacResultado<IdentityProviderDto>.Ok(IdentityProviderProjection.Projetar(provider));
    }
}

public sealed class AtualizarIdentityProviderUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IIdentityProviderRepository providers, ISegredoProtector protector,
    TimeProvider clock, ILogger<AtualizarIdentityProviderUseCase> logger) : IAtualizarIdentityProviderUseCase
{
    public async Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, Guid id, IdentityProviderInput input, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var provider = await providers.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (provider is null)
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.IdentityProviderNaoEncontrado, "Identity Provider não encontrado.");
        }

        var tipo = (input.Tipo ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(tipo))
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.TipoObrigatorio, "Tipo do Identity Provider é obrigatório.");
        }

        var parametrosProtegidos = string.IsNullOrEmpty(input.Parametros) ? null : protector.Proteger(input.Parametros);
        provider.Editar(tipo, input.DominiosAutorizados, parametrosProtegidos, clock.GetUtcNow());
        await providers.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Identity Provider atualizado. UnidadeNegocioId={UnidadeNegocioId} IdentityProviderId={IdentityProviderId}",
            unidadeNegocioId, provider.Id);

        return RbacResultado<IdentityProviderDto>.Ok(IdentityProviderProjection.Projetar(provider));
    }
}

public sealed class AlterarStatusIdentityProviderUseCase(
    IUnidadeNegocioRepository unidadesNegocio, IIdentityProviderRepository providers, TimeProvider clock,
    ILogger<AlterarStatusIdentityProviderUseCase> logger) : IAlterarStatusIdentityProviderUseCase
{
    public async Task<RbacResultado<IdentityProviderDto>> ExecuteAsync(Guid unidadeNegocioId, Guid id, bool ativo, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var provider = await providers.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (provider is null)
        {
            return RbacResultado<IdentityProviderDto>.Erro(RbacFalha.IdentityProviderNaoEncontrado, "Identity Provider não encontrado.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) provider.Ativar(agora); else provider.Inativar(agora);
        await providers.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Status do Identity Provider alterado. UnidadeNegocioId={UnidadeNegocioId} IdentityProviderId={IdentityProviderId} Ativo={Ativo}",
            unidadeNegocioId, provider.Id, ativo);

        return RbacResultado<IdentityProviderDto>.Ok(IdentityProviderProjection.Projetar(provider));
    }
}
