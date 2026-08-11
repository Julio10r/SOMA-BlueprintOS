using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

/// <summary>Monta as projeções de leitura de Perfil sem N+1, resolvendo permissões e contagem de usuários
/// em duas consultas agregadas por lote.</summary>
internal static class PerfilProjection
{
    public static async Task<IReadOnlyList<PerfilDto>> ProjetarAsync(
        IPerfilRepository perfis,
        IReadOnlyList<Perfil> origem,
        CancellationToken ct)
    {
        if (origem.Count == 0) return [];

        var ids = origem.Select(x => x.Id).ToArray();
        var permissoes = await perfis.ObterPermissoesPorPerfilAsync(ids, ct);
        var contagens = await perfis.ContarUsuariosPorPerfilAsync(ids, ct);

        return origem
            .Select(perfil => new PerfilDto(
                perfil.Id,
                perfil.Nome,
                perfil.Descricao,
                perfil.UnidadeNegocioId,
                perfil.Ativo,
                permissoes.TryGetValue(perfil.Id, out var codigos) ? codigos : [],
                contagens.TryGetValue(perfil.Id, out var total) ? total : 0,
                perfil.CriadoEm,
                perfil.AtualizadoEm))
            .ToArray();
    }

    public static async Task<PerfilDto> ProjetarUmAsync(IPerfilRepository perfis, Perfil perfil, CancellationToken ct) =>
        (await ProjetarAsync(perfis, [perfil], ct))[0];
}

public sealed class ListarPerfisUseCase(IPerfilRepository perfis) : IListarPerfisUseCase
{
    public async Task<IReadOnlyList<PerfilDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontrados = await perfis.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return await PerfilProjection.ProjetarAsync(perfis, encontrados, ct);
    }
}

public sealed class ObterPerfilUseCase(IPerfilRepository perfis) : IObterPerfilUseCase
{
    public async Task<PerfilDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct)
    {
        // Escopo por Unidade de Negócio aplicado na própria consulta: um Id válido de outra Unidade de
        // Negócio é indistinguível de um Id inexistente (não vaza existência de recurso alheio).
        var perfil = await perfis.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        return perfil is null ? null : await PerfilProjection.ProjetarUmAsync(perfis, perfil, ct);
    }
}

