# Runbook — Showcase Agent (Coleta de Catálogo, Grade e Fotos)

## Quando Usar

Use este runbook quando o Product Owner pedir a coleta do catálogo do Showcase (produtos, cores, grade, fotos), com ou sem enriquecimento de saldo WISE.

Instrução curta recomendada:

```text
Executar coleta Showcase com fotos e saldos WISE.
```

A marca/região **não** faz parte do gatilho — vem sempre da sessão autenticada no Chrome no momento da execução. Nenhuma marca é hardcoded neste runbook nem no coletor.

Conhecimento canônico: [`.ai/context/showcase-knowledge.md`](../../.ai/context/showcase-knowledge.md).

## Pré-Requisitos

- Chrome DevTools MCP conectado à sessão.
- Login manual do Product Owner no Showcase (`soma.compuwise.com.br` ou equivalente informado) — o agente nunca preenche credenciais.
- Node.js disponível para rodar [`scripts/showcase_collector/`](../../scripts/showcase_collector/).
- Para enriquecimento de saldo: WISE Agent disponível (ver [WiseAgentRunbook.md](./WiseAgentRunbook.md)) — mesmos pré-requisitos daquele runbook.
- Nunca imprimir token, senha ou connection string.

## Gate de Login

1. Abrir/reutilizar o Chrome via Chrome DevTools MCP na URL do Showcase.
2. Se a página carregada for a tela de login, **parar imediatamente**:
   ```text
   Página de login aberta. Aguardando você efetuar o login manualmente.
   ```
3. Aguardar confirmação explícita do Product Owner antes de continuar.
4. Nunca tentar preencher usuário, senha, OTP ou MFA.

## Gate de Contexto (obrigatório, a cada execução)

Depois do login confirmado, extrair o contexto da sessão atual (ver `showcase-knowledge.md`, seção "Como Extrair o Contexto da Sessão") e informar:

```text
SHOWCASE AUTENTICADO

Marca: <detectada ou "não identificada">
Mercado/Região: <detectado ou "não identificado">
Contexto/Collection: <detectado ou "não identificado">
Produtos disponíveis: <quantidade ou "não identificado">
```

Nunca inventar um valor que não pôde ser determinado com segurança — reportar como "não identificado".

## Fluxo Operacional

1. Confirmar login (Gate de Login).
2. Extrair contexto da sessão e emitir o bloco `SHOWCASE AUTENTICADO` (Gate de Contexto).
3. Exportar o contexto extraído como variáveis de ambiente (`SHOWCASE_TOKEN`, `SHOWCASE_BRAND_ID`, `SHOWCASE_COMPANY_ID`, `SHOWCASE_DEPT_ID`, `SHOWCASE_COLLECTION_ID`, `SHOWCASE_CUSTOMER_ID`, `SHOWCASE_PRICELIST`, `SHOWCASE_PAYMENT`, `SHOWCASE_ORDER_ID`) — nunca em arquivo versionado.
4. Rodar `node collect.js` em [`scripts/showcase_collector/`](../../scripts/showcase_collector/) — pagina o catálogo completo, coleta grade (`stock`) e baixa fotos, com checkpoint em `coleta_showcase.csv`.
5. Opcional: `node enrich.js` para completar `LINHA`/`BASE`/`FABRIC`.
6. Se o Product Owner pediu saldo WISE: passar a lista PRODUTO+COR coletada ao WISE Agent ([WiseAgentRunbook.md](./WiseAgentRunbook.md)), informando a campanha quando solicitado — o Showcase Agent nunca interpreta `WS_ESTOQUE_PRODUTOS` sozinho.
7. Rodar `node build_excel.js` para gerar `catalogo_showcase.xlsx` no layout validado.
8. Gerar relatório final: produtos/cores processados, fotos baixadas, erros, itens sem foto, itens sem correspondência WISE (quando aplicável).
9. Se a sessão expirar durante a coleta (HTML de login, HTTP 401/403), parar, salvar o parcial e pedir novo login — nunca contornar.

## Retomada

Reexecutar `node collect.js` é seguro: fotos já marcadas `ok` em `coleta_showcase.csv`, com arquivo local íntegro, não são baixadas de novo. O catálogo (paginação) é sempre repaginado do zero — é rápido e garante que nada novo tenha sido perdido.

## Segurança

- Comportamento padrão: **somente leitura** no Showcase — consultar, extrair, baixar, consolidar.
- Nunca alterar pedido, carrinho, cadastro, configuração ou qualquer dado do portal.
- Nunca disparar centenas de requisições simultâneas — respeitar a cadência já validada (ver `showcase-knowledge.md`, "Implementação Validada").
- Nunca persistir token/cookie/senha em Git ou memória permanente.

## Autoteste Conceitual

Ver seção "Autoteste Conceitual" em [`.ai/context/showcase-knowledge.md`](../../.ai/context/showcase-knowledge.md).
