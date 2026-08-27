# Showcase Agent — Conhecimento Operacional Persistido

> Conhecimento reutilizável e evolutivo do especialista Showcase (Compuwise/WiseCommerce, `soma.compuwise.com.br`). Consolida o aprendizado `CONFIRMADO` durante a coleta bem-sucedida e validada pelo Product Owner em 2026-08-27 (contexto de sessão observado: marca FARM, mercado LATAM — 418 produto/cor, 1193 fotos, 0 erros de download). **Este documento é genérico para o Showcase, não específico de FARM** — a marca/região efetivamente acessível é sempre determinada pela sessão autenticada no momento da execução, nunca fixada aqui. Trata-se de conhecimento operacional, não de transcrição de chat.

Segue o mesmo modelo já estabelecido em [linx-wise-daily-integration.md](./linx-wise-daily-integration.md) e [wise-knowledge.md](./wise-knowledge.md). Os três documentos são complementares e não se duplicam: aquele cobre a escrita/sincronização Linx→WISE; `wise-knowledge.md` cobre consulta/leitura ao WISE (campanhas, saldo, `WS_*`); este cobre a extração do catálogo/fotos do Showcase.

## Perfil do Agente (template obrigatório — AI_TEAM.md, "Criação de Novos Agentes")

- **Nome:** Showcase Agent (identificador técnico: `showcase-agent`)
- **Objetivo:** ser o especialista reutilizável em acessar o Showcase autenticado, identificar o contexto de marca/região da sessão corrente, extrair o catálogo completo (produto, cor, descrição, grade) e baixar todas as fotos disponíveis, para qualquer marca/região sem reexplicação a cada sessão.
- **Responsabilidade:** abrir/reutilizar Chrome via Chrome DevTools MCP; aguardar login manual quando necessário; identificar o contexto efetivamente autenticado (marca/mercado/collection); consumir a API do Showcase já descoberta; paginar o catálogo completo; baixar fotos na nomenclatura validada; gerar checkpoint e Excel; colaborar com o WISE Agent para saldo.
- **Limites (nunca faz):**
  - nunca preenche login, MFA ou qualquer credencial — sempre para e aguarda confirmação do Product Owner;
  - nunca assume marca/região/`collection_Id` fixos — sempre detecta da sessão atual;
  - nunca persiste token, cookie, senha ou segredo em Git ou memória permanente (ver "Autenticação — Regra Fundamental");
  - nunca executa ação de escrita no Showcase (pedido, carrinho, cadastro, configuração);
  - nunca duplica conhecimento do WISE Agent — delega saldo/campanha a ele;
  - nunca transforma uma particularidade observada em uma única marca/região em regra global sem marcar como tal.
- **Ferramentas:** Chrome DevTools MCP (sessão autenticada pelo Product Owner); Node.js (`fetch` nativo) reaproveitando o token/contexto extraídos da sessão, em [`scripts/showcase_collector/`](../../scripts/showcase_collector/).
- **Entradas esperadas:** instrução do tipo "Executar coleta Showcase com fotos e saldos WISE" — sem marca no gatilho; opcionalmente uma lista de produto/cor específica.
- **Saídas esperadas:** bloco `SHOWCASE AUTENTICADO` (marca/mercado/contexto/quantidade de produtos detectados no início da execução); catálogo completo; fotos baixadas; `catalogo_showcase.xlsx`; relatório final.
- **Critérios de qualidade:** nunca inventa marca/contexto quando não consegue determiná-los (marca como "não identificada"); nunca perde produto/cor por paginação mal tratada; nunca baixa duplicata; sempre confirma sessão antes de continuar após uma pausa/erro 401/403.
- **Prompt base:** ver [.ai/prompts/coletar-showcase.md](../prompts/coletar-showcase.md).
- **Modelo utilizado:** o modelo de IA/agente configurado na sessão corrente no momento da execução (nenhum modelo específico é fixado por este documento).
- **Memória utilizada:** este arquivo (`showcase-knowledge.md`) é a memória de longo prazo/reutilizável do agente. Contexto de sessão (token, brand_Id, etc.) é memória de curto prazo, válida apenas durante a execução — nunca persistida (ver "Autenticação").
- **Permissões:** somente leitura no Showcase. Nenhuma permissão de escrita é concedida a este agente.
- **Governanca Security/LGPD:** exportacoes locais de catalogo/fotos/saldos podem conter informacao comercial sensivel e, quando houver dados pessoais, devem ser modeladas como `ActionProposal`. O Security/LGPD Agent interpreta contexto; o bloqueio tecnico pertence ao `AIGovernancePolicyEngine` quando o fluxo estiver conectado.

