using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Domain.Identity;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Identity;

/// <summary>Garante, de forma idempotente, o catálogo inicial de Perfis de negócio (entregável #9 — Gate
/// Final da Onda 1) em uma Unidade de Negócio: <see cref="Perfil.AdministradorDeBu"/>,
/// <see cref="Perfil.Comprador"/>, <see cref="Perfil.Aprovador"/> e <see cref="Perfil.Requisitante"/>.
///
/// Deliberadamente NÃO cria <see cref="Perfil.AdministradorSenior"/> — esse Perfil tem semântica
/// corporativa própria e seu ciclo de vida pertence exclusivamente ao Bootstrap
/// (<c>ConcluirBootstrapUseCase</c>), nunca a este catálogo de negócio.
///
/// Chamado em dois pontos: (1) ao concluir o Bootstrap, para a Unidade de Negócio recém-criada; (2) ao
/// criar uma nova Unidade de Negócio via <c>CriarUnidadeNegocioUseCase</c>. Multi-BU consciente (todo
/// Perfil é escopado por <c>UnidadeNegocioId</c>, nunca global) e auditável (mesma trilha de
/// <c>ILogger</c> já usada pelos demais casos de uso administrativos).
///
/// Idempotência: reaproveita pelo índice único (<c>UnidadeNegocioId</c>, <c>Nome</c>) — mesmo padrão de
/// <c>ConcluirBootstrapUseCase</c> para o Administrador Sênior. Executar mais de uma vez para a mesma BU
/// nunca duplica Perfil nem remove um vínculo de permissão já concedido manualmente.</summary>
public sealed class CatalogoInicialPerfisDeNegocioUseCase(
    IPerfilRepository perfis,
    IPermissaoRepository permissoes,
    TimeProvider clock,
    ILogger<CatalogoInicialPerfisDeNegocioUseCase> logger)
{
    /// <summary>Matriz Perfil × Permissão do catálogo inicial (Gate Final da Onda 1, §16). Somente
    /// permissões NEGÓCIO (nunca <c>UnidadeNegocio.Gerenciar</c>, <c>ConfiguracaoErp.Gerenciar</c> ou
    /// <c>Sistema.Gerenciar</c> — essas são PRODUTO e ficam reservadas ao Administrador Sênior; conceder
    /// qualquer uma delas aqui contrariaria a invariante de não-escalonamento e a decisão do PO).
    /// Comprador/Aprovador usam somente permissões de Fornecedor já implementadas — <c>Pedido.*</c> não é
    /// atribuído a nenhum Perfil ainda: existe no catálogo (pré-provisionado, GAP-01) mas nenhum endpoint o
    /// exige, então atribuí-lo hoje não teria efeito de enforcement e anteciparia um módulo futuro.</summary>
    private static readonly IReadOnlyDictionary<string, string[]> MatrizPermissoes = new Dictionary<string, string[]>
    {
        [Perfil.AdministradorDeBu] =
        [
            PermissaoCatalogo.UsuarioGerenciar,
            PermissaoCatalogo.PerfilGerenciar,
            PermissaoCatalogo.FilialGerenciar,
            PermissaoCatalogo.CentroCustoGerenciar,
            PermissaoCatalogo.UnidadeAlocacaoGerenciar,
            PermissaoCatalogo.WorkflowGerenciar,
            PermissaoCatalogo.AlcadaGerenciar,
            PermissaoCatalogo.OrcamentoGerenciar,
        ],
        [Perfil.Comprador] = [PermissaoCatalogo.FornecedorCriar, PermissaoCatalogo.FornecedorEditar],
        [Perfil.Aprovador] = [PermissaoCatalogo.FornecedorAprovar],
        [Perfil.Requisitante] = [],
    };

    /// <summary>Apenas rastreia as escritas (mesmo <c>DbContext</c> compartilhado do chamador) — NÃO
    /// chama <c>SalvarAlteracoesAsync</c>. O chamador decide quando persistir, para poder compor esta
    /// operação dentro da própria transação (ex.: <c>ConcluirBootstrapUseCase</c>,
    /// <c>CriarUnidadeNegocioUseCase</c>) sem um <c>SaveChanges</c> intermediário.</summary>
    public async Task GarantirCatalogoAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var agora = clock.GetUtcNow();

        foreach (var (nomePerfil, codigosPermissao) in MatrizPermissoes)
        {
            var existente = await perfis.ObterPorNomeEUnidadeNegocioAsync(nomePerfil, unidadeNegocioId, ct);
            Perfil perfil;
            if (existente is null)
            {
                perfil = new Perfil(nomePerfil, DescricaoPadrao(nomePerfil), unidadeNegocioId, agora);
                await perfis.AdicionarAsync(perfil, ct);
                logger.LogInformation(
                    "Catálogo inicial de Perfis: '{Perfil}' criado. UnidadeNegocioId={UnidadeNegocioId}", nomePerfil, unidadeNegocioId);
            }
            else
            {
                perfil = existente;
            }

            if (codigosPermissao.Length == 0) continue;

            var permissoesResolvidas = await permissoes.ObterPorCodigosAsync(codigosPermissao, ct);
            await perfis.VincularPermissoesAsync(perfil.Id, permissoesResolvidas.Select(p => p.Id).ToArray(), ct);
        }
    }

    private static string DescricaoPadrao(string nomePerfil) => nomePerfil switch
    {
        Perfil.AdministradorDeBu => "Administração de negócio da própria Unidade de Negócio: usuários, perfis, estruturas administrativas e cadastros administrativos.",
        Perfil.Comprador => "Operação de compras conforme permissões atribuídas.",
        Perfil.Aprovador => "Aprovações conforme permissões e alçadas configuradas.",
        Perfil.Requisitante => "Requisições e acompanhamento das próprias operações, conforme funcionalidades disponíveis.",
        _ => nomePerfil,
    };
}
