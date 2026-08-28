# Orquestração

Este documento descreve como o backend coordena fluxos entre a camada HTTP, casos de uso de Application e os contratos de estratégia/memória — a capacidade de coordenação em si, não a tecnologia (IA) usada por trás de cada estratégia. É distinto de [docs/agents/Agents.md](../../agents/Agents.md), que descreve a arquitetura dos próprios agentes de IA.

## Fluxo implementado: recomendação consultiva de negociação

Único fluxo de orquestração real hoje no código (vertical slice A13):

```
Controller REST → Application Use Case → contratos de estratégia/memória → resultado de domínio → mapeamento HTTP
```

- **Endpoint:** `POST /api/v1/negociacoes/recomendacoes` (`NegotiationRecommendationController`).
- **Caso de uso:** `NegotiationRecommendationUseCase`, orquestra `INegotiationMemory` e `INegotiationStrategy` sem reimplementar regra de negócio na API.
- **Identidade:** adaptador `DevelopmentRequestIdentity`, contrato substituível, funciona apenas em `Development` — falha de forma segura (503) fora desse ambiente, preparado para futura substituição por Microsoft Entra ID (ADR-0011).
- **Regra de orquestração:** o Controller contém apenas recebimento HTTP, validação de contrato, chamada ao caso de uso, mapeamento de resposta, logging e tratamento padronizado de erros — nunca acessa diretamente estratégias, memória ou serviços de domínio.
- **Resposta:** propaga `requestId`, estratégia sugerida, justificativas, alertas, probabilidade de sucesso quando suportada, e `humanDecisionRequired: true` — a decisão final é sempre humana.

## Princípio geral

Toda orquestração futura (novos fluxos que coordenem múltiplos contratos de domínio, memória ou agentes a partir de uma entrada HTTP) segue o mesmo padrão: Controller fino → Application Use Case → contratos de domínio, sem acesso direto de infraestrutura a partir da API, e sem duplicar regra de negócio na camada de orquestração.
