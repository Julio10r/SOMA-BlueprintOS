using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Resolução das permissões efetivas de um usuário (O1.5 — RBAC Real).
///
/// Fonte única e exclusivamente do banco: <c>UsuariosPerfis</c> → <c>Perfis</c> (somente ativos) →
/// <c>PerfisPermissoes</c> → <c>Permissoes</c>. Nunca de um payload, header ou claim enviado pelo cliente.
/// Um usuário com múltiplos perfis recebe a UNIÃO das permissões (ADR-0020, itens 8 e 10).
///
/// <paramref name="unidadeNegocioId"/> é a Unidade de Negócio DA SESSÃO, e restringe a resolução aos
/// Perfis daquela Unidade. Sem esse filtro, um usuário vinculado a Perfis de duas Unidades de Negócio
/// carregaria, em uma sessão da Unidade A, permissões concedidas na Unidade B — enquanto todas as
/// leituras de dados são escopadas à Unidade A, o que seria escalonamento entre tenants.</summary>
public interface IPermissoesEfetivasResolver
{
    Task<IReadOnlyList<string>> ResolverAsync(Guid usuarioId, Guid unidadeNegocioId, CancellationToken ct);
}

/// <summary>Acesso de leitura ao catálogo global de permissões persistido.</summary>
public interface IPermissaoRepository
{
    /// <summary>Traduz códigos canônicos em Ids do catálogo. Retorna apenas os encontrados — o chamador
    /// compara a contagem para detectar códigos desconhecidos.</summary>
    Task<IReadOnlyList<Permissao>> ObterPorCodigosAsync(IReadOnlyCollection<string> codigos, CancellationToken ct);

    Task<IReadOnlyList<Permissao>> ListarAsync(CancellationToken ct);
}
