using System.Text.RegularExpressions;
using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;
using BlueprintOS.Application.Procurement.Suppliers.Contracts;
using BlueprintOS.Application.Procurement.Suppliers.Models;
using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.Extensions.Logging;

namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>Onda 2 (Multi-BU/Multi-ERP, 03/09/2026, decisão do Product Owner): Fornecedor pertence a uma
/// Unidade de Negócio — identidade funcional (UnidadeNegocioId, Cnpj_Cpf). Resolve a BU da identidade
/// autenticada com o mesmo fail-closed já usado pelos demais casos de uso administrativos (nunca assume
/// um default, nunca lê do corpo da requisição): <see cref="RequestIdentity.UnidadeNegocioId"/> ausente é
/// um erro, não uma Unidade de Negócio a inferir.</summary>
internal static class ContextoBuFornecedor
{
    public static Guid Resolver(RequestIdentity identity) => identity.UnidadeNegocioId
        ?? throw new UnauthorizedAccessException("Unidade de Negócio não resolvida para a identidade autenticada — operações de Fornecedor exigem contexto de BU (fail closed, Onda 2).");
}

/// <summary>Resolve a Unidade de Negócio (BU) real usada na integração ERP a partir do contexto de
/// sessão autenticado — nunca do frontend (B2.9, decisão do PO, seção 4). Cai em "DEFAULT" apenas
/// quando a claim de BU não está disponível (ex.: autenticação de Development), reaproveitando a
/// entrada já existente em appsettings — não é um valor de marca inventado, é o único ERP configurado
/// hoje. Reavaliar quando existir mais de uma BU/ERP real.</summary>
public sealed class ResolvedorBusinessUnit(IUnidadeNegocioRepository unidadesNegocio)
{
    public async Task<string> ResolverAsync(Guid? unidadeNegocioId, CancellationToken cancellationToken)
    {
        if (unidadeNegocioId is { } id)
        {
            var unidade = await unidadesNegocio.ObterPorIdAsync(id, cancellationToken);
            if (unidade is not null) return unidade.Slug.ToUpperInvariant();
        }

        return "DEFAULT";
    }
}

