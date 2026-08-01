Sprint: B2.1 — Validação Operacional e Sincronização de Fornecedores com ERP

Status:
CONCLUÍDA em 31/07/2026

Objetivo:
Validar a integração segura, observável e idempotente de fornecedores entre o +Compras e o ERP SOMA_DESENV, com resolução de adaptador por BU.

Entrega técnica desta etapa:
- Contratos `IErpFornecedorAdapter`, `IErpFornecedorAdapterResolver` e `ISincronizarFornecedorUseCase` na Application.
- `SomaDesenvolErpFornecedorAdapter` isolado na Infrastructure, com schema configurável, timeout, cancelamento e bloqueio explícito ao banco diferente de `SOMA_DESENV`.
- Vínculo externo composto por BU, ERP e identificador do fornecedor, com índice único filtrado.
- Importação ERP → +Compras, exportação +Compras → ERP e lote controlado (máximo de 100 itens).
- Status, origem, última sincronização, mensagem sanitizada e tabela de histórico de tentativas.
- Endpoints `POST /api/fornecedores/sincronizar` e `POST /api/fornecedores/sincronizar/lote`.
- Migration `202607310001_B21FornecedorSynchronization`, aplicada somente no banco de desenvolvimento +Compras, sem alterações destrutivas.

Validação automatizada:
- Build da solution: sucesso, 0 erros e 0 avisos.
- Testes unitários: 245 aprovados.
- Testes de integração: 3 aprovados.
- Cobertura nova: seleção por BU, mapeamento, importação, exportação, duplicidade, reexecução idempotente, falha sanitizada, cancelamento e isolamento.

Validação operacional:
- Conectividade real confirmada para +Compras e `SOMA_DESENV` em 31/07/2026.
- ERP_ID `277459` importado para um único fornecedor do +Compras e repetido sem duplicidade.
- Fornecedor fictício +Compras `59d3f811-23ce-4589-9c15-1679cea59afd` criado no ERP como `999999`; CNPJ atualizado de final `0195` para `0110` e confirmado diretamente no ERP.
- +Compras confirmou vínculos únicos e histórico persistido; as reexecuções retornaram status `Sincronizado`.

Limites:
- B3 não foi iniciada.
- Não há remoção automática de fornecedores.
- Escritas no ERP ficam disponíveis somente pelo adaptador selecionado e pelo endpoint operacional, sob confirmação do operador.
- Limitação ERP: `FORNECEDORES.FORNECEDOR` é FK para `CADASTRO_CLI_FOR.NOME_CLIFOR`; o nome não foi alterado para evitar operação destrutiva. A atualização validada usou CNPJ.
