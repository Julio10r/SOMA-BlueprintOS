# context/planner.md

> Escopo: o Planner é a implementação técnica da etapa "Planejamento" do fluxo oficial (WORKFLOW.md §3, §5) e da responsabilidade de planejamento do Maestro (AI_TEAM.md). Este documento descreve como uma demanda se transforma em um plano executável.

## Planejamento

Receber uma requisição (de um usuário, de um Workflow, ou de outro agente) e produzir um plano: uma sequência ordenada de tarefas necessárias para atender à requisição, com dependências entre elas identificadas.

## Decomposição

Quebrar cada etapa do plano em tarefas executáveis por um agente específico. Cada tarefa decomposta corresponde, em nível de processo, a um Task Packet — ver a estrutura mínima já definida em WORKFLOW.md §7 (ID, título, descrição, executor, entradas, saídas, critérios de aceite, testes obrigatórios). O Planner não redefine essa estrutura, apenas a produz automaticamente a partir do plano.

## Execução

Após a decomposição, o Planner entrega cada tarefa ao Runtime (ver [runtime.md](./runtime.md)), que seleciona e aciona o agente apropriado. O Planner não executa tarefas diretamente — apenas planeja, decompõe e acompanha.

## Validação

Ao receber o resultado de uma tarefa executada, o Planner valida o resultado contra os critérios de aceite definidos na decomposição. Validação insuficiente ou resultado fora do escopo esperado não é promovido como conclusão da etapa do plano.

## Replanejamento

Quando uma tarefa falha ou é concluída parcialmente, o Planner não descarta o plano inteiro: ele retorna à etapa de decomposição, ajustando apenas a(s) tarefa(s) afetada(s) — gerando novas tarefas corretivas, reatribuindo a outro agente, ou escalando (ver WORKFLOW.md §20, fluxo de escalonamento) quando a falha exigir decisão humana ou arquitetural.

```text
Planejamento → Decomposição → Execução → Validação
                    ↑                         │
                    └────── Replanejamento ───┘ (em caso de falha)
```

## Relação com WORKFLOW.md e AI_TEAM.md

O Planner é a peça técnica que implementa, em tempo de execução, tanto a etapa "Planejamento" do fluxo oficial (WORKFLOW.md) quanto a responsabilidade de "quebrar em tarefas, priorizar, distribuir, acompanhar execução, validar entregas" atribuída ao Maestro (AI_TEAM.md). Trata-se da mesma responsabilidade descrita em duas altitudes: processo (WORKFLOW.md), organização de agentes (AI_TEAM.md) e implementação técnica (este documento).

Ver também [runtime.md](./runtime.md), [agents.md](./agents.md).