/// <summary>Orquestra "Cadastrar Fornecedor" como um único comando funcional (B2.9, decisão do PO, seção
/// 3): persiste em +Compras e, na mesma operação, garante o vínculo no ERP via
/// <see cref="IGarantirFornecedorNoErpUseCase"/>. Não há transação distribuída entre as duas fronteiras
/// de persistência (seção 7) — se +Compras falha, o ERP nunca é chamado; se o ERP falha após +Compras já
/// ter persistido, o Fornecedor permanece salvo localmente com <c>StatusSincronizacao="Pendente"</c> (o
/// mesmo estado que uma reconsulta/nova tentativa converge naturalmente, pela idempotência já provada do
/// Adapter) e a falha não é propagada como erro do cadastro.</summary>
public sealed class CadastrarFornecedorUseCase(
    IFornecedorRepository repository,
    ICurrentIdentity identity,
    IGarantirFornecedorNoErpUseCase garantirNoErp,
    IVerificarFornecedorNoErpUseCase verificarNoErp,
    ISincronizarFornecedorUseCase sincronizar,
    ResolvedorBusinessUnit resolvedorBu,
    ILogger<CadastrarFornecedorUseCase> logger) : ICadastrarFornecedorUseCase
{
    /// <summary>ErpSistema fixo — único ERP configurado hoje (mesmo valor usado por
    /// <see cref="SincronizarFornecedorUseCase"/>/<see cref="GarantirFornecedorNoErpUseCase"/>).</summary>
    private const string ErpSistema = "SOMA_DESENV";

    public async Task<FornecedorDto> ExecuteAsync(CadastrarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        var requestIdentity = identity.GetRequired();
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(requestIdentity);
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        if (string.IsNullOrWhiteSpace(dto.NomeFantasia)) throw new ArgumentException("NomeFantasia is required.", nameof(dto.NomeFantasia));
        ValidarCamposObrigatoriosDeEndereco(dto.Categoria, dto.Cep, dto.Logradouro, dto.Numero, dto.Bairro, dto.Cidade, dto.Estado, dto.Pais);
        ValidarEmailETelefone(dto.Email, dto.Telefone);
        var documento = DocumentoFiscal.Create(dto.Cnpj_Cpf ?? Cnpj.Create(dto.Cnpj).Value);
        if (await repository.ExisteAsync(documento.Value, unidadeNegocioId, cancellationToken)) throw new InvalidOperationException("Já existe um fornecedor cadastrado com este CNPJ/CPF nesta Unidade de Negócio.");

        var businessUnit = await resolvedorBu.ResolverAsync(requestIdentity.UnidadeNegocioId, cancellationToken);

        // Gate de homologação de Fornecedores (2026-09-01): antes de criar, verificar se este
        // CNPJ/CPF já existe como Fornecedor no Linx (papel Fornecedor confirmado — não apenas
        // CADASTRO_CLI_FOR) — nunca duplicar. Decisão validada do PO (ver
        // linx-idempotencia-convergencia-create-update-fornecedor): "existe sem papel Fornecedor"
        // NÃO bloqueia aqui — segue o fluxo normal, que reaproveita o cadastro-base e apenas
        // adiciona o papel (GarantirNoErpSemFalharCadastroAsync/PapelAdicionado).
        var verificacao = await verificarNoErp.ExecuteAsync(businessUnit, documento.Value, cancellationToken);
        if (verificacao.Estado == EstadoFornecedorErp.ExisteComPapelFornecedor)
        {
            var fornecedorId = await ImportarExistenteDoErpAsync(businessUnit, verificacao.CodigoClifor, cancellationToken);
            throw new FornecedorJaExisteNoErpException(fornecedorId);
        }

        var fornecedor = new Fornecedor(Guid.NewGuid(), dto.RazaoSocial ?? dto.Nome, documento, dto.TipoPessoa, dto.Categoria, dto.Email, dto.Telefone,
            dto.Website, dto.Cidade, dto.Estado, dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, DateTimeOffset.UtcNow, unidadeNegocioId,
            nomeFantasia: dto.NomeFantasia, cep: dto.Cep, logradouro: dto.Logradouro, numero: dto.Numero, complemento: dto.Complemento, bairro: dto.Bairro);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        // CNAE principal só é persistido nesta operação explícita de cadastro (B2.8) — nunca a
        // partir de uma consulta isolada. Ambos os campos são opcionais/complementares.
        if (dto.CnaePrincipalCodigo is not null || dto.CnaePrincipalDescricao is not null)
            fornecedor.DefinirCnaePrincipal(dto.CnaePrincipalCodigo, dto.CnaePrincipalDescricao, DateTimeOffset.UtcNow);
        try
        {
            await repository.AdicionarAsync(fornecedor, cancellationToken);
        }
        catch (DuplicateRecordException)
        {
            // Concorrência real: outra requisição criou este CNPJ/CPF entre a checagem ExisteAsync e
            // este INSERT (índice único de Cnpj_Cpf). Decisão do PO: convergir para o registro já
            // criado em vez de falhar — nunca mascarar outra classe de erro SQL (a tradução para
            // DuplicateRecordException, em FornecedorRepository, só ocorre para violação de índice
            // único identificada com segurança).
            var jaCriado = await repository.ObterPorCnpjAsync(documento.Value, unidadeNegocioId, cancellationToken)
                ?? throw new InvalidOperationException("Já existe um fornecedor cadastrado com este CNPJ/CPF nesta Unidade de Negócio.");
            return FornecedorMapper.ToDto(jaCriado);
        }

        await GarantirNoErpSemFalharCadastroAsync(fornecedor, requestIdentity.UnidadeNegocioId, cancellationToken);

        return FornecedorMapper.ToDto(fornecedor);
    }

    /// <summary>CNPJ/CPF já existe como Fornecedor no Linx: nunca cria um novo registro local
    /// duplicado — importa o estado real do ERP (mesma engine de sincronização já usada pelo botão
    /// "Atualizar do ERP", <see cref="ISincronizarFornecedorUseCase"/>, direção ErpParaMaisCompras)
    /// para garantir que o fornecedor exista localmente antes de apontar o chamador para ele.</summary>
    private async Task<Guid> ImportarExistenteDoErpAsync(string businessUnit, string? codigoClifor, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(codigoClifor))
            throw new InvalidOperationException("Fornecedor já existe no ERP, mas o identificador não pôde ser determinado.");

        var resultado = await sincronizar.ExecuteAsync(
            new SincronizarFornecedorDto(businessUnit, ErpSistema, codigoClifor, FornecedorId: null, DirecaoSincronizacao.ErpParaMaisCompras, CorrelationId: null),
            cancellationToken);
        if (resultado.FornecedorId is not { } fornecedorId)
            throw new InvalidOperationException(resultado.Mensagem ?? "Não foi possível representar localmente o fornecedor já existente no ERP.");
        return fornecedorId;
    }

    internal async Task GarantirNoErpSemFalharCadastroAsync(Fornecedor fornecedor, Guid? unidadeNegocioId, CancellationToken cancellationToken)
    {
        try
        {
            var businessUnit = await resolvedorBu.ResolverAsync(unidadeNegocioId, cancellationToken);
            await garantirNoErp.ExecuteAsync(fornecedor.Id, businessUnit, new GarantirFornecedorNoErpDto(null), cancellationToken);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            logger.LogWarning(ex, "Falha ao garantir Fornecedor {FornecedorId} no ERP durante o cadastro; permanece Pendente para nova tentativa.", fornecedor.Id);
            fornecedor.RegistrarSincronizacao("Pendente", DateTimeOffset.UtcNow, ex.Message);
            await repository.AtualizarAsync(fornecedor, cancellationToken);
        }
    }

    /// <summary>Retest do Gate de Fornecedores (2026-09-01), item 6 — fecha o bypass de API confirmado na
    /// validação técnica: endereço completo era exigido só no frontend (<c>validateManualFornecedor</c> em
    /// <c>linxSupplierContract.ts</c>), então uma chamada direta a <c>POST /fornecedores</c> criava (e
    /// integrava ao Linx) um fornecedor sem CEP/Logradouro/Número/Bairro/Cidade/UF/País.</summary>
    private static void ValidarCamposObrigatoriosDeEndereco(string? categoria, string? cep, string? logradouro,
        string? numero, string? bairro, string? cidade, string? estado, string? pais)
    {
        if (string.IsNullOrWhiteSpace(categoria)) throw new ArgumentException("Categoria é obrigatória.", nameof(categoria));
        if (string.IsNullOrWhiteSpace(cep)) throw new ArgumentException("CEP é obrigatório.", nameof(cep));
        if (string.IsNullOrWhiteSpace(logradouro)) throw new ArgumentException("Logradouro é obrigatório.", nameof(logradouro));
        if (string.IsNullOrWhiteSpace(numero)) throw new ArgumentException("Número é obrigatório.", nameof(numero));
        if (string.IsNullOrWhiteSpace(bairro)) throw new ArgumentException("Bairro é obrigatório.", nameof(bairro));
        if (string.IsNullOrWhiteSpace(cidade)) throw new ArgumentException("Cidade é obrigatória.", nameof(cidade));
        if (string.IsNullOrWhiteSpace(estado)) throw new ArgumentException("UF é obrigatória.", nameof(estado));
        if (string.IsNullOrWhiteSpace(pais)) throw new ArgumentException("País é obrigatório.", nameof(pais));
    }

    /// <summary>Correção do Gate de Fornecedores (2026-09-01) — o comentário anterior deste use case
    /// documentava E-mail/Telefone como deliberadamente não exigidos aqui, para não quebrar cadastro real
    /// de fornecedores sem contato público. O Product Owner reverteu essa decisão explicitamente:
    /// E-mail e Telefone agora são obrigatórios no CADASTRO MANUAL (este use case), mesmo endpoint
    /// compartilhado com "Consultar por CNPJ" (mesmo formulário, mesmo <c>POST /fornecedores</c> — ver
    /// <c>FornecedoresPage.tsx</c>/<c>ManualFornecedorForm.tsx</c>). Isto não se aplica a
    /// <see cref="ISincronizarFornecedorUseCase"/> (import/hidratação a partir do Linx), que é um use
    /// case distinto e nunca passa por aqui.</summary>
    private static void ValidarEmailETelefone(string? email, string? telefone)
    {
        if (string.IsNullOrWhiteSpace(email)) throw new ArgumentException("E-mail é obrigatório.", nameof(email));
        if (!EmailFornecedorValidator.EhValido(email.Trim())) throw new ArgumentException("E-mail inválido.", nameof(email));
        if (string.IsNullOrWhiteSpace(telefone)) throw new ArgumentException("Telefone é obrigatório.", nameof(telefone));
        var digitos = new string(telefone.Where(char.IsDigit).ToArray());
        var minimoDigitos = telefone.TrimStart().StartsWith("+55", StringComparison.Ordinal) ? 8 : 4;
        if (digitos.Length < minimoDigitos) throw new ArgumentException("Telefone inválido.", nameof(telefone));
    }
}

