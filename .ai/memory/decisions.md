# decisions.md

O log canônico de ADRs do projeto é [.ai/DECISIONS.md](../DECISIONS.md). Este arquivo permanece reservado para memória operacional e não substitui ADRs.

## A10 — Adoção da Política de Autonomia da IA

Foi adotada oficialmente `AI_AUTONOMY_POLICY.md`, definindo autonomia total para melhorias internas compatíveis, proposta obrigatória para mudanças estruturais e proibição de decisões estratégicas sem aprovação humana.

## A12 — Catálogo oficial de oito fases e 56 Work Orders

Foi adotado o catálogo estratégico de oito fases e 56 Work Orders. A especificação não aprova execução nem altera fatos históricos; status Completed permanece condicionado a evidência de código, testes ou Git.

## ADR-0011 — Identidade temporária para desenvolvimento

Foi aprovada uma identidade temporária exclusivamente em `Development` para permitir persistência e vínculo de autoria de fornecedores antes do Entra ID. A camada de negócio dependerá de contrato substituível; produção continua bloqueada até H1/H2.

## ADR-0013 — Evolução operacional e inteligente do +Compras

Foi aceita a construção incremental: fluxos operacionais completos e manuais de fornecedores, itens e pedidos precedem inteligência avançada. O portal é a interface do +Compras; agentes atuam assistivamente por casos de uso e decisões críticas continuam sob confirmação humana.
# B2.1 — Fronteira de sincronização de fornecedores

- A sincronização usa contratos Application (`IErpFornecedorAdapter` e resolver por BU), mantendo o schema do ERP somente na Infrastructure.
- A chave de idempotência é `BU + ERP + ErpFornecedorId`; o +Compras mantém status, origem, última execução e histórico de tentativas.
- A importação atualiza somente campos corporativos explícitos (`Nome`, `Cnpj`, localidade e país); campos próprios do +Compras não são sobrescritos.
- A migration foi aplicada somente no +Compras e a escrita real foi executada somente com registros fictícios no `SOMA_DESENV`; nenhuma alteração de schema foi feita no ERP.
- O adaptador SOMA_DESENV trata `FORNECEDORES.FORNECEDOR` como chave externa imutável por FK e atualiza CNPJ como campo corporativo seguro.
- ADR-0015 reabriu a sprint para contrato canônico completo, sincronização temporal bidirecional, empate favorável ao +Compras, inativação lógica e auditoria append-only.

## B2.1 — Conclusão da sincronização de fornecedores

- B2.1 foi concluída em 01/08/2026: a sincronização permanece desacoplada por contrato e adaptador de BU, com importação, exportação, atualizações e inativações nos dois sentidos.
- A precedência temporal é normalizada em `America/Sao_Paulo` até o segundo; no empate, a decisão favorece o +Compras.
- Auditoria append-only preserva snapshots antes/depois, hashes, `CorrelationId`, histórico e idempotência. A geração de CLIFOR usa `LX_SEQUENCIAL`, inclusive sob concorrência.
- B2.1.1 foi concluída com o mapeamento canônico Linx → +Compras. B2.1.2 permanece Draft para validação estrutural futura; B2.2 permanece Draft como próxima evolução.

## Remoção do Docker do fluxo de desenvolvimento (03/08/2026)

- Docker foi removido do fluxo de desenvolvimento do BlueprintOS/+Compras: `Makefile`, `Dockerfile` e `docker-compose.yml` foram descontinuados (commits `601d937`, `7bf3bf4`).
- O ambiente oficial de Desenvolvimento Local passa a ser 100% sem containers: backend via `dotnet run`, frontend via `npm run dev`, banco SQL Server corporativo via VPN, orquestrados pelos scripts `start-dev.sh`/`stop-dev.sh`/`health-check.sh`.
- ADR-0018 (`.ai/DECISIONS.md`) foi atualizada para refletir esta decisão; não foi criada uma nova ADR, pois se trata de consolidação de uma decisão já aceita.
