# Deploy

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-08-05 15:25:57 UTC
- **Última atualização:** 2026-08-05

---

## Deploy

O ambiente oficial de desenvolvimento do BlueprintOS não usa Docker (ver
ADR-0018 em `.ai/DECISIONS.md`):

- **Backend** — API .NET executada diretamente via `dotnet run`
  (`backend/src/BlueprintOS.Api`), perfil `http` (`launchSettings.json`),
  porta `5262`.
- **Frontend** — React/Vite executado via `npm run dev`
  (`frontend/web`), porta `5173`.
- **Banco de dados** — sempre SQL Server externo (bancos corporativos
  `MAISCOMPRAS`/`SOMA_DESENV`, acessado via VPN), nunca um container local.
- **Docker** — `infrastructure/docker/.env.example` permanece reservado no
  repositório como documentação, sem containers ativos no fluxo de desenvolvimento.

Não há, até o momento, pipeline de CI/CD (ex.: GitHub Actions) nem ambiente
de homologação configurado no repositório.