/// <summary>Validação de e-mail do cadastro manual de Fornecedor — formato mínimo, mesma exigência do
/// frontend (<c>EMAIL_PATTERN</c> em <c>linxSupplierContract.ts</c>).</summary>
internal static partial class EmailFornecedorValidator
{
    [GeneratedRegex(@"^[^@\s]+@[^@\s]+\.[^@\s]+$")]
    private static partial Regex Formato();

    public static bool EhValido(string email) => Formato().IsMatch(email);
}

public sealed class AtualizarFornecedorUseCase(
    IFornecedorRepository repository,
    ICurrentIdentity identity,
    IGarantirFornecedorNoErpUseCase garantirNoErp,
    ResolvedorBusinessUnit resolvedorBu,
    ILogger<AtualizarFornecedorUseCase> logger) : IAtualizarFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, AtualizarFornecedorDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.Nome)) throw new ArgumentException("Nome is required.", nameof(dto.Nome));
        var requestIdentity = identity.GetRequired();
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(requestIdentity);
        var fornecedor = await repository.ObterPorIdAsync(id, cancellationToken);
        // Onda 2 (Multi-BU): um Fornecedor de outra Unidade de Negócio é tratado como inexistente —
        // isolamento entre BUs nunca vaza sequer a existência do registro.
        if (fornecedor is null || fornecedor.UnidadeNegocioId != unidadeNegocioId) return null;
        fornecedor.Atualizar(dto.RazaoSocial ?? dto.Nome, dto.Categoria, dto.Email, dto.Telefone, dto.Website, dto.Cidade, dto.Estado,
            dto.Pais, dto.Status ?? "Ativo", dto.ScoreIA, DateTimeOffset.UtcNow,
            nomeFantasia: dto.NomeFantasia, cep: dto.Cep, logradouro: dto.Logradouro, numero: dto.Numero, complemento: dto.Complemento, bairro: dto.Bairro);
        var documento = dto.Cnpj_Cpf ?? dto.Cnpj;
        if (!string.IsNullOrWhiteSpace(documento) && documento != fornecedor.Cnpj_Cpf) fornecedor.AtualizarDocumento(documento, dto.TipoPessoa, DateTimeOffset.UtcNow);
        if (dto.DadosCanonicos is not null) fornecedor.AplicarContratoCanonico(dto.DadosCanonicos, "MaisCompras", DateTimeOffset.UtcNow);
        if (dto.CnaePrincipalCodigo is not null || dto.CnaePrincipalDescricao is not null)
            fornecedor.DefinirCnaePrincipal(dto.CnaePrincipalCodigo, dto.CnaePrincipalDescricao, DateTimeOffset.UtcNow);
        await repository.AtualizarAsync(fornecedor, cancellationToken);

        try
        {
            var businessUnit = await resolvedorBu.ResolverAsync(requestIdentity.UnidadeNegocioId, cancellationToken);
            await garantirNoErp.ExecuteAsync(fornecedor.Id, businessUnit, new GarantirFornecedorNoErpDto(null), cancellationToken);
        }
        catch (ErpFornecedorEscritaException ex)
        {
            logger.LogWarning(ex, "Falha ao garantir Fornecedor {FornecedorId} no ERP durante a atualização; permanece Pendente para nova tentativa.", fornecedor.Id);
            fornecedor.RegistrarSincronizacao("Pendente", DateTimeOffset.UtcNow, ex.Message);
            await repository.AtualizarAsync(fornecedor, cancellationToken);
        }

        return FornecedorMapper.ToDto(fornecedor);
    }
}

