# Work Orders — Único Local Canônico

`.ai/work-orders/` é o único diretório de Work Orders do SOMA BlueprintOS. Uma Work Order transforma uma demanda aprovada em escopo rastreável, critérios de aceite e evidências de conclusão; ela não substitui o backlog, o planejamento ou a aprovação do Product Owner.

**`.ai/tasks/` e `.ai/workorders/` (sem hífen) não existem mais.** Nunca recriar esses diretórios. Cada Work Order existe em exatamente um arquivo, em exatamente um dos três subdiretórios abaixo.

## Estrutura

| Pasta | Significado | Critério |
|---|---|---|
| [`active/`](active/) | Em execução agora | Sprint aprovada e em andamento, registrada em `.ai/CURRENT_SPRINT.md` |
| [`backlog/`](backlog/) | Planejado, aprovado (não iniciado), parcial ou não comprovado | Catálogo estratégico completo (fases A–H) — ver [`backlog/README.md`](backlog/README.md) |
| [`completed/`](completed/) | Concluído | Evidência de build, testes, commit e push já registrada |

Atualmente `active/` está vazio: não há nenhuma sprint funcional em andamento (ver `.ai/CURRENT_SPRINT.md`).

## Regra de unicidade

Cada Work Order existe apenas uma vez, no diretório correspondente ao seu status real. Quando o status de uma Work Order muda (ex.: de `backlog/` para `active/`, ou de `active/` para `completed/`), o arquivo é **movido**, nunca copiado — não deve haver duas versões do mesmo código de Work Order em diretórios diferentes.

## Relação com os demais artefatos

- [`../templates/`](../templates/README.md) contém os modelos reutilizáveis. Copie o modelo adequado para `backlog/` (ou diretamente para `active/`, se já aprovada) antes de preenchê-lo.
- [WORKFLOW.md](../WORKFLOW.md), [VISION.md](../VISION.md), [PROJECT_STATE.md](../PROJECT_STATE.md) e [CURRENT_SPRINT.md](../CURRENT_SPRINT.md) são as fontes canônicas que devem ser lidas antes da execução.
- [BACKLOG.md](../BACKLOG.md) é a visão consolidada com objetivos e evidências de todas as 56 Work Orders estratégicas.

## Convenção de nomenclatura

Use apenas nomes em `PascalCase` sem espaços e a extensão `.md`.

| Tipo | Formato | Uso |
|---|---|---|
| Work Order de sprint | `A13-Descricao.md` | sprint ou entrega planejada identificada por fase e número |
| Work Order de sprint | `A14-Descricao.md` | próxima entrega sequencial da mesma fase |
| Work Order complementar | `B2.1-Descricao.md` | consolidação curta vinculada a uma sprint concluída, sem reutilizar seu identificador |
| Work Order complementar sequencial | `B2.2-Descricao.md` | próxima entrega planejada e dependente da conclusão da entrega complementar anterior |
| Épico | `EPIC-01-Nome.md` | iniciativa composta por múltiplas entregas |
| Refatoração | `R01-Descricao.md` | mudança estrutural sem alteração de comportamento aprovada |
| Hotfix | `HF01-Descricao.md` | correção urgente de incidente |
| Spike | `SP01-Descricao.md` | pesquisa técnica com decisão explícita |

O identificador é estável depois da criação. A descrição deve ser curta, legível e refletir o objetivo aprovado. Não reutilize um identificador, mesmo que a Work Order seja bloqueada ou cancelada.

## Ciclo de vida

1. A demanda é registrada no backlog e planejada conforme [WORKFLOW.md](../WORKFLOW.md). O arquivo nasce em `backlog/`.
2. O responsável escolhe um template em [`../templates/`](../templates/README.md), cria o arquivo em `backlog/` e preenche o escopo sem inventar requisitos.
3. A Work Order permanece `Draft` até obter objetivo, escopo, dependências, critérios de aceite e executor.
4. Após aprovação do Product Owner e registro em `CURRENT_SPRINT.md`, seu status passa a `Approved`/`In Progress` e o arquivo é movido para `active/`. Apenas uma Work Order pode estar aprovada/em execução por vez.
5. A execução ocorre somente dentro do escopo aprovado, com validações, revisão, documentação e Git Flow exigidos pelo projeto.
6. A Work Order é concluída apenas com evidências de build, testes, atualização documental, commit e push; o arquivo é então movido para `completed/`. Caso contrário, permanece em `active/` como `In Progress` ou `Blocked`.

Não criar Work Orders fora desta estrutura sem a aprovação e o contexto exigidos pelo workflow oficial.
