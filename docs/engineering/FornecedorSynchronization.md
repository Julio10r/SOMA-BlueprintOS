# Sincronização de fornecedores — B2.1

## Arquitetura

O caso de uso depende apenas de contratos da Application. `IErpFornecedorAdapterResolver` seleciona o adaptador autorizado para a BU; `SomaDesenvolErpFornecedorAdapter` é a implementação inicial isolada em Infrastructure. O domínio não conhece tabelas, colunas ou connection strings do ERP.

O vínculo externo é `BusinessUnit + ErpSistema + ErpFornecedorId`, com índice único filtrado. O contrato canônico cobre identificação, endereço, contato, dados fiscais/bancários/comerciais, classificação e indicadores de fornecimento. A tabela `FornecedoresSincronizacoes` é append-only e registra direção, origem/destino, decisão temporal, timestamps originais/normalizados, antes/depois, hashes, tentativa, duração, correlação e erro sanitizado. Não existe exclusão automática.

## Operação

- `POST /api/fornecedores/sincronizar` importa por identificador ERP ou exporta por `FornecedorId`.
- `POST /api/fornecedores/sincronizar/lote` exporta uma lista controlada, limitada a 100 itens.
- `OperacaoFornecedor.Inativar` realiza inativação lógica e pode ser reexecutada sem duplicar fornecedor ou escrita.
- A comparação temporal normaliza ambos os lados para `America/Sao_Paulo`, com precisão até o segundo. ERP mais recente vence; +Compras mais recente vence; empate divergente favorece +Compras; empate igual não escreve.
- A repetição consulta o vínculo externo e mantém o mesmo fornecedor; quando os dados não mudaram, não repete a atualização.
- Cancelamento propaga `CancellationToken`; comandos SQL têm timeout configurável em `ErpIntegration:TimeoutSeconds`.

## Configuração

`ConnectionStrings:MaisComprasConnection` é o banco próprio. `ConnectionStrings:ErpConnection` deve apontar exclusivamente para `SOMA_DESENV`. A BU autoriza seu ERP em `ErpIntegration:BusinessUnits:{BU}:ErpSistema`; o schema/tabela inicial usa `ErpIntegration:SomaDesenvol:Schema` e `Table`.

Credenciais devem permanecer em User Secrets ou variáveis de ambiente. Logs registram operação, ERP e status, nunca connection strings, senhas ou CNPJ.

## Validação operacional

1. Aplicar a migration no +Compras com `dotnet run --project backend/src/BlueprintOS.Api -- migrate`, após autorização operacional.
2. Consultar um fornecedor fictício existente no ERP e chamar o endpoint na direção `ErpParaMaisCompras` duas vezes; conferir uma linha em `Fornecedores` e duas tentativas auditadas, sem duplicidade.
3. Criar no +Compras um fornecedor de teste com identificador rastreável, exportar, consultar o ERP e atualizar somente um campo corporativo permitido; exportar novamente.
4. Guardar a correlação das respostas, IDs externos e consultas de conferência. Não remover o registro de teste sem registrar o procedimento.

A validação anterior foi revogada em 01/08/2026. A reabertura já confirmou, com o fornecedor fictício +Compras `8a86809e-b123-493d-8bb7-b855527e98a1`/ERP `900001`, importação, exportação, alteração de CNPJ, inativação nos dois sentidos, reexecução idempotente e auditoria (15 eventos, 0 falhas). A migration complementar `202608010001_B21CanonicalSupplierSynchronization` foi aplicada somente no +Compras dev; a procedure `LX_AZZ_GERAR_FORNECEDOR_LINX` não é chamada. A sprint permanece reaberta para revisão formal e confirmação dos cenários temporais/empate com dados ERP que exponham timestamp.
