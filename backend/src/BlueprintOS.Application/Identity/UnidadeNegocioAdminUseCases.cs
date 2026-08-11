using System.Text.RegularExpressions;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

internal static partial class UnidadeNegocioProjection
{
    public static UnidadeNegocioDto Projetar(UnidadeNegocio unidade) => new(unidade.Id, unidade.Nome, unidade.Slug, unidade.Ativa);

    /// <summary>Formato seguro para URL: minúsculas, dígitos e hífen, sem hífen nas pontas nem repetido —
    /// mesmo cuidado de segurança pedido explicitamente na Work Order O1.11 (nunca aceitar caracteres
    /// especiais no slug antes de persistir).</summary>
    [GeneratedRegex("^[a-z0-9]+(-[a-z0-9]+)*$")]
    public static partial Regex SlugValido();
}

/// <summary>O1.11 — `GET /me/unidades-negocio`. Sistema single-BU-por-usuário: hoje sempre devolve uma
/// única Unidade de Negócio (a da sessão) — sem qualquer associação N:N Usuário×Unidade de Negócio e sem
/// tocar em <c>SessionCurrentIdentity</c>/claims/cookies (O1.4.x).</summary>
public sealed class ListarMinhasUnidadesNegocioUseCase(IUnidadeNegocioRepository unidadesNegocio) : IListarMinhasUnidadesNegocioUseCase
{
    public async Task<IReadOnlyList<UnidadeNegocioDto>> ExecuteAsync(Guid unidadeNegocioDaSessao, CancellationToken ct)
    {
        var unidade = await unidadesNegocio.ObterPorIdAsync(unidadeNegocioDaSessao, ct);
        return unidade is null ? [] : [UnidadeNegocioProjection.Projetar(unidade)];
    }
}

/// <summary>O1.11 — Cadastro de Unidades de Negócio. Recurso CORPORATIVO: nunca filtrado pela Unidade de
/// Negócio de quem administra — a UN sendo administrada é o próprio recurso, protegido pela permissão
/// <c>UnidadeNegocio.Gerenciar</c>, não pelo escopo do usuário.</summary>
public sealed class ListarUnidadesNegocioUseCase(IUnidadeNegocioRepository unidadesNegocio) : IListarUnidadesNegocioUseCase
{
    public async Task<IReadOnlyList<UnidadeNegocioDto>> ExecuteAsync(CancellationToken ct)
    {
        var todas = await unidadesNegocio.ListarTodasAsync(ct);
        return todas.Select(UnidadeNegocioProjection.Projetar).ToArray();
    }
}

public sealed class CriarUnidadeNegocioUseCase(
    IUnidadeNegocioRepository unidadesNegocio, ILogger<CriarUnidadeNegocioUseCase> logger) : ICriarUnidadeNegocioUseCase
{
    public async Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(UnidadeNegocioCriarInput input, CancellationToken ct)
    {
        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Unidade de Negócio é obrigatório.");
        }

        var slug = (input.Slug ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(slug))
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.SlugObrigatorio, "Slug da Unidade de Negócio é obrigatório.");
        }

        if (!UnidadeNegocioProjection.SlugValido().IsMatch(slug))
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.SlugInvalido,
                "Slug inválido: use apenas letras minúsculas, números e hífen (sem espaços ou caracteres especiais).");
        }

        // Pré-checagem amigável; a garantia real é o índice único de Slug no SQL Server.
        if (await unidadesNegocio.ExisteComSlugAsync(slug, excluirId: null, ct))
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.SlugDuplicado, "Já existe uma Unidade de Negócio com este slug.");
        }

        var unidade = new UnidadeNegocio(nome, slug);
        await unidadesNegocio.AdicionarAsync(unidade, ct);
        await unidadesNegocio.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Unidade de Negócio criada. UnidadeNegocioId={UnidadeNegocioId} Slug={Slug}", unidade.Id, unidade.Slug);

        return RbacResultado<UnidadeNegocioDto>.Ok(UnidadeNegocioProjection.Projetar(unidade));
    }
}

public sealed class RenomearUnidadeNegocioUseCase(
    IUnidadeNegocioRepository unidadesNegocio, ILogger<RenomearUnidadeNegocioUseCase> logger) : IRenomearUnidadeNegocioUseCase
{
    public async Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(Guid id, UnidadeNegocioRenomearInput input, CancellationToken ct)
    {
        var unidade = await unidadesNegocio.ObterPorIdAsync(id, ct);
        if (unidade is null)
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.NomeObrigatorio, "Nome da Unidade de Negócio é obrigatório.");
        }

        unidade.Renomear(nome);
        await unidadesNegocio.SalvarAlteracoesAsync(ct);

        logger.LogInformation("Unidade de Negócio renomeada. UnidadeNegocioId={UnidadeNegocioId}", unidade.Id);

        return RbacResultado<UnidadeNegocioDto>.Ok(UnidadeNegocioProjection.Projetar(unidade));
    }
}

public sealed class AlterarStatusUnidadeNegocioUseCase(
    IUnidadeNegocioRepository unidadesNegocio, ILogger<AlterarStatusUnidadeNegocioUseCase> logger) : IAlterarStatusUnidadeNegocioUseCase
{
    public async Task<RbacResultado<UnidadeNegocioDto>> ExecuteAsync(Guid id, bool ativa, CancellationToken ct)
    {
        var unidade = await unidadesNegocio.ObterPorIdAsync(id, ct);
        if (unidade is null)
        {
            return RbacResultado<UnidadeNegocioDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        if (ativa) unidade.Ativar(); else unidade.Inativar();
        await unidadesNegocio.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Status da Unidade de Negócio alterado. UnidadeNegocioId={UnidadeNegocioId} Ativa={Ativa}", unidade.Id, ativa);

        return RbacResultado<UnidadeNegocioDto>.Ok(UnidadeNegocioProjection.Projetar(unidade));
    }
}
