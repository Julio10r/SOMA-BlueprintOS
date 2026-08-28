namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Operação de negócio expressa pelo domínio +Compras ao pedir para o ERP da BU "garantir" um
/// fornecedor a partir de um CNPJ — nunca "inserir" (ADR-0023, Gate Pré-B2.9, seção 7-A). O Adapter decide
/// internamente se a operação física é criação, adição de papel a um cadastro existente, ou atualização.
/// Nenhum vocabulário físico do ERP (CLIFOR, CADASTRO_CLI_FOR, LX_SEQUENCIAL) atravessa esta fronteira.</summary>
public sealed record GarantirFornecedorErpRequest(
    string BusinessUnit,
    string DocumentoFiscal,
    string Nome,
    string? RazaoSocial,
    string? Cidade,
    string? Estado,
    string? Pais,
    bool Ativo,
    string CorrelationId);

/// <summary>Caminho físico que o Adapter efetivamente executou para convergir ao estado desejado.</summary>
public enum OperacaoGarantirFornecedorErp
{
    /// <summary>Não existia cadastro para o CNPJ na BU: criado cadastro-base + papel Fornecedor.</summary>
    Criado,

    /// <summary>Já existia cadastro-base (outro papel: Cliente e/ou Filial) sem o papel Fornecedor: papel adicionado, papéis existentes preservados.</summary>
    PapelAdicionado,

    /// <summary>Fornecedor já existia: dados complementados/atualizados dentro do escopo autorizado.</summary>
    Atualizado
}

public sealed record GarantirFornecedorErpResultado(
    OperacaoGarantirFornecedorErp Operacao,
    string IdentificadorExterno,
    string BusinessUnit,
    string ErpSistema,
    DateTimeOffset ProcessadoEm,
    string CorrelationId);

/// <summary>Taxonomia de falhas do Adapter Linx — nunca expor <see cref="Exception"/> bruta (mensagem/stack/SQL)
/// ao chamador de negócio. Ver seção 31 do brief B2.9.</summary>
public enum ErpFornecedorErro
{
    /// <summary>Falha de conectividade com o servidor/banco do ERP.</summary>
    Conectividade,

    /// <summary>A operação excedeu o tempo limite configurado.</summary>
    Timeout,

    /// <summary>Conflito de concorrência que não pôde ser resolvido por convergência automática (ex.: corrida
    /// real de duplicidade que precisa ser reexecutada pelo chamador).</summary>
    ConflitoRecuperavel,

    /// <summary>Dado de entrada inválido para o contrato do Adapter (ex.: CNPJ vazio, BU não configurada).</summary>
    Validacao,

    /// <summary>Falha ao persistir no ERP (SQL não relacionado a concorrência/conectividade).</summary>
    Persistencia,

    /// <summary>Estado do ERP inconsistente com o esperado pelo Adapter (ex.: mapeamento de coluna ausente).</summary>
    InconsistenciaEstrutural
}

/// <summary>Exceção tipada e segura para expor ao chamador — nunca contém SQL, connection string, nome de
/// servidor ou stack trace do erro original na mensagem pública.</summary>
public sealed class ErpFornecedorEscritaException(ErpFornecedorErro tipo, string mensagem, Exception? causaOriginal = null)
    : Exception(mensagem, causaOriginal)
{
    public ErpFornecedorErro Tipo { get; } = tipo;
}

public interface IGarantirFornecedorErpAdapter
{
    string ErpSistema { get; }

    /// <summary>Garante que o Fornecedor identificado por CNPJ exista no ERP com os dados informados,
    /// convergindo automaticamente para CREATE/ADD_ROLE/UPDATE conforme o estado real do ERP no momento
    /// da escrita (reconsulta obrigatória — nunca confia em estado anterior). Idempotente: chamadas repetidas
    /// com os mesmos dados não duplicam nem falham.</summary>
    Task<GarantirFornecedorErpResultado> GarantirAsync(GarantirFornecedorErpRequest request, CancellationToken cancellationToken = default);
}

public interface IGarantirFornecedorErpAdapterResolver
{
    IGarantirFornecedorErpAdapter Resolver(string businessUnit);
}
