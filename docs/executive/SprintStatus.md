# Status da Sprint

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## Status da sprint mais recente

## Sprint de Infraestrutura — Remoção do Docker e Consolidação do Ambiente Local

**Status:** Concluída e encerrada em 03/08/2026.

**Objetivo:** Remover o Docker do fluxo de desenvolvimento e consolidar o ambiente local (sem containers) como ambiente oficial, mantendo a documentação de engenharia consistente.

**Entregas:**

- `Makefile`, `backend/src/BlueprintOS.Api/Dockerfile` e `infrastructure/docker/docker-compose.yml` removidos (`601d937`, `7bf3bf4`).
- Dependência opcional de SQL Server local em Docker removida antes da remoção completa (`601d937`).
- Scripts locais (`start-dev.sh`, `stop-dev.sh`, `health-check.sh`) confirmados como caminho oficial de orquestração de backend/frontend.
- `frontend/web/.env.example` atualizado para `http://localhost:5262` (API via `dotnet run`).
- `BlueprintOS.UnitTests.csproj` limpo de referências de pacote não utilizadas.
- Documentação de engenharia atualizada: `docs/Engineering Handbook.md`, `docs/INDEX.md`, `docs/assets/solution-tree.md`, `docs/engineering/Deploy.md`, `docs/engineering/FornecedorErpSynchronization.md`, `.ai/ENGINEERING_BLUEPRINT.md`, `.ai/content/engineering/08-devops.md`.
- ADR-0018 (`.ai/DECISIONS.md`) atualizada para remover a opção Docker do ambiente de execução.

**Validação:** `dotnet build backend/BlueprintOS.sln` com 0 erros e 0 avisos; `dotnet test backend/BlueprintOS.sln` com 286 testes aprovados (281 unitários + 5 integração), 0 falhas; `npm run build` do frontend (`tsc -b && vite build`) aprovado; scripts de desenvolvimento verificados como funcionais; branch sincronizada e working tree limpo antes do encerramento.

**Resultado:** Docker deixou de ser parte do fluxo de desenvolvimento. Nenhuma regra de negócio, contrato de API ou comportamento funcional foi alterado — escopo exclusivamente de infraestrutura e documentação. Projeto estável e apto para iniciar a próxima sprint funcional.
