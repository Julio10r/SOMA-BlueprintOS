# context/tech-stack.md

> Escopo: a lista oficial e canônica de tecnologias está em [PROJECT.md](../PROJECT.md) §4 — não repetida aqui.

## Filosofia de versionamento

O projeto prioriza versões LTS (Long-Term Support) ou correntes estáveis das tecnologias adotadas (ex.: .NET 9), evitando tanto versões experimentais/preview quanto versões defasadas sem suporte. O objetivo é reduzir risco de segurança e de manutenção, mantendo acesso a recursos modernos da plataforma sem instabilidade.

Upgrades de versão maior (ex.: de uma versão do .NET para outra) constituem mudança arquitetural e devem seguir o fluxo de WORKFLOW.md §16, incluindo registro como ADR em [DECISIONS.md](../DECISIONS.md) quando o impacto for relevante.

## Onde checar a versão exata em uso

Este documento e PROJECT.md descrevem a stack oficial em nível de decisão, não o número de versão exato vigente em um dado momento. Para a versão exata efetivamente em uso, consulte:

- arquivos `.csproj` / `Directory.Build.props` para versões de .NET e pacotes NuGet;
- `package.json` para versões de React, TypeScript e dependências de frontend;
- `Dockerfile` / `docker-compose.yml` para versões de imagens base;
- arquivos de configuração de CI/CD para versões de ferramentas de build.

Nunca assuma uma versão a partir da documentação sem confirmar nos arquivos de configuração do repositório, pois estes evoluem com o tempo e a documentação pode não ser atualizada na mesma cadência.
