# Prompt — Consultar WISE Agent

Use quando o Product Owner pedir, com qualquer frase equivalente a:

```text
Peça ao Agent WISE para buscar os saldos desses produtos.
```

ou:

```text
Use o Agent WISE para consultar a campanha 54.
```

ou qualquer pergunta sobre campanha, saldo/estoque WISE, estrutura `WS_*`, ou relacionamento Showcase ↔ WISE.

## Instrução para o Agent

1. Carregar o conhecimento canônico em [`.ai/context/wise-knowledge.md`](../context/wise-knowledge.md) antes de responder — nunca responder de memória de um chat anterior.
2. Se a tarefa envolver escrita na rotina diária Linx/WISE (`MB_PROD_EXTRA_WEB`, sincronização de estoque), usar em vez disso [`.ai/prompts/processar-planilha-integracao-linx-wise.md`](./processar-planilha-integracao-linx-wise.md) — este prompt é apenas para consulta/leitura.
3. Confirmar o ambiente antes de qualquer leitura sensível: `SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco;` (prosseguir apenas com `SRV-SOMADB`/`SOMA`).
4. Se a pergunta exigir `ID_CAMPANHA` e ele não tiver sido informado na própria tarefa, perguntar ao Product Owner — nunca escolher.
5. Responder sempre citando a fonte (tabela ou endpoint) e classificando o conhecimento usado (`CONFIRMADO`/`INFERIDO`/`AINDA_NAO_MAPEADO`, ver convenção em `wise-knowledge.md`).
6. Se a tarefa envolver o Showcase (`soma.compuwise.com.br`), seguir a mesma regra de login manual já documentada: nunca preencher credenciais, aguardar confirmação do Product Owner antes de qualquer navegação pós-login.
7. Se descobrir algo novo e confirmado sobre o WISE durante a tarefa, atualizar `.ai/context/wise-knowledge.md` na seção correspondente antes de finalizar.

## Nunca Fazer

- Nunca escolher `ID_CAMPANHA`.
- Nunca executar `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `MERGE`, `ALTER`, `DROP`, `CREATE` ou procedure de escrita automaticamente.
- Nunca preencher login/credenciais no Showcase.
- Nunca imprimir token, senha ou connection string.
- Nunca tratar um item marcado `INFERIDO` ou `AINDA_NAO_MAPEADO` em `wise-knowledge.md` como se fosse `CONFIRMADO`.
- Nunca duplicar a responsabilidade do Agent Linx (ver `wise-knowledge.md`, seção "Relação com o Agent Linx").

## Ver Também

- [`.ai/context/wise-knowledge.md`](../context/wise-knowledge.md) — conhecimento canônico do WISE Agent.
- [`docs/operations/WiseAgentRunbook.md`](../../docs/operations/WiseAgentRunbook.md) — runbook operacional.
- [`.ai/context/linx-wise-daily-integration.md`](../context/linx-wise-daily-integration.md) — rotina de escrita/sincronização diária Linx/WISE (domínio separado).
