Sprint: B2.1 — Validação Operacional e Sincronização de Fornecedores com ERP

Status:
EM EXECUÇÃO — implementação concluída; validação end-to-end pendente de aplicação autorizada das migrations

Objetivo:
Validar a integração segura, observável e idempotente de fornecedores entre o +Compras e o ERP SOMA_DESENV, com resolução de adaptador por BU.

Entrega técnica desta etapa:
- Contratos `IErpFornecedorAdapter`, `IErpFornecedorAdapterResolver` e `ISincronizarFornecedorUseCase` na Application.
- `SomaDesenvolErpFornecedorAdapter` isolado na Infrastructure, com schema configurável, timeout, cancelamento e bloqueio explícito ao banco diferente de `SOMA_DESENV`.
- Vínculo externo composto por BU, ERP e identificador do fornecedor, com índice único filtrado.
- Importação ERP → +Compras, exportação +Compras → ERP e lote controlado (máximo de 100 itens).
- Status, origem, última sincronização, mensagem sanitizada e tabela de histórico de tentativas.
- Endpoints `POST /api/fornecedores/sincronizar` e `POST /api/fornecedores/sincronizar/lote`.
- Migration `202607310001_B21FornecedorSynchronization`, sem alterações destrutivas.

Validação automatizada:
- Build da solution: sucesso, 0 erros e 0 avisos.
- Testes unitários: 245 aprovados.
- Testes de integração: 3 aprovados.
- Cobertura nova: seleção por BU, mapeamento, importação, exportação, duplicidade, reexecução idempotente, falha sanitizada, cancelamento e isolamento.

Validação operacional:
- Conectividade real confirmada fora do sandbox para +Compras e `SOMA_DESENV` em 31/07/2026.
- Migration e escrita de fornecedor ainda não executadas: requerem autorização explícita para mutar o banco compartilhado +Compras.
- Leitura e escrita real no ERP, persistência real e prova com fornecedor fictício permanecem pendentes até a aplicação da migration e a confirmação do mapeamento da tabela ERP.

Limites:
- B3 não foi iniciada.
- Não há remoção automática de fornecedores.
- Escritas no ERP ficam disponíveis somente pelo adaptador selecionado e pelo endpoint operacional, sob confirmação do operador.