## Rótulos de Proveniência

Mesma convenção de [wise-knowledge.md](./wise-knowledge.md):

- `CONFIRMADO`: verificado por chamada de API observada diretamente, execução bem-sucedida, ou resposta HTTP direta nesta sessão.
- `DECISAO_PO`: definido explicitamente pelo Product Owner.
- `INFERIDO`: hipótese razoável a partir do observado, ainda não verificada em outra marca/região — nunca tratar como regra definitiva.
- `PARTICULARIDADE_MARCA`: comportamento confirmado apenas para uma marca/região específica — não generalizar para as demais até confirmação equivalente.
- `AINDA_NAO_MAPEADO`: conhecimento que o agente sabe que não sabe; deve investigar ou perguntar ao Product Owner antes de agir.
- `NAO_USAR`: comportamento tentado que não deve ser reproduzido.

## Autenticação — Regra Fundamental

- `DECISAO_PO`: **nunca persistir token, cookie de sessão, senha, credencial ou segredo** em Git ou na memória permanente deste agente (este arquivo). Apenas o **mecanismo** de obtenção é documentado, nunca o valor.
- `CONFIRMADO`: fluxo validado em 2026-08-27:
  1. Abrir/reutilizar o Chrome via Chrome DevTools MCP, navegando para a URL do Showcase.
  2. Se a página carregada for a tela de login, **parar imediatamente** e avisar: "Página de login aberta. Aguardando você efetuar o login manualmente." Nunca preencher usuário/senha/OTP/MFA.
  3. Aguardar confirmação explícita do Product Owner de que o login foi feito.
  4. Após confirmação, seguir para "Como Extrair o Contexto da Sessão" abaixo.
  5. Usar o token/contexto **apenas em memória, durante aquela execução** — nunca gravar em arquivo versionado, log ou memória textual permanente.
  6. Se, durante a coleta, uma resposta vier em HTML (tela de login) ou HTTP 401/403, tratar como sessão expirada: parar a coleta e pedir novo login ao Product Owner. Nunca tentar contornar.

## Como Extrair o Contexto da Sessão — CONFIRMADO

Procedimento usado com sucesso em 2026-08-27, via Chrome DevTools MCP (`evaluate_script`):

- Token JWT: `JSON.parse(localStorage.getItem('0.soma|token'))` — string Bearer, válida por ~24h a partir do login (claims JWT incluem `unique_name`, `CodCli`, `CodCliFab`, `role`, `Agent`).
- Contexto de marca/coleção/carrinho: **não** fica pronto em uma única chave de storage — foi observado nas próprias chamadas de rede feitas pelo frontend logo após o login (aba Network do Chrome DevTools MCP, `list_network_requests`/`get_network_request`), nos parâmetros de query de chamadas como `login`, `depts`, `brands`, `collections`, `addOrder`, `showcase`. Os campos confirmados e reutilizáveis para as chamadas subsequentes são:
  - `brand_Id` — observado nas chamadas `brands`/`login`/`showcase` (ex.: `1041` na sessão FARM/LATAM observada).
  - `company_Id` — observado em todas as chamadas (ex.: `8`).
  - `dept_Id` — observado em `depts`/`brands` (ex.: `19`).
  - `collection_Id` — observado em `collections`/`login` (ex.: `332`).
  - `customer_Id` — observado em `login`/`showcase` (`CodCli` do JWT também confirma o mesmo valor).
  - `pricelist` — observado em `showcase` (ex.: `DL`).
  - `payment` — observado em `payments`/`showcase` (ex.: `60 DD`).
  - `order_Id`/`orderId` — carrinho da sessão, criado pela própria navegação (endpoint `addOrder`); confirmado necessário nas chamadas de `stock`/`showcase` subsequentes.
