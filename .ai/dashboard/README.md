# Dashboard — Camada de Estado Consolidado

## Objetivo

Desacoplar completamente a apresentação visual do projeto (Dashboard HTML, ou qualquer tecnologia futura — React, Grafana, Power BI) da lógica de negócio e da documentação oficial. `DASHBOARD_STATE.md` é o **Read Model oficial do projeto**: nenhuma superfície visual consome documentos do projeto diretamente; todas consomem exclusivamente `DASHBOARD_STATE.md`.

## Princípio arquitetural

Toda informação exibida em qualquer Dashboard deve existir previamente no `DASHBOARD_STATE.md`. Nenhum Dashboard pode:

- interpretar documentação;
- calcular indicadores;
- criar regras de negócio;
- inferir estados.

Sempre que um novo indicador for necessário, a ordem é sempre: **(1) atualizar o `DASHBOARD_STATE.md`, (2) atualizar o Dashboard.** Nunca o contrário. Esta regra vale para qualquer Dashboard, atual ou futuro (HTML, React, Power BI, Grafana etc.) — nenhuma interface pode depender diretamente dos documentos do projeto.

## Responsabilidade

- Esta área **não é fonte de verdade**. A documentação oficial (`.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DOCUMENTATION_STRATEGY.md` e demais) continua sendo a única fonte de verdade do projeto.
- `DASHBOARD_STATE.md` é um documento **derivado**, gerado a partir da leitura dessas fontes — nunca editado manualmente, nunca a origem de uma decisão.
- O Dashboard (HTML ou qualquer tecnologia futura) possui responsabilidade **exclusivamente visual**: não interpreta documentação, não calcula indicadores, não aplica regras de negócio. Ele apenas lê `DASHBOARD_STATE.md`.

## Cabeçalho do DASHBOARD_STATE

| Campo | Significado |
|---|---|
| Dashboard State | Identificador/versão interna deste Read Model |
| Schema Version | Versão da estrutura do documento (seções, campos, políticas) — incrementada quando a estrutura muda, independente do conteúdo |
| Project Version | Última tag/versão publicada do projeto (ex.: `v0.9.0-blueprint-foundation`) |
| Generated At | Data em que este estado foi gerado pela primeira vez |
| Last Update | Data da última execução de `[atualizar dashboard]` que alterou este documento |
| Status | Frase curta descrevendo a situação geral do projeto no momento da geração |

## Fontes oficiais e responsabilidade de cada uma

| Fonte | Alimenta no `DASHBOARD_STATE.md` |
|---|---|
| `.ai/ROADMAP.md` | Ondas, Gates, Datas (Planejada/Real/Replanejada), Marcos, percentual de Onda/MVP |
| `.ai/BACKLOG.md` | Work Orders, Status, reclassificação MVP 1.0/1.1, contagem por status |
| `.ai/PROJECT_STATE.md` | Situação atual, próximo marco, resumo executivo (fatos), qualidade (build/testes) |
| `.ai/CURRENT_SPRINT.md` | Trabalho em andamento, entregas da sprint corrente |
| `.ai/DOCUMENTATION_STRATEGY.md` | Saúde documental (documentos oficiais existentes e sua consistência) |
| `docs/product/` (`ComprasFuncional.md`, `ComprasUX.md`, `ComprasDataModel.md`) | Telas previstas/concluídas/em andamento, evolução do modelo de dados |
| `.ai/DECISIONS.md` | Decisões arquiteturais relevantes citadas no resumo executivo |
| `dist/health/DocumentationHealth.md` (quando publicado) | Indicadores de saúde documental (links inválidos, documentos sem título, cobertura) |

Demais documentos podem ser mapeados como fontes adicionais à medida que o Dashboard evoluir, seguindo o mesmo princípio: nunca uma fonte nova sem responsabilidade explícita registrada aqui.

## Regra de progresso das Ondas

As Ondas representam o cronograma oficial de entrega do MVP, **não** a ordem histórica em que funcionalidades foram desenvolvidas. Uma funcionalidade tecnicamente implementada antes da aprovação formal de sua Onda (ex.: Fornecedores, concluído tecnicamente antes do replanejamento do MVP 1.0 e posteriormente reclassificado na Onda 2) continua registrada normalmente na documentação e no backlog, mas **não antecipa artificialmente o percentual da Onda**. O percentual de uma Onda reflete exclusivamente entregáveis concluídos dentro da execução formal daquela Onda.

## Pesos Gerenciais do Roadmap

Os pesos de Foundation e das 5 Ondas (`DASHBOARD_STATE.md` §"Política dos pesos") são **Pesos Gerenciais do Roadmap** — não representam esforço técnico, quantidade de código, complexidade ou horas trabalhadas. Sua finalidade é exclusivamente permitir o acompanhamento executivo do progresso do MVP.

## Compatibilidade

Qualquer Dashboard futuro — HTML, React, Power BI, Grafana ou qualquer outra tecnologia — deve consumir exclusivamente o `DASHBOARD_STATE.md`. Nenhuma interface pode depender diretamente dos documentos do projeto. Esta regra não expira com a troca de tecnologia de apresentação; ela é a garantia de desacoplamento entre documentação (fonte de verdade) e visualização.

## Fluxo de atualização

O comando `[atualizar dashboard]` (ver especificação completa em `DASHBOARD_STATE.md` §Comando de Atualização) é o único mecanismo que regenera este documento. Nenhuma edição manual de `DASHBOARD_STATE.md` é válida — qualquer edição manual será sobrescrita na próxima atualização e deve ser tratada como um bug de processo, não como fonte de dado.

## Regra de manutenção

- `DASHBOARD_STATE.md` nunca é editado manualmente.
- Toda alteração de estado nasce em uma fonte oficial (tabela acima); o Dashboard apenas reflete, nunca origina.
- Nenhuma informação inexistente é inventada: um indicador sem fonte suficiente é registrado como "Não aplicável" ou "Pendente", nunca estimado.
- Percentuais são sempre derivados por cálculo a partir da documentação — nunca preenchidos manualmente quando puderem ser calculados (ver política de percentuais em `DASHBOARD_STATE.md`).

## Preparação para evolução futura

Nesta etapa, a área contém apenas `README.md` e `DASHBOARD_STATE.md`. A estrutura permite expansão futura sem quebra de compatibilidade — por exemplo, um `HISTORY.md` (série temporal de estados consolidados) ou um schema formal de validação (`DASHBOARD_SCHEMA.md`/JSON Schema) podem ser adicionados quando houver necessidade real, sem alterar o papel de `DASHBOARD_STATE.md` como único consumível pelo Dashboard. Nenhum desses arquivos adicionais é criado nesta etapa.
