using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Knowledge.Linx.Contracts;
using BlueprintOS.Application.Knowledge.Linx.Models;
using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.Application.Knowledge.Linx;

public sealed class RegistrarConhecimentoUseCase(ILinxKnowledgeRepository repositorio, TimeProvider clock) : IRegistrarConhecimentoUseCase
{
    public async Task<RbacResultado<LinxKnowledgeDto>> ExecuteAsync(RegistrarConhecimentoInput input, string ator, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Assunto))
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.AssuntoObrigatorio, "Assunto é obrigatório.");
        if (string.IsNullOrWhiteSpace(input.Conteudo))
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.ConteudoObrigatorio, "Conteúdo é obrigatório.");
        if (string.IsNullOrWhiteSpace(input.Fonte))
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.FonteObrigatoria, "Fonte é obrigatória.");
        if (input.Proveniencia == LinxConhecimentoProveniencia.Aprovado)
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.TransicaoProvenienciaInvalida, "Uma entrada nunca nasce Aprovada.");

        var agora = clock.GetUtcNow();

        if (input.VersaoRaizId is { } raiz)
        {
            var atual = await repositorio.ObterUltimaVersaoAsync(raiz, ct);
            if (atual is null)
                return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.ConhecimentoLinxNaoEncontrado, "Entrada de conhecimento não encontrada.");

            // Work Order, seção 12: se a versão atual já é Validada/Aprovada e o novo conteúdo diverge,
            // o conflito é registrado explicitamente — nunca substituído silenciosamente.
            var contradiz = atual.Proveniencia is LinxConhecimentoProveniencia.Validado or LinxConhecimentoProveniencia.Aprovado
                && !string.Equals(atual.Conteudo, input.Conteudo.Trim(), StringComparison.Ordinal);
            if (contradiz)
            {
                return RbacResultado<LinxKnowledgeDto>.Erro(
                    RbacFalha.ConflitoDeConhecimentoDetectado,
                    $"O novo conteúdo diverge da versão {atual.Versao} já '{atual.Proveniencia}'. Registre o conflito para tratamento/validação explícita antes de criar uma nova versão.");
            }

            var novaVersao = atual.NovaVersao(input.Conteudo, input.Proveniencia, input.Fonte, ator, input.Tags, agora);
            await repositorio.AdicionarAsync(novaVersao, ct);
            return RbacResultado<LinxKnowledgeDto>.Ok(Projetar(novaVersao));
        }

        var entrada = LinxKnowledgeEntry.Criar(
            input.Especialista, input.Categoria, input.Assunto, input.Conteudo, input.Proveniencia,
            input.Fonte, ator, input.UnidadeNegocioId, input.Tags, agora);

        await repositorio.AdicionarAsync(entrada, ct);
        return RbacResultado<LinxKnowledgeDto>.Ok(Projetar(entrada));
    }

    internal static LinxKnowledgeDto Projetar(LinxKnowledgeEntry e) => new(
        e.Id, e.VersaoRaizId, e.EntradaAnteriorId, e.Versao, e.Especialista, e.Categoria, e.Assunto,
        e.Conteudo, e.Proveniencia, e.Fonte, e.Ator, e.UnidadeNegocioId, e.Tags, e.CriadoEm, e.AtualizadoEm);
}

public sealed class PromoverConhecimentoUseCase(ILinxKnowledgeRepository repositorio, TimeProvider clock) : IPromoverConhecimentoUseCase
{
    public async Task<RbacResultado<LinxKnowledgeDto>> ExecuteAsync(Guid id, LinxConhecimentoProveniencia novaProveniencia, string ator, CancellationToken ct)
    {
        var entrada = await repositorio.ObterPorIdAsync(id, ct);
        if (entrada is null)
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.ConhecimentoLinxNaoEncontrado, "Entrada de conhecimento não encontrada.");

        try
        {
            entrada.Promover(novaProveniencia, ator, clock.GetUtcNow());
        }
        catch (InvalidOperationException ex)
        {
            return RbacResultado<LinxKnowledgeDto>.Erro(RbacFalha.TransicaoProvenienciaInvalida, ex.Message);
        }

        await repositorio.AtualizarProvenienciaAsync(entrada, ct);
        return RbacResultado<LinxKnowledgeDto>.Ok(RegistrarConhecimentoUseCase.Projetar(entrada));
    }
}

public sealed class BuscarConhecimentoUseCase(ILinxKnowledgeRepository repositorio) : IBuscarConhecimentoUseCase
{
    public async Task<IReadOnlyList<LinxKnowledgeDto>> ExecuteAsync(LinxKnowledgeFiltro filtro, CancellationToken ct)
    {
        var resultados = await repositorio.BuscarUltimasVersoesAsync(filtro, ct);
        return resultados.Select(RegistrarConhecimentoUseCase.Projetar).ToArray();
    }
}

public sealed class ObterHistoricoConhecimentoUseCase(ILinxKnowledgeRepository repositorio) : IObterHistoricoConhecimentoUseCase
{
    public async Task<RbacResultado<IReadOnlyList<LinxKnowledgeDto>>> ExecuteAsync(Guid versaoRaizId, CancellationToken ct)
    {
        var historico = await repositorio.ObterHistoricoAsync(versaoRaizId, ct);
        if (historico.Count == 0)
            return RbacResultado<IReadOnlyList<LinxKnowledgeDto>>.Erro(RbacFalha.ConhecimentoLinxNaoEncontrado, "Entrada de conhecimento não encontrada.");

        return RbacResultado<IReadOnlyList<LinxKnowledgeDto>>.Ok(historico.Select(RegistrarConhecimentoUseCase.Projetar).ToArray());
    }
}
