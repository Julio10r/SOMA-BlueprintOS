# Runbooks

Não há, até o momento, um catálogo formal e completo de runbooks de produção — o BlueprintOS ainda não está em operação em produção (ver `.ai/ROADMAP.md`).

Já existem, porém, orientações operacionais e lições de troubleshooting reais registradas ao longo do projeto — por exemplo, em `.ai/memory/completed_sprints.md` (incidentes encontrados e corrigidos em validações reais contra o ERP/SQL Server corporativo) e em `.ai/memory/known_issues.md`. Um catálogo de runbooks formal será consolidado quando houver operação real em produção — ver [Operations.md](./Operations.md) para o ambiente atual.

## Runbooks operacionais já consolidados

- [Carga e Integração Diária Linx/WISE](./LinxWiseDailyIntegrationRunbook.md) — workflow diário para planilha `MB_PROD_EXTRA_WEB`, validações Linx, integração WISE via Linked Server e atualização de `WS_ESTOQUE_PRODUTOS`.
