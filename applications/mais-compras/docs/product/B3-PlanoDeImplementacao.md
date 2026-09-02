# B3 — Plano de Implementação (Item Fiscal)

## Status

Discovery **HOMOLOGADO pelo Product Owner** (01/09/2026 → 02/09/2026). Este plano organiza a implementação em blocos pequenos, testáveis e homologáveis individualmente, seguindo `applications/mais-compras/docs/architecture/CadFormFactory.md`, `ADR-0024` (`.ai/DECISIONS.md`), `ContratoFuncionalPreliminar-B3-ItemFiscal.md` e `B3-ConsolidacaoFinalDiscovery.md`. Cada bloco exige autorização explícita do Product Owner antes de começar — a aprovação de um bloco não autoriza o próximo.

**Status por bloco (homologação do Product Owner, 02/09/2026 — encerramento de sessão)**:

| Bloco | Status |
|---|---|
| Bloco 1 — Conta Contábil | **HOMOLOGADO** |
| Bloco 2 — Unidade de Medida | **HOMOLOGADO** |
| Bloco 3 — Item Fiscal | **HOMOLOGADO** |
| Bloco 4 — Referências por Fornecedor | **HOMOLOGADO** |
| Bloco 5A — Sincronização Linx → +Compras (leitura) | **NÃO INICIADO** — próximo ponto de retomada: pré-validação dos dados reais do Linx antes de implementar |
| Bloco 5B — Escrita governada (+Compras → ERP) | **NÃO INICIADO** — bloqueado até validação dedicada (ver seção do bloco) |
| Bloco 6 — RBAC completo, Gate Técnico e Homologação final | **NÃO INICIADO** |

Blocos 1–4 implementados, testados (unitário, integração, RBAC, E2E real contra `MAISCOMPRAS`) e homologados nesta mesma sessão de trabalho — ver commit correspondente para o detalhamento técnico completo (migrations, arquivos, testes) e os relatórios de cada bloco na conversa de implementação.

Princípio geral de sequenciamento: cadastros de apoio (Conta Contábil, Unidade) antes do Item Fiscal (que depende deles); Item Fiscal local antes de sincronização com o Linx (que é o ponto de maior risco); leitura antes de escrita governada no Linx (mesma maturidade já aplicada a Fornecedores — B2.1 leitura/escrita simples → B2.9 Adapter Linx, bloqueado até validação dedicada).

---

## Bloco 1 — Cadastro de apoio: Conta Contábil (leitura Linx)

**Objetivo**: disponibilizar no +Compras uma listagem local, somente leitura, de Contas Contábeis válidas (`CTB_CONTA_PLANO`), para uso como cadastro de apoio (seleção) no Item Fiscal e em futuros cadastros.

- **Backend**: entidade `ContaContabil` (Domain: Código, Descrição, Ativo); casos de uso de sincronização/consulta (Application); reader Linx read-only (Infrastructure, mesmo padrão de `SomaFilialReader`/`SomaCentroCustoReader`, perfil `linx-development`/`linx-production` via `LinxConnectionStringResolver`).
- **Banco**: migration nova tabela `ContaContabil` no +Compras (espelho local). **Nenhuma alteração no Linx.**
- **Integração Linx**: leitura de `CTB_CONTA_PLANO` (`CONTA_CONTABIL`, `DESC_CONTA`, `INATIVA`) — mesmo padrão dos demais leitores read-only já existentes (`SomaFilialReader` etc.), sem escrita.
- **Frontend/UX**: tela simples de listagem + ação "Sincronizar com Linx" (reaproveitar padrão visual já usado em Filiais/Centros de Custo, se existir componente compartilhado).
- **RBAC**: reavaliar catálogo existente antes de criar permissão nova (candidato: `Sistema.Gerenciar` ou uma nova `ContaContabil.Sincronizar`/`Visualizar` — decidir no início do bloco, conforme CadFormFactory §7).
- **Testes**: unit (mapeamento Linx → local), integração (sincronização real contra `SOMA_DESENV`), regressão.
- **Critérios de aceite**: sincronização real comprovada (não só `HTTP 200`); contas inativas sinalizadas corretamente; build limpo.
- **Dependências**: nenhuma — pode começar imediatamente.

## Bloco 2 — Cadastro de apoio: Unidade (leitura Linx)

