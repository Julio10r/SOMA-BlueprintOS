# context/runtime.md

> Escopo: o Runtime é o substrato técnico de execução de agentes e tarefas, implementado principalmente pelos módulos **Agents** e **Workflow** (ver ARCHITECTURE.md §7). Este documento descreve como uma requisição se transforma em execução real.

## Ciclo de execução

1. **Requisição** — chega via API, evento de domínio ou trigger de Workflow.
2. **Tarefa (Task)** — o Planner (ver [planner.md](./planner.md)) decompõe a requisição em uma ou mais tarefas executáveis, equivalentes a um Task Packet (ver WORKFLOW.md §7).
3. **Seleção de agente** — o Runtime consulta o registro de agentes disponíveis e seleciona o agente cuja responsabilidade e ferramentas atendem à tarefa.
4. **Execução** — o agente selecionado executa a tarefa usando suas ferramentas, com acesso à memória de curto prazo (contexto de execução, ver [memory.md](./memory.md)).
5. **Resultado** — o resultado é retornado ao chamador (Planner, Workflow ou Maestro conceitual) usando Result Pattern (ver ARCHITECTURE.md §11).

## Registro e descoberta de agentes

Todo agente deve se registrar no Runtime antes de poder ser selecionado para execução. O registro segue o template de criação de agentes definido em AI_TEAM.md (seção "Criação de Novos Agentes"), com metadados adicionais específicos do Runtime:

- identificador único do agente;
- lista de ferramentas (tools) que o agente pode invocar;
- escopo de módulos/dados que o agente pode acessar.

A descoberta é automática: o Runtime mantém um registro consultável de agentes e ferramentas ativos, permitindo que o Planner escolha o agente adequado sem acoplamento direto a uma implementação específica (mesmo princípio de Contracts entre módulos, ARCHITECTURE.md §9).

## Modelo de execução de ferramentas

Ferramentas (tools) são unidades de capacidade que um agente pode invocar durante a execução de uma tarefa (ex.: consultar o Knowledge, chamar uma API externa, escrever em um repositório via Contract). Cada ferramenta:

- possui uma interface bem definida de entrada/saída;
- é executada de forma assíncrona (ARCHITECTURE.md §11);
- deve registrar logs e, quando aplicável, métricas de uso (ver [observability.md](./observability.md)).

## Orquestração

O conceito de "Maestro" definido em AI_TEAM.md é, em nível de produto, o papel de orquestração. No Runtime, esse papel é implementado como um componente de orquestração que:

- recebe o plano gerado pelo Planner;
- distribui tarefas aos agentes registrados;
- acompanha a execução e agrega resultados;
- nunca executa lógica de negócio diretamente — apenas coordena.

Isto é, o Runtime é a camada técnica sobre a qual o conceito de Maestro é implementado; AI_TEAM.md descreve o modelo organizacional, este documento descreve sua execução técnica.

Ver também [agents.md](./agents.md), [planner.md](./planner.md), [AI_TEAM.md](../AI_TEAM.md).
