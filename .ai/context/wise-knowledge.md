# WISE Agent — Conhecimento Operacional Persistido

> Conhecimento reutilizável e evolutivo do especialista WISE (ambiente WISE/Compuwise, Linked Server `WISE_AZURE`, banco `SOMA_LINX`, Showcase). Consolida o aprendizado confirmado durante a sessão de coleta do Showcase FARM em 2026-08-27. Trata-se de conhecimento operacional, não de transcrição de chat — deve ser tratado como fonte viva e atualizada a cada nova descoberta confirmada (ver seção "Como Evoluir Este Documento").

Este documento segue o mesmo modelo já estabelecido em [linx-wise-daily-integration.md](./linx-wise-daily-integration.md) para a rotina diária Linx/WISE. Os dois documentos são complementares: aquele cobre a rotina de escrita/sincronização Linx→WISE; este cobre o especialista de leitura/consulta ao ambiente WISE em geral (campanhas, estoque, Showcase).

## Perfil do Agente (template obrigatório — AI_TEAM.md, "Criação de Novos Agentes")

- **Nome:** WISE Agent (identificador técnico: `wise-agent`)
- **Objetivo:** ser o especialista reutilizável em consulta, leitura e interpretação do ambiente WISE — campanhas, estoque, produto/cor, grades/saldos e integração com o Showcase — para que outros agentes e o Product Owner não precisem reexplicar essa estrutura a cada sessão.
- **Responsabilidade:** consultar `[WISE_AZURE].[SOMA_LINX].[dbo].*` e a API do Showcase autenticada; interpretar corretamente campanhas, saldos, grades e o relacionamento PRODUTO+COR; documentar novo conhecimento confirmado neste arquivo.
- **Limites (nunca faz):**
  - nunca escreve no WISE por conta própria (ver "Segurança" abaixo — isso é domínio da rotina Linx/WISE já documentada em [linx-wise-daily-integration.md](./linx-wise-daily-integration.md), não deste agente);
  - nunca escolhe `ID_CAMPANHA` sozinho — sempre pergunta ao Product Owner, salvo quando a campanha já foi explicitamente informada na tarefa;
  - nunca transforma inferência em regra definitiva sem marcá-la como `INFERIDO`;
  - nunca duplica responsabilidade do Agent Linx (ver "Relação com o Agent Linx");
  - nunca imprime segredo/senha/token/connection string.
- **Ferramentas:** acesso SQL somente leitura ao SQL Server de produção Linx (`LINX_PROD_*`, mesmo mecanismo já usado pela rotina diária Linx/WISE) navegando via Linked Server `WISE_AZURE`; e, quando a tarefa envolver o Showcase, Chrome DevTools MCP com sessão autenticada pelo Product Owner (nunca preenche login).
- **Entradas esperadas:** pergunta em linguagem natural sobre campanha/produto/cor/saldo/grade WISE, ou uma lista de produto/cor (ex.: vinda de uma planilha ou do catálogo Showcase) para cruzar com saldo WISE.
- **Saídas esperadas:** resposta direta com dado(s) consultado(s), sempre indicando a fonte (tabela/endpoint) e classificando o conhecimento usado (`CONFIRMADO`/`INFERIDO`/`AINDA NÃO MAPEADO`); para escrita, apenas uma proposta de alteração aguardando autorização explícita (nunca a execução).
- **Critérios de qualidade:** nunca inventa coluna, tabela, campanha ou mapeamento de tamanho não verificado; sempre confirma ambiente antes de qualquer leitura sensível; atualiza este arquivo quando descobre algo novo e confirmado.
- **Prompt base:** ver [.ai/prompts/consultar-wise.md](../prompts/consultar-wise.md).
- **Modelo utilizado:** o modelo de IA configurado no `IAIRuntime` do BlueprintOS no momento da execução (nenhum modelo específico é fixado por este documento).
- **Memória utilizada:** este arquivo (`wise-knowledge.md`) é a memória de longo prazo/reutilizável do agente. Memória de curto prazo (contexto da tarefa atual) segue o modelo padrão descrito em [context/memory.md](./memory.md).
- **Permissões:** somente leitura por padrão no WISE (ver "Segurança"). Nenhuma permissão de escrita é concedida a este agente.

## Rótulos de Proveniência

Mesma convenção de [linx-wise-daily-integration.md](./linx-wise-daily-integration.md):

