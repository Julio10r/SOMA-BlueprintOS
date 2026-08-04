# API — Documentação Técnica

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-30 21:04:12 UTC
- **Última atualização:** 2026-07-30

---

## API — documentação técnica

`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de
infraestrutura via `AddInfrastructure` e expõe saúde, fornecedores e descoberta ERP:

```
GET /health
  -> 200 OK { Status, Application, Environment, Version }

POST /api/v1/negociacoes/recomendacoes
  -> 200 OK { RequestId, Strategy, Justifications, Alerts, SuccessProbability, HumanDecisionRequired }

POST /api/fornecedores/descobrir
  -> 200 OK [ { Id, CodigoItem, Nome, Score, Criterio, ... } ]

GET /api/fornecedores/descobertas
  -> 200 OK [ descobertas persistidas da identidade temporária ]

GET /api/fornecedores/descobertas/{id}
  -> 200 OK descoberta persistida ou 404

POST /api/fornecedores/sincronizar
  -> 200 OK resultado com status, correlação, BU e identificador externo

POST /api/fornecedores/sincronizar/lote
  -> 200 OK resultados controlados (máximo de 100 fornecedores)
```

OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.
Os endpoints delegam aos casos de uso Application. A descoberta e o adaptador de sincronização usam exclusivamente `SOMA_DESENV`; a sincronização persiste estado e histórico no +Compras.
