# Sistema de Work Orders

Templates oficiais reutilizáveis para conduzir a engenharia do SOMA BlueprintOS. Servem a qualquer membro da equipe e a agentes como Codex e Claude Code; não substituem a aprovação do Product Owner nem o workflow oficial.

## Fontes canônicas

Antes de preencher ou executar qualquer template, leia [VISION.md](../VISION.md), [WORKFLOW.md](../WORKFLOW.md), [PROJECT_STATE.md](../PROJECT_STATE.md) e [CURRENT_SPRINT.md](../CURRENT_SPRINT.md), além da documentação específica da sprint. Arquitetura, padrões e Definition of Done permanecem nas fontes canônicas; os templates as referenciam sem as duplicar.

## Escolha do template

| Template | Utilize quando | Resultado esperado | Código por padrão |
|---|---|---|---|
| [Work Order](./WORK_ORDER_TEMPLATE.md) | houver desenvolvimento incremental aprovado | mudança implementada e validada | permitido no escopo aprovado |
| [Epic](./EPIC_TEMPLATE.md) | a iniciativa reunir múltiplas entregas dependentes | visão, decomposição e conclusão do épico | apenas pelas Work Orders aprovadas |
| [Audit](./AUDIT_TEMPLATE.md) | for necessário avaliar estado, risco ou conformidade | achados classificados e evidências | não permitido |
| [Refactor](./REFACTOR_TEMPLATE.md) | for preciso melhorar estrutura sem alterar comportamento | melhoria comprovada antes/depois | permitido no escopo aprovado |
| [Hotfix](./HOTFIX_TEMPLATE.md) | houver incidente urgente | correção segura e plano de rollback | permitido para a correção aprovada |
| [Spike](./SPIKE_TEMPLATE.md) | houver incerteza técnica relevante | decisão documentada e próximos passos | não definitivo |
| [Release](./RELEASE_TEMPLATE.md) | uma versão estiver pronta para encerramento | pacote de release validado | somente ajustes estritamente necessários |

## Fluxo recomendado

1. Selecionar o template que corresponde ao tipo de demanda.
2. Preencher metadados, objetivo, escopo, dependências, riscos e aceite, sem inventar requisitos.
3. Garantir a Definition of Ready e a aprovação exigida por [WORKFLOW.md](../WORKFLOW.md).
4. Executar somente o escopo autorizado, mantendo Clean Architecture, SOLID, Design System e padrões existentes.
5. Validar build, testes aplicáveis e links Markdown; registrar evidências no relatório final.
6. Atualizar os documentos canônicos aplicáveis e seguir o Git Flow e Conventional Commits definidos no projeto.

## Boas práticas

- Use uma única fonte para cada decisão e prefira links a repetir regras oficiais.
- Declare explicitamente o que não será feito; melhorias fora do escopo são registradas, não implementadas.
- Trate MSSQL, compatibilidade, segurança e observabilidade conforme o impacto real da demanda.
- Não inicie uma sprint sem Work Order aprovada e registrada em `CURRENT_SPRINT.md`.
- Mantenha o relatório final factual: resultados, riscos e evidências devem refletir o que foi executado.

## Uso

Copie o template escolhido para o local de documentação definido pela sprint e preencha os campos vazios. O conteúdo entre colchetes é um marcador a substituir; ele não é requisito nem exemplo de domínio.
