# Estrutura do Repositório SOMA-BlueprintOS

Este documento descreve a organização física do repositório após a
reorganização multi-aplicação (ver
`docs/repository/RepositoryReorganization-Audit.md` para a auditoria que
fundamentou as decisões, e `docs/repository/RepositoryReorganization-Final.md`
para o relatório da execução). Ele existe para que qualquer pessoa (ou
agente) que clone o repositório saiba, sem precisar perguntar, onde cada
tipo de conteúdo nasce.

**Princípio-guia**: organizar por RESPONSABILIDADE e OWNERSHIP, nunca por
estética.

## Árvore de topo

```
SOMA-BlueprintOS/
├── agents/                  # Ecossistema de Agents (governança, contratos, catálogo)
├── applications/
│   └── mais-compras/        # Aplicação +Compras (único produto hoje)
├── shared/                  # Componentes reutilizáveis entre aplicações futuras
├── tools/                   # Infraestrutura/tooling do repositório (validadores, scripts de agent)
├── scripts/                 # Automações compartilhadas (dev, integrações operacionais)
├── docs/
│   └── repository/          # Documentação sobre o repositório em si
├── infrastructure/          # Scaffolding de infraestrutura (terraform/docker/k8s/nginx/monitoring)
├── .ai/                     # Contexto e memória de trabalho da AI Factory (ver classificação abaixo)
├── .empty/                  # Quarentena — nunca fonte canônica, nunca descarte silencioso
└── dist/                    # Saída gerada (guias publicados) — não editar à mão
```

## `agents/` — ecossistema de Agents

Tudo que define identidade, contrato, governança e catálogo dos Agents.

- `agents/<agent-id>/agent.yaml` — manifesto canônico de cada Agent.
- `agents/AGENT_CONTRACT.md`, `agents/EXECUTION_POLICY.md`,
  `agents/DATABASE_CONNECTION_POLICY.md`,
  `agents/CAPABILITY_GAP_AND_AGENT_EVOLUTION_POLICY.md`,
  `agents/USER_ARTIFACT_LEARNING_POLICY.md` — políticas canônicas, provider-agnostic.
- `agents/docs/` — documentação específica de Agents (`Agents.md`,
  `AgentsCatalog.html`, `AgentsCatalog.generated.html`, série `ai-factory/*`,
  `AIGovernance.md`, diagramas `.mmd`).

**Regra para novo conteúdo**: só entra aqui o que define identidade,
contrato, capacidade ou governança de um Agent. Documentação operacional de
uma aplicação (runbooks de deploy, troubleshooting de uma feature) NÃO vai
para cá mesmo que mencione um Agent — vai para `applications/<app>/docs/` ou
para a raiz de `docs/` se for verdadeiramente transversal.

## `applications/<nome>/` — produtos/aplicações

Cada aplicação é uma unidade autocontida:

```
applications/mais-compras/
├── backend/     # solução .NET completa (sln, src, tests, tools)
├── frontend/    # SPA React/Vite completa
├── docs/        # documentação específica do produto +Compras
└── resources/   # recursos específicos da aplicação (não reutilizáveis)
```

**Regra para nova aplicação**: crie `applications/<nome-canônico>/` com a
mesma forma (backend/frontend/docs/resources conforme aplicável). Nunca
misture código de duas aplicações na mesma árvore `backend/`ou `frontend/`.
Se dois produtos precisarem do mesmo componente, o componente vai para
`shared/`, não é duplicado.

## `shared/` — componentes realmente reutilizáveis

```
shared/
└── design-system/   # tokens, ícones, templates, presets multi-marca (AZZAS/GDT)
```

**Regra**: só entra em `shared/` o que tem justificativa concreta de reuso
por mais de uma aplicação hoje ou em um roadmap já definido. Não é uma
"gaveta de miscelânea" — arquitetura e conhecimento genuinamente
transversais (ex.: princípios de domínio compartilhados entre futuras
aplicações) devem crescer como `shared/architecture/` e `shared/knowledge/`
apenas quando esse conteúdo existir de fato; hoje o conteúdo de arquitetura
levantado na auditoria (RBAC, security design, domain principles) é
específico do domínio do +Compras e por isso vive em
`applications/mais-compras/docs/architecture/`, não em `shared/`.

## `tools/` e `scripts/`

- `tools/agents/` — validador de manifesto de Agents, gerador de catálogo e
  demais utilitários de tooling do ecossistema de Agents.
- `scripts/` — automações de desenvolvimento e operação que atravessam (ou
  ainda não foram atribuídas a) uma única aplicação: `start-dev.sh`,
  `stop-dev.sh`, `health-check.sh`, scripts de integração Linx/Wise.

**Regra**: um script exclusivo de uma aplicação deve migrar para
`applications/<app>/scripts/` quando essa pasta existir; um script
específico de um Agent avalia `agents/<id>/` ou `tools/agents/` conforme a
responsabilidade atual. Não mova scripts só por estética.

## `docs/`

- `docs/repository/` — documentação sobre a organização do próprio
  repositório (este arquivo, a auditoria, o relatório final, diagramas de
  solução/dependências).
- `docs/audits/` — arquivo local de auditorias históricas (gitignored,
  conteúdo misto Agents/+Compras acumulado ao longo do projeto); mantido
  como está por não ser rastreado pelo git — candidato a triagem futura,
  fora do escopo desta reorganização física.

Documentação específica de aplicação vai em
`applications/<app>/docs/`; documentação específica de Agents vai em
`agents/docs/`. Não duplicar documentos entre essas três árvores.

## `.ai/`

Contexto e memória de trabalho da AI Factory (`PROJECT_STATE.md`,
`BACKLOG.md`, `work-orders/`, `context/`, `memory/`, `dashboard/`, etc.).
Mantido na raiz sem movimentação em massa: é consumido por processos e
scripts (ex.: `scripts/linx_wise_daily_integration.py`) via paths relativos
`ROOT / ".ai" / ...`, e boa parte do seu conteúdo é genuinamente transversal
ao projeto (não específico só de +Compras ou só de Agents). Itens com forte
evidência de obsolescência foram movidos para `.empty/` (ver
`QUARANTINE_MANIFEST.md`); o restante permanece e deve continuar sendo
tratado como está até uma decisão explícita de triagem futura.

## `.empty/` — quarentena

Nunca é fonte canônica. Só recebe itens com evidência concreta de
obsolescência, duplicação ou saída gerada sem valor de referência. Nada é
apagado nesta reorganização — remoção definitiva exige revisão humana
explícita, documentada em `.empty/QUARANTINE_MANIFEST.md`.

## `infrastructure/`, `dist/`, `mcp/`

- `infrastructure/` — scaffolding de infraestrutura (terraform/docker/k8s/
  nginx/monitoring) ainda majoritariamente vazio; fica na raiz por ser
  potencialmente compartilhado entre futuras aplicações, não específico do
  +Compras.
- `dist/` — saída gerada (guias publicados client/engineering/executive);
  nunca editar à mão, é artefato de build.
- `mcp/design-system/` — contém apenas um `README.md` (não é uma duplicata
  de ativos de `shared/design-system/`); mantido como está.

## Itens fora de escopo desta reorganização

- `.myNotes` — sinalizado por conter possível credencial em texto claro;
  problema de segurança que requer ação humana direta (rotação +
  remoção/gitignore), não uma decisão de "para onde mover".
- `downloads/` — 586MB, gitignored, conteúdo temporário local; não
  versionado, não movido.
