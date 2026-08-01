# Sincronização de fornecedores — B2.1

## Arquitetura

O caso de uso depende apenas de contratos da Application. `IErpFornecedorAdapterResolver` seleciona o adaptador autorizado para a BU; `SomaDesenvolErpFornecedorAdapter` é a implementação inicial isolada em Infrastructure. O domínio não conhece tabelas, colunas ou connection strings do ERP.

O vínculo externo é `BusinessUnit + ErpSistema + ErpFornecedorId`, com índice único filtrado. A tabela `FornecedoresSincronizacoes` registra direção, status, correlação, data e erro sanitizado. Não existe exclusão automática.

## Operação

- `POST /api/fornecedores/sincronizar` importa por identificador ERP ou exporta por `FornecedorId`.
- `POST /api/fornecedores/sincronizar/lote` exporta uma lista controlada, limitada a 100 itens.
- A repetição consulta o vínculo externo e atualiza o mesmo fornecedor; não cria novo vínculo.
- Cancelamento propaga `CancellationToken`; comandos SQL têm timeout configurável em `ErpIntegration:TimeoutSeconds`.

## Configuração

`ConnectionStrings:MaisComprasConnection` é o banco próprio. `ConnectionStrings:ErpConnection` deve apontar exclusivamente para `SOMA_DESENV`. A BU autoriza seu ERP em `ErpIntegration:BusinessUnits:{BU}:ErpSistema`; o schema/tabela inicial usa `ErpIntegration:SomaDesenvol:Schema` e `Table`.

Credenciais devem permanecer em User Secrets ou variáveis de ambiente. Logs registram operação, ERP e status, nunca connection strings, senhas ou CNPJ.

## Validação operacional

1. Aplicar a migration no +Compras com `dotnet run --project backend/src/BlueprintOS.Api -- migrate`, após autorização operacional.
2. Consultar um fornecedor fictício existente no ERP e chamar o endpoint na direção `ErpParaMaisCompras` duas vezes; conferir uma linha em `Fornecedores` e duas tentativas auditadas, sem duplicidade.
3. Criar no +Compras um fornecedor de teste com identificador rastreável, exportar, consultar o ERP e atualizar somente um campo corporativo permitido; exportar novamente.
4. Guardar a correlação das respostas, IDs externos e consultas de conferência. Não remover o registro de teste sem registrar o procedimento.

A conectividade foi confirmada em 31/07/2026 para ambos os bancos. A leitura/escrita end-to-end permanece pendente da migration e da homologação do mapeamento da tabela ERP.
