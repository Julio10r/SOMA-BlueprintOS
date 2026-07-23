# context/agents.md

> Escopo: define o ciclo de vida técnico de um agente dentro do módulo Agents e sua relação com Runtime, Memory e Planner. Para o modelo organizacional de especialistas, ver [AI_TEAM.md](../AI_TEAM.md).

## O que é um agente

Um agente é a composição de:

- um **prompt** (definição de comportamento e limites);
- um conjunto de **ferramentas** (tools) que pode invocar;
- uma **memória** própria (curto e médio prazo, ver [memory.md](./memory.md));
- acesso a um **planner** para tarefas que exigem decomposição adicional;
- um **modelo** de IA subjacente.

Esses campos espelham o template de criação de agentes definido em AI_TEAM.md (seção "Criação de Novos Agentes") — este documento não o repete, apenas descreve como cada campo se conecta ao Runtime.

## Ciclo de vida

1. **Criação** — o agente é definido seguindo o template de AI_TEAM.md, com nome, objetivo, responsabilidade, limites, ferramentas, entradas, saídas, critérios de qualidade, prompt base, modelo, memória e permissões.
2. **Registro** — o agente é registrado no Runtime (ver [runtime.md](./runtime.md)), tornando-se descobrível para seleção em tarefas compatíveis com sua responsabilidade.
3. **Execução** — o agente recebe tarefas via Task (nunca diretamente de outro agente, conforme AI_TEAM.md, seção Comunicação), executa usando suas ferramentas e retorna um resultado.
4. **Observação** — toda execução deve ser observável (logs, métricas, rastreamento — ver [observability.md](./observability.md)), permitindo auditoria do que o agente fez e por quê.
5. **Desativação** — um agente pode ser desativado (removido do registro do Runtime) sem afetar outros agentes, desde que nenhuma tarefa em andamento dependa exclusivamente dele.

## Responsabilidades e limites

Cada agente possui responsabilidade única (AI_TEAM.md, princípio de Especialização) e nunca:

- inventa informações fora do seu contexto;
- executa tarefas fora do seu domínio declarado;
- altera memória de outro agente ou o trabalho de outro agente;
- responde diretamente ao usuário sem passar pela orquestração (Maestro/Runtime).

## Relação com Memory e Planner

- **Memory:** cada agente tem acesso à sua própria memória de curto prazo (contexto de execução da tarefa atual) e pode consultar memória de médio/longo prazo relevante ao seu domínio (ver [memory.md](./memory.md)).
- **Planner:** quando uma tarefa recebida por um agente é grande demais para execução direta, o agente delega ao Planner para decomposição adicional (ver [planner.md](./planner.md)).

## Template de criação

Use o template definido em [AI_TEAM.md](../AI_TEAM.md), seção "Criação de Novos Agentes". Em nível de Runtime, adicione também:

- identificador técnico único de registro;
- escopo de módulos e Contracts que o agente está autorizado a acessar;
- estratégia de fallback caso a execução falhe (ver replanejamento em [planner.md](./planner.md)).

Nenhum agente pode ser registrado sem essa estrutura completa.
