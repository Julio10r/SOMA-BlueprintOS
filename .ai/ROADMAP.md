# ROADMAP.md

> Roadmap oficial do SOMA BlueprintOS / +Compras, replanejado para o **MVP 1.0** segundo a estratégia **Frontend First**. Não descreve sprints — para detalhe de sprint atual, ver `.ai/CURRENT_SPRINT.md`; para histórico de sprints concluídas, ver `.ai/memory/completed_sprints.md`.
>
> A arquitetura definida em `ARCHITECTURE.md` e `ENGINEERING_BLUEPRINT.md` permanece inalterada por este replanejamento. O que muda é exclusivamente a ordem e o agrupamento de entrega.
>
> O catálogo estratégico histórico de oito fases e 56 Work Orders permanece em `.ai/work-orders/backlog/README.md` e `.ai/BACKLOG.md`, agora reclassificado por Onda do MVP 1.0 ou por MVP 1.1 — ver a seção "Reclassificação oficial" em `BACKLOG.md`.

## Estratégia oficial: Frontend First

Consolidada como estratégia definitiva de desenvolvimento do projeto. Toda funcionalidade segue obrigatoriamente esta sequência:

```
Ideia
  ↓
+Compras Funcional
  ↓
Validação de negócio
  ↓
UX
  ↓
Mock navegável
  ↓
Blueprint do Banco
  ↓
APIs
  ↓
Integrações
  ↓
Implementação
  ↓
Testes
  ↓
Homologação
```

**Nenhuma funcionalidade pode ser implementada antes da aprovação do Mock navegável.** `+Compras Funcional` (o que o sistema faz) e `+Compras UX` (como o usuário utiliza o sistema) precedem o mock e são atualizados antes de qualquer código — ver `.ai/DOCUMENTATION_STRATEGY.md` para a definição desses dois artefatos e sua distinção da Arquitetura Técnica (como o sistema foi construído).

Em nível de Onda, o mesmo fluxo se aplica de forma agregada: nenhuma Onda avança sem que a anterior tenha entregue seu marco e sido aprovada por seu Gate (ver "Gates de aprovação" abaixo). Este roadmap não contém prazo total de projeto, datas absolutas ou cronograma por calendário — apenas duração planejada por onda, marcos, dependências, critérios de aceite e o rastreamento de datas descrito em "Política de acompanhamento das Ondas". Se uma onda ultrapassar sua duração planejada, considera-se atraso daquela onda, não do projeto como um todo.

### Processo de implementação (ciclo oficial)

```
+Compras Funcional → +Compras UX → Blueprint Banco → APIs → Integrações → Implementação → Testes → Homologação
```

Este é o fluxo oficial de qualquer entrega no projeto, do nascimento da especificação até a homologação.

## Ondas do MVP 1.0

### Onda 1 — Fundação Funcional

- **Duração planejada:** 12 dias.
- **Objetivo:** construir a fundação funcional do produto.
- **Inclui:** frontend navegável completo; Administração (Unidade de Negócio, Usuários, Perfis, Permissões, Identity Providers, Configuração ERP, Workflow, Aprovação, Controle Orçamentário, Configurações, Feature Flags, Parâmetros); UX validada; blueprint completo do banco; estrutura administrativa.
- **Dependências:** nenhuma além da fundação arquitetural já entregue (Fase 0 concluída — ver seção "Fundação arquitetural" abaixo).
- **Critério de aceite:** produto navegável, com Administração operável e blueprint de banco completo e aprovado, antes de iniciar a Onda 2.
- **Entrega:** produto navegável.

### Onda 2 — Cadastros

- **Duração planejada:** 15 dias.
- **Objetivo:** cadastros completos com sincronização ERP.
- **Inclui:** fornecedores, item fiscal (B3 — cadastro único, comprovado pelo Discovery que não há catálogos mestres separados de material e serviço no Linx), categorias, compradores, centros de custo — todos com sincronização ERP.
- **Dependências:** Onda 1 concluída (Administração e blueprint de banco).
- **Critério de aceite:** todos os cadastros operáveis pelo frontend, sincronizados com o ERP conforme a Estratégia de Integração (ver abaixo).
- **Entrega:** cadastros completos.

### Onda 3 — Processo de Compras

- **Duração planejada:** 15 dias.
- **Objetivo:** processo completo de Compras.
- **Inclui:** solicitação, cotação, negociação por IA, workflow, controle orçamentário, aprovação, pedido.
- **Dependências:** Onda 2 concluída (cadastros de fornecedor, item, comprador e centro de custo).
- **Critério de aceite:** ciclo completo solicitação → pedido operável de ponta a ponta pelo frontend, com aprovação e controle orçamentário funcionando.
- **Entrega:** processo completo de Compras.

### Onda 4 — Integrações Operacionais

- **Duração planejada:** 12 dias.
- **Objetivo:** integrações operacionais.
- **Inclui:** ERP, Nota Fiscal, Pagamento.
- **Dependências:** Onda 3 concluída (pedido existente para vincular nota fiscal e pagamento).
- **Critério de aceite:** integrações operando de ponta a ponta, respeitando a Estratégia de Integração com ERP (ver abaixo) — nenhuma alteração estrutural no ERP.
- **Entrega:** integrações operacionais completas.

### Onda 5 — Go Live

- **Duração planejada:** 10 dias.
- **Objetivo:** Go Live.
- **Inclui:** homologação, observabilidade, performance, segurança, estabilização.
- **Dependências:** Ondas 1 a 4 concluídas.
- **Critério de aceite:** homologação aprovada, observabilidade e segurança mínimas operando, performance validada.
- **Entrega:** produto em produção.

## Gates de aprovação

