using System.Text.RegularExpressions;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

/// <summary>Monta as projeções de leitura de Usuário sem N+1 (mesmo padrão de <c>PerfilProjection</c>, O1.5).</summary>
internal static class UsuarioProjection
{
    public static async Task<IReadOnlyList<UsuarioDto>> ProjetarAsync(
        IUsuarioRepository usuarios, IReadOnlyList<Usuario> origem, CancellationToken ct)
    {
        if (origem.Count == 0) return [];

        var ids = origem.Select(x => x.Id).ToArray();
        var perfis = await usuarios.ObterPerfisPorUsuarioAsync(ids, ct);
        var centrosCusto = await usuarios.ObterCentrosCustoPorUsuarioAsync(ids, ct);

        return origem
            .Select(usuario => new UsuarioDto(
                usuario.Id,
                usuario.Nome,
                usuario.Email,
                usuario.UnidadeNegocioId,
                usuario.EstaAtivo(),
                perfis.TryGetValue(usuario.Id, out var vinculados) ? vinculados : [],
                centrosCusto.TryGetValue(usuario.Id, out var codigos) ? codigos : [],
                usuario.TodosCentrosCusto,
                usuario.CriadoEm,
                usuario.AtualizadoEm))
            .ToArray();
    }

    public static async Task<UsuarioDto> ProjetarUmAsync(IUsuarioRepository usuarios, Usuario usuario, CancellationToken ct) =>
        (await ProjetarAsync(usuarios, [usuario], ct))[0];
}

/// <summary>Validação e resolução compartilhada dos Perfis informados no vínculo. Um Id de Perfil fora do
/// catálogo da Unidade de Negócio da sessão é rejeitado, nunca ignorado silenciosamente (mesmo cuidado de
/// <c>PermissoesRequisitadas</c>, O1.5) — evita que um Id de outra Unidade de Negócio seja aceito.</summary>
internal static class PerfisRequisitados
{
    public static async Task<RbacResultado<IReadOnlyList<Perfil>>> ResolverAsync(
        IPerfilRepository perfis, IReadOnlyList<Guid>? solicitados, Guid unidadeNegocioId, CancellationToken ct)
    {
        var ids = (solicitados ?? []).Distinct().ToArray();
        if (ids.Length == 0) return RbacResultado<IReadOnlyList<Perfil>>.Ok([]);

        var encontrados = await perfis.ObterPorIdsEUnidadeNegocioAsync(ids, unidadeNegocioId, ct);
        if (encontrados.Count != ids.Length)
        {
            return RbacResultado<IReadOnlyList<Perfil>>.Erro(
                RbacFalha.PerfilInvalido,
                "Um ou mais Perfis informados não existem nesta Unidade de Negócio.");
        }

        return RbacResultado<IReadOnlyList<Perfil>>.Ok(encontrados);
    }

    /// <summary>Regra de não-escalonamento de privilégio aplicada ao VÍNCULO de Perfil (não à edição do
    /// Perfil em si, já coberta pela O1.5): quem gerencia Usuários mas não possui todas as permissões de um
    /// Perfil não pode conceder esse Perfil a ninguém, incluindo a si mesmo. Sem esta checagem,
    /// <c>Usuario.Gerenciar</c> seria um caminho indireto para obter qualquer permissão do sistema —
    /// bastaria vincular-se a um Perfil "Administrador Sênior" já existente.</summary>
    public static async Task<string[]> PermissoesAcimaDoAtorAsync(
        IPerfilRepository perfis, IReadOnlyCollection<Perfil> solicitados, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct)
    {
        if (solicitados.Count == 0) return [];

        var doAtor = new HashSet<string>(permissoesDoAtor ?? [], StringComparer.OrdinalIgnoreCase);
        var mapa = await perfis.ObterPermissoesPorPerfilAsync(solicitados.Select(x => x.Id).ToArray(), ct);

        return mapa.Values
            .SelectMany(codigos => codigos)
            .Where(codigo => !doAtor.Contains(codigo))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .OrderBy(x => x, StringComparer.Ordinal)
            .ToArray();
    }
}

/// <summary>Validação de e-mail compartilhada por Criar/Atualizar Usuário. Formato mínimo apenas — a
/// verificação real da caixa postal é o próprio fluxo de Login OTP (O1.4.2).</summary>
internal static partial class EmailUsuarioValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();

    public static bool EhValido(string email) => Formato().IsMatch(email);
}