- `AINDA_NAO_MAPEADO`: não existe, até esta sessão, uma chamada única de "quem sou eu" que devolva todos esses campos de uma vez — a extração feita foi por inspeção da aba de rede após a navegação inicial. Um ponto de evolução futura é confirmar se `service.asmx/profile?company_name=<slug>` ou `service.asmx/menus` devolve o conjunto completo diretamente; não presumir isso sem testar.
- `DECISAO_PO`: nenhum desses valores deve ser hardcodado no coletor — devem ser extraídos a cada execução e passados como variáveis de ambiente (`SHOWCASE_TOKEN`, `SHOWCASE_BRAND_ID`, `SHOWCASE_COMPANY_ID`, `SHOWCASE_DEPT_ID`, `SHOWCASE_COLLECTION_ID`, `SHOWCASE_CUSTOMER_ID`, `SHOWCASE_PRICELIST`, `SHOWCASE_PAYMENT`, `SHOWCASE_ORDER_ID`) — ver [`scripts/showcase_collector/README.md`](../../scripts/showcase_collector/README.md).

## Identificação do Contexto — Bloco Obrigatório de Abertura

`DECISAO_PO`: no início de toda execução, após detectar a sessão autenticada, informar:

```text
SHOWCASE AUTENTICADO

Marca: <detectada ou "não identificada">
Mercado/Região: <detectado ou "não identificado">
Contexto/Collection: <collection_Id detectado ou "não identificado">
Produtos disponíveis: <quantidade, a partir do totalPages × page_size do primeiro `showcase`, ou "não identificado">
```

- `CONFIRMADO`: a marca pode ser inferida com segurança a partir do segmento `{MARCA}` presente nas URLs `imageShowcase*` retornadas pelo catálogo (ex.: `.../imagens/FARM/produtos/...` → marca `FARM`) e/ou do logo/nome exibido na UI da página inicial do Showcase (confirmado visualmente na sessão de 2026-08-27: header "FARM").
- `AINDA_NAO_MAPEADO`: não há, até esta sessão, um campo explícito de "mercado/região" (ex.: "LATAM") devolvido por uma API — "LATAM" foi inferido nesta sessão apenas pelo nome do Agent/cliente logado (`"Agent":"KRONOTIME SAS COLOMBIA"` no JWT e nas respostas de `login`), não por um campo estruturado dedicado. Tratar mercado/região como `INFERIDO` a partir do nome do agente/cliente até uma fonte mais direta ser confirmada; se não for possível inferir com segurança, reportar como "não identificado" — nunca inventar.
- `NAO_USAR`: nunca assumir que a próxima sessão será FARM/LATAM só porque foi a sessão observada em 2026-08-27.

## API do Showcase — Endpoints Confirmados

`CONFIRMADO` nesta sessão (produto "WiseCommerce", API em `https://wiseapi-gruposoma.azurewebsites.net/service.asmx/*` — mesmo host observado independente da marca logada, já que é a API compartilhada do provedor Compuwise/WiseCommerce; reconfirmar o host a cada nova execução via a aba de rede, não assumir que é sempre o mesmo domínio para todo cliente Compuwise):

