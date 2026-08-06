# Sprint de Infraestrutura — Remoção do Docker e Consolidação do Ambiente Local

Status:
Concluída e encerrada em 03/08/2026, com auditoria final aprovada.

Objetivo:
Remover o Docker do fluxo de desenvolvimento do BlueprintOS/+Compras e consolidar o ambiente local (sem containers) como ambiente oficial de desenvolvimento, mantendo a documentação de engenharia consistente com essa decisão.

Entregas:

- `Makefile`, `backend/src/BlueprintOS.Api/Dockerfile` e `infrastructure/docker/docker-compose.yml` removidos do repositório (commits `601d937`, `7bf3bf4`).
- Dependência opcional de SQL Server local em Docker removida antes da remoção completa (commit `601d937`).
- Scripts de desenvolvimento local (`start-dev.sh`, `stop-dev.sh`, `health-check.sh`) confirmados como caminho oficial para subir/parar/verificar backend e frontend.
- `frontend/web/.env.example` atualizado para apontar por padrão para `http://localhost:5262` (API via `dotnet run`), sem referência a Docker.
- `BlueprintOS.UnitTests.csproj` limpo de referências de pacote não utilizadas.
- Documentação de engenharia revisada para remover referências a Docker como ambiente ativo: `docs/Engineering Handbook.md`, `docs/INDEX.md`, `docs/assets/solution-tree.md`, `docs/engineering/Deploy.md`, `docs/engineering/FornecedorErpSynchronization.md`, `.ai/ENGINEERING_BLUEPRINT.md`, `.ai/content/engineering/08-devops.md`.
- ADR-0018 (`.ai/DECISIONS.md`) atualizada para refletir o ambiente local sem Docker.

Validações executadas:

- `dotnet build backend/BlueprintOS.sln`: aprovado, 0 erros e 0 avisos.
- `dotnet test backend/BlueprintOS.sln`: aprovado, 286 testes (281 unitários + 5 integração), 0 falhas, 0 ignorados.
- `npm run build` (`tsc -b && vite build`) em `frontend/web`: aprovado.
- Scripts de desenvolvimento (`start-dev.sh`/`stop-dev.sh`/`health-check.sh`) verificados como funcionais.
- Branch `feature/a13-procurement-vertical-slice` sincronizada com o remoto; working tree limpo antes desta atualização de encerramento.
- Auditoria final de consistência: nenhum resíduo funcional, referência quebrada a Docker ou documento contraditório encontrado.

Resultado:

Docker deixou de ser parte do fluxo de desenvolvimento do projeto. O ambiente oficial de desenvolvimento é local (backend via `dotnet run`, frontend via `npm run dev`/Vite, banco SQL Server corporativo via VPN), sem containers. Nenhuma regra de negócio, contrato de API ou comportamento funcional foi alterado por esta sprint — o escopo foi exclusivamente de infraestrutura e documentação.

Riscos remanescentes:

- Nenhum risco funcional identificado. `infrastructure/docker/` permanece reservado no repositório apenas como diretório documentado (sem `docker-compose.yml`/`Dockerfile` ativos); se não houver uso futuro, sua remoção completa pode ser avaliada em uma sprint futura de limpeza.
- CI/CD e ambiente de homologação continuam não implementados (fora do escopo desta sprint).

---

## Encerramento de sprint

Nenhuma sprint funcional está em andamento. O projeto foi replanejado oficialmente para o MVP 1.0 (estratégia Frontend First, ver `.ai/ROADMAP.md`); a próxima Work Order candidata é a **Onda 1 — Fundação Funcional** (frontend navegável + Administração + blueprint completo do banco), e depende de aprovação explícita do Product Owner (ver `.ai/PROJECT_STATE.md` e `.ai/BACKLOG.md`).

O histórico completo da sprint funcional anterior (B2.1.3 — Endurecimento da Integração ERP de Fornecedores, concluída em 02/08/2026) está arquivado em `.ai/memory/completed_sprints.md`.
