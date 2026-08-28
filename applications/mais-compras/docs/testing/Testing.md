# Testes

- **Framework:** xUnit, sem biblioteca de mocking — fakes escritos manualmente.
- **Prioridade de cobertura:** Application → Domain → Integration → End-to-End (ainda não há testes E2E).
- Contagem de testes não é fixada aqui, pois cresce a cada sprint. Para o resultado da última execução real de build e testes, ver `.ai/PROJECT_STATE.md`.

```bash
dotnet test backend/BlueprintOS.sln
```

## Critérios obrigatórios por Work Order

Toda Work Order concluída exige, no mínimo: build sem erros nem warnings críticos novos, testes unitários aplicáveis aprovados, e teste de integração quando a Work Order expuser um endpoint HTTP real. Smoke test é exigido para fluxos ponta a ponta expostos publicamente. Ver `.ai/templates/WORK_ORDER_TEMPLATE.md` para a seção "Testes" padrão.
