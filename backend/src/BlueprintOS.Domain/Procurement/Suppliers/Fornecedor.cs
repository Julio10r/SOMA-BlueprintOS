namespace BlueprintOS.Domain.Procurement.Suppliers;

/// <summary>Aggregate root representing a supplier owned by a procurement user.</summary>
public sealed class Fornecedor
{
    private Fornecedor() { }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt)
        : this(id, nome, DocumentoFiscal.Create(cnpj.Value), "PJ", categoria, email, telefone, website, cidade, estado, pais, status, scoreIA,
            temporaryUserId, createdAt, null, null, null)
    {
    }

    public Fornecedor(Guid id, string nome, Cnpj cnpj, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt, string? businessUnit, string? erpSistema, string? erpFornecedorId)
        : this(id, nome, DocumentoFiscal.Create(cnpj.Value), "PJ", categoria, email, telefone, website, cidade, estado, pais, status, scoreIA,
            temporaryUserId, createdAt, businessUnit, erpSistema, erpFornecedorId)
    {
    }

    public Fornecedor(Guid id, string razaoSocial, DocumentoFiscal documentoFiscal, string? tipoPessoa, string? categoria, string? email, string? telefone,
        string? website, string? cidade, string? estado, string? pais, string status, decimal? scoreIA,
        Guid temporaryUserId, DateTimeOffset createdAt, string? businessUnit = null, string? erpSistema = null, string? erpFornecedorId = null)
    {
        if (string.IsNullOrWhiteSpace(razaoSocial)) throw new ArgumentException("RazaoSocial is required.", nameof(razaoSocial));
        if (temporaryUserId == Guid.Empty) throw new ArgumentException("TemporaryUserId is required.", nameof(temporaryUserId));
        Id = id == Guid.Empty ? Guid.NewGuid() : id;
        RazaoSocial = razaoSocial.Trim(); Cnpj_Cpf = documentoFiscal.Value; TipoPessoa = tipoPessoa?.Trim();
        Categoria = categoria?.Trim(); Email = email?.Trim();
        Telefone = telefone?.Trim(); Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim();
        Pais = pais?.Trim(); Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA;
        TemporaryUserId = temporaryUserId; CreatedAt = createdAt; UpdatedAt = createdAt;
        BusinessUnit = businessUnit?.Trim(); ErpSistema = erpSistema?.Trim(); ErpFornecedorId = erpFornecedorId?.Trim();
        OrigemInformacao = "MaisCompras"; StatusSincronizacao = "Pendente";
    }

    public Guid Id { get; private set; }
    public string RazaoSocial { get; private set; } = null!;
    public string Cnpj_Cpf { get; private set; } = null!;
    public string Nome => RazaoSocial;
    public string Cnpj => Cnpj_Cpf;
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
    public bool Beneficiador { get; private set; }
    public bool Licenciado { get; private set; }
    public Guid? CondicaoPagamentoDominioId { get; private set; }
    public Guid? TipoFornecedorDominioId { get; private set; }
    public Guid? SubtipoFornecedorDominioId { get; private set; }
    public string? HashDadosSincronizaveis { get; private set; }
    public string? OrigemUltimaAlteracao { get; private set; }
    public int Versao { get; private set; } = 1;

    /// <summary>Código do CNAE principal, dígitos puros (ex.: "6201501"). Complementar/opcional —
    /// ausência não impede o cadastro do Fornecedor (B2.8, seção H de
    /// docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md). CNAEs secundários não são persistidos.</summary>
    public string? CnaePrincipalCodigo { get; private set; }
    public string? CnaePrincipalDescricao { get; private set; }

    public void Atualizar(string nome, string? categoria, string? email, string? telefone, string? website,
        string? cidade, string? estado, string? pais, string status, decimal? scoreIA, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        RazaoSocial = nome.Trim(); Categoria = categoria?.Trim(); Email = email?.Trim(); Telefone = telefone?.Trim();
        Website = website?.Trim(); Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        Status = string.IsNullOrWhiteSpace(status) ? "Ativo" : status.Trim(); ScoreIA = scoreIA; UpdatedAt = updatedAt;
        OrigemUltimaAlteracao = "MaisCompras"; Versao++;
    }

    public void AplicarDadosCorporativos(string nome, string? cnpj, string? cidade, string? estado, string? pais,
        string businessUnit, string erpSistema, string erpFornecedorId, DateTimeOffset updatedAt)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome is required.", nameof(nome));
        RazaoSocial = nome.Trim();
        if (!string.IsNullOrWhiteSpace(cnpj)) Cnpj_Cpf = DocumentoFiscal.Create(cnpj).Value;
        Cidade = cidade?.Trim(); Estado = estado?.Trim(); Pais = pais?.Trim();
        BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim();
        OrigemInformacao = "ERP"; UpdatedAt = updatedAt;
        OrigemUltimaAlteracao = "ERP"; Versao++;
    }

    public void AplicarContratoCanonico(FornecedorCanonico dados, string origem, DateTimeOffset alteradoEm)
    {
        RazaoSocial = dados.RazaoSocial.Trim(); Cnpj_Cpf = DocumentoFiscal.Create(dados.DocumentoFiscal).Value;
        if (string.Equals(origem, "ERP", StringComparison.OrdinalIgnoreCase)) NomeFantasia = dados.NomeFantasia?.Trim();
        TipoPessoa = dados.TipoPessoa?.Trim(); Pais = dados.Pais; InscricaoEstadual = dados.InscricaoEstadual; InscricaoMunicipal = dados.InscricaoMunicipal;
        Cep = dados.Cep; Logradouro = dados.Logradouro; Numero = dados.Numero; Complemento = dados.Complemento; Bairro = dados.Bairro;
        Cidade = dados.Cidade; Estado = dados.Uf; CodigoMunicipio = dados.CodigoMunicipio; Ddd = dados.Ddd; Telefone = dados.Telefone;
        Email = dados.EmailComercial; EmailFiscal = dados.EmailFiscal; Banco = dados.Banco; Agencia = dados.Agencia; Conta = dados.Conta; DigitosConta = dados.DigitosConta;
        CondicaoPagamento = dados.CondicaoPagamento; TipoFornecedor = dados.TipoFornecedor; SubtipoFornecedor = dados.SubtipoFornecedor; ContaContabil = dados.ContaContabil;
        RegimeFiscal = dados.RegimeFiscal; SimplesNacional = dados.SimplesNacional; CategoriasFornecimento = dados.CategoriasFornecimento;
        ForneceMateriais = dados.ForneceMateriais; ForneceConsumo = dados.ForneceConsumo; ForneceServicos = dados.ForneceServicos; ForneceProdutos = dados.ForneceProdutos;
        Beneficiador = dados.Beneficiador; Licenciado = dados.Licenciado;
        Status = dados.Ativo ? "Ativo" : "Inativo"; HashDadosSincronizaveis = dados.HashDadosSincronizaveis; UpdatedAt = alteradoEm;
        OrigemUltimaAlteracao = origem; OrigemInformacao = origem; Versao++;
    }

    public void AlterarStatus(bool ativo, DateTimeOffset alteradoEm, string origem)
    { Status = ativo ? "Ativo" : "Inativo"; UpdatedAt = alteradoEm; OrigemUltimaAlteracao = origem; Versao++; }

    public void RegistrarVinculoErp(string businessUnit, string erpSistema, string erpFornecedorId)
    { BusinessUnit = businessUnit.Trim(); ErpSistema = erpSistema.Trim(); ErpFornecedorId = erpFornecedorId.Trim(); }

    public void AtualizarDocumento(string documentoFiscal, string? tipoPessoa, DateTimeOffset alteradoEm)
    { Cnpj_Cpf = DocumentoFiscal.Create(documentoFiscal).Value; TipoPessoa = tipoPessoa?.Trim() ?? TipoPessoa; UpdatedAt = alteradoEm; OrigemUltimaAlteracao = "MaisCompras"; Versao++; }

    public void AplicarEnriquecimentoCnpj(IReadOnlyDictionary<string, string?> campos, DateTimeOffset alteradoEm)
    {
        foreach (var campo in campos)
        {
            var valor = campo.Value?.Trim();
            switch (campo.Key)
            {
                case nameof(RazaoSocial) when !string.IsNullOrWhiteSpace(valor): RazaoSocial = valor; break;
                case nameof(Cep): Cep = valor; break;
                case nameof(Logradouro): Logradouro = valor; break;
                case nameof(Numero): Numero = valor; break;
                case nameof(Complemento): Complemento = valor; break;
                case nameof(Bairro): Bairro = valor; break;
                case nameof(Cidade): Cidade = valor; break;
                case nameof(Estado): Estado = valor; break;
                case nameof(Email): Email = valor; break;
                case nameof(Telefone): Telefone = valor; break;
                case nameof(CnaePrincipalCodigo): CnaePrincipalCodigo = NormalizarCnaeCodigo(valor); break;
                case nameof(CnaePrincipalDescricao): CnaePrincipalDescricao = string.IsNullOrWhiteSpace(valor) ? null : valor; break;
            }
        }

        UpdatedAt = alteradoEm; OrigemUltimaAlteracao = "ConsultaCnpj"; Versao++;
    }

    /// <summary>Persiste o CNAE principal (código + descrição) — só chamado a partir da operação
    /// explícita de cadastro/atualização de Fornecedor (B2.8, ADR-0023). Nunca invocado a partir de
    /// uma consulta isolada: consultar CNPJ não altera Fornecedor. Complementar/opcional — ambos os
    /// parâmetros podem ser nulos sem impedir a persistência do Fornecedor.</summary>
    public void DefinirCnaePrincipal(string? codigo, string? descricao, DateTimeOffset alteradoEm)
    {
        CnaePrincipalCodigo = NormalizarCnaeCodigo(codigo);
        CnaePrincipalDescricao = string.IsNullOrWhiteSpace(descricao) ? null : descricao.Trim();
        UpdatedAt = alteradoEm; Versao++;
    }

    /// <summary>Normaliza o código do CNAE para dígitos puros (ex.: "62.01-5/01" -> "6201501").
    /// A máscara é responsabilidade exclusiva de apresentação — a persistência privilegia a
    /// representação canônica estável. Centralizado aqui para não espalhar `Replace`/regex pelo
    /// código (Provider, Application e Domain reutilizam este método).</summary>
    public static string? NormalizarCnaeCodigo(string? codigo)
    {
        if (string.IsNullOrWhiteSpace(codigo)) return null;
        var digitos = new string(codigo.Where(char.IsDigit).ToArray());
        return digitos.Length == 0 ? null : digitos;
    }

    public void VincularDominios(Guid? condicaoPagamentoId, Guid? tipoFornecedorId, Guid? subtipoFornecedorId, DateTimeOffset alteradoEm)
    {
        CondicaoPagamentoDominioId = condicaoPagamentoId; TipoFornecedorDominioId = tipoFornecedorId; SubtipoFornecedorDominioId = subtipoFornecedorId;
        UpdatedAt = alteradoEm; Versao++;
    }

    public void RegistrarSincronizacao(string status, DateTimeOffset quando, string? mensagem = null)
    {
        StatusSincronizacao = status.Trim(); UltimaSincronizacaoEm = quando;
        MensagemErroSincronizacao = string.IsNullOrWhiteSpace(mensagem) ? null : mensagem.Trim();
    }
}