Nenhuma Onda pode iniciar sem aprovação formal do Gate da Onda anterior pelo Product Owner.

| Onda | Gate |
|---|---|
| Onda 1 — Fundação Funcional | Frontend navegável aprovado |
| Onda 2 — Cadastros | Cadastros homologados |
| Onda 3 — Processo de Compras | Processo completo ponta a ponta funcionando |
| Onda 4 — Integrações Operacionais | Integrações ERP/Fiscal/Pagamentos homologadas |
| Onda 5 — Go Live | Go Live aprovado |

## Política de acompanhamento das Ondas

Cada Onda é rastreada com os seguintes campos, a partir do início de sua execução:

- **Data Planejada** — baseline do projeto; definida uma única vez e **nunca alterada** depois de registrada.
- **Data Real** — registrada ao término efetivo da Onda.
- **Data Replanejada** — recalculada para as Ondas restantes sempre que uma Onda termina, com base no desvio observado.
- **Status** — não iniciada / em andamento / concluída / atrasada.
- **Gate de Aprovação** — aprovado / pendente, conforme a tabela acima.

A Data Planejada de cada Onda é preenchida somente quando sua execução for formalmente aprovada (não antes, e não neste documento de forma retroativa), preservando a regra de não antecipar cronograma por calendário. Esta política se aplica a todos os roadmaps executivos futuros do projeto.

## Escopo do Roadmap Executivo

O Roadmap Executivo (público Diretoria, ver `docs/Executive Report.md` e `docs/executive/BlueprintOS_Executive_Blueprint.md`) acompanha exclusivamente: Ondas, Marcos, Datas (Planejada/Real/Replanejada), Status e Gates. Ele **não detalha Work Orders individuais** — estas permanecem ferramenta exclusiva de engenharia, rastreadas em `.ai/BACKLOG.md` e `.ai/work-orders/`.

## Definição oficial de "Pronto"

Uma funcionalidade só é considerada concluída quando possuir, cumulativamente:

- Mock aprovado
- `+Compras Funcional` atualizado
- `+Compras UX` atualizado
- Banco definido
- APIs definidas
- Integrações definidas
- Workflow definido
- IA definida (quando aplicável)
- Critérios de aceite definidos
- Implementação concluída
- Testes aprovados
- Homologação realizada

**Implementação isoladamente não caracteriza conclusão.** Esta definição de "Pronto" é específica do produto/Onda e complementa, sem substituir, a Definition of Done técnica de `WORKFLOW.md` §19 (`context/definition-of-done.md`).

## Fundação arquitetural (concluída, não faz parte das Ondas)

A Fase 0 — Fundação (arquitetura, padrões, workflow, Publication Engine, Work Orders, ADR-0019) está concluída e não é reaberta por este replanejamento; ver `.ai/PROJECT_STATE.md` para evidências. As Ondas do MVP 1.0 partem dessa fundação já validada.

## Versão 1.1 (pós-MVP 1.0)

Movidos oficialmente para a versão 1.1, fora do escopo do MVP 1.0:

- ESG.
- Portal de Fornecedores.
- Marketplace.
- Analytics avançado.
- Previsão de Demanda.
- Previsão de Preços.
- Jurídico.
- Compliance.
- Gestão de Riscos.

A arquitetura permanece preparada para essas capacidades (contratos, camadas e módulos já contemplam sua futura implementação); apenas o roadmap de entrega muda.

## Administração (Onda 1)

A Administração é implementada já na Onda 1, e não como capacidade tardia. Inclui: Unidade de Negócio, Usuários, Perfis, Permissões, Identity Providers, Configuração ERP, Workflow, Aprovação, Controle Orçamentário, Configurações, Feature Flags e Parâmetros. Toda configuração é preparada para múltiplas Unidades de Negócio desde a Onda 1; a primeira implantação pode operar com uma única `UnidadeNegocioId = SOMA`, sem comprometer a arquitetura multiempresa (ver `ARCHITECTURE.md` §16).

## Estratégia de banco de dados

Durante o desenvolvimento das Ondas 1 a 4, tabelas podem ser recriadas, migrations podem ser refeitas, FKs podem ser alteradas e a estrutura pode evoluir continuamente — não há compromisso de estabilidade de schema antes do Go Live. Antes da Onda 5 (Go Live), toda estrutura integrada ao ERP deve reproduzir exatamente o ERP como modelo estrutural canônico (nomes, tipos, precisão, escala, tamanho, collate, PK, FK, índices e regras de negócio compatíveis) — nunca criar uma estrutura própria diferente quando já existir equivalente no ERP.

## Estratégia de integração com o ERP

O ERP nunca sofre alterações estruturais: são proibidos `CREATE`, `ALTER`, `DROP`, triggers, CDC, Change Tracking, criação de índices ou qualquer alteração física no ERP. A única escrita permitida é através das tabelas e contratos oficiais já existentes. Antes de implementar qualquer integração (Onda 4) deve existir uma auditoria técnica da tabela ERP envolvida, avaliando estratégia de sincronização, desempenho, custo, impacto, riscos e recomendação técnica — a integração só é implementada depois dessa auditoria.

## Observações

- As Ondas são sequenciais por dependência funcional (frontend antes de banco final, cadastros antes de processo de compras, processo antes de integrações, tudo antes de Go Live).
- Este roadmap não substitui o planejamento de sprint (ver `WORKFLOW.md` §5 e §17) nem o catálogo estratégico de 56 Work Orders, que permanece a referência de escopo de longo prazo reclassificada por Onda/versão em `BACKLOG.md`.
- Este roadmap deve ser revisado a cada mudança relevante de escopo aprovada pelo Product Owner.
