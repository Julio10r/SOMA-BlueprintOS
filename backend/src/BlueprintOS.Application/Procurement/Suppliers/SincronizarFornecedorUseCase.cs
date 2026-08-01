using System.Diagnostics;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
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
    private static readonly TimeZoneInfo SaoPaulo = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "E. South America Standard Time" : "America/Sao_Paulo");

    public async Task<SincronizacaoFornecedorResultado> ExecuteAsync(SincronizarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        Validar(dto);
        var started = Stopwatch.GetTimestamp();
        var userId = identity.GetRequired().UserId;
        var correlationId = SanitizarCorrelationId(dto.CorrelationId);
        var adapter = adapterResolver.Resolver(dto.BusinessUnit, dto.ErpSistema);
        var local = dto.FornecedorId is { } id ? await fornecedorRepository.ObterPorIdAsync(id, userId, cancellationToken) : null;
        var snapshotAntes = local is null ? null : Snapshot(local);
        string? externalId = dto.ErpFornecedorId ?? local?.ErpFornecedorId;
        try
        {
            if (dto.Direcao == DirecaoSincronizacao.ErpParaMaisCompras)
                return await ImportarAsync(dto, adapter, local, externalId!, correlationId, started, userId, snapshotAntes, cancellationToken);
            return await ExportarAsync(dto, adapter, local, externalId, correlationId, started, snapshotAntes, cancellationToken);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested) { throw; }
        catch (Exception ex)
        {
            var mensagem = SanitizarErro(ex);
            if (local is not null) { local.RegistrarSincronizacao("Falhou", Now(), mensagem); await fornecedorRepository.AtualizarAsync(local, cancellationToken); }
            await AuditarAsync(dto, local, externalId, correlationId, "Falhou", "Falha", null, null, mensagem, started, snapshotAntes, cancellationToken);
            return new(local?.Id, dto.BusinessUnit, dto.ErpSistema, externalId, "Falhou", correlationId, Now(), mensagem);
        }
    }

    private async Task<SincronizacaoFornecedorResultado> ImportarAsync(SincronizarFornecedorDto dto, IErpFornecedorAdapter adapter,
        Fornecedor? local, string externalId, string correlationId, long started, Guid userId, string? snapshotAntes, CancellationToken ct)
    {
        var externo = await adapter.ObterAsync(externalId, ct) ?? throw new InvalidOperationException("Fornecedor não encontrado no ERP.");
        local ??= await sincronizacaoRepository.ObterPorChaveErpAsync(dto.BusinessUnit, dto.ErpSistema, externo.Id, userId, ct);
        var erpDados = Canonical(externo);
        if (local is null)
        {
            local = new Fornecedor(Guid.NewGuid(), erpDados.RazaoSocial, DocumentoFiscal.Create(erpDados.DocumentoFiscal), erpDados.TipoPessoa, null, null, null, null,
                externo.Cidade, externo.Estado, externo.Pais, externo.Ativo ? "Ativo" : "Inativo", null, userId, Now(),
                dto.BusinessUnit, dto.ErpSistema, externo.Id);
            local.AplicarContratoCanonico(erpDados, "ERP", Normalize(externo.UltimaAlteracaoEm ?? Now()));
            await fornecedorRepository.AdicionarAsync(local, ct);
            return await FinishAsync(dto, local, externo, correlationId, "Importado", "ERP", "+Compras", "ERP mais recente", started, snapshotAntes, ct);
        }

        // A importação também consolida o vínculo externo; isso permite reconciliar uma criação
        // confirmada no ERP quando a persistência local falhou após o commit remoto.
        local.RegistrarVinculoErp(dto.BusinessUnit, dto.ErpSistema, externo.Id);
        var localTime = Normalize(local.UpdatedAt); var erpTime = Normalize(externo.UltimaAlteracaoEm ?? localTime);
        var same = Same(local, erpDados, externo.DadosCanonicos is not null || !string.IsNullOrWhiteSpace(externo.HashDadosSincronizaveis));
        if (!externo.Ativo && local.Status != "Inativo") { local.AlterarStatus(false, erpTime, "ERP"); await fornecedorRepository.AtualizarAsync(local, ct); }
        else if (erpTime > localTime && !same) { local.AplicarContratoCanonico(erpDados, "ERP", erpTime); await fornecedorRepository.AtualizarAsync(local, ct); }
        else if (erpTime == localTime && !same) { /* desempate é +Compras; nenhuma escrita local */ }
        else if (erpTime < localTime && !same) return await ExportarAsync(dto with { Direcao = DirecaoSincronizacao.MaisComprasParaErp }, adapter, local, externo.Id, correlationId, started, snapshotAntes, ct);
        return await FinishAsync(dto, local, externo, correlationId, same ? "NenhumaAlteracao" : erpTime == localTime ? "ConflitoMaisComprasPrevaleceu" : "Importado", erpTime > localTime ? "ERP" : "+Compras", erpTime > localTime ? "+Compras" : "ERP", same ? "Dados iguais" : erpTime == localTime ? "Empate: +Compras prevalece" : "+Compras mais recente", started, snapshotAntes, ct);
    }

    private async Task<SincronizacaoFornecedorResultado> ExportarAsync(SincronizarFornecedorDto dto, IErpFornecedorAdapter adapter,
        Fornecedor? local, string? externalId, string correlationId, long started, string? snapshotAntes, CancellationToken ct)
    {
        if (local is null) throw new InvalidOperationException("Fornecedor não encontrado no +Compras.");
        if (dto.Operacao == OperacaoFornecedor.Inativar) local.AlterarStatus(false, Now(), "+Compras");
        ErpFornecedorDto? remoto = string.IsNullOrWhiteSpace(externalId) ? null : await adapter.ObterAsync(externalId, ct);
        var localData = Canonical(local);
        ErpFornecedorDto externo;
        if (remoto is null) externo = await adapter.CriarAsync(ToWrite(localData, externalId ?? Guid.NewGuid().ToString("N")), ct);
        else
        {
            var erpData = Canonical(remoto); var localTime = Normalize(local.UpdatedAt); var erpTime = Normalize(remoto.UltimaAlteracaoEm ?? DateTimeOffset.MinValue);
            var same = Same(local, erpData, remoto.DadosCanonicos is not null || !string.IsNullOrWhiteSpace(remoto.HashDadosSincronizaveis));
            if (same && dto.Operacao == OperacaoFornecedor.Sincronizar)
                return await FinishAsync(dto, local, remoto, correlationId, "NenhumaAlteracao", "+Compras", "ERP", "Dados iguais", started, snapshotAntes, ct);
            if (erpTime > localTime && !same && dto.Operacao == OperacaoFornecedor.Sincronizar)
                return await FinishAsync(dto, local, remoto, correlationId, "ConflitoErpMaisRecente", "ERP", "+Compras", "ERP mais recente; não sobrescrever", started, snapshotAntes, ct);
            externo = dto.Operacao == OperacaoFornecedor.Inativar
                ? await adapter.InativarAsync(remoto.Id, ct)
                : await adapter.AtualizarAsync(ToWrite(localData, remoto.Id), ct);
        }
        // A resposta ERP pode ser parcial; exportação não pode apagar campos exclusivos do +Compras.
        local.RegistrarVinculoErp(dto.BusinessUnit, dto.ErpSistema, externo.Id);
        if (dto.Operacao == OperacaoFornecedor.Inativar) local.AlterarStatus(false, Now(), "+Compras");
        else local.AplicarDadosCorporativos(externo.Nome, externo.Cnpj, externo.Cidade, externo.Estado, externo.Pais,
            dto.BusinessUnit, dto.ErpSistema, externo.Id, Normalize(externo.UltimaAlteracaoEm ?? Now()));
        local.RegistrarVinculoErp(dto.BusinessUnit, dto.ErpSistema, externo.Id);
        local.RegistrarSincronizacao("Sincronizado", Now()); await fornecedorRepository.AtualizarAsync(local, ct);
        return await FinishAsync(dto, local, externo, correlationId, dto.Operacao == OperacaoFornecedor.Inativar ? "Inativado" : "Exportado", "+Compras", "ERP", "+Compras mais recente", started, snapshotAntes, ct);
    }

    private async Task<SincronizacaoFornecedorResultado> FinishAsync(SincronizarFornecedorDto dto, Fornecedor local, ErpFornecedorDto externo,
        string correlationId, string status, string origem, string destino, string decisao, long started, string? snapshotAntes, CancellationToken ct)
    {
        local.RegistrarSincronizacao("Sincronizado", Now()); await fornecedorRepository.AtualizarAsync(local, ct);
        await AuditarAsync(dto, local, externo.Id, correlationId, status, decisao, origem, destino, null, started, snapshotAntes, ct);
        return new(local.Id, dto.BusinessUnit, dto.ErpSistema, externo.Id, "Sincronizado", correlationId, Now(), null);
    }

    private async Task AuditarAsync(SincronizarFornecedorDto dto, Fornecedor? local, string? erpId, string correlationId,
        string status, string decisao, string? origem, string? destino, string? erro, long started, string? snapshotAntes, CancellationToken ct)
    {
        var now = Now(); var before = snapshotAntes; var after = local is null ? null : Snapshot(local); var hashAntes = before is null ? null : Hash(before); var hashDepois = after is null ? null : Hash(after);
        var duration = (int)Math.Max(0, Stopwatch.GetElapsedTime(started).TotalMilliseconds);
        await sincronizacaoRepository.AdicionarAsync(new(Guid.NewGuid(), dto.BusinessUnit, dto.ErpSistema, erpId ?? "N/A", local?.Id,
            dto.Direcao.ToString(), status, correlationId, now, erro, origem, destino,
            local is null ? null : local.UpdatedAt.ToString("O"), null, Normalize(local?.UpdatedAt ?? now).ToString("O"), null, decisao,
            null, before, after, hashAntes, hashDepois, 1, duration), ct);
    }

    private static FornecedorCanonico Canonical(ErpFornecedorDto value)
    {
        var canonical = value.DadosCanonicos ?? new(
        RazaoSocial: value.Nome, NomeFantasia: null, DocumentoFiscal: value.Cnpj ?? "00000000000000", TipoPessoa: null,
        Pais: value.Pais, InscricaoEstadual: null, InscricaoMunicipal: null, Cep: null, Logradouro: null, Numero: null,
        Complemento: null, Bairro: null, Cidade: value.Cidade, Uf: value.Estado, CodigoMunicipio: null, Ddd: null, Telefone: null,
        EmailComercial: null, EmailFiscal: null, Banco: null, Agencia: null, Conta: null, DigitosConta: null, CondicaoPagamento: null,
        TipoFornecedor: null, SubtipoFornecedor: null, ContaContabil: null, RegimeFiscal: null, SimplesNacional: null,
        CategoriasFornecimento: null, ForneceMateriais: false, ForneceConsumo: false, ForneceServicos: false, ForneceProdutos: false,
        Beneficiador: false, Licenciado: false,
        Ativo: value.Ativo, DataUltimaAlteracao: value.UltimaAlteracaoEm ?? DateTimeOffset.UtcNow,
        HashDadosSincronizaveis: value.HashDadosSincronizaveis ?? string.Empty);
        var hash = string.IsNullOrWhiteSpace(value.HashDadosSincronizaveis) ? Hash(JsonSerializer.Serialize(canonical with { HashDadosSincronizaveis = string.Empty })) : value.HashDadosSincronizaveis;
        return canonical with { Ativo = value.Ativo, DataUltimaAlteracao = value.UltimaAlteracaoEm ?? canonical.DataUltimaAlteracao, HashDadosSincronizaveis = hash };
    }
    private static FornecedorCanonico Canonical(Fornecedor value) => new(value.RazaoSocial, null, value.Cnpj_Cpf, value.TipoPessoa, value.Pais, value.InscricaoEstadual, value.InscricaoMunicipal,
        value.Cep, value.Logradouro, value.Numero, value.Complemento, value.Bairro, value.Cidade, value.Estado, value.CodigoMunicipio, value.Ddd, value.Telefone, value.Email, value.EmailFiscal,
        value.Banco, value.Agencia, value.Conta, value.DigitosConta, value.CondicaoPagamento, value.TipoFornecedor, value.SubtipoFornecedor, value.ContaContabil, value.RegimeFiscal,
        value.SimplesNacional, value.CategoriasFornecimento, value.ForneceMateriais, value.ForneceConsumo, value.ForneceServicos,
        value.ForneceProdutos, value.Beneficiador, value.Licenciado, value.Status != "Inativo", value.UpdatedAt,
        value.HashDadosSincronizaveis ?? string.Empty);
    private static ErpFornecedorParaEscrita ToWrite(FornecedorCanonico data, string id) => new(id, data.RazaoSocial, data.DocumentoFiscal, data.Cidade, data.Uf, data.Pais, data.Ativo, data.DataUltimaAlteracao, data.HashDadosSincronizaveis, data);
    private static bool Same(Fornecedor local, FornecedorCanonico remoto, bool contratoCompleto)
    {
        if (!contratoCompleto) return local.RazaoSocial == remoto.RazaoSocial && local.Cnpj_Cpf == remoto.DocumentoFiscal && (local.Status != "Inativo") == remoto.Ativo && local.Cidade == remoto.Cidade && local.Estado == remoto.Uf;
        if (!string.IsNullOrWhiteSpace(local.HashDadosSincronizaveis) && local.HashDadosSincronizaveis == remoto.HashDadosSincronizaveis) return true;
        var localData = Canonical(local) with { DataUltimaAlteracao = default, HashDadosSincronizaveis = string.Empty };
        var remoteData = remoto with { DataUltimaAlteracao = default, HashDadosSincronizaveis = string.Empty };
        return localData == remoteData;
    }
    private static string Snapshot(Fornecedor x) => JsonSerializer.Serialize(new { x.Id, x.RazaoSocial, x.NomeFantasia, x.Cnpj_Cpf, x.TipoPessoa, x.Beneficiador, x.Licenciado, x.Status, x.UpdatedAt, x.BusinessUnit, x.ErpSistema, x.ErpFornecedorId, x.Versao });
    private static string Hash(string value) => Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(value)));
    private static DateTimeOffset Normalize(DateTimeOffset value) => TimeZoneInfo.ConvertTime(value, SaoPaulo);
    private static DateTimeOffset Now() => DateTimeOffset.UtcNow;
    private static void Validar(SincronizarFornecedorDto dto) { if (string.IsNullOrWhiteSpace(dto.BusinessUnit)) throw new ArgumentException("BU é obrigatória."); if (string.IsNullOrWhiteSpace(dto.ErpSistema)) throw new ArgumentException("ERP é obrigatório."); if (dto.Direcao == DirecaoSincronizacao.ErpParaMaisCompras && string.IsNullOrWhiteSpace(dto.ErpFornecedorId)) throw new ArgumentException("Identificador ERP é obrigatório."); if (dto.Direcao == DirecaoSincronizacao.MaisComprasParaErp && dto.FornecedorId is null) throw new ArgumentException("Fornecedor do +Compras é obrigatório."); }
    private static string SanitizarCorrelationId(string? value) => string.IsNullOrWhiteSpace(value) ? Guid.NewGuid().ToString("N") : value.Trim()[..Math.Min(value.Trim().Length, 100)];
    private static string SanitizarErro(Exception ex) => ex is TimeoutException ? "Tempo limite excedido ao comunicar com o ERP." : "Falha ao comunicar com o ERP.";
    public async Task<IReadOnlyList<SincronizacaoFornecedorResultado>> ExecutarLoteAsync(SincronizarFornecedoresLoteDto dto, CancellationToken cancellationToken = default) { var result = new List<SincronizacaoFornecedorResultado>(); foreach (var id in dto.FornecedorIds.Take(Math.Clamp(dto.Limite <= 0 ? 50 : dto.Limite, 1, 100))) result.Add(await ExecuteAsync(new(dto.BusinessUnit, dto.ErpSistema, null, id, DirecaoSincronizacao.MaisComprasParaErp, dto.CorrelationId), cancellationToken)); return result; }
}