/// <summary>Implementa "Excluir Fornecedor" (rota HTTP <c>DELETE /fornecedores/{id}</c>, contrato externo
/// mantido) como inativação semântica, nunca como remoção física (DR-18, Design Review Pós-Onda 1: nem
/// +Compras nem ERP executam DELETE físico como operação funcional). Reaproveita
/// <see cref="Fornecedor.AlterarStatus"/>, o mesmo mecanismo já usado pela sincronização com o ERP.</summary>
public sealed class InativarFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IInativarFornecedorUseCase
{
    public async Task<bool> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(identity.GetRequired());
        var fornecedor = await repository.ObterPorIdAsync(id, cancellationToken);
        if (fornecedor is null || fornecedor.UnidadeNegocioId != unidadeNegocioId) return false;
        fornecedor.AlterarStatus(false, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AtualizarAsync(fornecedor, cancellationToken);
        return true;
    }
}

/// <summary>Ativa/inativa um Fornecedor nos dois sentidos (rota semântica <c>PATCH /fornecedores/{id}/status</c>,
/// O1.x). Reaproveita <see cref="Fornecedor.AlterarStatus"/>, o mesmo mecanismo já usado por
/// <see cref="InativarFornecedorUseCase"/> e pela sincronização com o ERP — nunca remove a linha
/// fisicamente (DR-18).</summary>
public sealed class AlterarStatusFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IAlterarStatusFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, bool ativo, CancellationToken cancellationToken = default)
    {
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(identity.GetRequired());
        var fornecedor = await repository.ObterPorIdAsync(id, cancellationToken);
        if (fornecedor is null || fornecedor.UnidadeNegocioId != unidadeNegocioId) return null;
        fornecedor.AlterarStatus(ativo, DateTimeOffset.UtcNow, "MaisCompras");
        await repository.AtualizarAsync(fornecedor, cancellationToken);
        return FornecedorMapper.ToDto(fornecedor);
    }
}

