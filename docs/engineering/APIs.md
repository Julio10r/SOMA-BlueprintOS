# API — Documentação Técnica

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 15:26:33 UTC
- **Última atualização:** 2026-07-23

---

## API — documentação técnica

`BlueprintOS.Api` é um Minimal API (.NET 9) que hoje registra os serviços de
infraestrutura via `AddInfrastructure` e expõe um único endpoint:

```
GET /health
  -> 200 OK { Status, Application, Environment, Version }
```

OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.
Nenhum controller de negócio foi adicionado ao projeto `BlueprintOS.Api` até o momento.