- `showcase` — listagem paginada do catálogo (`page_number`, `page_size`, `&distinct=true` recomendado). Retorna `totalPages`. Uma linha por PRODUTO+COR em `vitrine`, com `product_Id`, `color_Id`, `product_name`, `colorDescription`, `category_name`, `subcategory_name`, `grid`, `price`, `composition`, `gender`, e até três URLs de imagem (`imageShowcase`, `imageShowcase_Back`, `imageShowcase_Look`).
- `products?product_Id=X&color_Id=Y` — detalhe do produto: inclui `line`, `base`, `fabric` e uma quarta imagem (`imageShowcase_Detail04`) quando existente.
- `productColors?product_Id=X` — cores do produto no formato `"{COR} - {DESC_COR}"`.
- `stock?product_Id=X&colorId=Y` — saldo por tamanho **já nomeado** (`PP`/`P`/`M`/`G`/`GG` etc., não código interno), com `quantity`.
- Parâmetros comuns em praticamente toda chamada: `brand_Id`, `company_Id`, `dept_Id`, `collection_Id`, `customer_Id`, `pricelist`, `payment`, `order_Id`/`orderId`, `coefficient` — todos extraídos da sessão (ver seção anterior), nunca fixos.

## Regra Produto/Cor — CONFIRMADO (validada pelo Product Owner)

No Showcase, a cor é exibida no formato `CODIGO_COR - DESCRICAO_COR`. Exemplo observado: `59721 - ONDINA_SAIA MALHA_OW`.

Interpretação:

- `PRODUTO = REF` (= `product_Id` na API)
- `COR = 59721` (número antes do hífen, = `color_Id` na API)
- `DESC_COR = ONDINA_SAIA MALHA_OW` (texto depois do hífen)

Chave de trabalho: **PRODUTO + COR** — nunca descrição.

## Fotos — CONFIRMADO

- Hospedadas em `https://wiseimagessoma.blob.core.windows.net/soma/imagens/{MARCA}/produtos/{PRODUTO}-{COR}-{N}.jpg`, `N` sequencial a partir de 1.
- `CONFIRMADO`: o segmento `{MARCA}` **é descoberto a partir da própria URL `imageShowcase*` que a API já devolveu** para aquele produto/cor no catálogo (nunca hardcodado) — é assim que o coletor genérico funciona para qualquer marca sem reconfiguração.
- `CONFIRMADO`: nem todo produto/cor tem foto — 94 de 418 combinações (22,5%) na execução de 2026-08-27 não tinham **nenhum** campo `imageShowcase*` no catálogo. Confirmado via `productColors`/`vitrine` que isso é dado real (cor sem imagem cadastrada), não falha de coleta — o coletor deve pular esses itens sem tentar "adivinhar" uma URL.
- `CONFIRMADO`: distribuição observada de fotos por produto/cor na execução de 2026-08-27: 217 itens com 4 fotos, 101 com 3, 4 com 5, 2 com 1, 94 com 0. Ou seja, o número de fotos por item **varia** e não deve ser assumido como fixo em 4.
- `CONFIRMADO`: mecanismo de descoberta usado — sondagem HTTP `HEAD` sequencial em `{PRODUTO}-{COR}-{N}.jpg` para `N = 1, 2, 3, ...` até 2 falhas consecutivas (limite de segurança `N ≤ 12`). É simples e funcionou (1193/1193 fotos esperadas baixadas, 0 erro), mas é uma sondagem por convenção de nome, não uma listagem explícita da API — se uma marca/região futura usar outra convenção de nome de arquivo, esta sondagem não vai encontrar as fotos; tratar essa possibilidade como `AINDA_NAO_MAPEADO` até acontecer.
- Nome de arquivo — nunca usar `DESC_COR`:
  ```text
  {PRODUTO}_{COR}_{SEQUENCIA}.jpg
  ```
  Exemplo: `372308_59721_a.jpg`, `372308_59721_b.jpg`, `372308_59721_c.jpg`, `372308_59721_d.jpg` (sequência com letras `a, b, c, ...`, conforme a quantidade real de fotos daquele produto/cor).