/// <summary>Validação compartilhada do conjunto de permissões enviado pelo cliente. Um código fora do
/// catálogo é rejeitado, nunca ignorado silenciosamente — ignorar permitiria que um erro de digitação
/// resultasse em um Perfil com menos acesso que o operador acredita ter concedido.</summary>
internal static class PermissoesRequisitadas
{
    public static async Task<RbacResultado<IReadOnlyList<Permissao>>> ResolverAsync(
        IPermissaoRepository permissoes,
        IReadOnlyList<string>? solicitadas,
        CancellationToken ct)
    {
        var codigos = (solicitadas ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .ToArray();

        var desconhecidos = codigos.Where(x => !PermissaoCatalogo.Existe(x)).Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
        if (desconhecidos.Length > 0)
        {
            return RbacResultado<IReadOnlyList<Permissao>>.Erro(
                RbacFalha.PermissaoDesconhecida,
                $"Permissão desconhecida: {string.Join(", ", desconhecidos)}.");
        }

        var canonicos = codigos
            .Select(x => PermissaoCatalogo.Normalizar(x)!)
            .Distinct(StringComparer.Ordinal)
            .ToArray();

        if (canonicos.Length == 0) return RbacResultado<IReadOnlyList<Permissao>>.Ok([]);

        var encontradas = await permissoes.ObterPorCodigosAsync(canonicos, ct);
        if (encontradas.Count != canonicos.Length)
        {
            // O catálogo em código e o catálogo persistido divergiram (seed não aplicado). Falha explícita:
            // nunca conceder um Perfil parcialmente montado.
            var ausentes = canonicos.Except(encontradas.Select(x => x.Codigo), StringComparer.OrdinalIgnoreCase);
            return RbacResultado<IReadOnlyList<Permissao>>.Erro(
                RbacFalha.PermissaoDesconhecida,
                $"Permissão não encontrada no catálogo persistido: {string.Join(", ", ausentes)}.");
        }

        return RbacResultado<IReadOnlyList<Permissao>>.Ok(encontradas);
    }
}

/// <summary>Regra de não-escalonamento de privilégio: ninguém concede uma permissão que não possui.
///
/// Sem esta regra, <c>Perfil.Gerenciar</c> seria equivalente a super-administrador: o portador poderia
/// editar o próprio Perfil acrescentando todo o catálogo e, como as permissões efetivas são reresolvidas a
/// cada requisição, já teria acesso total na chamada seguinte — sem novo login e sem rastro. A ADR-0020
/// (item 8) trata <c>Perfil.Gerenciar</c> como uma permissão atômica entre outras, não como acesso
/// irrestrito, então a delegação da gestão de Perfis não pode implicar concessão ilimitada.
///
/// O Administrador Sênior possui o catálogo completo e por isso nunca é afetado por esta verificação.</summary>
internal static class NaoEscalonamento
{
    public static string[] PermissoesAcimaDoAtor(
        IReadOnlyCollection<Permissao> solicitadas, IReadOnlyList<string> permissoesDoAtor)
    {
        var doAtor = new HashSet<string>(permissoesDoAtor ?? [], StringComparer.OrdinalIgnoreCase);
        return solicitadas
            .Select(x => x.Codigo)
            .Where(codigo => !doAtor.Contains(codigo))
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Impede o auto-bloqueio administrativo: nenhuma operação pode deixar a Unidade de Negócio sem
/// nenhum Perfil ativo capaz de gerenciar Perfis (<c>Perfil.Gerenciar</c>). Sem esta invariante, um
/// administrador poderia — por engano ou por ataque de negação de serviço — remover a última permissão
/// administrativa e tornar o RBAC irrecuperável pela própria aplicação.</summary>
internal static class PerfilAdministrativoInvariante
{
    public const string Mensagem =
        "Esta operação deixaria a Unidade de Negócio sem nenhum Perfil ativo com a permissão "
        + PermissaoCatalogo.PerfilGerenciar + " e usuários vinculados. Garanta outro Perfil ativo, com essa "
        + "permissão e ao menos um usuário vinculado, antes de prosseguir.";

    /// <summary>Simula o estado final e verifica se ao menos um Perfil ativo COM USUÁRIO VINCULADO mantém
    /// <c>Perfil.Gerenciar</c>. <paramref name="perfilAlterado"/> é o Perfil sob edição;
    /// <paramref name="ativoDepois"/>/<paramref name="temPermissaoDepois"/> descrevem seu estado futuro.
    ///
    /// A exigência de usuário vinculado é essencial: um Perfil administrativo ativo mas sem ninguém
    /// vinculado não preserva acesso nenhum. Sem essa checagem, bastaria criar um Perfil "Temp" com
    /// <c>Perfil.Gerenciar</c> e zero usuários para que a invariante autorizasse remover a permissão do
    /// Perfil realmente em uso — e o Bootstrap Mode nunca reabre (ADR-0020, item 12), então a recuperação
    /// exigiria SQL direto no banco.</summary>
    public static bool Preservada(
        IReadOnlyList<Perfil> todosDaUnidade,
        IReadOnlyDictionary<Guid, IReadOnlyList<string>> permissoesAtuais,
        IReadOnlyDictionary<Guid, int> usuariosPorPerfil,
        Guid perfilAlterado,
        bool ativoDepois,
        bool temPermissaoDepois)
    {
        foreach (var perfil in todosDaUnidade)
        {
            var ativo = perfil.Id == perfilAlterado ? ativoDepois : perfil.Ativo;
            if (!ativo) continue;

            if (!usuariosPorPerfil.TryGetValue(perfil.Id, out var usuarios) || usuarios == 0) continue;

            var tem = perfil.Id == perfilAlterado
                ? temPermissaoDepois
                : permissoesAtuais.TryGetValue(perfil.Id, out var codigos)
                    && codigos.Any(c => string.Equals(c, PermissaoCatalogo.PerfilGerenciar, StringComparison.OrdinalIgnoreCase));

            if (tem) return true;
        }

        return false;
    }
}

public sealed class CriarPerfilUseCase(
    IPerfilRepository perfis,
    IPermissaoRepository permissoes,
    TimeProvider clock) : ICriarPerfilUseCase
{
    public async Task<RbacResultado<PerfilDto>> ExecuteAsync(
        PerfilInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct)
    {
        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.NomeObrigatorio, "Nome do Perfil é obrigatório.");
        }

        // Pré-checagem amigável; a garantia real é o índice único (UnidadeNegocioId, Nome) no SQL Server.
        var existente = await perfis.ObterPorNomeEUnidadeNegocioAsync(nome, unidadeNegocioId, ct);
        if (existente is not null)
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.NomeDuplicado, "Já existe um Perfil com este nome nesta Unidade de Negócio.");
        }