- `CONFIRMADO`: verificado por consulta somente-leitura executada, resposta de API observada diretamente, ou schema/documento oficial.
- `DECISAO_PO`: definido explicitamente pelo Product Owner.
- `INFERIDO`: hipótese razoável a partir do que foi observado, ainda não verificada diretamente — nunca tratar como regra definitiva.
- `AINDA_NAO_MAPEADO`: conhecimento que este agente sabe que não sabe; deve consultar banco/código/documentação ou perguntar ao Product Owner antes de agir.
- `NAO_USAR`: comportamento legado ou tentado que não deve ser reproduzido.

## Ambiente e Acesso — CONFIRMADO

- `CONFIRMADO`: não existe uma conexão SQL direta e dedicada ao WISE nesta base de conhecimento. O acesso de leitura ao WISE a partir do SQL Server de produção Linx acontece via **Linked Server `WISE_AZURE`**, usando nomes de quatro partes: `[WISE_AZURE].[SOMA_LINX].[dbo].[TABELA]`. Isso reaproveita exatamente o mecanismo já confirmado e documentado em [linx-wise-daily-integration.md](./linx-wise-daily-integration.md#wise-destination) — este agente não introduz uma nova credencial.
- `CONFIRMADO`: variáveis de ambiente já usadas para a conexão de origem (Linx/SRV-SOMADB), reaproveitáveis para alcançar o Linked Server `WISE_AZURE`:
  - `LINX_PROD_SERVER`
  - `LINX_PROD_DATABASE`
  - `LINX_PROD_USER`
  - `LINX_PROD_PASSWORD`
- `CONFIRMADO`: antes de qualquer leitura sensível, confirmar o ambiente com `SELECT @@SERVERNAME AS servidor, DB_NAME() AS banco;` — prosseguir apenas se `servidor = SRV-SOMADB` e `banco = SOMA` (mesmo gate já documentado na rotina diária).
- `NAO_USAR`: nunca imprimir senha, connection string ou conteúdo de `.env`.
- `AINDA_NAO_MAPEADO`: nesta sessão (coleta Showcase FARM, 2026-08-27), não havia nenhum servidor MCP de SQL Server conectado — a consulta SQL direta pedida pelo Product Owner (`SELECT * FROM [WISE_AZURE].[SOMA_LINX].[DBO].WS_ESTOQUE_PRODUTOS WHERE ID_CAMPANHA = '54' AND DT_EXCLUSAO IS NULL`) não pôde ser executada por falta de ferramenta conectada, não por restrição de acesso. Uma sessão futura com MCP SQL Server configurado (ou com `.venv`/`pyodbc` disponível, como na rotina diária) deve conseguir executá-la.
- `CONFIRMADO`: quando o acesso SQL direto não está disponível, o próprio ambiente WISE expõe uma **API HTTP do Showcase** (ver seção dedicada abaixo) que devolve saldo por tamanho já nomeado, autenticada por sessão de usuário — via de consulta alternativa, não substitui a tabela bruta quando ela for explicitamente pedida.

## Tabela `WS_ESTOQUE_PRODUTOS` — CONFIRMADO / INFERIDO

- `DECISAO_PO`: tabela de referência para saldo/estoque WISE por campanha: `[WISE_AZURE].[SOMA_LINX].[DBO].WS_ESTOQUE_PRODUTOS`.
- `DECISAO_PO`: consulta de referência para saldo ativo de uma campanha:
  ```sql
  SELECT *
  FROM [WISE_AZURE].[SOMA_LINX].[DBO].WS_ESTOQUE_PRODUTOS
  WHERE ID_CAMPANHA = '54'
    AND DT_EXCLUSAO IS NULL
  ```
- `CONFIRMADO` (via [linx-wise-daily-integration.md](./linx-wise-daily-integration.md#ws_estoque_produtos)): chave operacional da tabela é `ID_CAMPANHA + PRODUTO + COR_PRODUTO`. `DT_EXCLUSAO IS NULL` identifica registro ativo; `DT_EXCLUSAO` preenchido é inativação lógica — a tabela nunca sofre `DELETE` físico.
- `CONFIRMADO` (mesma fonte): colunas de grade confirmadas na tabela são `ES1..ES16` (saldo por posição de tamanho) e `ESTOQUE` (saldo total), alimentadas a partir de `SALDO_DISPONIVEL` e `D1..D16` retornados pela function `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)`. Ou seja: **a fonte correta de saldo para escrita é a function, não uma leitura direta e isolada da tabela** — mas para leitura/consulta (o que este agente faz), a leitura direta de `WS_ESTOQUE_PRODUTOS` filtrada por `ID_CAMPANHA` e `DT_EXCLUSAO IS NULL` é o caminho correto e já usado como consulta de referência pelo Product Owner.
- `AINDA_NAO_MAPEADO`: mapeamento explícito de `ES1..ES16` para os rótulos de tamanho de venda (PP/P/M/G/GG etc.) não foi verificado diretamente nesta sessão — não presumir que `ES1..ES16` correspondem em ordem direta a PP/P/M/G/GG sem confirmar contra a grade real de cada produto (mesma cautela documentada na rotina diária, onde `TAM_1..TAM_7` mapeiam para `EX1..EX7` da tabela **`MB_PROD_EXTRA_WEB`**, uma tabela Linx diferente de `WS_ESTOQUE_PRODUTOS`; não confundir os dois conjuntos de colunas `EXn`/`ESn`, que pertencem a tabelas e sistemas diferentes).
- `CONFIRMADO` (via API do Showcase, ver abaixo): quando o saldo é obtido pelo endpoint `stock` do Showcase, ele já vem com o tamanho nomeado (`"size":"PP"`, `"size":"P"`, etc.), sem exigir esse mapeamento — via alternativa útil quando a leitura direta de `ES1..ES16` não estiver disponível ou não estiver mapeada.
- `AINDA_NAO_MAPEADO`: schema completo de colunas de `WS_ESTOQUE_PRODUTOS` (tipos, todas as colunas além das citadas acima) não foi lido diretamente nesta sessão — antes de uma tarefa que dependa de outras colunas, consultar `INFORMATION_SCHEMA.COLUMNS` via `[WISE_AZURE].[SOMA_LINX]` ou pedir ao Product Owner.

## Campanhas — CONFIRMADO / DECISAO_PO

- `DECISAO_PO`: `ID_CAMPANHA` nunca deve ser escolhido pelo agente — sempre perguntar ao Product Owner, a menos que já tenha sido informado explicitamente na tarefa (ex.: "consulte a campanha 54").
- `CONFIRMADO`: campanha usada como exemplo de trabalho nesta sessão: `ID_CAMPANHA = '54'` (valor fornecido pelo Product Owner, não escolhido pelo agente).
- `CONFIRMADO`: no Showcase (camada de aplicação), o conceito equivalente de "sessão de compra" aparece como `collection_Id` (ex.: `332`) e `order_Id`/`orderId` (carrinho, ex.: `949493`) — não confirmado se há correspondência 1:1 entre `collection_Id` do Showcase e `ID_CAMPANHA` do WISE; tratar como `AINDA_NAO_MAPEADO` até validação direta.

## Showcase — API HTTP Confirmada (Compuwise/WiseCommerce)

`CONFIRMADO` nesta sessão (coleta Showcase FARM, 2026-08-27, ambiente `soma.compuwise.com.br`, marca FARM):

- Frontend: `https://soma.compuwise.com.br` (produto "WiseCommerce"). Login manual obrigatório do Product Owner — o agente nunca preenche usuário/senha/OTP.
- API interna consumida pelo frontend, mesma origem de dados usada para exibir o Showcase: `https://wiseapi-gruposoma.azurewebsites.net/service.asmx/*`.
- Autenticação: Bearer JWT (`HS512`, claims incluem `unique_name`, `CodCli`, `CodCliFab`, `role`, `Agent`), válido por ~24h a partir do login. Obtido lendo `localStorage['0.soma|token']` na sessão autenticada do navegador (JSON string). **Nunca persistir esse token em arquivo versionado, memória textual ou log** — é uma credencial de sessão do Product Owner, não um segredo do agente.
- Parâmetros comuns observados em praticamente toda chamada: `brand_Id`, `company_Id`, `dept_Id`, `collection_Id`, `customer_Id`, `pricelist`, `payment`, `order_Id`, `coefficient` — variam por marca/cliente logado; não fixar valores como universais sem reconfirmar a sessão.
- Endpoints confirmados e sua função:
  - `showcase` — listagem paginada do catálogo (`page_number`, `page_size`, retorna `totalPages`). Uma linha por PRODUTO+COR em `vitrine`. Suporta `&distinct=true` para evitar duplicidade entre páginas.
  - `products?product_Id=X&color_Id=Y` — detalhe do produto: descrição, composição, preço, categoria, `line`, `base`, `fabric`, até 4 imagens (`imageShowcase`, `imageShowcase_Back`, `imageShowcase_Look`, `imageShowcase_Detail04`).
  - `productColors?product_Id=X` — cores do produto no formato `"{COR} - {DESC_COR}"`.
  - `stock?product_Id=X&colorId=Y` — saldo por tamanho **já nomeado** (`PP`/`P`/`M`/`G`/`GG`, não códigos internos), com `quantity` por tamanho.
  - `getItems`, `getSlideShow*`, `deliveries`, `tiposProduto` — endpoints auxiliares de UI, não centrais para a coleta de catálogo/estoque.
- Imagens: hospedadas em `https://wiseimagessoma.blob.core.windows.net/soma/imagens/{MARCA}/produtos/{PRODUTO}-{COR}-{N}.jpg` (`N` = 1, 2, 3, 4... sequencial). Nem todo produto/cor tem imagem — confirmado que combinações existem no catálogo sem nenhum campo `imageShowcase*` presente (estoque zerado de imagem, não erro de coleta).
- `NAO_USAR`: nunca preencher, alterar carrinho, finalizar pedido ou qualquer ação de escrita durante uma tarefa de consulta/leitura — o Showcase é ambiente transacional real (o `order_Id`/carrinho observado nesta sessão pertence à conta logada do Product Owner).

## Relacionamento Showcase ↔ WISE — CONFIRMADO

- `CONFIRMADO` (regra fornecida e validada pelo Product Owner nesta sessão): no Showcase, a cor é exibida no formato `CODIGO_COR - DESCRICAO_COR`, por exemplo `59721 - ONDINA_SAIA MALHA_OW`. Interpretação:
  - `COR = 59721`
  - `DESC_COR = ONDINA_SAIA MALHA_OW`
- `DECISAO_PO`: relacionamento lógico esperado:
  - Showcase `REF` (= `product_Id` na API) ↔ WISE `PRODUTO`
  - Showcase `COR` (= `color_Id` na API, número antes do hífen) ↔ WISE `COR_PRODUTO`
  - Chave lógica de cruzamento: **PRODUTO + COR** (nunca usar descrição como chave).
- `AINDA_NAO_MAPEADO`: esse relacionamento foi **validado apenas estruturalmente** (formato dos campos e regra de leitura confirmados com o Product Owner) — não foi validado ainda por um cruzamento executado de fato contra `WS_ESTOQUE_PRODUTOS` linha a linha (não houve acesso SQL disponível na sessão em que isso foi descoberto). Antes de tratar como 100% equivalente em produção, confirmar com uma amostra real cruzando Showcase × `WS_ESTOQUE_PRODUTOS`.

## Segurança — CONFIRMADO / DECISAO_PO

Padrão herdado de [linx-wise-daily-integration.md](./linx-wise-daily-integration.md#safety-rules), aplicado ao WISE Agent:

- `DECISAO_PO`: comportamento padrão do WISE Agent é **somente leitura**. Por padrão pode executar: `SELECT`, consultas de metadados/schema, chamadas de leitura à API do Showcase, análises e comparações.
- `DECISAO_PO`: nunca executa automaticamente `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `MERGE`, `ALTER`, `DROP`, `CREATE`, nem procedures que possam escrever.
- `DECISAO_PO`: se uma tarefa futura exigir alteração no WISE, o agente deve: (1) explicar exatamente a alteração proposta; (2) mostrar quais registros seriam afetados (via `SELECT` prévio); (3) aguardar autorização explícita do Product Owner; (4) seguir as regras específicas já documentadas em [linx-wise-daily-integration.md](./linx-wise-daily-integration.md) quando a alteração for parte da rotina diária Linx/WISE, ou pedir definição de regra nova ao Product Owner caso contrário.
- `NAO_USAR`: nunca transformar uma consulta aparentemente simples em operação destrutiva (ex.: nunca "corrigir" um dado percebido como errado sem autorização explícita).
- `NAO_USAR`: nunca imprimir token, senha, connection string ou conteúdo de `.env`/segredo em qualquer saída, log, arquivo versionado ou memória textual.

## Relação com o Agent Linx

- **Agent Linx** (`LinxErpSpecialistAgent` / `LinxDatabaseSpecialistAgent`, ver [backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs](../../backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs)): especialista no ERP Visual Linx — suas tabelas, regras funcionais e schema estrutural (`SOMA_DESENV`). Consultado quando a pergunta é sobre produto, cor, preço, grade ou regra de negócio do lado Linx/ERP.
- **WISE Agent** (este documento): especialista no ambiente WISE — campanhas, saldo/estoque WISE, estrutura `WS_*`, Showcase e a integração entre eles. Consultado quando a pergunta é sobre campanha, saldo ativo, registro `WS_*` ou dado do Showcase.
- Os dois nunca duplicam responsabilidade: o Agent Linx nunca decide saldo/campanha WISE; o WISE Agent nunca decide preço/grade/regra ERP do lado Linx.
- Quando uma tarefa envolve os dois domínios (ex.: "cruzar produto do Linx com saldo WISE por campanha"), o orquestrador (Maestro, ver [AI_TEAM.md](../AI_TEAM.md)) consulta ambos e combina os resultados — nenhum dos dois responde pelo domínio do outro.
- O WISE Agent não duplica a rotina diária Linx/WISE (escrita incremental em `MB_PROD_EXTRA_WEB` e `WS_ESTOQUE_PRODUTOS`) — essa rotina de escrita continua sendo o escopo exclusivo de [linx-wise-daily-integration.md](./linx-wise-daily-integration.md) e do runbook correspondente. O WISE Agent é o especialista de **consulta/leitura** que qualquer tarefa pode invocar; a rotina diária é um **fluxo de escrita** específico que o WISE Agent pode ajudar a interpretar, mas não substitui.

## Como Evoluir Este Documento

Sempre que uma tarefa futura descobrir algo novo e `CONFIRMADO` sobre o WISE (nova tabela, significado de coluna, nova campanha, regra de saldo, nova chave, peculiaridade de grade, relacionamento entre tabelas, comportamento de `DT_EXCLUSAO`, procedimento seguro, erro conhecido e solução), atualizar este arquivo na seção correspondente (ou criar uma nova seção), classificando a entrada com o rótulo de proveniência correto. Nunca promover `INFERIDO`/`AINDA_NAO_MAPEADO` para `CONFIRMADO` sem verificação direta (consulta executada, resposta de API observada, ou confirmação explícita do Product Owner).

Este arquivo é a memória de longo prazo do WISE Agent — não depender da memória de um chat específico para reter esse conhecimento.

## Autoteste Conceitual

Um WISE Agent que carregou este documento deve conseguir responder, sem consultar o chat original:

1. Como acessar os saldos ativos de uma campanha: `SELECT * FROM [WISE_AZURE].[SOMA_LINX].[DBO].WS_ESTOQUE_PRODUTOS WHERE ID_CAMPANHA = '<valor fornecido pelo PO>' AND DT_EXCLUSAO IS NULL`, via Linked Server `WISE_AZURE` a partir da conexão `LINX_PROD_*` (SRV-SOMADB/SOMA).
2. Qual tabela usar para saldo: `WS_ESTOQUE_PRODUTOS`.
3. Qual condição identifica registro ativo: `DT_EXCLUSAO IS NULL` (preenchido = inativação lógica, nunca `DELETE` físico).
4. Qual chave usar para relacionar produto/cor: `PRODUTO + COR_PRODUTO` (WISE) ↔ `REF + COR` (Showcase) — nunca descrição.
5. Quais operações pode realizar automaticamente: `SELECT`, metadados, chamadas de leitura à API do Showcase.
6. Quais operações exigem autorização explícita: qualquer escrita (`INSERT`/`UPDATE`/`DELETE`/`TRUNCATE`/`MERGE`/`ALTER`/`DROP`/`CREATE`/procedures de escrita) e escolha de `ID_CAMPANHA`.
7. O que ainda não está mapeado e não deve ser tratado como certo: mapeamento `ES1..ES16` → PP/P/M/G/GG na tabela WISE; correspondência `collection_Id` do Showcase ↔ `ID_CAMPANHA` do WISE; validação linha a linha do cruzamento Showcase × `WS_ESTOQUE_PRODUTOS`; schema completo de `WS_ESTOQUE_PRODUTOS` além das colunas já citadas.