public sealed class ListarUsuariosUseCase(IUsuarioRepository usuarios) : IListarUsuariosUseCase
{
    public async Task<IReadOnlyList<UsuarioDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontrados = await usuarios.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return await UsuarioProjection.ProjetarAsync(usuarios, encontrados, ct);
    }
}

public sealed class ObterUsuarioUseCase(IUsuarioRepository usuarios) : IObterUsuarioUseCase
{
    public async Task<UsuarioDto?> ExecuteAsync(Guid id, Guid unidadeNegocioId, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        return usuario is null ? null : await UsuarioProjection.ProjetarUmAsync(usuarios, usuario, ct);
    }
}

public sealed class CriarUsuarioUseCase(
    IUsuarioRepository usuarios,
    IPerfilRepository perfis,
    TimeProvider clock) : ICriarUsuarioUseCase
{
    public async Task<RbacResultado<UsuarioDto>> ExecuteAsync(
        UsuarioInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct) =>
        await CriarUsuarioUseCase.Executar(usuarios, perfis, clock, permissoesDoAtor, input, unidadeNegocioId, ct);

    internal static async Task<RbacResultado<UsuarioDto>> Executar(
        IUsuarioRepository usuarios, IPerfilRepository perfis, TimeProvider clock,
        IReadOnlyList<string>? permissoesDoAtor, UsuarioInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.NomeObrigatorio, "Nome do usuário é obrigatório.");
        }

        var email = (input.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (string.IsNullOrWhiteSpace(email))
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.EmailObrigatorio, "E-mail é obrigatório.");
        }
        if (!EmailUsuarioValidator.EhValido(email))
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.EmailInvalido, "E-mail em formato inválido.");
        }

        // Pré-checagem amigável; a garantia real é o índice único em Email no SQL Server (global, não só
        // por Unidade de Negócio — um e-mail identifica uma única pessoa em todo o +Compras).
        var existente = await usuarios.ObterPorEmailAsync(email, ct);
        if (existente is not null)
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.EmailDuplicado, "Já existe um usuário com este e-mail.");
        }

        var perfisResolvidos = await PerfisRequisitados.ResolverAsync(perfis, input.Perfis, unidadeNegocioId, ct);
        if (!perfisResolvidos.Sucesso)
        {
            return RbacResultado<UsuarioDto>.Erro(perfisResolvidos.Falha, perfisResolvidos.Mensagem!);
        }

        if (permissoesDoAtor is not null)
        {
            var acimaDoAtor = await PerfisRequisitados.PermissoesAcimaDoAtorAsync(perfis, perfisResolvidos.Valor!, permissoesDoAtor, ct);
            if (acimaDoAtor.Length > 0)
            {
                return RbacResultado<UsuarioDto>.Erro(
                    RbacFalha.EscalonamentoDePrivilegio,
                    $"Você não pode vincular Perfis com permissões que não possui: {string.Join(", ", acimaDoAtor)}.");
            }
        }

        var agora = clock.GetUtcNow();
        var usuario = new Usuario(email, nome, unidadeNegocioId, input.TodosCentrosCusto, agora);
        await usuarios.AdicionarAsync(usuario, ct);
        await usuarios.SubstituirPerfisAsync(usuario.Id, perfisResolvidos.Valor!.Select(x => x.Id).ToArray(), ct);
        await usuarios.SubstituirCentrosCustoAsync(usuario.Id, NormalizarCentrosCusto(input.CentrosCusto), ct);
        await usuarios.SalvarAlteracoesAsync(ct);

        return RbacResultado<UsuarioDto>.Ok(await UsuarioProjection.ProjetarUmAsync(usuarios, usuario, ct));
    }

    internal static string[] NormalizarCentrosCusto(IReadOnlyList<string>? codigos) =>
        (codigos ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
}

