# Auditoria do Repositório — Etapa 1: Higiene e Artefatos Gerados

**Data:** 30/07/2026
**Escopo:** somente higiene de arquivos locais e regras de ignore. Nenhum código-fonte, teste, arquitetura ou documentação funcional foi alterado.

## Itens encontrados e removidos

- 17 arquivos `.DS_Store` encontrados na raiz e em diretórios de código, documentação e metadados; removidos por serem resíduos do macOS.
- 18 diretórios `bin/` e `obj/` sob `backend/`; removidos por serem saídas intermediárias e de compilação do .NET.
- `dist/` com 9 arquivos de relatório (Markdown, HTML e PDF) e um `.DS_Store`; removido após análise.

Nenhum desses caminhos estava versionado, confirmado por `git ls-files | grep -E '(^|/)(bin|obj|dist)(/|$)|\\.DS_Store$'`.

## Regras adicionadas ao .gitignore

- `__MACOSX/`
- `.coverage/`, `*.coverage` e `*.coveragexml`
- `frontend/dist/`
- `*.bak` e `*.swp`

As regras já existentes para `.DS_Store`, `Thumbs.db`, `**/bin/`, `**/obj/`, `TestResults/`, `coverage/`, `node_modules/`, `.vs/`, `.idea/`, `*.log`, `*.tmp` e `/dist/` foram preservadas. Não foram adicionadas regras que ignorem documentos de negócio em `docs/`.

## Análise de dist

`dist/` era um **artefato local removível**: é gerado pelo Publication Engine por `dotnet run -- publish`, está explicitamente ignorado por `/dist/`, não possui arquivos rastreados e a documentação técnica o descreve como saída não versionada. Não foi classificado como entrega oficial versionada.

## Itens preservados

- Todos os arquivos de código, testes e documentação funcional rastreados.
- Documentos de trabalho em `docs/presentations/`, incluindo os quatro arquivos +COMPRAS explicitamente preservados.
- Alterações locais paralelas autorizadas em `.ai/AI_BEHAVIOR.md`, `.ai/DOCUMENTATION_UPDATE_COMMAND.md` e documentos do roadmap gerencial. Elas não foram modificadas, staged ou incluídas nesta auditoria.
- `.vscode/` estava vazio; nenhuma configuração compartilhável foi removida.

## Arquivos sensíveis

Foram localizados apenas arquivos de exemplo/configuração e referências de código/documentação para credenciais: `.env.example`, `infrastructure/docker/.env.docker.example`, `infrastructure/docker/.env.docker`, `appsettings.json`, configuração OpenAI e testes. A inspeção foi feita por caminhos e ocorrências, sem expor valores. Não foi identificado segredo versionado nesta etapa.

## Riscos e pendências

- Após a remoção de `obj/`, a validação requer restauração NuGet completa.
- O restore serial concluiu usando o cache local, porém emitiu `NU1900` para API, Infrastructure e projetos de teste: o nuget.org não estava acessível para consulta de dados de vulnerabilidade. O aviso não impediu restore, build ou testes; requer nova consulta de vulnerabilidades quando a rede estiver disponível.
- Os diretórios `bin/` e `obj/` foram naturalmente recriados durante build/testes e permanecem ignorados. Não são arquivos versionados nem mudança de escopo desta auditoria.

## Resultado de validação

| Verificação | Resultado |
|---|---|
| Caminhos temporários versionados | Nenhum |
| Fonte, testes ou documentação funcional removidos | Não |
| Restore | Sucesso com `--disable-parallel`; 4 avisos `NU1900` de consulta de vulnerabilidades |
| Build pós-limpeza | Sucesso — 0 erros; 4 avisos `NU1900` |
| Testes pós-limpeza | Sucesso — 231 aprovados (230 unitários, 1 integração), 0 falhos e 0 ignorados |
| Frontend | Não há projeto frontend funcional configurado |

## Próxima etapa recomendada

Restabelecer o acesso ao nuget.org e repetir a consulta de vulnerabilidades para eliminar o aviso `NU1900`. Nenhuma limpeza adicional é necessária; os intermediários regenerados estão cobertos pelo `.gitignore`.
