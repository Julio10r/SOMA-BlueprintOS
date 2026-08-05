# API — Documentação Técnica

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## API — documentação técnica

`BlueprintOS.Api` é um Minimal API (.NET 9) que registra os serviços de
infraestrutura via `AddInfrastructure` e expõe saúde, recomendação consultiva de
negociação e o vertical slice de Fornecedores (cadastro, descoberta, consulta/
enriquecimento de CNPJ e sincronização com o ERP):

```
GET /health
  -> 200 OK { Status, Application, Environment, Version }

POST /api/v1/negociacoes/recomendacoes
  -> 200 OK { RequestId, Strategy, Justifications, Alerts, SuccessProbability, HumanDecisionRequired }

GET  /fornecedores                          -> busca/listagem de fornecedores
POST /fornecedores                          -> cadastro de fornecedor
GET  /fornecedores/{id}                     -> detalhe do fornecedor
PUT  /fornecedores/{id}                     -> atualização do fornecedor
POST /fornecedores/consulta-cnpj            -> consulta de CNPJ (BrasilAPI)
POST /fornecedores/{id}/enriquecimento-cnpj -> análise de divergências de CNPJ
POST /fornecedores/{id}/enriquecimento-cnpj/aprovar -> aprova o enriquecimento
POST /fornecedores/{id}/enriquecimento-cnpj/rejeitar -> rejeita o enriquecimento

POST /api/fornecedores/descobrir            -> descoberta de fornecedores no ERP
GET  /api/fornecedores/descobertas          -> lista descobertas registradas

POST /api/fornecedores/sincronizar          -> sincroniza um fornecedor com o ERP
POST /api/fornecedores/sincronizar/lote     -> sincroniza um lote de fornecedores
GET  /api/fornecedores/sincronizar-erp      -> executa e audita a sincronização ERP → +Compras
```

OpenAPI (`AddOpenApi`/`MapOpenApi`) está habilitado em ambiente de desenvolvimento.
Os controllers delegam a casos de uso Application, reutilizando contratos de domínio,
memória e estratégia; a API permanece fina, sem regra de negócio própria.
