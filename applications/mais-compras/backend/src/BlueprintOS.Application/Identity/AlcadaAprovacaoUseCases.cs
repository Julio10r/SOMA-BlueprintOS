using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity;

internal static class AlcadaAprovacaoProjection
{
    public static AlcadaAprovacaoDto Projetar(AlcadaAprovacao alcada) => new(
        alcada.Id, alcada.Nome, alcada.UnidadeNegocioId, alcada.Criterio, alcada.ValorMinimo, alcada.ValorMaximo,
        alcada.CentroCustoMetadadoId, alcada.Nivel, alcada.AprovadorUsuarioId, alcada.AprovadorPerfilId,
        alcada.Ativo, alcada.CriadoEm, alcada.AtualizadoEm);
}

/// <summary>O1.12 — Fundação de Administração de Alçadas de Aprovação. CRUD administrativo por Unidade de
/// Negócio, sem exclusão física. Nenhum motor de avaliação/execução de aprovação é acionado aqui — apenas
/// validação estrutural do cadastro (nível, faixa de valor, exatamente um aprovador, FKs pertencentes à
/// mesma Unidade de Negócio).</summary>
public sealed class ListarAlcadasAprovacaoUseCase(IAlcadaAprovacaoRepository alcadas) : IListarAlcadasAprovacaoUseCase
{
    public async Task<IReadOnlyList<AlcadaAprovacaoDto>> ExecuteAsync(Guid unidadeNegocioId, CancellationToken ct)
    {
        var encontradas = await alcadas.ListarPorUnidadeNegocioAsync(unidadeNegocioId, ct);
        return encontradas.Select(AlcadaAprovacaoProjection.Projetar).ToArray();
    }
}

/// <summary>Validações compartilhadas entre criação e edição.</summary>
internal static class AlcadaAprovacaoValidacoes
{
    public static async Task<RbacFalha?> ValidarAsync(
        AlcadaAprovacaoInput input, Guid unidadeNegocioId,
        ICentroCustoMetadadoRepository centrosCusto, IUsuarioRepository usuarios, IPerfilRepository perfis, CancellationToken ct)
    {
        if (string.IsNullOrWhiteSpace(input.Nome)) return RbacFalha.NomeObrigatorio;
        if (input.Nivel < 1) return RbacFalha.NivelInvalido;

        if (input.ValorMinimo is not null && input.ValorMaximo is not null && input.ValorMinimo > input.ValorMaximo)
        {
            return RbacFalha.FaixaDeValorInvalida;
        }

        var possuiUsuario = input.AprovadorUsuarioId is not null && input.AprovadorUsuarioId != Guid.Empty;
        var possuiPerfil = input.AprovadorPerfilId is not null && input.AprovadorPerfilId != Guid.Empty;
        if (possuiUsuario == possuiPerfil) return RbacFalha.AprovadorInvalido;

        if (possuiUsuario)
        {
            var usuario = await usuarios.ObterPorIdEUnidadeNegocioAsync(input.AprovadorUsuarioId!.Value, unidadeNegocioId, ct);
            if (usuario is null) return RbacFalha.AprovadorInvalido;
        }
        else
        {
            var perfil = await perfis.ObterPorIdEUnidadeNegocioAsync(input.AprovadorPerfilId!.Value, unidadeNegocioId, ct);
            if (perfil is null) return RbacFalha.AprovadorInvalido;
        }

        if (input.Criterio == CriterioAlcada.CentroCusto)
        {
            if (input.CentroCustoMetadadoId is null || input.CentroCustoMetadadoId == Guid.Empty)
            {
                return RbacFalha.CentroCustoObrigatorio;
            }

            var centroCusto = await centrosCusto.ObterPorIdEUnidadeNegocioAsync(input.CentroCustoMetadadoId.Value, unidadeNegocioId, ct);
            if (centroCusto is null) return RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio;
        }

        return null;
    }

    public static string MensagemDe(RbacFalha falha) => falha switch
    {
        RbacFalha.NomeObrigatorio => "Nome da Alçada de Aprovação é obrigatório.",
        RbacFalha.NivelInvalido => "Nível deve ser maior ou igual a 1.",
        RbacFalha.FaixaDeValorInvalida => "Valor mínimo não pode ser maior que o valor máximo.",
        RbacFalha.AprovadorInvalido => "Exatamente um aprovador deve ser informado: um Usuário OU um Perfil, nunca os dois nem nenhum, e deve pertencer a esta Unidade de Negócio.",
        RbacFalha.CentroCustoObrigatorio => "Centro de Custo é obrigatório quando o critério é Centro de Custo.",
        RbacFalha.CentroCustoInvalidoNaUnidadeDeNegocio => "Centro de Custo informado não pertence a esta Unidade de Negócio.",
        _ => "Requisição inválida.",
    };
}

