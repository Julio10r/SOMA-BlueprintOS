Sprint: B2 — Descoberta Inteligente de Fornecedores

Status:
CONCLUÍDA

Objetivo:
Consultar o ERP SOMA_DESENV para localizar fornecedores relacionados a item, descrição ou categoria, calcular score explicável e persistir todas as descobertas no +Compras.

Entrega:
- `DescobrirFornecedoresUseCase` com identidade temporária e persistência por descoberta.
- `FornecedorDescoberto` e `ScoreFornecedor` no domínio.
- `ErpFornecedorDiscoveryRepository` somente leitura, com proteção para usar exclusivamente o banco `SOMA_DESENV`.
- Endpoints `POST /api/fornecedores/descobrir`, `GET /api/fornecedores/descobertas` e `GET /api/fornecedores/descobertas/{id}`.
- Migration `202607300002_B2FornecedorDiscovery` somente para o banco +Compras.

Regra de score:
- Item exato: 100
- Família: 80
- Categoria: 60
- Histórico: 40

Validação:
- Build da solution: 0 erros e 0 avisos.
- Testes unitários: 240 aprovados.
- Testes de integração: 2 aprovados, incluindo persistência/isolation no +Compras em memória.
- O SQL Server ERP `SOMA_DESENV` não estava acessível neste ambiente de execução (timeout); o adaptador está preparado para validação operacional com `ErpConnection` configurada.

Limites:
- O fluxo B2 é somente leitura no ERP; não há escrita aplicável em `SOMA_DESENV`.
- A ADR de identidade temporária permanece válida.
- A Sprint B3 não foi iniciada.
