# context/memory.md

> Escopo: descreve os níveis de memória usados por agentes e pelo processo de engenharia do BlueprintOS, implementados pelo módulo Memory. Distingue contexto de execução (efêmero) de contexto persistente (que sobrevive entre execuções).

## Os três níveis de memória

### Curto prazo (contexto de execução)

Escopo: uma única execução de tarefa por um agente.

É o contexto que o agente "vê" enquanto executa: a tarefa recebida, o histórico imediato da conversa/execução, resultados intermediários de ferramentas invocadas. É descartado ao final da execução, salvo o que for explicitamente promovido a médio ou longo prazo.

### Médio prazo (contexto de sprint/conversa)

Escopo: uma sprint ou uma sequência de tarefas relacionadas.

Corresponde aos arquivos em `.ai/memory/`:

- `completed_sprints.md` — o que já foi entregue;
- `known_issues.md` — problemas conhecidos e não resolvidos;
- `patterns.md` — padrões identificados que devem ser reaproveitados.

A atualização desses arquivos é obrigatória ao final de toda tarefa concluída, conforme WORKFLOW.md §14. Este documento não redefine quando atualizar — apenas classifica esses arquivos como memória de médio prazo.

### Longo prazo (base de conhecimento organizacional)

Escopo: conhecimento persistente da organização, independente de sprint ou tarefa.

Implementado pelo módulo Knowledge (ver [knowledge.md](./knowledge.md)) — documentos, políticas, dados históricos indexados para recuperação (embeddings/RAG). É a memória que um agente Especialista IA consulta para responder com base em conhecimento organizacional real, e não apenas no contexto da execução atual.

## Contexto de execução vs. contexto persistente

| | Contexto de execução | Contexto persistente |
|---|---|---|
| Duração | uma execução | entre execuções, sprints, ou indefinida |
| Onde vive | memória do agente durante a tarefa | `.ai/memory/*.md`, base de conhecimento (Knowledge) |
| Quem escreve | o próprio agente, durante a execução | processo de encerramento de tarefa (WORKFLOW.md §14) |
| Exemplo | resultado parcial de uma ferramenta | um padrão registrado em `patterns.md` |

## Regra geral

Nenhum agente altera memória persistente diretamente e sem processo — a promoção de contexto de execução para memória de médio/longo prazo segue o fluxo de encerramento de tarefa definido em WORKFLOW.md, não uma escrita ad-hoc pelo agente (ver também AI_TEAM.md, seção Regras: "um agente nunca altera memória diretamente").

Ver também [runtime.md](./runtime.md), [agents.md](./agents.md), [knowledge.md](./knowledge.md).