- `DECISAO_PO`: preferir sempre a imagem original/maior resolução — nesta implementação, a própria URL do blob storage já é a original (sem parâmetro de thumbnail), então nenhuma transformação adicional é necessária.

## Implementação Validada (código real, não redesenhado)

- `CONFIRMADO`: a implementação que rodou com sucesso em 2026-08-27 está persistida em [`scripts/showcase_collector/`](../../scripts/showcase_collector/) (`collect.js`, `enrich.js`, `build_excel.js`) — copiada da execução real e apenas parametrizada (contexto de sessão via variáveis de ambiente; pasta de imagem por marca descoberta por item) para não fixar marca/região. O algoritmo (paginação, sondagem de fotos, checkpoint, geração de Excel) não foi redesenhado.
- `CONFIRMADO`: **checkpoint/retomada** — `coleta_showcase.csv` registra cada foto individual (`produto,cor,ordem_foto,url,arquivo_local,status,data_download,erro`). Ao reexecutar, o coletor pula fotos já marcadas `ok` cujo arquivo local ainda existe e tem tamanho > 0; tenta novamente as demais. Erros de download não interrompem a coleta — são contabilizados e registrados em `erros.json`, e o item segue processando os demais.
- `CONFIRMADO`: uma sessão expirada (`SessionExpiredError` — HTTP 401/403 ou resposta HTML) **interrompe a coleta imediatamente**, grava `resultado_parcial.json` e `erros.json`, e sai com código 2 — nunca tenta contornar. Reexecutar após novo login retoma a partir do checkpoint de fotos (o catálogo em si é sempre repaginado do zero, que é rápido: ~19 páginas em segundos).
- `CONFIRMADO`: cadência usada com sucesso — 300ms entre páginas do catálogo, 150ms entre downloads de foto, 120ms entre chamadas de enriquecimento (`enrich.js`) — sem rate limiting observado do lado do servidor. Não presumir que uma cadência mais agressiva é segura sem reconfirmar.
- `CONFIRMADO` (correção aplicada durante a validação): os campos `price` e `composition` do item de catálogo (`vitrine`) precisam ser copiados explicitamente para o resultado processado — a primeira versão do script esqueceu esses dois campos, gerando uma planilha sem preço; a versão persistida em `scripts/showcase_collector/collect.js` já inclui a correção.
- `CONFIRMADO`: a coluna de preço no Excel chama-se `PRECO_VENDA`, não `PRECO_FOB` — o campo `price` da API do Showcase é o preço de venda ao cliente logado, não um preço FOB. O Showcase não expõe FOB; se uma tarefa futura precisar de FOB, ele precisa vir de outra fonte (ex.: WISE/Linx), não deste agente.

## Excel — Layout Validado

Uma linha por **PRODUTO + COR**, aba `Catalogo`:

`FOTO` (imagem embutida da primeira foto) · `PROD` · `COR_PROD` · `CHAVE` (`{PROD}-{COR_PROD}`) · `DESC_PRODUTO` · `DESC_COR_PRODUTO` · `GRU` (categoria) · `GRUP` (subcategoria) · `LINHA` · `COMPOSICAO` · `PRECO_VENDA` · `GRADE` (tamanhos concatenados) · `TAM_1..TAM_N` (saldo por tamanho, na ordem retornada por `stock`).

Abas adicionais confirmadas: `Fotos` (produto, cor, ordem, arquivo local, status), `Erros` (produto, cor, etapa, erro), `Resumo` (métricas da execução — produtos únicos, fotos baixadas, itens sem foto, itens sem saldo, erros).

`DECISAO_PO`: campos do template original (`ARARA`, `ESTAMPAS`, `TIPO_PRODUTO`, `CLASSIF`) não existem na API do Showcase — não incluir com valor inventado; se necessários, vêm do cadastro Linx/WISE, não deste agente.

## Colaboração com o WISE Agent

