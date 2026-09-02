namespace BlueprintOS.Application.Procurement.Suppliers.Contracts;

/// <summary>Operação de negócio expressa pelo domínio +Compras ao pedir para o ERP da BU "garantir" um
/// fornecedor a partir de um CNPJ — nunca "inserir" (ADR-0023, Gate Pré-B2.9, seção 7-A). O Adapter decide
/// internamente se a operação física é criação, adição de papel a um cadastro existente, ou atualização.
/// Nenhum vocabulário físico do ERP (CLIFOR, CADASTRO_CLI_FOR, LX_SEQUENCIAL) atravessa esta fronteira.
///
/// Escopo de campos de endereço (Cep/Logradouro/Numero/Complemento/Bairro/Cidade) autorizado pela consulta
/// formal ao conhecimento Linx (Retest do Gate de Fornecedores, 2026-09-01): estes são colunas físicas reais
/// de <c>CADASTRO_CLI_FOR</c> (bloco "principal", nunca os blocos `COBRANCA_`/`ENTREGA_` — fora de escopo,
/// GAP-LINX-ENDERECO-MULTIPLO permanece aberto), e a decisão já Validada do PO para o caso "cadastro já
/// existe" é "atualizar/complementar com os dados confirmados pelo usuário, sem sobrescrever campos fora do
/// escopo" (`Gate-PreB29-AdapterLinxFornecedor.md`, unidade `linx-idempotencia-convergencia-create-update`).
/// Telefone/E-mail ficam de fora deste escopo por enquanto — a única fonte que os confirma como fisicamente
/// compatíveis é um doc de aplicação (não o conhecimento Linx validado), então tratá-los é Capability Gap.</summary>
public sealed record GarantirFornecedorErpRequest(
    string BusinessUnit,
    string DocumentoFiscal,
    string Nome,
    string? RazaoSocial,
    string? Cidade,
    string? Estado,
    string? Pais,
    bool Ativo,
    string CorrelationId,
    string? Cep = null,
    string? Logradouro = null,
    string? Numero = null,
    string? Complemento = null,
    string? Bairro = null);

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
