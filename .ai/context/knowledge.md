# context/knowledge.md

> Escopo: descreve o papel conceitual do módulo Knowledge (ver ARCHITECTURE.md §7). Este módulo ainda não possui detalhe de implementação registrado no handbook — as afirmações abaixo permanecem no nível de contrato/responsabilidade, não de implementação específica.

## Responsabilidade

O módulo Knowledge é responsável por reter e disponibilizar conhecimento organizacional para consulta por agentes e por usuários, funcionando como a base de memória de longo prazo do BlueprintOS (ver [memory.md](./memory.md)).

## Etapas conceituais

- **Ingestão** — recebimento de documentos e dados organizacionais (políticas, processos, históricos) para incorporação à base de conhecimento.
- **Indexação** — organização do conteúdo ingerido de forma recuperável, tipicamente via embeddings/representações vetoriais, para suportar busca semântica.
- **Recuperação (retrieval)** — consulta à base de conhecimento a partir de uma pergunta ou necessidade de contexto, retornando os trechos mais relevantes.

Essas três etapas formam o padrão geralmente conhecido como RAG (Retrieval-Augmented Generation), citado em AI_TEAM.md como responsabilidade do Especialista IA.

## Como alimenta Memory e Agents

- **Memory:** o Knowledge é a implementação técnica da memória de longo prazo descrita em [memory.md](./memory.md) — o que nela é chamado de "base de conhecimento organizacional" corresponde ao conteúdo indexado pelo Knowledge.
- **Agents:** agentes com responsabilidade de consulta a conhecimento organizacional (equivalente ao Especialista IA de AI_TEAM.md) usam o Knowledge como ferramenta durante a execução (ver [runtime.md](./runtime.md), modelo de execução de ferramentas).

## Contrato esperado

Como qualquer módulo, Knowledge deve ser consumido por outros módulos exclusivamente via Contracts (ARCHITECTURE.md §9) — por exemplo, uma operação de busca semântica exposta como contrato, sem que o módulo consumidor acesse diretamente a infraestrutura de indexação/armazenamento vetorial do Knowledge.

## O que este documento não define

Este documento não especifica motor de indexação, provedor de embeddings, ou schema de armazenamento — essas decisões devem ser registradas como ADR em [DECISIONS.md](../DECISIONS.md) quando implementadas, seguindo o fluxo de mudança arquitetural de WORKFLOW.md §16.

## Conhecimento operacional persistido em Markdown

Enquanto o módulo Knowledge persistente/versionado não expõe uma rotina de ingestão automática para este tipo de runbook operacional, conhecimentos canônicos validados podem ser persistidos em `.ai/context/` e linkados a partir deste handbook.

Entradas atuais:

- [linx-wise-daily-integration.md](./linx-wise-daily-integration.md) — conhecimento operacional aprovado para a rotina diária Linx/WISE (`MB_PROD_EXTRA_WEB`, validações Linx, WISE e `WS_ESTOQUE_PRODUTOS`).
- [wise-knowledge.md](./wise-knowledge.md) — conhecimento operacional do WISE Agent: ambiente WISE, Linked Server `WISE_AZURE`, campanhas, saldo/estoque, estrutura `WS_*`, e relacionamento Showcase ↔ WISE.
- [showcase-knowledge.md](./showcase-knowledge.md) — conhecimento operacional do Showcase Agent: autenticação/sessão, detecção de marca/região, API do Showcase, regra produto/cor, download de fotos e layout de Excel validado — genérico para qualquer marca/região, nunca fixado.
- [../../agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md](../../agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.generated.md) — conhecimento Linx do domínio Fornecedor/CNPJ (`CADASTRO_CLI_FOR`, `LX_SEQUENCIAL`, triggers, `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`, decisões de arquitetura do fluxo +Compras×Linx) consumido por `linx-erp-specialist-agent`/`linx-database-specialist-agent`. Diferente dos itens acima, este arquivo é **gerado deterministicamente** por `tools/agents/generate-linx-fornecedor-knowledge.js` a partir da fonte estruturada `agents/knowledge/linx-fornecedor-cnpj/linx-fornecedor-knowledge.source.json` — nunca editar o `.generated.md` a mão.