        var resolvidas = await PermissoesRequisitadas.ResolverAsync(permissoes, input.Permissoes, ct);
        if (!resolvidas.Sucesso)
        {
            return RbacResultado<PerfilDto>.Erro(resolvidas.Falha, resolvidas.Mensagem!);
        }

        var acimaDoAtor = NaoEscalonamento.PermissoesAcimaDoAtor(resolvidas.Valor!, permissoesDoAtor);
        if (acimaDoAtor.Length > 0)
        {
            return RbacResultado<PerfilDto>.Erro(
                RbacFalha.EscalonamentoDePrivilegio,
                $"Você não pode conceder permissões que não possui: {string.Join(", ", acimaDoAtor)}.");
        }

        var agora = clock.GetUtcNow();
        var perfil = new Perfil(nome, input.Descricao ?? string.Empty, unidadeNegocioId, agora);
        await perfis.AdicionarAsync(perfil, ct);
        await perfis.VincularPermissoesAsync(perfil.Id, resolvidas.Valor!.Select(x => x.Id).ToArray(), ct);
        await perfis.SalvarAlteracoesAsync(ct);

        return RbacResultado<PerfilDto>.Ok(await PerfilProjection.ProjetarUmAsync(perfis, perfil, ct));
    }
}

public sealed class AtualizarPerfilUseCase(
    IPerfilRepository perfis,
    IPermissaoRepository permissoes,
    TimeProvider clock) : IAtualizarPerfilUseCase
{
    public async Task<RbacResultado<PerfilDto>> ExecuteAsync(
        Guid id, PerfilInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct)
    {
        var perfil = await perfis.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (perfil is null)
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.PerfilNaoEncontrado, "Perfil não encontrado.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.NomeObrigatorio, "Nome do Perfil é obrigatório.");
        }

        var homonimo = await perfis.ObterPorNomeEUnidadeNegocioAsync(nome, unidadeNegocioId, ct);
        if (homonimo is not null && homonimo.Id != perfil.Id)
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.NomeDuplicado, "Já existe um Perfil com este nome nesta Unidade de Negócio.");
        }

        var resolvidas = await PermissoesRequisitadas.ResolverAsync(permissoes, input.Permissoes, ct);
        if (!resolvidas.Sucesso)
        {
            return RbacResultado<PerfilDto>.Erro(resolvidas.Falha, resolvidas.Mensagem!);
        }

        var acimaDoAtor = NaoEscalonamento.PermissoesAcimaDoAtor(resolvidas.Valor!, permissoesDoAtor);
        if (acimaDoAtor.Length > 0)
        {
            return RbacResultado<PerfilDto>.Erro(
                RbacFalha.EscalonamentoDePrivilegio,
                $"Você não pode conceder permissões que não possui: {string.Join(", ", acimaDoAtor)}.");
        }

        var manteraGerenciarPerfil = resolvidas.Valor!
            .Any(x => string.Equals(x.Codigo, PermissaoCatalogo.PerfilGerenciar, StringComparison.OrdinalIgnoreCase));

        var todos = await perfis.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        var permissoesAtuais = await perfis.ObterPermissoesPorPerfilAsync(todos.Select(x => x.Id).ToArray(), ct);
        var usuariosPorPerfil = await perfis.ContarUsuariosPorPerfilAsync(todos.Select(x => x.Id).ToArray(), ct);
        if (!PerfilAdministrativoInvariante.Preservada(
                todos, permissoesAtuais, usuariosPorPerfil, perfil.Id, perfil.Ativo, manteraGerenciarPerfil))
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.UltimoPerfilAdministrativo, PerfilAdministrativoInvariante.Mensagem);
        }

        var agora = clock.GetUtcNow();
        perfil.Atualizar(nome, input.Descricao ?? string.Empty, agora);
        await perfis.SubstituirPermissoesAsync(perfil.Id, resolvidas.Valor!.Select(x => x.Id).ToArray(), ct);
        await perfis.SalvarAlteracoesAsync(ct);

        return RbacResultado<PerfilDto>.Ok(await PerfilProjection.ProjetarUmAsync(perfis, perfil, ct));
    }
}

