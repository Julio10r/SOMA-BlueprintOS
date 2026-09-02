namespace BlueprintOS.Application.Procurement.Suppliers;

/// <summary>Gate de homologação de Fornecedores (2026-09-01): sinaliza que o CNPJ/CPF informado já
/// existe como Fornecedor no Linx (papel Fornecedor confirmado, não apenas CADASTRO_CLI_FOR) — o
/// cadastro local NÃO foi criado como um novo registro; <see cref="FornecedorId"/> aponta para o
/// registro local que representa esse fornecedor (importado do ERP nesta mesma operação quando
/// ainda não existia localmente). O controller mapeia esta exceção para uma resposta que o
/// frontend usa para abrir diretamente a tela de detalhe desse fornecedor, em vez de duplicar.</summary>
public sealed class FornecedorJaExisteNoErpException(Guid fornecedorId) : Exception("Fornecedor já existe no ERP.")
{
    public Guid FornecedorId { get; } = fornecedorId;
}
