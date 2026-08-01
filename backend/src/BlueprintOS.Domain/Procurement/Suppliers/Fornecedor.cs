namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Aggregate root representing a supplier owned by a procurement user.</summary>
public sealed class Fornecedor
{
    private Fornecedor() { }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt)
        : this(id, nome, cnpj, categoria, email, telefone, website, cidade, estado, pais, status, scoreIA,
            temporaryUserId, createdAt, null, null, null)
    {
    }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt, string? businessUnit, string? erpSistema, string? erpFornecedorId)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        if (temporaryUserId == Guid.Empty) throw new ArgumentException("TemporaryUserId is required.", nameof(temporaryUserId));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        Nome = nome.Trim(); Cnpj = cnpj.Value; Categoria = categoria?.Trim(); Email = email?.Trim();
        Telefone = telefone?.Trim(); Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim();
        Pais = pais?.Trim(); Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA;
        TemporaryUserId = temporaryUserId; CreatedAt = createdAt; UpdatedAt = createdAt;
        BusinessUnit = businessUnit?.Trim(); ErpSistema = erpSistema?.Trim(); ErpFornecedorId = erpFornecedorId?.Trim();
        OrigemInformacao = "MaisCompras"; StatusSincronizacao = "Pendente";
    }

    public Guid Id { get; private set; }
    public string Nome { get; private set; } = null!;
    public string Cnpj { get; private set; } = null!;
    public string? Categoria { get; private set; }
    public string? Email { get; private set; }
    public string? Telefone { get; private set; }
    public string? Website { get; private set; }
    public string? Cidade { get; private set; }
    public string? Estado { get; private set; }
    public string? Pais { get; private set; }
    public string Status { get; private set; } = null!;
    public decimal? ScoreIA { get; private set; }
    public Guid TemporaryUserId { get; private set; }
    public DateTimeOffset CreatedAt { get; private set; }
    public DateTimeOffset UpdatedAt { get; private set; }
    public string? BusinessUnit { get; private set; }
    public string? ErpSistema { get; private set; }
    public string? ErpFornecedorId { get; private set; }
    public string OrigemInformacao { get; private set; } = "MaisCompras";
    public DateTimeOffset? UltimaSincronizacaoEm { get; private set; }
    public string StatusSincronizacao { get; private set; } = "Pendente";
    public string? MensagemErroSincronizacao { get; private set; }
    public string? NomeFantasia { get; private set; }
    public string? TipoPessoa { get; private set; }
    public string? InscricaoEstadual { get; private set; }
    public string? InscricaoMunicipal { get; private set; }
    public string? Cep { get; private set; }
    public string? Logradouro { get; private set; }
    public string? Numero { get; private set; }
    public string? Complemento { get; private set; }
    public string? Bairro { get; private set; }
    public string? CodigoMunicipio { get; private set; }
    public string? Ddd { get; private set; }
    public string? EmailFiscal { get; private set; }
    public string? Banco { get; private set; }
    public string? Agencia { get; private set; }
    public string? Conta { get; private set; }
    public string? DigitosConta { get; private set; }
    public string? CondicaoPagamento { get; private set; }
    public string? TipoFornecedor { get; private set; }
    public string? SubtipoFornecedor { get; private set; }
    public string? ContaContabil { get; private set; }
    public string? RegimeFiscal { get; private set; }
    public bool? SimplesNacional { get; private set; }
    public string? CategoriasFornecimento { get; private set; }
    public bool ForneceMateriais { get; private set; }
    public bool ForneceConsumo { get; private set; }
    public bool ForneceServicos { get; private set; }
    public bool ForneceProdutos { get; private set; }
    public string? HashDadosSincronizaveis { get; private set; }
    public string? OrigemUltimaAlteracao { get; private set; }
    public int Versao { get; private set; } = 1;

    public void Atualizar(string nome, string? categoria, string? email, string? telefone, string? website,
        string? cidade, string? estado, string? pais, string status, decimal? scoreIA, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome.Trim(); Categoria = categoria?.Trim(); Email = email?.Trim(); Telefone = telefone?.Trim();
        Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA; UpdatedAt = updatedAt;
        OrigemUltimaAlteracao = "MaisCompras"; Versao++;
    }

    public void AplicarDadosCorporativos(string nome, string? cnpj, string? cidade, string? estado, string? pais,
        string businessUnit, string erpSistema, string erpFornecedorId, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        Nome = nome.Trim();
        if (!string.IsNullOrWhiteSpace(cnpj)) Cnpj = global::BlueprintOS.Domain.Procurement.Suppliers.Cnpj.Create(cnpj).Value;
        Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim();
        OrigemInformacao = "ERP"; UpdatedAt = updatedAt;
        OrigemUltimaAlteracao = "ERP"; Versao++;
    }

    public void AplicarContratoCanonico(FornecedorCanonico dados, string origem, DateTimeOffset alteradoEm)
    {
        Nome = dados.RazaoSocial.Trim(); NomeFantasia = dados.NomeFantasia?.Trim(); Cnpj = global::BlueprintOS.Domain.Procurement.Suppliers.Cnpj.Create(dados.DocumentoFiscal).Value;
        TipoPessoa = dados.TipoPessoa; Pais = dados.Pais; InscricaoEstadual = dados.InscricaoEstadual; InscricaoMunicipal = dados.InscricaoMunicipal;
        Cep = dados.Cep; Logradouro = dados.Logradouro; Numero = dados.Numero; Complemento = dados.Complemento; Bairro = dados.Bairro;
        Cidade = dados.Cidade; Estado = dados.Uf; CodigoMunicipio = dados.CodigoMunicipio; Ddd = dados.Ddd; Telefone = dados.Telefone;
        Email = dados.EmailComercial; EmailFiscal = dados.EmailFiscal; Banco = dados.Banco; Agencia = dados.Agencia; Conta = dados.Conta; DigitosConta = dados.DigitosConta;
        CondicaoPagamento = dados.CondicaoPagamento; TipoFornecedor = dados.TipoFornecedor; SubtipoFornecedor = dados.SubtipoFornecedor; ContaContabil = dados.ContaContabil;
        RegimeFiscal = dados.RegimeFiscal; SimplesNacional = dados.SimplesNacional; CategoriasFornecimento = dados.CategoriasFornecimento;
        ForneceMateriais = dados.ForneceMateriais; ForneceConsumo = dados.ForneceConsumo; ForneceServicos = dados.ForneceServicos; ForneceProdutos = dados.ForneceProdutos;
        Status = dados.Ativo ? "Ativo" : "Inativo"; HashDadosSincronizaveis = dados.HashDadosSincronizaveis; UpdatedAt = alteradoEm;
        OrigemUltimaAlteracao = origem; OrigemInformacao = origem; Versao++;
    }

    public void AlterarStatus(bool ativo, DateTimeOffset alteradoEm, string origem)
    { Status = ativo ? "Ativo" : "Inativo"; UpdatedAt = alteradoEm; OrigemUltimaAlteracao = origem; Versao++; }

    public void RegistrarVinculoErp(string businessUnit, string erpSistema, string erpFornecedorId)
    { BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim(); }

    public void AtualizarDocumento(string cnpj, DateTimeOffset alteradoEm)
    { Cnpj = global::BlueprintOS.Domain.Procurement.Suppliers.Cnpj.Create(cnpj).Value; UpdatedAt = alteradoEm; OrigemUltimaAlteracao = "MaisCompras"; Versao++; }

    public void RegistrarSincronizacao(string status, DateTimeOffset quando, string? mensagem = null)
    {
        StatusSincronizacao = status.Trim(); UltimaSincronizacaoEm = quando;
        MensagemErroSincronizacao = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim();
    }
}