public sealed class AlterarStatusPerfilUseCase(IPerfilRepository perfis, TimeProvider clock) : IAlterarStatusPerfilUseCase
{
    public async Task<RbacResultado<PerfilDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var perfil = await perfis.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (perfil is null)
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.PerfilNaoEncontrado, "Perfil não encontrado.");
        }

        var todos = await perfis.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        var permissoesAtuais = await perfis.ObterPermissoesPorPerfilAsync(todos.Select(x => x.Id).ToArray(), ct);
        var usuariosPorPerfil = await perfis.ContarUsuariosPorPerfilAsync(todos.Select(x => x.Id).ToArray(), ct);
        var temGerenciarPerfil = permissoesAtuais.TryGetValue(perfil.Id, out var codigos)
            && codigos.Any(c => string.Equals(c, PermissaoCatalogo.PerfilGerenciar, StringComparison.OrdinalIgnoreCase));

        if (!PerfilAdministrativoInvariante.Preservada(
                todos, permissoesAtuais, usuariosPorPerfil, perfil.Id, ativo, temGerenciarPerfil))
        {
            return RbacResultado<PerfilDto>.Erro(RbacFalha.UltimoPerfilAdministrativo, PerfilAdministrativoInvariante.Mensagem);
        }

        var agora = clock.GetUtcNow();
        if (ativo) perfil.Ativar(agora); else perfil.Inativar(agora);
        await perfis.SalvarAlteracoesAsync(ct);

        return RbacResultado<PerfilDto>.Ok(await PerfilProjection.ProjetarUmAsync(perfis, perfil, ct));
    }
}

/// <summary>O catálogo devolvido à interface vem do banco (fonte de verdade da autorização), enriquecido
/// com os metadados de apresentação de <see cref="PermissaoCatalogo"/>. Códigos presentes no banco mas
/// ausentes do catálogo em código são omitidos — a interface nunca oferece uma permissão que a aplicação
/// não sabe interpretar.</summary>
public sealed class ListarCatalogoPermissoesUseCase(IPermissaoRepository permissoes) : IListarCatalogoPermissoesUseCase
{
    public async Task<IReadOnlyList<PermissaoCatalogoDto>> ExecuteAsync(CancellationToken ct)
    {
        var persistidas = await permissoes.ListarAsync(ct);
        var codigosPersistidos = persistidas.Select(x => x.Codigo).ToHashSet(StringComparer.OrdinalIgnoreCase);

        return PermissaoCatalogo.Todas
            .Where(x => codigosPersistidos.Contains(x.Codigo))
            .Select(x => new PermissaoCatalogoDto(x.Codigo, x.Recurso, x.Acao, x.Descricao))
            .ToArray();
    }
}
