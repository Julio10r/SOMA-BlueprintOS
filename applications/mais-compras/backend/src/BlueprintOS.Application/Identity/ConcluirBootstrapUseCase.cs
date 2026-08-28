using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Identity.Security;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

/// <summary>Conclusão transacional do Bootstrap e estabelecimento do primeiro Administrador Sênior (O1.4.3.2;
/// security-design-auth-o1.4.md §20.9/§20.15; Work Order O1.4.3, seção 13). Todas as escritas (Usuario,
/// UnidadeNegocio, Perfil, UsuarioPerfil, BootstrapEstado) são rastreadas no mesmo <c>DbContext</c>
/// compartilhado pelos repositórios injetados e persistidas em uma única chamada a <c>SaveChangesAsync</c>
/// (via <see cref="IBootstrapEstadoRepository.SalvarAlteracoesAsync"/>) — a transação implícita mais simples
/// descrita na Work Order, suficiente porque nenhuma etapa intermediária depende de uma leitura pós-escrita
/// de outra etapa dentro da mesma execução. Se qualquer etapa lançar (violação de índice único sob corrida,
/// perda do compare-and-swap de <see cref="BootstrapEstado.RowVersion"/>), nenhuma escrita é persistida.</summary>
public sealed class ConcluirBootstrapUseCase(
    IBootstrapEstadoRepository estados,
    IBootstrapSessaoRepository bootstrapSessoes,
    IUnidadeNegocioRepository unidadesNegocio,
    IUsuarioRepository usuarios,
    IPerfilRepository perfis,
    IPermissaoRepository permissoes,
    IUsuarioPerfilRepository usuariosPerfis,
    CatalogoInicialPerfisDeNegocioUseCase catalogoInicialPerfis,
    TimeProvider clock,
    ILogger<ConcluirBootstrapUseCase> logger) : IConcluirBootstrapUseCase
{
    private const string MotivoGenericoIndisponivel = "Bootstrap indisponível.";
    private const string MotivoGenericoPayloadInvalido = "Dados informados são inválidos.";

    public async Task<ConcluirBootstrapResultado> ExecuteAsync(
        Guid bootstrapSessaoId,
        UnidadeNegocioBootstrapPayload unidadeNegocioPayload,
        AdministradorSeniorBootstrapPayload administradorPayload,
        CancellationToken ct)
    {
        var agora = clock.GetUtcNow();

        // Passo 1 (seção 13) — última barreira antes da escrita, mesmo já checada pela política
        // BootstrapAuthenticated. Linha ausente é tratada com a mesma severidade de "concluído".
        var estado = await estados.ObterAsync(ct);
        if (estado is null || estado.Concluido)
        {
            logger.LogInformation("Tentativa de reabertura pós-conclusão do Bootstrap rejeitada.");
            return Falha(MotivoGenericoIndisponivel);
        }

        // A sessão já foi autenticada pela política BootstrapAuthenticated; revalidada aqui apenas para
        // obter o e-mail candidato com segurança — nunca aceito do payload (seção 13, passo 3).
        var sessao = await bootstrapSessoes.ObterPorIdAsync(bootstrapSessaoId, ct);
        if (sessao is null || !sessao.EstaValidaEm(agora))
        {
            logger.LogInformation("Conclusão do Bootstrap rejeitada — sessão de Bootstrap inválida.");
            return Falha(MotivoGenericoIndisponivel);
        }

        var nomeAdministrador = administradorPayload.Nome?.Trim();
        if (string.IsNullOrWhiteSpace(nomeAdministrador))
        {
            return Falha(MotivoGenericoPayloadInvalido);
        }

        UnidadeNegocio unidadeNegocio;
        try
        {
            unidadeNegocio = await ResolverUnidadeNegocioAsync(unidadeNegocioPayload, ct);
        }
        catch (ArgumentException)
        {
            return Falha(MotivoGenericoPayloadInvalido);
        }

        var unidadeNegocioEhNova = unidadeNegocioPayload.Id is null;

        if (!unidadeNegocioEhNova && await unidadesNegocio.PossuiAdministradorSeniorAtivoAsync(unidadeNegocio.Id, ct))
        {
            logger.LogInformation("Conclusão do Bootstrap rejeitada — Unidade de Negócio já possui Administrador Sênior ativo.");
            return Falha(MotivoGenericoPayloadInvalido);
        }

        if (unidadeNegocioEhNova)
        {
            await unidadesNegocio.AdicionarAsync(unidadeNegocio, ct);
        }

        // Passo 3 (seção 13) — e-mail sempre o já validado por OTP na BootstrapSessao, nunca o do payload.
        var usuario = new Usuario(sessao.EmailCandidato, nomeAdministrador, unidadeNegocio.Id);
        await usuarios.AdicionarAsync(usuario, ct);

        // Passo 4 (seção 13) — cria ou reaproveita o Perfil "Administrador Sênior" pelo índice único
        // (UnidadeNegocioId, Nome) fechado em O1.4.3.1.
        var perfil = await perfis.ObterPorNomeEUnidadeNegocioAsync(Perfil.AdministradorSenior, unidadeNegocio.Id, ct);
        if (perfil is null)
        {
            perfil = new Perfil(
                Perfil.AdministradorSenior,
                "Acesso administrativo integral do +Compras, criado pelo Bootstrap Mode (ADR-0020, item 12).",
                unidadeNegocio.Id,
                agora);
            await perfis.AdicionarAsync(perfil, ct);
        }

        // O1.5 (RBAC Real) — o Perfil "Administrador Sênior" recebe o catálogo completo de permissões.
        // Sem isto, a partir da O1.5 o primeiro administrador criado pelo Bootstrap teria zero permissões
        // efetivas e nenhum endpoint administrativo seria acessível a ninguém — o sistema nasceria
        // irrecuperável. Idempotente (`VincularPermissoesAsync` ignora vínculos já existentes), portanto
        // também completa um Perfil criado por um Bootstrap anterior à O1.5.
        var catalogoCompleto = await permissoes.ObterPorCodigosAsync(PermissaoCatalogo.Codigos, ct);
        await perfis.VincularPermissoesAsync(perfil.Id, catalogoCompleto.Select(x => x.Id).ToArray(), ct);

        // Passo 5 (seção 13).
        var vinculo = new UsuarioPerfil(usuario.Id, perfil.Id);
        await usuariosPerfis.AdicionarAsync(vinculo, ct);

        // Gate Final da Onda 1 (entregável #9) — catálogo inicial de Perfis de negócio (Administrador de
        // BU, Comprador, Aprovador, Requisitante) nasce junto com a própria Unidade de Negócio, na mesma
        // transação. Idempotente: reexecutar o Bootstrap sobre uma BU existente (ex.: corrida perdida)
        // nunca duplica Perfil.
        await catalogoInicialPerfis.GarantirCatalogoAsync(unidadeNegocio.Id, ct);

        // Passo 6 (seção 13) — invariante trivialmente satisfeita (primeira criação); método de domínio
        // reutilizável chamado por consistência de código, não uma cópia da regra (seção 14).
        AdministradorSeniorInvariantService.GarantirQueRestaAoMenosUmAdministradorSeniorAtivo(quantidadeAtivaAposOperacao: 1);

        // Passo 7 (seção 13) — compare-and-swap via RowVersion: se outra conclusão venceu a corrida entre a
        // leitura acima e este SaveChangesAsync, a exceção de concorrência é lançada abaixo e nenhuma
        // escrita (Usuario/UnidadeNegocio/Perfil/UsuarioPerfil incluídos) é persistida.
        estado.Concluir(usuario.Id, agora);
        await estados.AtualizarAsync(estado, ct);

        try
        {
            // Passo 9 (seção 13) — única chamada a SaveChangesAsync para todas as entidades da transação.
            await estados.SalvarAlteracoesAsync(ct);
        }
        catch (ConcurrencyConflictException)
        {
            logger.LogInformation("Conclusão do Bootstrap perdeu corrida de concorrência — outra conclusão já efetivada.");
            return Falha(MotivoGenericoIndisponivel);
        }
        catch (DuplicateRecordException)
        {
            // Corrida sobre o índice único de Perfil (UnidadeNegocioId, Nome) — outra conclusão concorrente
            // criou o mesmo Perfil "Administrador Sênior" entre a leitura e esta escrita.
            logger.LogInformation("Conclusão do Bootstrap perdeu corrida de concorrência sobre o Perfil Administrador Sênior.");
            return Falha(MotivoGenericoIndisponivel);
        }

        // Passo 10 (seção 13) — invalidação de uso único, sempre após sucesso.
        sessao.MarcarUsada(agora);
        await bootstrapSessoes.AtualizarAsync(sessao, ct);
        await bootstrapSessoes.SalvarAlteracoesAsync(ct);

        logger.LogInformation(
            "Bootstrap concluído. UsuarioId={UsuarioId} UnidadeNegocioId={UnidadeNegocioId}",
            usuario.Id, unidadeNegocio.Id);

        return new ConcluirBootstrapResultado(true, null, usuario.Id, usuario.Email, usuario.Nome, unidadeNegocio.Id);
    }

    private async Task<UnidadeNegocio> ResolverUnidadeNegocioAsync(UnidadeNegocioBootstrapPayload payload, CancellationToken ct)
    {
        if (payload.Id is { } id)
        {
            var existente = await unidadesNegocio.ObterPorIdAsync(id, ct);
            if (existente is null)
            {
                throw new ArgumentException("Unidade de Negócio informada não existe.", nameof(payload));
            }

            return existente;
        }

        var nome = payload.Nome?.Trim();
        var slug = payload.Slug?.Trim();
        if (string.IsNullOrWhiteSpace(nome) || string.IsNullOrWhiteSpace(slug))
        {
            throw new ArgumentException("Nome e slug da Unidade de Negócio são obrigatórios para criação.", nameof(payload));
        }

        return new UnidadeNegocio(nome, slug);
    }

    private static ConcluirBootstrapResultado Falha(string motivo) =>
        new(false, motivo, null, null, null, null);
}
