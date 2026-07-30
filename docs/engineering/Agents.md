# Agentes de IA

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 18:24:18 UTC
- **Última atualização:** 2026-07-30

---

## Agentes de IA

O módulo `BlueprintOS.Core.Agents` define o runtime de agentes especializados:

- `IAgent` — contrato base implementado por todos os agentes.
- `BaseAgent` — classe base que injeta `IAIRuntime` (e, opcionalmente, `IKnowledgeService`)
  nos agentes concretos.
- `EchoAgent` — agente de referência/diagnóstico.
- `KnowledgeAgent` — agente que consulta o módulo `Knowledge` para responder com base em
  conhecimento organizacional indexado.
- `AgentFactory` — fábrica que cria instâncias de agentes via reflexão, injetando o
  runtime de IA e o serviço de conhecimento quando aplicável.

O módulo `AI.Negotiation` complementa a fundação com memória de negociação
(`INegotiationMemory`) e um motor de estratégia baseado em regras (`INegotiationStrategy`).
Não há, no código atual, um agente concreto Buyer sênior.