public sealed class CriarAlcadaAprovacaoUseCase(
    IAlcadaAprovacaoRepository alcadas, IUnidadeNegocioRepository unidadesNegocio, ICentroCustoMetadadoRepository centrosCusto,
    IUsuarioRepository usuarios, IPerfilRepository perfis, TimeProvider clock) : ICriarAlcadaAprovacaoUseCase
{
    public async Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(AlcadaAprovacaoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        if (await unidadesNegocio.ObterPorIdAsync(unidadeNegocioId, ct) is null)
        {
            return RbacResultado<AlcadaAprovacaoDto>.Erro(RbacFalha.UnidadeNegocioNaoEncontrada, "Unidade de Negócio não encontrada.");
        }

        var falha = await AlcadaAprovacaoValidacoes.ValidarAsync(input, unidadeNegocioId, centrosCusto, usuarios, perfis, ct);
        if (falha is not null)
        {
            return RbacResultado<AlcadaAprovacaoDto>.Erro(falha.Value, AlcadaAprovacaoValidacoes.MensagemDe(falha.Value));
        }

        var agora = clock.GetUtcNow();
        var alcada = new AlcadaAprovacao(
            input.Nome.Trim(), unidadeNegocioId, input.Criterio, input.ValorMinimo, input.ValorMaximo,
            input.CentroCustoMetadadoId, input.Nivel, input.AprovadorUsuarioId, input.AprovadorPerfilId, agora);

        await alcadas.AdicionarAsync(alcada, ct);
        await alcadas.SalvarAlteracoesAsync(ct);

        return RbacResultado<AlcadaAprovacaoDto>.Ok(AlcadaAprovacaoProjection.Projetar(alcada));
    }
}

public sealed class AtualizarAlcadaAprovacaoUseCase(
    IAlcadaAprovacaoRepository alcadas, ICentroCustoMetadadoRepository centrosCusto,
    IUsuarioRepository usuarios, IPerfilRepository perfis, TimeProvider clock) : IAtualizarAlcadaAprovacaoUseCase
{
    public async Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(Guid id, AlcadaAprovacaoInput input, Guid unidadeNegocioId, CancellationToken ct)
    {
        var alcada = await alcadas.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (alcada is null)
        {
            return RbacResultado<AlcadaAprovacaoDto>.Erro(RbacFalha.AlcadaAprovacaoNaoEncontrada, "Alçada de Aprovação não encontrada.");
        }

        var falha = await AlcadaAprovacaoValidacoes.ValidarAsync(input, unidadeNegocioId, centrosCusto, usuarios, perfis, ct);
        if (falha is not null)
        {
            return RbacResultado<AlcadaAprovacaoDto>.Erro(falha.Value, AlcadaAprovacaoValidacoes.MensagemDe(falha.Value));
        }

        var agora = clock.GetUtcNow();
        alcada.Editar(
            input.Nome.Trim(), input.Criterio, input.ValorMinimo, input.ValorMaximo,
            input.CentroCustoMetadadoId, input.Nivel, input.AprovadorUsuarioId, input.AprovadorPerfilId, agora);
        await alcadas.SalvarAlteracoesAsync(ct);

        return RbacResultado<AlcadaAprovacaoDto>.Ok(AlcadaAprovacaoProjection.Projetar(alcada));
    }
}

public sealed class AlterarStatusAlcadaAprovacaoUseCase(IAlcadaAprovacaoRepository alcadas, TimeProvider clock) : IAlterarStatusAlcadaAprovacaoUseCase
{
    public async Task<RbacResultado<AlcadaAprovacaoDto>> ExecuteAsync(Guid id, bool ativo, Guid unidadeNegocioId, CancellationToken ct)
    {
        var alcada = await alcadas.ObterPorIdEUnidadeNegocioAsync(id, unidadeNegocioId, ct);
        if (alcada is null)
        {
            return RbacResultado<AlcadaAprovacaoDto>.Erro(RbacFalha.AlcadaAprovacaoNaoEncontrada, "Alçada de Aprovação não encontrada.");
        }

        var agora = clock.GetUtcNow();
        if (ativo) alcada.Ativar(agora); else alcada.Inativar(agora);
        await alcadas.SalvarAlteracoesAsync(ct);

        return RbacResultado<AlcadaAprovacaoDto>.Ok(AlcadaAprovacaoProjection.Projetar(alcada));
    }
}
