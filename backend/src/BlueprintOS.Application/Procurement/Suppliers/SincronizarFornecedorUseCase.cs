using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Application.Procurement.Suppliers;

public sealed class SincronizarFornecedorUseCase(
    IFornecedorRepository fornecedorRepository,
    IFornecedorSincronizacaoRepository sincronizacaoRepository,
    IErpFornecedorAdapterResolver adapterResolver,
    ICurrentIdentity identity) : ISincronizarFornecedorUseCase
{
    public async Task<SincronizacaoFornecedorResultado> ExecuteAsync(SincronizarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        Validar(dto);
        var userId = identity.GetRequired().UserId;
        var correlationId = SanitizarCorrelationId(dto.CorrelationId);
        var adapter = adapterResolver.Resolver(dto.BusinessUnit, dto.ErpSistema);
        var quando = DateTimeOffset.UtcNow;
        Fornecedor? local = dto.FornecedorId is { } id ? await fornecedorRepository.ObterPorIdAsync(id, userId, cancellationToken) : null;
        string? erpId = dto.ErpFornecedorId;
        try
        {
            if (dto.Direcao == DirecaoSincronizacao.ErpParaMaisCompras)
            {
                var externo = await adapter.ObterAsync(dto.ErpFornecedorId!, cancellationToken);
                if (externo is null) return await RegistrarFalhaAsync(dto, local, erpId, correlationId, "Fornecedor não encontrado no ERP.", quando, cancellationToken);
                local ??= await sincronizacaoRepository.ObterPorChaveErpAsync(dto.BusinessUnit, dto.ErpSistema, externo.Id, userId, cancellationToken);
                if (local is null)
                {
                    local = new Fornecedor(Guid.NewGuid(), externo.Nome, Cnpj.Create(externo.Cnpj ?? "00000000000000"), null, null, null, null,
                        externo.Cidade, externo.Estado, externo.Pais, "Ativo", null, userId, quando, dto.BusinessUnit, dto.ErpSistema, externo.Id);
                    await fornecedorRepository.AdicionarAsync(local, cancellationToken);
                }
                else
                {
                    local.AplicarDadosCorporativos(externo.Nome, externo.Cnpj, externo.Cidade, externo.Estado, externo.Pais,
                        dto.BusinessUnit, dto.ErpSistema, externo.Id, quando);
                    await fornecedorRepository.AtualizarAsync(local, cancellationToken);
                }
                erpId = externo.Id; local.RegistrarSincronizacao("Sincronizado", quando); await fornecedorRepository.AtualizarAsync(local, cancellationToken);
            }
            else
            {
                if (local is null) return await RegistrarFalhaAsync(dto, null, erpId, correlationId, "Fornecedor não encontrado no +Compras.", quando, cancellationToken);
                var payload = new ErpFornecedorParaEscrita(local.ErpFornecedorId ?? string.Empty, local.Nome, local.Cnpj, local.Cidade, local.Estado, local.Pais);
                var externo = string.IsNullOrWhiteSpace(local.ErpFornecedorId)
                    ? await adapter.CriarAsync(payload with { Id = Guid.NewGuid().ToString("N") }, cancellationToken)
                    : await adapter.AtualizarAsync(payload, cancellationToken);
                erpId = externo.Id; local.AplicarDadosCorporativos(externo.Nome, externo.Cnpj, externo.Cidade, externo.Estado, externo.Pais,
                    dto.BusinessUnit, dto.ErpSistema, externo.Id, quando);
                local.RegistrarSincronizacao("Sincronizado", quando); await fornecedorRepository.AtualizarAsync(local, cancellationToken);
            }
            var resultado = new SincronizacaoFornecedorResultado(local.Id, dto.BusinessUnit, dto.ErpSistema, erpId, "Sincronizado", correlationId, quando, null);
            await sincronizacaoRepository.AdicionarAsync(new(Guid.NewGuid(), dto.BusinessUnit, dto.ErpSistema, erpId!, local.Id, dto.Direcao.ToString(), resultado.Status, correlationId, quando, null), cancellationToken);
            return resultado;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            var mensagem = SanitizarErro(ex);
            return await RegistrarFalhaAsync(dto, local, erpId, correlationId, mensagem, quando, cancellationToken);
        }
    }

    public async Task<IReadOnlyList<SincronizacaoFornecedorResultado>> ExecutarLoteAsync(SincronizarFornecedoresLoteDto dto, CancellationToken cancellationToken = default)
    {
        var limite = Math.Clamp(dto.Limite <= 0 ? 50 : dto.Limite, 1, 100);
        var resultados = new List<SincronizacaoFornecedorResultado>();
        foreach (var id in dto.FornecedorIds.Take(limite))
        {
            resultados.Add(await ExecuteAsync(new(dto.BusinessUnit, dto.ErpSistema, null, id, DirecaoSincronizacao.MaisComprasParaErp, dto.CorrelationId), cancellationToken));
        }
        return resultados;
    }

    private async Task<SincronizacaoFornecedorResultado> RegistrarFalhaAsync(SincronizarFornecedorDto dto, Fornecedor? local, string? erpId,
        string correlationId, string mensagem, DateTimeOffset quando, CancellationToken ct)
    {
        local?.RegistrarSincronizacao("Falhou", quando, mensagem); if (local is not null) await fornecedorRepository.AtualizarAsync(local, ct);
        if (!string.IsNullOrWhiteSpace(erpId)) await sincronizacaoRepository.AdicionarAsync(new(Guid.NewGuid(), dto.BusinessUnit, dto.ErpSistema, erpId, local?.Id, dto.Direcao.ToString(), "Falhou", correlationId, quando, mensagem), ct);
        return new(local?.Id, dto.BusinessUnit, dto.ErpSistema, erpId, "Falhou", correlationId, quando, mensagem);
    }

    private static void Validar(SincronizarFornecedorDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.BusinessUnit)) throw new ArgumentException("BU é obrigatória.");
        if (string.IsNullOrWhiteSpace(dto.ErpSistema)) throw new ArgumentException("ERP é obrigatório.");
        if (dto.Direcao == DirecaoSincronizacao.ErpParaMaisCompras && string.IsNullOrWhiteSpace(dto.ErpFornecedorId)) throw new ArgumentException("Identificador ERP é obrigatório.");
        if (dto.Direcao == DirecaoSincronizacao.MaisComprasParaErp && dto.FornecedorId is null) throw new ArgumentException("Fornecedor do +Compras é obrigatório.");
    }
    private static string SanitizarCorrelationId(string? value) => string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim()[..Math.Min(value.Trim().Length, 100)];
    private static string SanitizarErro(Exception ex) => ex is TimeoutException ? "Tempo limite excedido ao comunicar com o ERP." : "Falha ao comunicar com o ERP.";
}
