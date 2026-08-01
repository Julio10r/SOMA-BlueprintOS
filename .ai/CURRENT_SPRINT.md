Sprint: B2.1 — Validação Operacional e Sincronização de Fornecedores com ERP

Status:
VALIDAÇÃO TÉCNICA COMPLETA em 01/08/2026 — sprint permanece aberta para revisão formal do relatório

Subetapa B2.1.1:
IMPLEMENTADA E VALIDADA — mapeamento canônico ERP → +Compras completo; encerramento formal depende da revisão do relatório.

Objetivo:
Completar o contrato canônico e validar a sincronização bidirecional completa de fornecedores entre o +Compras e o ERP configurado por BU, incluindo atualização, inativação, regra temporal, empate favorável ao +Compras, auditoria imutável e idempotência.

Entrega técnica desta etapa:
- Contratos `IErpFornecedorAdapter`, `IErpFornecedorAdapterResolver` e `ISincronizarFornecedorUseCase` na Application.
- `SomaDesenvolErpFornecedorAdapter` isolado na Infrastructure, com schema configurável, timeout, cancelamento e bloqueio explícito ao banco diferente de `SOMA_DESENV`.
- Vínculo externo composto por BU, ERP e identificador do fornecedor, com índice único filtrado.
- Importação ERP → +Compras, exportação +Compras → ERP e lote controlado (máximo de 100 itens).
- Status, origem, última sincronização, mensagem sanitizada e tabela de histórico de tentativas.
- Endpoints `POST /api/fornecedores/sincronizar` e `POST /api/fornecedores/sincronizar/lote`.
- Migration `202607310001_B21FornecedorSynchronization`, aplicada somente no banco de desenvolvimento +Compras, sem alterações destrutivas.

- Build da solution: sucesso, 0 erros e 0 avisos.
- Testes unitários: 249 aprovados.
- Testes de integração: 3 aprovados.
- Cobertura nova: seleção por BU, mapeamento, importação, exportação, duplicidade, reexecução idempotente, falha sanitizada, cancelamento e isolamento.

Validação operacional concluída nesta reabertura:
- Conectividade real confirmada para +Compras e `SOMA_DESENV` em 01/08/2026.
- ERP_ID `277459` importado para um único fornecedor do +Compras e repetido sem duplicidade.
- Fornecedor fictício `8a86809e-b123-493d-8bb7-b855527e98a1` exportado como ERP_ID `900001`; atualização de CNPJ para final `0110` confirmada por consulta posterior.
- Inativação +Compras→ERP e ERP→+Compras confirmada; estado final no +Compras: `Inativo`, CNPJ final `0110` e vínculo `900001`.
- Auditoria consultada por endpoint somente leitura: 15 eventos, 0 falhas, 15 correlações e 11 eventos com snapshots antes/depois.
- `CLIFOR` real `315501` foi retornado por `LX_SEQUENCIAL` e confirmado em `FORNECEDORES.COD_FORNECEDOR/CLIFOR` e `CADASTRO_CLI_FOR.COD_CLIFOR/CLIFOR`; a exportação foi reconciliada sem gerar novo código.
- Criações concorrentes reais retornaram `315502` e `315503`, distintos, confirmados nas duas tabelas e sem duplicidade.
- O timestamp real foi validado em `CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA`, com fallback/espelho em `FORNECEDORES.DATA_PARA_TRANSFERENCIA`; a leitura normaliza para `America/Sao_Paulo` com precisão de segundo.
- O registro inválido `00000*` foi inativado, sem exclusão física, com correlação `b21-invalid-clifor-inactivate-final-erp`; estado final `INATIVO=True`.
- Reexecução, atualização e inativação do fornecedor `315501` reutilizaram o mesmo identificador; auditoria registrou as correlações e os snapshots.
- B2.1.1 importou o fornecedor fictício `315504` com razão social, fantasia, endereço, contatos, banco, dados fiscais/comerciais, indicadores e hash; a repetição preservou `Versao=4` e não gerou alteração.

Escopo desta reabertura:
- ampliar o agregado e DTOs para identificação, endereço, contato, fiscal, bancário, comercial, classificação, BU e estado de sincronização;
- criar contrato genérico de integração, comparação temporal em `America/Sao_Paulo` com precisão de segundo e operações de inativação;
- substituir o histórico mínimo por auditoria imutável com antes/depois, hashes, decisão, conflito, tentativa e reprocessamento;
- criar migration complementar somente no +Compras e executar validação end-to-end com dados fictícios.

Limites:
- B3 não foi iniciada e permanece fora do escopo.
- Não há remoção automática de fornecedores.
- A procedure `LX_AZZ_GERAR_FORNECEDOR_LINX` é apenas referência funcional; não será chamada nem copiada.
- O ERP SOMA_DESENV mantém a limitação de FK entre `FORNECEDORES.FORNECEDOR` e `CADASTRO_CLI_FOR.NOME_CLIFOR`; o adaptador deve preservar chaves físicas e atualizar somente campos suportados.

Próxima sprint planejada:
- B2.1.2 — Alinhamento Estrutural ERP Linx x +Compras, em `Draft`, como próxima pendência da B2.1. A atividade somente compara e planeja correções de tipo, tamanho, nullable, collation e validações; não cria migration nesta etapa.
- B2.2 — Enriquecimento Cadastral de Fornecedores por CNPJ permanece em `Draft`, após B2.1/B2.1.2. A consulta externa será apenas sugestão revisável; não haverá atualização automática do cadastro ou do ERP.
