# Prompt — Executar Coleta Showcase (com Fotos e Saldos WISE)

Use quando o Product Owner pedir, com qualquer frase equivalente a:

```text
Executar coleta Showcase com fotos e saldos WISE.
```

A marca/região **nunca** faz parte do gatilho — o contexto vem da sessão autenticada no Chrome no momento da execução.

## Instrução para o Agent

1. Carregar o conhecimento canônico em [`.ai/context/showcase-knowledge.md`](../context/showcase-knowledge.md) antes de agir — nunca responder/agir de memória de um chat anterior.
2. Abrir/reutilizar o Chrome via Chrome DevTools MCP. Se cair na tela de login, **parar** e aguardar confirmação do Product Owner (nunca preencher credenciais).
3. Após confirmação, extrair o contexto da sessão (token + `brand_Id`/`company_Id`/`dept_Id`/`collection_Id`/`customer_Id`/`pricelist`/`payment`/`order_Id`) conforme "Como Extrair o Contexto da Sessão" em `showcase-knowledge.md`. Manter apenas em memória da execução — nunca gravar em arquivo versionado.
4. Informar o bloco de abertura:
   ```text
   SHOWCASE AUTENTICADO

   Marca: <detectada ou "não identificada">
   Mercado/Região: <detectado ou "não identificado">
   Contexto/Collection: <detectado ou "não identificado">
   Produtos disponíveis: <quantidade ou "não identificado">
   ```
5. Rodar a coleta usando a implementação validada em [`scripts/showcase_collector/`](../../scripts/showcase_collector/) (`collect.js` → `enrich.js` opcional → `build_excel.js`), com o contexto extraído como variáveis de ambiente (ver `scripts/showcase_collector/README.md`).
6. Para saldo WISE: passar o catálogo PRODUTO+COR coletado ao WISE Agent (ver [`.ai/prompts/consultar-wise.md`](./consultar-wise.md)) — nunca reimplementar a lógica de campanha/estoque aqui.
7. Gerar `catalogo_showcase.xlsx` no layout validado (documentado em `showcase-knowledge.md`, seção "Excel — Layout Validado") e um relatório final (produtos processados, fotos baixadas, erros, itens sem foto, itens sem correspondência WISE).
8. Se descobrir algo novo e confirmado (novo endpoint, particularidade de marca/região), atualizar `.ai/context/showcase-knowledge.md` antes de finalizar.

## Nunca Fazer

- Nunca preencher login/credenciais/MFA no Showcase.
- Nunca persistir token, cookie, senha ou segredo em Git ou memória permanente.
- Nunca assumir marca/região/`collection_Id` fixos — sempre detectar da sessão atual.
- Nunca executar ação de escrita no Showcase (pedido, carrinho, cadastro, configuração).
- Nunca duplicar a lógica do WISE Agent para saldo/campanha.
- Nunca tratar produto/cor sem imagem como erro de coleta.
- Nunca contornar uma sessão expirada — parar e pedir novo login.

## Ver Também

- [`.ai/context/showcase-knowledge.md`](../context/showcase-knowledge.md) — conhecimento canônico do Showcase Agent.
- [`docs/operations/ShowcaseAgentRunbook.md`](../../docs/operations/ShowcaseAgentRunbook.md) — runbook operacional.
- [`.ai/prompts/consultar-wise.md`](./consultar-wise.md) — WISE Agent, para enriquecimento de saldo.
- [`scripts/showcase_collector/`](../../scripts/showcase_collector/) — implementação validada.
