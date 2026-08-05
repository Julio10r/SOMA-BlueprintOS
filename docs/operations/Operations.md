# Operações

## Ambiente de execução

O ambiente oficial de desenvolvimento do BlueprintOS não usa Docker (ADR-0018):

- **Backend** — API .NET executada diretamente via `dotnet run` (`backend/src/BlueprintOS.Api`), perfil `http` (`launchSettings.json`), porta `5262`.
- **Frontend** — React/Vite executado via `npm run dev` (`frontend/web`), porta `5173`.
- **Banco de dados** — sempre SQL Server externo (bancos corporativos `MAISCOMPRAS`/`SOMA_DESENV`, acessado via VPN), nunca um container local. Ver [Database.md](../database/Database.md).
- **Docker** — `infrastructure/docker/.env.example` permanece reservado no repositório como documentação, sem containers ativos no fluxo de desenvolvimento.

```bash
# Subir backend e frontend em segundo plano (sem Docker)
./scripts/start-dev.sh

# Parar
./scripts/stop-dev.sh

# Ver status
./scripts/health-check.sh
```

Para rodar o backend manualmente:

```bash
dotnet build backend/BlueprintOS.sln
dotnet test backend/BlueprintOS.sln
dotnet run --project backend/src/BlueprintOS.Api
```

Variáveis de ambiente sensíveis (ex.: `AI__OpenAI__ApiKey`) seguem o padrão `.env.example` — nunca commitadas com valor real.

Não há, até o momento, pipeline de CI/CD (ex.: GitHub Actions) nem ambiente de homologação configurado no repositório.

## Deploy

Hoje o deploy é local, sem Docker: backend via `dotnet run` e frontend via `npm run dev`, ambos orquestrados por `./scripts/start-dev.sh`. Terraform, Kubernetes, Nginx, observabilidade e GCP estão planejados ou reservados, sem implementação no repositório — ver `.ai/work-orders/backlog/fase-h/`.

## Git Flow

Branches:

- `main` — nunca recebe commit direto.
- `feature/`, `bugfix/`, `hotfix/`, `release/` — todo trabalho parte daqui.

Commits no formato `tipo: descrição` (ex.: `feat: add planner module`, `fix: correct workflow validation`, `docs: update architecture`).

```bash
git add .
git commit -m "tipo: descrição"
git push
```

Todo Pull Request deve conter: objetivo, mudanças, impactos, testes realizados e checklist — ver `.ai/STANDARDS.md` para o guia completo.

Ver também [Runbooks.md](./Runbooks.md) para orientações operacionais de troubleshooting.
