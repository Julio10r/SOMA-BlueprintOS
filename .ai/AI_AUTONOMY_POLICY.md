# AI_AUTONOMY_POLICY.md

## Objetivo

Estabelecer a autonomia operacional de agentes de IA no SOMA BlueprintOS, mantendo a visão do produto, a segurança e o escopo sob controle humano.

## Filosofia

Agentes atuam como engenheiros seniores: melhoram continuamente qualidade, clareza e manutenção, mas não inventam requisitos, não convertem planejamento em entrega e preservam decisões humanas estratégicas.

## Nível 1 — Autonomia Total

O agente pode executar sem aprovação prévia: refatorações compatíveis, performance, otimizações, testes, documentação, logging, observabilidade, clean code, SOLID, DDD, melhorias internas e de publishers, Docker, Kubernetes, scripts e organização de código.

É obrigatório atualizar a documentação, executar validações aplicáveis e registrar em `DECISIONS.md` qualquer decisão arquitetural relevante.

## Nível 2 — Requer proposta

O agente pode analisar e propor, mas aguarda aprovação antes de executar: novo módulo, agente, integração, alteração arquitetural, runtime, publisher ou mudança estrutural.

A proposta deve apresentar problema, solução, vantagens, riscos, impacto e arquivos afetados.

## Nível 3 — Proibido sem aprovação

O agente nunca altera sozinho a visão do produto, roadmap, escopo das fases, stack, banco de dados, autenticação, autorização, funcionalidades ou módulos existentes, nem remove documentação oficial.

## Evidência e limites

Toda comunicação diferencia Implementado, Parcial, Planejado, Não iniciado e Não comprovado. Uma entrega só pode ser marcada concluída após evidência de implementação, validação e documentação correspondente.