public sealed class AtualizarUsuarioUseCase(
    IUsuarioRepository usuarios,
    IPerfilRepository perfis,
    TimeProvider clock) : IAtualizarUsuarioUseCase
{
    public async Task<RbacResultado<UsuarioDto>> ExecuteAsync(
        Guid id, UsuarioInput input, Guid unidadeNegocioId, IReadOnlyList<string> permissoesDoAtor, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (usuario is null)
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.UsuarioNaoEncontrado, "Usuário não encontrado.");
        }

        var nome = (input.Nome ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(nome))
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.NomeObrigatorio, "Nome do usuário é obrigatório.");
        }

        // O e-mail não é editável nesta sprint (identifica a conta e o fluxo de Login OTP); o payload pode
        // reenviar o mesmo e-mail atual, mas alterá-lo é rejeitado explicitamente — nunca ignorado.
        var emailInformado = (input.Email ?? string.Empty).Trim().ToLowerInvariant();
        if (!string.IsNullOrWhiteSpace(emailInformado) && emailInformado != usuario.Email)
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.EmailInvalido, "O e-mail do usuário não pode ser alterado.");
        }

        var perfisResolvidos = await PerfisRequisitados.ResolverAsync(perfis, input.Perfis, unidadeNegocioId, ct);
        if (!perfisResolvidos.Sucesso)
        {
            return RbacResultado<UsuarioDto>.Erro(perfisResolvidos.Falha, perfisResolvidos.Mensagem!);
        }

        var acimaDoAtor = await PerfisRequisitados.PermissoesAcimaDoAtorAsync(perfis, perfisResolvidos.Valor!, permissoesDoAtor, ct);
        if (acimaDoAtor.Length > 0)
        {
            return RbacResultado<UsuarioDto>.Erro(
                RbacFalha.EscalonamentoDePrivilegio,
                $"Você não pode vincular Perfis com permissões que não possui: {string.Join(", ", acimaDoAtor)}.");
        }

        var agora = clock.GetUtcNow();
        usuario.Atualizar(nome, input.TodosCentrosCusto, agora);
        await usuarios.SubstituirPerfisAsync(usuario.Id, perfisResolvidos.Valor!.Select(x => x.Id).ToArray(), ct);
        await usuarios.SubstituirCentrosCustoAsync(usuario.Id, CriarUsuarioUseCase.NormalizarCentrosCusto(input.CentrosCusto), ct);
        await usuarios.SalvarAlteracoesAsync(ct);

        return RbacResultado<UsuarioDto>.Ok(await UsuarioProjection.ProjetarUmAsync(usuarios, usuario, ct));
    }
}

public sealed class AlterarStatusUsuarioUseCase(IUsuarioRepository usuarios, TimeProvider clock) : IAlterarStatusUsuarioUseCase
{
    public async Task<RbacResultado<UsuarioDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var usuario = await usuarios.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (usuario is null)
        {
            return RbacResultado<UsuarioDto>.Erro(RbacFalha.UsuarioNaoEncontrado, "Usuário não encontrado.");
        }

        if (!ativo && usuario.EstaAtivo())
        {
            // Regra do Administrador Sênior (D1, ADR-0021), reaproveitando a invariante já criada no
            // Bootstrap (O1.4.3.2): a Unidade de Negócio nunca pode ficar sem nenhum Administrador Sênior
            // ativo. Conta os vínculos ativos EXCLUINDO o próprio usuário sob inativação — é exatamente o
            // estado que resultaria da operação.
            var restantes = await usuarios.ContarAdministradoresSeniorAtivosAsync(unidadeNegocioId, usuario.Id, ct);
            var perfisDoUsuario = await usuarios.ObterPerfisPorUsuarioAsync([usuario.Id], ct);
            var eraAdministradorSenior = perfisDoUsuario.TryGetValue(usuario.Id, out var vinculos)
                && vinculos.Any(p => string.Equals(p.Nome, Perfil.AdministradorSenior, StringComparison.Ordinal) && p.Ativo);

            if (eraAdministradorSenior)
            {
                try
                {
                    AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo(restantes);
                }
                catch (UltimoAdministradorSeniorAtivoException ex)
                {
                    return RbacResultado<UsuarioDto>.Erro(RbacFalha.UltimoAdministradorSeniorAtivo, ex.Message);
                }
            }
        }

        var agora = clock.GetUtcNow();
        if (ativo) usuario.Ativar(agora); else usuario.Inativar(agora);
        await usuarios.SalvarAlteracoesAsync(ct);

        return RbacResultado<UsuarioDto>.Ok(await UsuarioProjection.ProjetarUmAsync(usuarios, usuario, ct));
    }
}
