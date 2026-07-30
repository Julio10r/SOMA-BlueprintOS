# Auditoria do Repositório — Etapa 2: Arquivos Obsoletos, Duplicados e Órfãos

**Data:** 30/07/2026

## 1. Resumo executivo

Foram analisados **629 arquivos versionados** (199 Markdown), oito projetos .NET, os diretórios principais e os arquivos de infraestrutura. Não foi removido, movido, renomeado ou alterado nenhum item auditado.

Foram identificados **13 grupos candidatos**: 0 para remoção automática; 4 para atualização de documentação derivada; 5 para consolidação ou decisão de governança; 2 placeholders de infraestrutura/metadados; 2 grupos para investigação. A principal divergência é documental: o estado canônico registra A12, mas relatórios derivados ainda apresentam A10 como a última sprint.

## 2. Metodologia

- Inventário com `git ls-files`, `find` e inspeção dos diretórios solicitados.
- Verificação de inclusão na solution, referências de projeto, DI, pontos de entrada, Docker/Makefile e referências Markdown/código.
- Comparação entre estado canônico, documentos derivados, histórico Git e documentação de arquitetura.
- Nenhuma classificação de obsolescência foi baseada apenas no nome do arquivo.

## 3. Inventário por diretório

| Diretório | Arquivos versionados | Classificação |
|---|---:|---|
| `.ai` | 141 | Governança, contexto, memória, conteúdo-fonte e Work Orders |
| `backend` | 304 | Código, testes e solution; 8 projetos, todos presentes na solution |
| `docs` | 173 | Documentação canônica, derivada, histórica, templates e design system |
| `infrastructure` | 3 | Docker funcional; demais subdiretórios são esqueletos locais não versionados |
| `agents`, `database`, `frontend`, `shared`, `workers` | 0 | Estruturas locais planejadas, sem artefatos Git |

## 4. Candidatos a remoção

Nenhum item é recomendado para remoção automática.

| Caminho | Categoria | Evidência | Confiança | Risco | Recomendação |
|---|---|---|---|---|---|
| `infrastructure/docker/docker-compose.override.yml` | Remover | Arquivo versionado vazio; o Makefile usa explicitamente apenas `docker-compose.yml`; não há conteúdo a preservar. | Alta | Baixo | Aprovar remoção em etapa posterior, após confirmar que nenhum desenvolvedor depende do caminho local. |
| `.editorconfig`, `.gitattributes`, `LICENSE` | Atualizar | Versionados e vazios; não exercem a finalidade sugerida pelos nomes. | Alta | Médio/Alto | Decidir se serão preenchidos (especialmente LICENSE, com revisão legal) ou removidos em mudança dedicada. |

## 5. Candidatos a consolidação

| Caminho | Categoria | Evidência | Confiança | Risco | Recomendação |
|---|---|---|---|---|---|
| `.ai/PROJECT_VISION.md`, `PROJECT_PHILOSOPHY.md`, `PROJECT_SCOPE.md` versus `VISION.md` | Consolidar | Os três primeiros são de 23/07; `VISION.md` é a visão consolidada posterior e tem mais referências no fluxo atual. | Média | Médio | Definir `VISION.md` como fonte única e converter os demais em histórico/redirecionamento ou incorporar conteúdo único. |
| `.ai/DEVELOPMENT_WORKFLOW.md` versus `WORKFLOW.md` | Consolidar | Ambos definem processo de desenvolvimento; `WORKFLOW.md` contém Work Orders, autonomia e checklist de commit. | Alta | Médio | Manter `WORKFLOW.md` canônico e reduzir o documento antigo a referência controlada. |
| `.ai/memory/{architecture,patterns}.md`, `.ai/prompts/{claude,codex,review}.md`, `.ai/tasks/README.md` | Consolidar | Seis arquivos versionados vazios; são referenciados apenas por estrutura ou links, sem conteúdo operacional. | Alta | Baixo | Preencher conforme fluxo aprovado ou substituir por índice único; não remover sem decisão de governança. |
| `docs/templates/{ADR,API,Feature,RFC,Sprint,Task,Workflow}.md` | Consolidar | Sete templates versionados vazios. | Alta | Médio | Reaproveitar/alinhar com templates e Work Orders em `.ai`, ou descontinuar formalmente em etapa futura. |
| `docs/AI Factory/` versus `.ai/` | Consolidar | Arquivos AI Factory listados no índice como conteúdo, mas vários estão vazios; `.ai` contém o fluxo e contexto vivos. | Média | Médio | Decidir se `docs/AI Factory` é documentação histórica/pública ou se deve ser regenerada a partir de `.ai`. |

## 6. Candidatos a renomeação

Nenhum candidato comprovado. O sufixo `-v2` em `docs/design-system/preview/component-header-v2.html` não basta como evidência; não foram encontradas referências suficientes para escolher entre as duas variantes.