**Objetivo**: mesma estrutura do Bloco 1, para `UNIDADES` (unidade de medida).

- **Backend/Banco/Integração/Frontend/RBAC/Testes**: mesmo padrão do Bloco 1, aplicado a `UNIDADES` (`UNIDADE`, `DESC_UNIDADE`).
- **Pré-requisito interno deste bloco**: comprovar estrutura real de `UNIDADES` em `SOMA_DESENV` (GAP não bloqueante já identificado no discovery — primeira tarefa do bloco, rápida, mesmo mecanismo de schema discovery já usado).
- **Critérios de aceite**: mesmos do Bloco 1, aplicados a Unidade.
- **Dependências**: nenhuma — pode rodar em paralelo ao Bloco 1.

## Bloco 3 — Item Fiscal: domínio, persistência e CRUD local

**Objetivo**: cadastro funcional de Item Fiscal no +Compras — código, descrição (granularidade livre, decisão da área de Compras), unidade (seleção, Bloco 2), conta contábil (seleção, obrigatória, Bloco 1), situação (ativo/inativo). **Sem integração Linx ainda** — autoridade compartilhada/sincronização é o Bloco 5.

- **Backend**: entidade `ItemFiscal` (Domain); casos de uso Criar/Editar/Inativar/Consultar (Application); mapeamento EF Core (Infrastructure); API REST (Api).
- **Banco**: migration nova tabela `ItemFiscal` (+Compras), com referência lógica às tabelas de apoio dos Blocos 1/2.
- **Integração Linx**: nenhuma neste bloco.
- **Frontend/UX**: tela "Dados Gerais" (aba única neste bloco — a aba de Referências por Fornecedor entra no Bloco 4), Design System, validações frontend + backend.
- **RBAC**: `ItemFiscal.Visualizar`/`Criar`/`Editar`/`Inativar` (novas — nenhum domínio existente cobre Item Fiscal); confirmar nomenclatura com o catálogo real antes de codar.
- **Testes**: unit (Conta Contábil obrigatória, duplicidade local por código, granularidade livre não bloqueada por regra inventada); API (401/403/payload válido/inválido/duplicidade/tentativa de bypass de validação de frontend); frontend (render, required, RBAC visual, loading/erro/sucesso).
- **Critérios de aceite**: CRUD completo funcional isolado no +Compras; gate técnico interno aprovado.
- **Dependências**: Blocos 1 e 2 (selects precisam de dados reais).

## Bloco 4 — Referências por Fornecedor (local)

**Objetivo**: gerenciar a coleção Fornecedor × Código do Fornecedor vinculada ao Item Fiscal (espelho local de `ITEM_FISCAL_REF_FORNECEDOR`), ainda sem sincronizar com o Linx.

- **Backend**: entidade filha (Fornecedor, Código do Item no Fornecedor); casos de uso adicionar/editar/remover, vinculados a um Fornecedor já existente no +Compras.
- **Banco**: migration tabela filha.
- **Integração Linx**: nenhuma neste bloco (leitura/sincronização real de `ITEM_FISCAL_REF_FORNECEDOR` fica para uma extensão futura do Bloco 5, e o uso em XML NF-e/NFS-e é dependência futura fora da B3).
- **Frontend/UX**: aba "Referências por Fornecedor" dentro da tela de Item Fiscal (grid — adicionar/editar/remover), conforme organização por abas já homologada.
- **RBAC**: reaproveitar `ItemFiscal.Editar` (parte do mesmo cadastro) — evitar permissão nova sem necessidade comprovada.
- **Testes**: unit, API, frontend (grid, fornecedor precisa existir, duplicidade fornecedor+item).
- **Critérios de aceite**: usuário gerencia referências locais vinculadas a Item Fiscal e Fornecedor existentes.
- **Dependências**: Bloco 3; cadastro de Fornecedor (já implementado).

## Bloco 5 — Sincronização Linx ↔ +Compras do Item Fiscal

**Objetivo**: aplicar a autoridade compartilhada homologada (Last Write Wins via `DATA_PARA_TRANSFERENCIA`, fallback Linx prevalece por `ADR-0024`). Dividido em dois sub-blocos por risco.

### 5A — Leitura/importação (ERP → +Compras)

