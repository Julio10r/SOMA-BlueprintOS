# Cadernos por Onda

## Objetivo

Registro versionado, curto e datável de decisões, achados e pendências arquiteturais/funcionais/técnicas
que **não** justificam por si só uma Work Order ou ADR imediata, mas que precisam ficar rastreadas para a
onda correta (ou para o Encerramento do Projeto). Não substitui `.ai/DECISIONS.md` (ADRs), `.ai/BACKLOG.md`
(Work Orders) nem `.ai/PROJECT_STATE.md`/`.ai/CURRENT_SPRINT.md` (estado/execução) — é o lugar para anotar
algo **antes** de virar uma dessas três coisas, ou para registrar uma decisão que deliberadamente só será
tratada mais adiante.

## Arquivos

- `Onda-1.md`, `Onda-2.md`, `Onda-3.md`, ... — um arquivo por onda do roadmap (`.ai/ROADMAP.md`/`BACKLOG.md`).
  Uma anotação é sempre registrada no arquivo da onda em que o achado/decisão **surgiu** — mesmo que o campo
  `Tratar em` diga que ela só será endereçada mais adiante (próxima onda, onda específica, ou Encerramento do
  Projeto). Não mover a entrada para o arquivo de destino quando "Tratar em" chegar; ela permanece no arquivo
  de origem para preservar rastreabilidade histórica, apenas com `Status`/`Decisão` atualizados.
- `Encerramento-Projeto.md` — itens que só fazem sentido resolver (ou só podem ser plenamente validados) ao
  final do projeto (ex.: generalização Multi-BU/Multi-ERP quando existir uma segunda Unidade de Negócio
  real). Consolida especificamente os itens transversais/finais que váRias ondas apontaram como "Tratar em:
  Encerramento do projeto" — não é um resumo geral do projeto.

O campo `Tratar em` do template abaixo é o que define **quando** agir sobre uma entrada — nunca quando ela
foi descoberta nem em qual arquivo ela vive. Uma entrada pode nascer na Onda 2 e ter `Tratar em: Onda
específica (nome)` ou `Encerramento do projeto`, permanecendo registrada em `Onda-2.md`.

## Template de entrada

Cada anotação usa este formato mínimo:

```markdown
### <Título curto>

- **Origem:** <de onde veio o achado — sprint, discovery, auditoria, sessão>
- **Assunto:** <uma frase>
- **Tipo:** Arquitetura | Funcional | Técnico | UX | Segurança | Integração | Governança | Documentação
- **Tratar em:** Nesta onda | Próxima onda | Onda específica (nome) | Encerramento do projeto | Somente documentação
- **Status:** Pendente | Em análise | Decidido | Implementado | Descartado
- **Resumo:** <o que é, por que importa>
- **Decisão:** <se já existe uma decisão do Product Owner, registrar aqui — senão omitir a linha>
```

## Regras de uso

- Entradas nunca são apagadas quando o status muda — apenas atualizadas (`Status`, `Decisão`). Histórico de
  mudança de status vive no próprio texto da entrada (ex.: "Status: Decidido em <data> — antes Em análise").
- Uma entrada "Decidido"/"Implementado" que gerar código real deve referenciar a Work Order/ADR/commit
  correspondente assim que existir, mas continua registrada aqui para rastreabilidade histórica.
- Não recriar aqui o que já está em `.ai/DECISIONS.md` (ADR) — se uma entrada amadurece para ADR, ela
  referencia o número do ADR e é marcada `Status: Decidido`.
