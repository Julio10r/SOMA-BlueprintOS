# context/architecture.md

> Escopo: complemento a [ARCHITECTURE.md](../ARCHITECTURE.md), que é o documento canônico de arquitetura. Este arquivo não repete regras já definidas ali — apenas relaciona os módulos existentes com a AI Factory descrita em [AI_TEAM.md](../AI_TEAM.md).

## Módulos e capacidades da AI Factory

AI_TEAM.md descreve uma fábrica de especialistas coordenados por um Maestro. Os módulos definidos em ARCHITECTURE.md §7 são o substrato técnico que sustenta essas capacidades:

| Módulo | Capacidade da AI Factory que sustenta |
|---|---|
| Agents | Runtime dos especialistas (execução, ferramentas, ciclo de vida) |
| Planner | Planejamento e decomposição de tarefas (função do Maestro) |
| Memory | Memória própria de cada agente (curto, médio e longo prazo) |
| Knowledge | Base de conhecimento usada pelo Especialista IA e por RAG |
| Workflow | Orquestração de fluxos de negócio de múltiplas etapas |
| Identity | Autenticação/autorização de usuários e de agentes |
| Notifications | Comunicação de resultados a usuários e sistemas |
| Procurement | Domínio de negócio automatizado por agentes especialistas |
| Dashboard / Analytics | Observabilidade de negócio sobre o trabalho da AI Factory |

## Comunicação entre agentes e módulos

Assim como módulos só se comunicam via Contracts (ARCHITECTURE.md §9), agentes só se comunicam via Tasks (AI_TEAM.md, seção Comunicação) — nunca diretamente entre si. Este é o mesmo princípio de baixo acoplamento aplicado em duas camadas: técnica (módulos) e organizacional (agentes).

Para o funcionamento técnico da execução de agentes, ver [runtime.md](./runtime.md) e [agents.md](./agents.md).