## 7. Candidatos a movimentação

| Caminho | Categoria | Evidência | Confiança | Risco | Recomendação |
|---|---|---|---|---|---|
| `docs/DocumentationHealth.md` | Investigar | É saída configurada de `IDocumentationHealthService`, mas está versionado. | Média | Médio | Decidir se deve permanecer como relatório versionado ou migrar para artefato de CI; não mover nesta etapa. |
| Diretórios locais vazios (`agents/`, `database/`, `frontend/`, `workers/`, `shared/`, `integrations/`, `scripts/` e subdiretórios de infraestrutura) | Preservar | Não são versionados; representam a arquitetura alvo e não contêm arquivos para Git mover. | Alta | Baixo | Não incluir placeholders até haver implementação aprovada; atualizar índices que os apresentem como existentes. |

## 8. Documentação conflitante

| Caminho | Categoria | Evidência | Confiança | Risco | Recomendação |
|---|---|---|---|---|---|
| `docs/Executive Report.md`, `Product Blueprint.md`, `Engineering Handbook.md` | Atualizar | Referenciados pelo README, mas ainda apresentam A10 como sprint atual/mais recente; o estado canônico registra A12. | Alta | Médio | Atualizar/republicar em sprint documental aprovada, preservando o caráter por público. |
| `docs/executive/{Dashboard,Releases,Roadmap,SprintStatus,BlueprintOS_Executive_Blueprint.*}`, `docs/client/Changelog.md` | Atualizar | Conteúdo derivado publicado em 30/07 ainda referencia A10; a fonte canônica contém A11/A12. | Alta | Médio | Executar pipeline de publicação controlado e revisar o diff antes de aceitar saídas derivadas. |
| `docs/presentations/ROADMAP_UPDATE.md` | Atualizar | A orientação slide a slide recomenda A11 como próxima sprint e A10 como última, contrariando A11/A12 concluídas e ausência de próxima aprovada. | Alta | Alto | Atualizar somente com aprovação do Product Owner, por ser insumo executivo. |
| `docs/INDEX.md` | Atualizar | Descreve subáreas de banco, infraestrutura, agentes, memória e prompts como conteúdo disponível, embora estejam vazias ou não versionadas. | Alta | Médio | Reclassificar explicitamente como planejadas ou remover as promessas do índice em etapa documental. |

## 9. Código ou projetos órfãos

- **Nenhum projeto órfão confirmado:** os 8 `.csproj` encontrados pertencem a `backend/BlueprintOS.sln`.
- **Nenhuma duplicidade de nome de arquivo C# confirmada** fora de saídas `bin/`/`obj/`.
- **Nenhuma interface sem implementação confirmada:** os contratos analisados possuem implementação e/ou registro compatível; os serviços de documentação/publicação estão registrados no container de DI.
- `IAdrService`/`MarkdownAdrService` não é chamado pelos CLIs atuais, mas ADR-0009 documenta explicitamente que é um ponto de extensão testado. **Preservar**, não classificar como código morto.
- Não há worker, frontend, schema de banco ou projeto de agentes versionado para auditar como órfão.

## 10. Scripts e infraestrutura obsoletos

- `Makefile`, `docker-compose.yml` e `Dockerfile` possuem referências coerentes ao backend atual.
- `docker-compose.override.yml` está vazio (candidato listado na seção 4).
- Não há pipelines GitHub Actions, Terraform, Kubernetes, Nginx, monitoramento, shell scripts, PowerShell ou SQL versionados; são estruturas planejadas, não implementações abandonadas comprovadas.

## 11. Itens preservados

- Todos os oito projetos .NET, código e testes.
- Os documentos de negócio em `docs/presentations/`, incluindo trabalhos paralelos não rastreados.
- Design system, assets, templates PowerPoint e variantes de preview sem evidência conclusiva de duplicidade.
- Documentos e contratos deliberadamente extensíveis, como o serviço de ADR.

## 12. Itens que exigem decisão do Product Owner

1. Escolher a fonte oficial única entre documentos de visão/escopo legados e `VISION.md`.
2. Aprovar a republicação dos documentos por público e do roteiro executivo para eliminar o estado A10 obsoleto.
3. Definir o destino dos templates e prompts vazios: preencher, consolidar ou retirar do repositório.
4. Decidir se `DocumentationHealth.md` é evidência versionada ou artefato de CI.
5. Aprovar eventual remoção de `docker-compose.override.yml` vazio e a política para arquivos de metadados vazios.

## 13. Plano recomendado para a Etapa 3

1. Aprovar um pacote documental pequeno, com uma fonte canônica por assunto.
2. Regerar documentos derivados em ambiente controlado e revisar as diferenças factuais antes de versionar.
3. Tratar placeholders vazios em alteração separada, com decisão explícita sobre cada grupo.
4. Só então avaliar remoções, movimentos ou renomeações de baixo risco.