public sealed class ObterFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IObterFornecedorUseCase
{
    public async Task<FornecedorDto?> ExecuteAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(identity.GetRequired());
        var fornecedor = await repository.ObterPorIdAsync(id, cancellationToken);
        return fornecedor is not null && fornecedor.UnidadeNegocioId == unidadeNegocioId ? FornecedorMapper.ToDto(fornecedor) : null;
    }
}

public sealed class PesquisarFornecedorUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IPesquisarFornecedorUseCase
{
    public async Task<IReadOnlyList<FornecedorDto>> ExecuteAsync(string? termo, CancellationToken cancellationToken = default)
    {
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(identity.GetRequired());
        var fornecedores = string.IsNullOrWhiteSpace(termo)
            ? await repository.ListarAsync(unidadeNegocioId, cancellationToken)
            : await repository.PesquisarAsync(termo, unidadeNegocioId, cancellationToken);
        return fornecedores.Select(FornecedorMapper.ToDto).ToArray();
    }
}

/// <summary>Pesquisa paginada, filtrável por status e ordenável de Fornecedores (O1.x, redesenho da tela
/// de Fornecedores). Delega paginação/filtro/ordenação ao repositório no nível de IQueryable — nenhuma
/// materialização acontece antes do Skip/Take. Onda 2: escopada pela Unidade de Negócio da sessão.</summary>
public sealed class PesquisarFornecedorPaginadoUseCase(IFornecedorRepository repository, ICurrentIdentity identity) : IPesquisarFornecedorPaginadoUseCase
{
    public async Task<FornecedorPesquisaPaginadaDto> ExecuteAsync(PesquisarFornecedorPaginadoParametros parametros, CancellationToken cancellationToken = default)
    {
        var unidadeNegocioId = ContextoBuFornecedor.Resolver(identity.GetRequired());
        var status = ParseStatus(parametros.Status);
        var (campo, descendente) = ParseSort(parametros.Sort);
        var page = parametros.Page < 1 ? 1 : parametros.Page;
        var pageSize = parametros.PageSize < 1 ? 20 : parametros.PageSize;

        var resultado = await repository.PesquisarPaginadoAsync(parametros.Termo, status, campo, descendente, page, pageSize, unidadeNegocioId, cancellationToken);
        return new FornecedorPesquisaPaginadaDto(resultado.Items.Select(FornecedorMapper.ToDto).ToArray(), resultado.TotalCount, resultado.Page, resultado.PageSize);
    }

