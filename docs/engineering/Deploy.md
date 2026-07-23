# Deploy

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 14:53:48 UTC
- **Última atualização:** 2026-07-23

---

## Deploy

O deploy do BlueprintOS é baseado em containers Docker:

- **`backend/src/BlueprintOS.Api/Dockerfile`** — build multi-stage: publica
  `BlueprintOS.Api` com o SDK .NET 9 e executa a imagem publicada sobre
  `mcr.microsoft.com/dotnet/aspnet:9.0`, expondo a porta `8080`
  (`ASPNETCORE_URLS=http://+:8080`).
- **`infrastructure/docker/docker-compose.yml`** e
  **`docker-compose.override.yml`** — orquestração local dos serviços.

Não há, até o momento, pipeline de CI/CD (ex.: GitHub Actions) configurado no
repositório.
