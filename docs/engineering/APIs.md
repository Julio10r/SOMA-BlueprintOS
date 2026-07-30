# API — Documentação Técnica

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 21:04:12 UTC
- **Última atualização:** 2026-07-30

---

## API — documentação técnica

`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de
infraestrutura via `AddInfrastructure` e expõe saúde e uma recomendação consultiva:

```
GET /health
  -> 200 OK { Status, Application, Environment, Version }

POST /api/v1/negociacoes/recomendacoes
  -> 200 OK { RequestId, Strategy, Justifications, Alerts, SuccessProbability, HumanDecisionRequired }
```

OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.
O controller delega ao caso de uso Application, reutiliza contratos de memória e estratégia e não altera estado.