    private static FornecedorStatusFiltro ParseStatus(string? status) => status?.Trim().ToLowerInvariant() switch
    {
        "ativo" => FornecedorStatusFiltro.Ativo,
        "inativo" => FornecedorStatusFiltro.Inativo,
        _ => FornecedorStatusFiltro.Todos,
    };

    private static (FornecedorOrdenacaoCampo Campo, bool Descendente) ParseSort(string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort)) return (FornecedorOrdenacaoCampo.RazaoSocial, false);
        var descendente = sort.StartsWith('-');
        var nome = descendente ? sort[1..] : sort;
        var campo = nome.Trim().ToLowerInvariant() switch
        {
            "cnpj" => FornecedorOrdenacaoCampo.Cnpj,
            "status" => FornecedorOrdenacaoCampo.Status,
            "createdat" or "criadoem" => FornecedorOrdenacaoCampo.CreatedAt,
            _ => FornecedorOrdenacaoCampo.RazaoSocial,
        };
        return (campo, descendente);
    }
}

internal static class FornecedorMapper
{
    public static FornecedorDto ToDto(Fornecedor value) => new(value.Id, value.Nome, value.Cnpj, value.Categoria, value.Email,
        value.Telefone, value.Website, value.Cidade, value.Estado, value.Pais, value.Status, value.ScoreIA,
        value.CreatedAt, value.UpdatedAt, value.NomeFantasia, value.TipoPessoa, value.InscricaoEstadual,
        value.InscricaoMunicipal, value.Cep, value.Logradouro, value.Numero, value.Complemento, value.Bairro, value.CodigoMunicipio,
        value.Ddd, value.EmailFiscal, value.Banco, value.Agencia, value.Conta, value.DigitosConta, value.CondicaoPagamento,
        value.TipoFornecedor, value.SubtipoFornecedor, value.ContaContabil, value.RegimeFiscal, value.SimplesNacional,
        value.CategoriasFornecimento, value.ForneceMateriais, value.ForneceConsumo, value.ForneceServicos, value.ForneceProdutos,
        value.BusinessUnit, value.ErpSistema, value.ErpFornecedorId, value.Versao, value.HashDadosSincronizaveis,
        value.Cnpj_Cpf, value.RazaoSocial, value.Beneficiador, value.Licenciado, value.CondicaoPagamentoDominioId,
        value.TipoFornecedorDominioId, value.SubtipoFornecedorDominioId, value.CnaePrincipalCodigo, value.CnaePrincipalDescricao,
        value.StatusSincronizacao, value.UltimaSincronizacaoEm, value.MensagemErroSincronizacao);
}