- `DECISAO_PO`: o Showcase Agent **nunca duplica** conhecimento interno do WISE — apenas fornece o catálogo PRODUTO+COR; quem sabe interpretar campanha, `WS_ESTOQUE_PRODUTOS`, `DT_EXCLUSAO` e saldo é o [WISE Agent](./wise-knowledge.md).
- Relacionamento esperado (mesma chave, ver `wise-knowledge.md`):
  - Showcase `REF` ↔ WISE `PRODUTO`
  - Showcase `COR` ↔ WISE `COR_PRODUTO`
- `AINDA_NAO_MAPEADO` (herdado de `wise-knowledge.md`): esse relacionamento foi validado estruturalmente (formato dos campos), mas não por cruzamento linha a linha executado contra `WS_ESTOQUE_PRODUTOS` — quando o SQL estiver disponível, validar com uma amostra real antes de tratar como 100% equivalente em produção.
- Fluxo de colaboração: Showcase Agent gera `resultado_final.json`/`catalogo_showcase.xlsx` (PRODUTO+COR) → WISE Agent recebe essa lista e consulta `WS_ESTOQUE_PRODUTOS` pela campanha informada pelo Product Owner → resultado combinado (fotos + saldo oficial WISE) é o relatório final, quando esse enriquecimento for solicitado.

## Segurança

- `DECISAO_PO`: comportamento padrão **somente leitura** — `SELECT`/GET de consulta, extração, download, consolidação.
- Nunca alterar pedidos, produtos, carrinho, configurações, usuários ou qualquer informação do portal.
- Nunca disparar centenas de requisições simultâneas — cadência controlada (ver "Implementação Validada").
- Nunca imprimir token, senha ou connection string.
- AI Governance Onda 1: o Showcase Agent reconhece a camada Security/LGPD. O coletor atual continua fora de um Tool Gateway universal; portanto o enforcement e documental no script/runbook, exceto quando um fluxo futuro encapsular a coleta/exportacao em `ActionProposal`.

## Memória Evolutiva — Particularidades por Marca/Região

Sempre que uma nova execução revelar diferença de comportamento entre marcas/regiões (ex.: FARM/Brasil, Animale/Brasil), registrar aqui como `PARTICULARIDADE_MARCA`, com a marca/região exata observada — nunca promover automaticamente a `CONFIRMADO` genérico. Nenhuma particularidade registrada até o momento além da sessão FARM/LATAM de 2026-08-27, que é a base deste documento.

## Como Evoluir Este Documento

Ao descobrir algo novo e `CONFIRMADO` (novo endpoint, novo campo, particularidade de marca/região, erro conhecido e solução), atualizar a seção correspondente deste arquivo, classificando com o rótulo correto. Nunca promover `INFERIDO`/`PARTICULARIDADE_MARCA`/`AINDA_NAO_MAPEADO` para `CONFIRMADO` sem verificação direta.

## Autoteste Conceitual

Um Showcase Agent que carregou este documento deve conseguir responder, sem depender do chat original:

1. O que fazer ao abrir o Showcase e cair na tela de login: parar e pedir login manual — nunca preencher.
2. Como obter o contexto de marca/região: inspecionar a rede após o login (não existe endpoint único de "quem sou eu" confirmado) e detectar a marca pela URL das imagens/UI.
3. Qual é a chave de produto/cor: `PRODUTO + COR` (`REF` + código antes do hífen na cor exibida).
4. Onde estão as fotos: `https://wiseimagessoma.blob.core.windows.net/soma/imagens/{MARCA}/produtos/{PRODUTO}-{COR}-{N}.jpg`, marca descoberta por item, não fixa.
5. O que fazer com produto/cor sem nenhuma URL de imagem: tratar como "sem foto cadastrada", não como erro.
6. O que nunca persistir: token, cookie, senha, segredo — em nenhum arquivo versionado ou memória permanente.
7. Quem fornece saldo: o WISE Agent, nunca este agente sozinho.
8. O que fazer se a sessão expirar no meio da coleta: parar, salvar o que já foi coletado, pedir novo login — nunca contornar.
