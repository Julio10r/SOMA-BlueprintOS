# Dashboard — Camada de Estado Consolidado

## Objetivo

Desacoplar completamente a apresentação visual do projeto (Dashboard HTML, ou qualquer tecnologia futura — React, Grafana, Power BI) da lógica de negócio e da documentação oficial. Nenhuma superfície visual consome documentos do projeto diretamente; todas consomem exclusivamente `DASHBOARD_STATE.md`.

## Responsabilidade

- Esta área **não é fonte de verdade**. A documentação oficial (`.ai/ROADMAP.md`, `.ai/BACKLOG.md`, `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/DOCUMENTATION_STRATEGY.md` e demais) continua sendo a única fonte de verdade do projeto.
- `DASHBOARD_STATE.md` é um documento **derivado**, gerado a partir da leitura dessas fontes — nunca editado manualmente, nunca a origem de uma decisão.
- O Dashboard (HTML ou qualquer tecnologia futura) possui responsabilidade **exclusivamente visual**: não interpreta documentação, não calcula indicadores, não aplica regras de negócio. Ele apenas lê `DASHBOARD_STATE.md`.

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

## Fluxo de atualização

O comando `[atualizar dashboard]` (especificação completa e permanente em [`DASHBOARD_UPDATE_COMMAND.md`](./DASHBOARD_UPDATE_COMMAND.md); resumo operacional também em `DASHBOARD_STATE.md` §Comando de Atualização) é o único mecanismo que regenera este documento. Nenhuma edição manual de `DASHBOARD_STATE.md` é válida — qualquer edição manual será sobrescrita na próxima atualização e deve ser tratada como um bug de processo, não como fonte de dado.

## Regra de manutenção

- `DASHBOARD_STATE.md` nunca é editado manualmente.
- Toda alteração de estado nasce em uma fonte oficial (tabela acima); o Dashboard apenas reflete, nunca origina.
- Nenhuma informação inexistente é inventada: um indicador sem fonte suficiente é registrado como "Não aplicável" ou "Pendente", nunca estimado.
- Percentuais são sempre derivados por cálculo a partir da documentação — nunca preenchidos manualmente quando puderem ser calculados (ver política de percentuais em `DASHBOARD_STATE.md`).

## Progresso Técnico vs. Contribuição ao MVP

Cada Onda do `DASHBOARD_STATE.md` registra dois indicadores explicitamente distintos, que nunca são misturados nem exibidos como um único número:

- **Progresso Técnico:** execução comprovada dos entregáveis. Deriva exclusivamente da contagem de entregáveis com status "Concluído" (mais a fração de entregáveis "Em desenvolvimento" que já possuam percentual individual registrado — nunca estimado na ausência desse dado) sobre o total de entregáveis da Onda, independentemente de a Onda ter sido formalmente iniciada.
- **Contribuição ao MVP (pontos):** Peso Gerencial da Onda × Progresso Técnico da Onda. A partir da Work Order "Ajuste Final de Percentuais, Gantt e Resumo Executivo dos MVPs" (06/08/2026), **contribui proporcionalmente ao MVP Global mesmo quando a Onda ainda está com Status "Planejado"** — não há mais nenhuma condição de início formal da Onda para que sua contribuição seja contada. É o indicador que, somado entre todos os componentes, alimenta o Percentual Global do MVP 1.0 (Σ Peso Gerencial × Progresso Técnico).

O Dashboard (HTML ou qualquer tecnologia futura) apenas renderiza os valores já calculados no `DASHBOARD_STATE.md`, sempre rotulados individualmente; nenhum cálculo, mistura ou substituição de um indicador pelo outro ocorre fora do `DASHBOARD_STATE.md`. O Percentual Global do MVP 1.0 é apresentado com seu valor exato (ex.: 28,6%) disponível em tooltip/detalhe acessível, e arredondado apenas para exibição principal (ex.: 29%) — o arredondamento é responsabilidade exclusivamente visual do Dashboard, nunca um recálculo do indicador.

## Roadmap dos Produtos (aba Executive)

A seção "Roadmap dos Produtos" do `DASHBOARD_STATE.md` consolida, para a aba Executive, o resumo do MVP 1.0 (objetivo geral, Percentual Global Atual, Onda Atual, Marco Final) e do MVP 1.1 (objetivo geral e escopo adiado). O Dashboard nunca lê `.ai/ROADMAP.md` ou `.ai/BACKLOG.md` diretamente para montar esta seção, e o escopo do MVP 1.1 nunca é uma lista fixa no HTML — é sempre derivado desta seção do `DASHBOARD_STATE.md` durante sua atualização.

## Preparação para evolução futura

Nesta etapa, a área contém apenas `README.md` e `DASHBOARD_STATE.md`. A estrutura permite expansão futura sem quebra de compatibilidade — por exemplo, um `HISTORY.md` (série temporal de estados consolidados) ou um schema formal de validação (`DASHBOARD_SCHEMA.md`/JSON Schema) podem ser adicionados quando houver necessidade real, sem alterar o papel de `DASHBOARD_STATE.md` como único consumível pelo Dashboard. Nenhum desses arquivos adicionais é criado nesta etapa.
