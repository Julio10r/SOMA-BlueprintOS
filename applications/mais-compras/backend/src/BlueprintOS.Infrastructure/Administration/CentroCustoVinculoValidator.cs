using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Domain.Identity;
using BlueprintOS.Infrastructure.Integrations.ERP.Contracts;

namespace BlueprintOS.Infrastructure.Administration;

/// <summary>Implementação real de <see cref="ICentroCustoVinculoValidator"/> (resolução da dívida O1.6-L2).
///
/// Para cada código ERP informado no vínculo Usuário×Centro de Custo:
/// 1. Se já existe um <see cref="CentroCustoMetadado"/> com esse código (em QUALQUER Unidade de Negócio —
///    <c>ObterPorCodigoErpGlobalAsync</c>), o vínculo só é aceito se o metadado pertencer à MESMA Unidade de
///    Negócio da sessão. Pertencer a outra Unidade de Negócio é rejeitado — é exatamente o vetor de vínculo
///    cross-BU que esta sprint fecha.
/// 2. Se não existe metadado ainda, o código é validado contra o ERP real
///    (<see cref="ICentroCustoErpReader.BuscarPorCodigoAsync"/>). Inexistente no ERP → rejeitado. Existente
///    → um <see cref="CentroCustoMetadado"/> é criado "sob demanda", ancorado à Unidade de Negócio da
///    sessão (Ativo por padrão) — é isso que faz o próximo vínculo ao mesmo código, mesmo que de outra
///    sessão da mesma Unidade de Negócio, resolver em memória sem nova consulta ao ERP, e é isso que impede
///    a mesma tentativa futura de outra Unidade de Negócio (regra 1).
///
/// Decisão explícita (ver relatório final da O1.7): validação em tempo de execução no caso de uso, em vez de
/// FK física em `UsuariosCentrosCusto` — a FK física exigiria que o metadado já existisse ANTES do primeiro
/// vínculo, o que não é garantido (a tela de Centros de Custo só cria metadado na primeira edição/ativação
/// manual). Criar o metadado "sob demanda" aqui, no momento do vínculo, é a forma mais simples de garantir a
/// integridade sem introduzir uma ordem de operações artificial para o usuário final.
///
/// DEB-15/M2 (Gate Final pós-O1.14): este método NUNCA chama <c>SalvarAlteracoesAsync</c> — apenas rastreia
/// o novo <see cref="CentroCustoMetadado"/> via <c>AdicionarAsync</c> no mesmo <c>DbContext</c> compartilhado
/// pelo repositório de Usuário injetado no caso de uso chamador (<c>CriarUsuarioUseCase</c>/
/// <c>AtualizarUsuarioUseCase</c>). Antes desta correção, a ancoragem do metadado era persistida aqui, em uma
/// chamada a <c>SaveChangesAsync</c> separada da que grava o Usuário e o vínculo Usuário×Centro de Custo: se
/// a segunda chamada falhasse (ex.: corrida no índice único de e-mail, violação de concorrência nos Perfis),
/// o metadado já estaria commitado no banco — ancorando permanentemente aquele código ERP a esta Unidade de
/// Negócio (índice único global de <c>CodigoErp</c>) sem que o vínculo Usuário×Centro de Custo que motivou a
/// ancoragem jamais tivesse sido criado. Persistir tudo em uma única chamada (mesmo padrão de
/// <c>ConcluirBootstrapUseCase</c>) garante que, se qualquer etapa falhar, nenhuma escrita — incluindo a
/// ancoragem do metadado — é persistida.</summary>
public sealed class CentroCustoVinculoValidator(
    ICentroCustoErpReader reader,
    ICentroCustoMetadadoRepository metadados,
    TimeProvider clock) : ICentroCustoVinculoValidator
{
    public async Task<RbacResultado<IReadOnlyList<string>>> ValidarEAncorarAsync(
        IReadOnlyList<string>? codigosErp, Guid unidadeNegocioId, CancellationToken ct)
    {
        var normalizados = (codigosErp ?? [])
            .Where(x => !string.IsNullOrWhiteSpace(x))
            .Select(x => x.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        if (normalizados.Length == 0)
        {
            return RbacResultado<IReadOnlyList<string>>.Ok(Array.Empty<string>());
        }

        var agora = clock.GetUtcNow();

        foreach (var codigo in normalizados)
        {
            var ancoraExistente = await metadados.ObterPorCodigoErpGlobalAsync(codigo, ct);
            if (ancoraExistente is not null)
            {
                if (ancoraExistente.UnidadeNegocioId != unidadeNegocioId)
                {
                    return RbacResultado<IReadOnlyList<string>>.Erro(
                        RbacFalha.CentroCustoInvalido,
                        $"O Centro de Custo '{codigo}' pertence a outra Unidade de Negócio e não pode ser vinculado.");
                }

                continue;
            }

            var doErp = await reader.BuscarPorCodigoAsync(codigo, ct);
            if (doErp is null)
            {
                return RbacResultado<IReadOnlyList<string>>.Erro(
                    RbacFalha.CentroCustoInvalido,
                    $"O Centro de Custo '{codigo}' não existe no ERP.");
            }

            // Apenas rastreado no DbContext compartilhado — persistido junto com Usuario/vínculos pelo
            // SalvarAlteracoesAsync único do caso de uso chamador (ver nota de classe acima, DEB-15/M2).
            await metadados.AdicionarAsync(new CentroCustoMetadado(codigo, unidadeNegocioId, agora), ct);
        }

        return RbacResultado<IReadOnlyList<string>>.Ok(normalizados);
    }
}