- **Backend**: reader read-only de `CADASTRO_ITEM_FISCAL` (mesmo padrão `SomaFornecedorReader`); comparação de `DATA_PARA_TRANSFERENCIA` para decidir o lado mais recente; aplicação da regra de inativação (Linx inativa → +Compras inativa); log/auditoria de conflito detectado.
- **Banco**: nenhuma alteração no Linx; possível campo de auditoria/histórico de sincronização no +Compras (mesmo padrão de `SincronizacaoFornecedor`).
- **Integração Linx**: leitura paginada/lotes de `CADASTRO_ITEM_FISCAL`, mesmo padrão operacional já validado em Fornecedores (B2.1.3).
- **Frontend**: tela de monitoramento de sincronização (reaproveitar padrão já existente de Monitor de Integrações, se aplicável).
- **RBAC**: reaproveitar padrão de sincronização já existente (`Fornecedor.Editar`/`Sistema.Gerenciar` equivalente para Item Fiscal).
- **Testes**: integração real contra `SOMA_DESENV`; cenários de conflito (ambos alterados, timestamps próximos, Linx indisponível — nunca falso sucesso).
- **Critérios de aceite**: sincronização de leitura validada end-to-end contra `SOMA_DESENV` real.
- **Dependências**: Bloco 3.

### 5B — Escrita governada (+Compras → ERP) — **maior risco, bloco separado e mais tardio**

- Segue integralmente o Governed Write Stack já existente (`ActionProposal` → Policy Engine → Recovery Package → execução → Post-Write Validation → auditoria), mesmo padrão usado no ajuste de grade PED e desenhado (ainda bloqueado) para o Adapter Linx de Fornecedor (B2.9).
- **Explicitamente BLOQUEADO até existir uma sessão dedicada de validação com especialista Visual Linx** — mesmo precedente do Adapter Linx de Fornecedor. Não presumir liberação automática só porque o discovery foi homologado.
- **Critérios de aceite**: só após aprovação humana explícita e Gate próprio (análogo ao Gate do B2.9).
- **Dependências**: 5A.

## Bloco 6 — RBAC completo, Gate Técnico e Homologação

**Objetivo**: fechar o checklist do `CadFormFactory.md` §12 e homologar a B3 (excluindo 5B, que segue seu próprio Gate).

- Revisão final de RBAC frontend/backend (403 real, sem ação visível sem permissão).
- Testes de falha de integração (ERP indisponível, timeout, conflito, duplicidade — nunca falso sucesso).
- Build limpo (backend + frontend), migrations sem pendência, console/network sem erro.
- Gate técnico registrado + homologação do Product Owner.
- **Dependências**: Blocos 1–4 e 5A (5B pode ficar pendente, registrado, sem bloquear este Gate — mesmo tratamento dado ao B2.9 em relação ao Gate Final da Onda 1).

---

## Próximo ponto de retomada — Bloco 5A

Blocos 1–4 **HOMOLOGADOS** (ver tabela de status acima). Próxima atividade autorizada para a próxima sessão:

**Bloco 5A — pré-validação dos dados reais do Linx antes da implementação da sincronização.**

Antes de codar o Bloco 5A (leitura/importação `CADASTRO_ITEM_FISCAL`, comparação via `DATA_PARA_TRANSFERENCIA`, regra de inativação Linx→+Compras), validar contra dados reais de `SOMA_DESENV`/`SOMA_PROD` (conforme disponibilidade):
- Volume real de `CADASTRO_ITEM_FISCAL` (paginação/lotes necessários, mesmo cuidado já validado em Fornecedores B2.1.3).
- Casos reais de `DATA_PARA_TRANSFERENCIA` nula/inconsistente, se existirem.
- Confirmar que a estrutura de `ITEM_FISCAL_REF_FORNECEDOR` (comprovada no discovery) permanece estável para uma eventual extensão futura de sincronização dessa coleção.
- Reavaliar, com dados reais, o risco assumido na unicidade `(Fornecedor, CodigoItemFornecedor)` do Bloco 4 (não comprovada em Linx) — verificar se há duplicidade real que exigiria tratamento manual antes de importar.

**Explicitamente NÃO iniciar** nesta próxima sessão sem essa pré-validação: implementação do Bloco 5A, Bloco 5B, XML NF-e/NFS-e, Orçamento, Pedido, Entrada Fiscal, ou o Gate final (Bloco 6).
