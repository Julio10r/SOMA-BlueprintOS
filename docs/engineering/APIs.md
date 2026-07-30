# API — Documentação Técnica

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 18:24:18 UTC
- **Última atualização:** 2026-07-30

---

## API — documentação técnica

`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de
infraestrutura via `AddInfrastructure` e expõe saúde e o primeiro slice consultivo de negociação:

```
GET /health
  -> 200 OK { Status, Application, Environment, Version }

POST /api/v1/negotiations/history
  -> 201 Created com o histórico consolidado do fornecedor (transitório)
GET /api/v1/negotiations/suppliers/{supplierId}
  -> 200 OK com histórico ou 404 Not Found
POST /api/v1/negotiations/recommendations
  -> 200 OK com recomendação explicável e humanDecisionRequired: true
```

OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.
Os endpoints de negociação são consultivos, sem persistência durável, identidade ou ação automática.
