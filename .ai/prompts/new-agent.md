# Template: Criar novo agente

Uso: iniciar a criação de um novo agente de IA no BlueprintOS.

## Antes de começar, consultar

- [../AI_TEAM.md](../AI_TEAM.md) — template obrigatório de criação de agentes (seção "Criação de Novos Agentes")
- [../context/agents.md](../context/agents.md) — ciclo de vida e relação com Runtime/Memory/Planner
- [../context/runtime.md](../context/runtime.md) — como o agente será registrado e executado
- [../ARCHITECTURE.md](../ARCHITECTURE.md) — regras de módulo e camada aplicáveis, se o agente pertencer a um módulo específico

## Placeholders a preencher

- `{{nome_do_agente}}`
- `{{objetivo}}`
- `{{responsabilidade}}`
- `{{limites}}`
- `{{ferramentas}}`
- `{{entradas}}`
- `{{saidas}}`
- `{{criterios_de_qualidade}}`
- `{{prompt_base}}`
- `{{modelo}}`
- `{{memoria_utilizada}}`
- `{{permissoes}}`
- `{{modulo_relacionado}}` (se aplicável)

## Prompt

Crie um agente chamado `{{nome_do_agente}}` com o objetivo de `{{objetivo}}`.

Responsabilidade: `{{responsabilidade}}`.
Limites (o que este agente nunca deve fazer): `{{limites}}`.
Ferramentas disponíveis: `{{ferramentas}}`.
Entradas esperadas: `{{entradas}}`.
Saídas esperadas: `{{saidas}}`.
Critérios de qualidade: `{{criterios_de_qualidade}}`.
Prompt base: `{{prompt_base}}`.
Modelo: `{{modelo}}`.
Memória utilizada: `{{memoria_utilizada}}`.
Permissões: `{{permissoes}}`.

Siga rigorosamente o template de AI_TEAM.md. Nenhum campo pode ficar vazio. Ao final, registre o agente no Runtime conforme context/runtime.md.
