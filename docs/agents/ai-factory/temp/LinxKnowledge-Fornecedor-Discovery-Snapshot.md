> **STATUS: TEMPORÁRIO — AGUARDANDO INGESTÃO NA MEMÓRIA DOS AGENTS LINX**
>
> **FINALIDADE**: preservação estruturada do conhecimento levantado no discovery de Fornecedor/CNPJ (SOMA_DESENV, via VPN autorizada pelo PO) para ingestão posterior na memória persistente dos Agents Especialistas Linx (`LinxKnowledgeEntry`), quando o GAP de infraestrutura descrito na seção 12 for resolvido.
>
> **NÃO É**: substituto definitivo da memória persistente (`LinxKnowledgeEntry`/`LinxKnowledgeRepository`). Este arquivo vive em local versionado pelo git (não em `docs/audits/`, que é ignorado) exatamente para não perder o conhecimento até que a ingestão real seja possível — mas não é a base de conhecimento oficial dos Agents, nem substitui o processo de proveniência/promoção já existente no código.
>
> **Documentos-fonte** (preservados intactos, evidência histórica): `docs/audits/Discovery-Fornecedor-CNPJ-Linx-Compras.md` e `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`.

# Snapshot de Conhecimento Linx — Discovery de Fornecedores

## Como ler este documento

Cada unidade de conhecimento abaixo está estruturada com os campos que mapeiam diretamente para `LinxKnowledgeEntry` (`backend/src/BlueprintOS.Domain/Knowledge/Linx/LinxKnowledgeEntry.cs`), mais campos auxiliares que o discovery já produziu e que ajudam na conversão futura:

- **Título** → vira `Assunto`.
- **Categoria** → vira `Categoria` (enum real: `SchemaTabelaColuna`, `RegraFuncional`, `FluxoErp`, `Integracao`, `HistoricoDecisao`).
- **Escopo** → vira `UnidadeNegocioId` (GLOBAL = `null`; se algum dia se confirmar que um achado é específico de uma BU/filial, precisará de um `UnidadeNegocioId` real).
- **Domínio** → campo auxiliar deste snapshot (não existe na entidade), usado aqui só para navegação (`GLOBAL LINX` / `DOMÍNIO FORNECEDOR` / `ARQUITETURA DE FRONTEIRA`).
- **Conteúdo** → vira `Conteudo` (tratado sempre como DADO recuperado pelos Agents, nunca como instrução privilegiada — conforme o próprio comentário da entidade).
- **Proveniência** → vira `Proveniencia` (`Descoberto`/`Inferido`/`Validado`/`Aprovado` — nunca nasce `Aprovado`, conforme `LinxKnowledgeEntry.Criar`).
- **Confiança** → campo auxiliar (ALTA/MÉDIA/BAIXA) deste snapshot — não existe na entidade, mas ajuda a decidir se, no momento da ingestão, a proveniência deveria já nascer como `Validado` (se a evidência for muito forte e alguém confirmar) ou ficar em `Descoberto`/`Inferido`.
- **Evidência** → vira `Fonte` (rastreabilidade obrigatória — `LinxKnowledgeEntry.Criar` rejeita `Fonte` vazia).
- **Tags** → vira `Tags`.
- **Dependências/Limitações** → campos auxiliares deste snapshot, para orientar quem for quebrar isso em múltiplas entradas.
- **Ator sugerido** (`LinxErpSpecialist` ou `LinxDatabaseSpecialist`) — indicado quando óbvio pelo tipo de achado (fato físico de banco → `LinxDatabaseSpecialist`; interpretação funcional → `LinxErpSpecialist`).

**Regra de exclusão aplicada**: nenhum valor real de sequencial, nenhum dado de fornecedor real, nenhuma credencial/connection string/detalhe de VPN, nenhum dump de SQL/SELECT completo, nenhuma contagem circunstancial sem valor arquitetural futuro. Onde o discovery original tinha uma contagem real (ex.: "X registros com flag Y"), este snapshot preserva apenas a **conclusão estrutural** ("múltiplos papéis simultâneos são comuns, não excepcionais"), não o número absoluto.

---

## 1. Conhecimento GLOBAL/reutilizável do ecossistema Linx

### 1.1 — Ferramentas de discovery Linx (`lx_cade`, `lx_cade_coluna`, `anm_busca_instrucao`)

- **Título**: Ferramentas auxiliares de discovery de schema/código Linx
- **Categoria**: `SchemaTabelaColuna`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: Existem 3 procedures auxiliares no ambiente Linx, todas comprovadamente READ-ONLY (wrappers de `SELECT` sobre catálogos do SQL Server): `LX_CADE(@TEXTO)` busca tabelas/views/functions/procedures por nome (via `sys.objects`+`sys.schemas`, filtro `LIKE`); `LX_CADE_COLUNA(@texto)` busca tabelas por nome de coluna, retornando tipo formatado (via `sys.columns`+`sys.types`); `ANM_BUSCA_INSTRUCAO(@INSTRUCAO)` busca texto literal dentro da definição SQL de procedures/functions/triggers/views (via `syscomments`/`sysobjects`, equivalente funcional a `sys.sql_modules`). Quando disponível, o próprio `sys.sql_modules`/`sys.objects`/`sys.columns` do SQL Server produz o mesmo resultado com mais controle de filtro — as três ferramentas são conveniências, não a única via de discovery.
- **Limitações**: nenhuma das três altera dados; todas dependem do catálogo do SQL Server já popular (não funcionam para objetos criptografados/`WITH ENCRYPTION`). Antes de qualquer uso, confirmar via `OBJECT_DEFINITION` que a definição lida corresponde de fato a um wrapper de `SELECT` — não assumir apenas pelo nome.
- **Proveniência**: Descoberto
- **Confiança**: ALTA (definição SQL completa lida e confirmada nesta sessão)
- **Evidência**: `OBJECT_DEFINITION` de cada procedure, lido em sessão de discovery com VPN/acesso READ-ONLY autorizado ao SOMA_DESENV (12/08/2026)
- **Tags**: `discovery-sql`, `ferramentas-linx`, `read-only`, `metodologia`
- **Dependências**: nenhuma
- **Ator sugerido**: LinxDatabaseSpecialist

### 1.2 — `LX_SEQUENCIAL` e tabela `SEQUENCIAIS`

- **Título**: Mecanismo de geração de código sequencial no Linx
- **Categoria**: `RegraFuncional`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: `LX_SEQUENCIAL(@TABELA_COLUNA, @EMPRESA, @SEQUENCIA OUTPUT, @UPDATE_SEQUENCIAL=1, @NEWVALUE=NULL)` é a procedure real de geração/consulta de código sequencial no Linx (nome real no singular — hipóteses anteriores mencionavam "lx_sequenciais" no plural, mas o objeto real chama-se `LX_SEQUENCIAL`). Quando `@UPDATE_SEQUENCIAL=1` (padrão), incrementa a tabela `SEQUENCIAIS` (`UPDATE ... SET SEQUENCIA = SEQUENCIA = <valor+1> WHERE TABELA_COLUNA=@TABELA_COLUNA`) e retorna o novo valor formatado com zeros à esquerda (`TAMANHO` da tabela). Tem variante por empresa via `EMPRESA_SEQUENCIAIS` quando o parâmetro `CTRL_MULTI_EMPRESA` está ativo. Quando `@UPDATE_SEQUENCIAL=0`, apenas lê o próximo valor sem consumir. **Não há hint de lock/transação explícita (`WITH (UPDLOCK, HOLDLOCK)`) na definição lida** — a atomicidade depende só do comportamento padrão de um único `UPDATE`. **Incremento é sempre fixo em +1** — não há coluna de incremento configurável na tabela `SEQUENCIAIS`.
- **Estrutura da tabela `SEQUENCIAIS`** (sem valores reais): PK = `TABELA_COLUNA` (varchar 37); colunas `DESCRICAO`, `SEQUENCIA` (armazenada como string, não int), `TAMANHO` (usado para padding), `OBS1..OBS8`, `DATA_PARA_TRANSFERENCIA`, `PERMITE_POR_EMPRESA` (bit), `APLICACAO`.
- **Limitações/riscos**: chamar essa procedure **consome de forma não reversível** um contador compartilhado com processos de negócio reais — nunca deve ser executada fora de um teste explicitamente autorizado e mesmo assim com extrema cautela; não deve ser executada por Agents automatizados sem gate humano. Existem **sequenciais aparentemente duplicados/concorrentes para o mesmo propósito conceitual** em pelo menos um caso observado no domínio Fornecedor (ver 2.5) — isso é um padrão de risco a verificar em qualquer novo domínio investigado, não um fato isolado de Fornecedor.
- **Proveniência**: Descoberto (mecanismo e estrutura); Inferido (que a ausência de lock explícito é intencional/aceitável pelo padrão Linx)
- **Confiança**: ALTA (definição completa lida via `OBJECT_DEFINITION`, procedure nunca executada)
- **Evidência**: leitura de `OBJECT_DEFINITION('LX_SEQUENCIAL')` e estrutura de `SEQUENCIAIS` via `sys.columns`, sessão de discovery READ-ONLY (12/08/2026); procedure NUNCA executada em nenhuma sessão
- **Tags**: `sequencial`, `geracao-de-codigo`, `concorrencia`, `risco-escrita`
- **Dependências**: nenhuma
- **Ator sugerido**: LinxDatabaseSpecialist (estrutura) / LinxErpSpecialist (interpretação de risco de concorrência)

### 1.3 — Metodologia de discovery SQL reutilizável

- **Título**: Metodologia de discovery estrutural para qualquer entidade Linx com finalidade de escrita futura
- **Categoria**: `RegraFuncional`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: Regra consolidada e testada na prática neste discovery: **para qualquer entidade Linx investigada com finalidade de integração de escrita futura, a descoberta de schema físico (tabela/coluna/tipo/FK) nunca é suficiente isoladamente.** É preciso investigar, em conjunto: (1) triggers (evento, condições, colunas lidas/alteradas, validações, bloqueios, tabelas secundárias afetadas, procedures chamadas, cascatas); (2) stored procedures/functions relacionadas; (3) views relevantes; (4) sequenciais/geradores de chave; (5) efeitos colaterais em sistemas externos (filas de replicação, integrações SAP/parceiros); (6) se a entidade é uma tabela-mãe multiuso especializada por outras tabelas via flags; (7) múltiplas implementações reais de escrita (não confiar em uma única procedure isolada) comparadas entre si para separar padrão recorrente de peculiaridade de uma integração específica. Nenhum desses itens, isoladamente, define o contrato real de escrita do Linx — só a combinação de todos, com comparação entre múltiplas amostras, produz confiança suficiente para desenhar um Adapter.
- **Limitações**: mesmo seguindo todos os passos, recorrência (mesmo alta) não é igual a "oficial"/"aprovado" — nenhuma das implementações lidas neste discovery foi confirmada como a rotina manual usada na tela do Visual Linx; a metodologia reduz incerteza, não a elimina.
- **Proveniência**: Inferido (metodologia derivada da experiência deste discovery, ainda não validada em um segundo domínio Linx diferente de Fornecedor)
- **Confiança**: MÉDIA (funcionou uma vez, em um domínio; precisa se repetir em outro domínio — Filiais/Itens/Pedidos/Notas Fiscais — para subir a confiança)
- **Evidência**: aplicação prática desta metodologia ao domínio Fornecedor nesta sessão de discovery (ver seção 3 e o Playbook, seção 10)
- **Tags**: `metodologia`, `discovery-sql`, `reutilizavel`, `escrita-futura`
- **Dependências**: 1.1, 1.2, seção 6 (triggers/efeitos colaterais), seção 10 (Playbook)
- **Ator sugerido**: LinxErpSpecialist

---

## 2. Conhecimento do domínio Fornecedor

### 2.1 — `CADASTRO_CLI_FOR` como entidade-base multiuso

- **Título**: `CADASTRO_CLI_FOR` é tabela-mãe multiuso, não exclusiva de Fornecedor
- **Categoria**: `SchemaTabelaColuna`
- **Escopo**: GLOBAL
- **Domínio**: DOMÍNIO FORNECEDOR
- **Conteúdo**: Não existe no banco uma tabela chamada literalmente "CliFor" — o nome aparece só em tabelas satélite (`CLIFOR_INTERCOMPANY`, `EVENTOS_CLIFOR`, `MCX_PARAMETROS_CLIFOR`). A entidade-base real é `dbo.CADASTRO_CLI_FOR`, com PK `NOME_CLIFOR` (varchar 25) e identificadores alternativos `CLIFOR`/`COD_CLIFOR` (char 6), usados como FK por dezenas de tabelas satélite. **Nenhuma dessas colunas de chave é `IDENTITY`** — o valor precisa ser fornecido por quem insere (aplicação/rotina Linx), nunca gerado automaticamente pelo SQL Server. `CADASTRO_CLI_FOR` se especializa em papéis através de flags BIT (`INDICA_FORNECEDOR`, `INDICA_CLIENTE`, `IND_REPRESENTANTE`, `INDICA_FILIAL`), cada papel com sua tabela especializada correspondente (ver 2.2). **Confirmado por evidência estrutural (não por contagem de dados reais)**: um mesmo registro pode acumular múltiplos papéis simultaneamente (ex.: ser Fornecedor e Cliente ao mesmo tempo) — isso não é caso raro, é padrão estrutural esperado do modelo. Não há CHECK constraint no banco ligando a flag à existência da linha na tabela especializada — essa consistência é mantida apenas por disciplina de triggers/aplicação, não por constraint física, e observou-se na prática que pode haver pequenas divergências reais entre flag e tabela especializada.
- **Limitações**: a estrutura completa de `FORNECEDORES`/`CLIENTES_ATACADO`/`FILIAIS` foi lida com profundidades diferentes (ver 2.2) — `FORNECEDORES` foi mapeada campo a campo; `CLIENTES_ATACADO`/`FILIAIS` só tiveram nomes de coluna listados, não tipos/PK/FK completos.
- **Proveniência**: Descoberto (estrutura, chaves, ausência de IDENTITY, ausência de CHECK constraint) / Inferido (que a inconsistência flag↔tabela é sempre por falha de disciplina, e não por design intencional em algum caso específico não investigado)
- **Confiança**: ALTA
- **Evidência**: `sys.tables`/`sys.columns`/`sys.indexes`/`sys.foreign_keys` sobre `CADASTRO_CLI_FOR`, sessão de discovery READ-ONLY (12/08/2026); hipótese originalmente levantada pelo Product Owner (especialista Visual Linx) e depois confirmada tecnicamente por evidência estrutural e agregações — trajetória preservada: **conhecimento funcional informado pelo PO → confrontado com evidência real do banco → confirmado**
- **Tags**: `cadastro-cli-for`, `entidade-base`, `multiuso`, `flags-de-papel`, `chave-nao-identity`
- **Dependências**: 1.2 (chaves não são geradas por IDENTITY, dependem de mecanismo externo)
- **Ator sugerido**: LinxDatabaseSpecialist (estrutura) / LinxErpSpecialist (interpretação de papéis simultâneos)

### 2.2 — Especializações: `FORNECEDORES`, `CLIENTES_ATACADO`, `FILIAIS`

- **Título**: Tabelas de especialização de papel sobre `CADASTRO_CLI_FOR`
- **Categoria**: `SchemaTabelaColuna`
- **Escopo**: GLOBAL
- **Domínio**: DOMÍNIO FORNECEDOR
- **Conteúdo**: `FORNECEDORES` (PK `FORNECEDOR` varchar 25; FKs para `CADASTRO_CLI_FOR` via `CLIFOR`/`COD_FORNECEDOR`/`FORNECEDOR`) guarda exclusivamente atributos de negócio do papel de fornecedor — classificação (`TIPO`, `SUBTIPO_FORNECEDOR`, `CENTRO_CUSTO`), fiscal/financeiro (`CONTA_CONTABIL`, `CONDICAO_PGTO`, `MOEDA`), flags de fornecimento (`FORNECE_MATERIAIS`, `FORNECE_PROD_ACAB`, `FORNECE_MAT_CONSUMO`, `BENEFICIADOR`, `FORNECE_OUTROS`, `INDICA_TRANSPORTADORA`, `INDICA_MARKDOWN`, `INDICA_INTERMEDIADOR`), compliance (`BLOQUEIO_COMPLINCE`, `INDICA_CQFOR`), licenciamento (`LICENCIADO`, `LICENCIADO_ROYALTIES`). **Não tem nenhuma coluna de endereço, telefone, e-mail ou razão social** — tudo isso fica exclusivamente na tabela-mãe. `CLIENTES_ATACADO` e `FILIAIS` seguem o mesmo padrão (dezenas de colunas de negócio específicas ao papel — crédito/bloqueios/expedição para cliente; estoque/controle fiscal/conta bancária para filial —, sem endereço/telefone próprios), mas foram lidas só por nome de coluna nesta rodada, não em profundidade de tipo/PK/FK.
- **Limitações**: profundidade de leitura desigual entre as três tabelas de especialização (ver Conteúdo).
- **Proveniência**: Descoberto (para `FORNECEDORES`, leitura completa); Descoberto parcial (para `CLIENTES_ATACADO`/`FILIAIS`, só nomes de coluna)
- **Confiança**: ALTA para `FORNECEDORES`; MÉDIA para `CLIENTES_ATACADO`/`FILIAIS`
- **Evidência**: `sys.columns`/`sys.indexes` sobre as três tabelas, sessão de discovery READ-ONLY (12/08/2026)
- **Tags**: `fornecedores`, `clientes-atacado`, `filiais`, `especializacao-de-papel`
- **Dependências**: 2.1
- **Ator sugerido**: LinxDatabaseSpecialist

### 2.3 — Modelo de endereço/contato/CNAE/QSA no Linx físico

- **Título**: Estrutura física de endereço, contato, CNAE e ausência de QSA em `CADASTRO_CLI_FOR`
- **Categoria**: `SchemaTabelaColuna`
- **Escopo**: GLOBAL
- **Domínio**: DOMÍNIO FORNECEDOR
- **Conteúdo**: Endereço **não é uma tabela separada** — são colunas de texto direto em `CADASTRO_CLI_FOR`, **triplicadas** em três blocos paralelos (principal, cobrança `COBRANCA_*`, entrega `ENTREGA_*`), cada bloco com seu próprio CGC/IE e FK de UF (`UNIDADES_FEDERACAO`) e país (`PAISES`). O código de município IBGE é preenchido automaticamente por trigger via lookup cidade+UF. Contato: DDD já vem **separado** do telefone em colunas próprias (`DDD1`/`TELEFONE1`, `DDD2`/`TELEFONE2`, `DDDFAX`/`FAX`), e existem **dois e-mails distintos** (`EMAIL` comercial/geral e `EMAIL_NFE` fiscal). CNAE: **uma única coluna** (`CNAE varchar(7)`) — apenas o principal, sem estrutura para CNAEs secundários. QSA: **nenhuma coluna de sócios/quadro societário na tabela mestre** — confirma que o Linx físico não modela isso no cadastro comercial.
- **Limitações**: nenhuma.
- **Proveniência**: Descoberto
- **Confiança**: ALTA
- **Evidência**: `sys.columns` sobre `CADASTRO_CLI_FOR`, sessão de discovery READ-ONLY (12/08/2026)
- **Tags**: `endereco`, `contato`, `ddd-telefone`, `cnae`, `qsa`, `estrutura-fisica`
- **Dependências**: 2.1
- **Ator sugerido**: LinxDatabaseSpecialist

### 2.4 — Chaves e geração de código no domínio Fornecedor

- **Título**: `CLIFOR`/`COD_CLIFOR` via `LX_SEQUENCIAL`; `NOME_CLIFOR` via sanitização de string
- **Categoria**: `RegraFuncional`
- **Escopo**: GLOBAL
- **Domínio**: DOMÍNIO FORNECEDOR
- **Conteúdo**: Em 4 das 5 implementações de escrita lidas em profundidade (ver seção 3), `CLIFOR`/`COD_CLIFOR`/`COD_FORNECEDOR` vêm de uma única chamada a `LX_SEQUENCIAL @TABELA_COLUNA='FORNECEDORES.CLIFOR'`, e o mesmo valor retornado é reaproveitado nas três colunas. `NOME_CLIFOR` **nunca** vem de sequencial — é sempre construído por sanitização de string de um campo de nome de origem (razão social, nome fantasia, ou um campo específico da integração), removendo espaço inicial e caracteres especiais (reforçado por uma trigger real que bloqueia inserts com nome mal formatado). O campo de origem exato e o algoritmo de sanitização variam entre implementações (nível 2 de recorrência, não nível 1).
- **Limitações**: não foi confirmado se esse é também o comportamento da tela manual do Visual Linx (permanece DESCONHECIDO, ver seção 9).
- **Proveniência**: Descoberto (padrão em 4/5 amostras lidas)
- **Confiança**: ALTA para "vem de `LX_SEQUENCIAL`"; MÉDIA para o algoritmo exato de sanitização do nome (varia)
- **Evidência**: leitura de `OBJECT_DEFINITION` de 5 procedures de integração, sessão de discovery READ-ONLY (12/08/2026)
- **Tags**: `geracao-de-codigo`, `nome-clifor`, `sequencial`, `padrao-recorrente`
- **Dependências**: 1.2, seção 3, seção 4
- **Ator sugerido**: LinxErpSpecialist

### 2.5 — Anomalia confirmada: sequencial de cliente usado para papel de fornecedor

- **Título**: `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` usa sequencial de cliente, não de fornecedor
- **Categoria**: `HistoricoDecisao`
- **Escopo**: GLOBAL
- **Domínio**: DOMÍNIO FORNECEDOR — **ESPECÍFICO/ANOMALIA, NUNCA PADRÃO GLOBAL**
- **Conteúdo**: Das 5 implementações lidas, `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` é a única que chama `LX_SEQUENCIAL @TABELA_COLUNA='CLIENTES_ATACADO.CLIFOR'` para gerar o código de um registro que será marcado `INDICA_FORNECEDOR=1` — a linha equivalente para `'FORNECEDORES.CLIFOR'` existe no código-fonte dessa procedure, mas está **comentada**, sem explicação documentada de por que foi desativada. Também nessa mesma procedure, `NOME_CLIFOR` é construído com um prefixo fixo de marca (`'AZCB-'` + trecho da razão social + dígitos do CNPJ), diferente do padrão de sanitização de nome fantasia/campo de origem visto nas demais 4 implementações.
- **Limitações**: **não se inventa a razão da divergência** — pode ser bug, decisão de negócio específica ao parceiro dessa integração, ou legado não documentado. Isso não deve ser copiado como alternativa válida em nenhum Adapter futuro.
- **Proveniência**: Descoberto (o fato da divergência, por leitura direta de código); nunca promovido a regra geral
- **Confiança**: ALTA de que é uma anomalia isolada; **não aplicável** perguntar "confiança de que é o padrão" — é o oposto do padrão
- **Evidência**: leitura de `OBJECT_DEFINITION('p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR')`, sessão de discovery READ-ONLY (12/08/2026)
- **Tags**: `anomalia`, `nao-generalizar`, `sequencial-de-cliente`, `p_rsv`
- **Dependências**: 1.2, 2.4, 3.5
- **Ator sugerido**: LinxErpSpecialist

---

## 3. As 5 procedures analisadas (síntese, sem SQL integral)

Cada entrada abaixo é uma unidade de conhecimento própria, categoria `Integracao`, escopo GLOBAL, domínio DOMÍNIO FORNECEDOR, proveniência Descoberto, confiança ALTA (definição SQL completa lida via `OBJECT_DEFINITION`), evidência = leitura direta na sessão de discovery READ-ONLY (12/08/2026), ator sugerido LinxErpSpecialist. Tags comuns: `procedure-de-integracao`, `cadastro-fornecedor`.

### 3.1 — `LX_AZZ_GERAR_FORNECEDOR_LINX`
- **Finalidade aparente**: integração de parceiro (prefixo `AZZ`, provável grupo Azzas/Arezzo — mesma família de nomenclatura vista em outras procedures do banco).
- **Sequencial**: `LX_SEQUENCIAL('FORNECEDORES.CLIFOR')` — chave correta.
- **Tabelas manipuladas**: tabela temporária de staging (`#gerar_fornecedores`, pré-populada externamente) → `CADASTRO_CLI_FOR` → `FORNECEDORES`.
- **Ordem**: verificação de existência (`@existe_clifor`) → obtenção de sequencial → INSERT `CADASTRO_CLI_FOR` (com `INDICA_FORNECEDOR=1, INDICA_CLIENTE=0`) → INSERT `FORNECEDORES`; há branch de UPDATE para registro existente.
- **Duplicidade**: critério por `NOME_CLIFOR` exato.
- **Transação**: `BEGIN TRAN`/`COMMIT`/`ROLLBACK` com `TRY`/`CATCH` — confirmado explicitamente.
- **Peculiaridade não-generalizável**: dependência de tabela temporária externa pré-carregada por outro processo (não visível nesta procedure isoladamente).

### 3.2 — `LX_GS_GERAR_ALTERAR_FORNECEDOR_OBC_LINX`
- **Finalidade aparente**: integração com plataforma/parceiro "OBC" — cria e altera fornecedor existente (nome já indica ambas as operações).
- **Sequencial**: `LX_SEQUENCIAL('FORNECEDORES.CLIFOR')` — chave correta.
- **Tabelas manipuladas**: `CADASTRO_CLI_FOR` → `FORNECEDORES`; lógica de `@nome_clifor_alteracao` para localizar registro já existente via join com `FORNECEDORES`/`CLIENTES_ATACADO`/`FILIAIS`.
- **Ordem**: mesmo padrão de 3.1.
- **Duplicidade**: por nome/relacionamento já existente.
- **Transação**: `BEGIN TRAN`/`COMMIT`/`ROLLBACK` com `TRY`/`CATCH` — confirmado.
- **Peculiaridade**: é a única, entre as 5, com nome explícito de "gerar E alterar" — tratamento simétrico de criação/atualização mais evidente que nas demais.

### 3.3 — `PROC_GS_INTEGRA_FORNECEDOR_REDMINE`
- **Finalidade aparente**: processo corporativo interno — integração com sistema de tickets Redmine (provável fluxo de solicitação/aprovação de cadastro por lote).
- **Sequencial**: `LX_SEQUENCIAL('FORNECEDORES.CLIFOR')` para o código do fornecedor; e adicionalmente `LX_SEQUENCIAL('LOG_INTEG_FORNECEDOR_REDMINE.LOTE')` para numerar o lote de importação — **único caso de uso de dois sequenciais diferentes na mesma procedure**.
- **Tabelas manipuladas**: tabela temporária de staging (`#fornecedor_a_cadastrar`, com `ID_REDMINE`) → `CADASTRO_CLI_FOR` → `FORNECEDORES`.
- **Ordem**: `BEGIN TRY` → `BEGIN TRAN` → sequencial → INSERT `CADASTRO_CLI_FOR` → INSERT `FORNECEDORES` → `COMMIT`.
- **Duplicidade**: único caso, entre os 5, que verifica por **CNPJ** (`CGC_CPF`) como critério primário, além de nome fantasia — as demais usam nome como critério principal.
- **Transação**: `BEGIN TRY`/`BEGIN TRAN`/`COMMIT`/`ROLLBACK`/`CATCH` — confirmado explicitamente e de forma mais completa que as demais.
- **Peculiaridade**: sequencial adicional de lote (`LOG_INTEG_FORNECEDOR_REDMINE.LOTE`), específico a esse fluxo de importação via ticket.

### 3.4 — `PROC_HRG_CADASTRA_ZTBMM_FORNE_SOMA`
- **Finalidade aparente**: integração SAP **confirmada por conteúdo, não só pelo nome** — lê de uma view (`VW_HRG_ZTBMM_FORNE_SOMA`) sobre uma tabela cujo nome segue a convenção SAP de tabela customizada (`Z`-table), com colunas de vocabulário SAP (mandante, código de fornecedor SAP, data/hora de criação SAP).
- **Sequencial**: `LX_SEQUENCIAL('FORNECEDORES.CLIFOR')` — chave correta.
- **Tabelas manipuladas**: view de staging SAP → `CADASTRO_CLI_FOR` → `FORNECEDORES`; usa cursor para processar em lote.
- **Ordem**: cursor sobre staging → sanitização de nome (via function reutilizável dedicada, diferente da cadeia de `REPLACE` manual vista em outras procedures) → múltiplas checagens de existência (por propriedade SAP dedicada, depois por nome/razão social com e sem sanitização) → INSERT `CADASTRO_CLI_FOR` → INSERT `FORNECEDORES`.
- **Duplicidade**: critério composto — primeiro por uma propriedade específica de identificação SAP, só então por nome/razão social (com e sem sanitização).
- **Transação**: usa `ROLLBACK`, mas `BEGIN TRAN`/`TRY` explícitos não foram confirmados na leitura desta rodada — pendência de leitura mais completa se necessário no futuro.
- **Peculiaridade não-generalizável**: vocabulário e tabelas SAP específicos; uso de function reutilizável de sanitização de nome (padrão melhor que a cadeia manual de `REPLACE` vista em outras procedures — vale como referência de boa prática se um Adapter Linx próprio precisar sanitizar nomes).

### 3.5 — `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR`
- **Finalidade aparente**: integração de marketplace/parceiro (prefixo "RSV") com vocabulário SAP (`grupo_conta`, `ORG_COMPRA`, `codigo_sap`) — ao final, chama uma procedure própria de retorno de status para SAP.
- **Sequencial**: **anomalia confirmada** — usa `'CLIENTES_ATACADO.CLIFOR'`, não `'FORNECEDORES.CLIFOR'` (ver 2.5).
- **Tabelas manipuladas**: `CADASTRO_CLI_FOR` → `FORNECEDORES`; branch condicional conforme já existir ou não o registro.
- **Ordem**: verificação de existência → sequencial (chave divergente) → geração de nome via concatenação de prefixo fixo + razão social + dígitos do CNPJ → INSERT `CADASTRO_CLI_FOR` (com flags de papel setadas conforme o branch) → INSERT `FORNECEDORES` → chamadas de retorno de status a uma API/procedure externa.
- **Duplicidade**: por `MAX(CLIFOR)` associado ao CNPJ já atribuído (não é bem um critério de existência no sentido estrito, é obtenção de código já vinculado).
- **Transação**: `TRY`/`CATCH` com `goto` de desvio de fluxo em erro; `BEGIN TRAN` explícito não confirmado.
- **Peculiaridade não-generalizável**: sequencial de cliente para papel de fornecedor (anomalia, ver 2.5); prefixo fixo `AZCB-` no nome; vocabulário SAP/marketplace específico.

---

## 4. Padrão recorrente por nível de confiança

**Nível 1 — ALTA confiança (evidência em 4/5 ou 5/5 das implementações lidas em profundidade)**:
- Uso de `LX_SEQUENCIAL` para gerar o código do CliFor — **5/5**.
- `CLIFOR`/`COD_CLIFOR` sempre vêm da saída de `LX_SEQUENCIAL`, nunca de geração própria em SQL puro — **5/5** (mesmo a implementação com chave divergente usa a procedure, só com o parâmetro errado).
- `NOME_CLIFOR` sempre derivado por sanitização de string de um campo de nome/razão social, nunca sequencial puro — **5/5** (algoritmo de sanitização varia).
- Ordem fixa INSERT `CADASTRO_CLI_FOR` seguido de INSERT `FORNECEDORES`, nunca o inverso — **5/5**.
- Flags de papel (`INDICA_FORNECEDOR`/`INDICA_CLIENTE`) setadas explicitamente no próprio INSERT em `CADASTRO_CLI_FOR`, nunca deixadas para trigger/default — **4/4** das implementações onde esse detalhe foi lido explicitamente (a 5ª também seta, mas em branch condicional).
- Verificação de existência prévia antes de decidir INSERT vs. UPDATE — **5/5**.

**Nível 2 — MÉDIA confiança (evidência em 2-3 das 5, variação relevante entre elas)**:
- Uso de transação explícita (`BEGIN TRAN`/`COMMIT`/`ROLLBACK` com `TRY`/`CATCH`) — confirmado explicitamente em **3/5**; nas outras 2 há `ROLLBACK`/`CATCH` mas `BEGIN TRAN` não foi confirmado na leitura desta rodada (pode existir e não ter sido visto, não é uma negação confirmada).
- Critério de duplicidade por nome/razão social sanitizados (isolado ou combinado com outro critério) — **3/5**; apenas **1/5** usa CNPJ como critério primário.
- Preenchimento de endereço completo (múltiplos campos) no mesmo INSERT — confirmado em leitura detalhada em **3/5**; as outras 2 não tiveram esse trecho lido a fundo (não é negação, é lacuna de leitura).

**Nível 3 — ESPECÍFICO de uma única integração (NUNCA generalizar/copiar como padrão)**:
- Uso do sequencial `CLIENTES_ATACADO.CLIFOR` para registro de fornecedor (só em `p_RSV_...`) — contradiz o Nível 1 (5/5 usam a chave correta); é anomalia confirmada, não alternativa válida.
- Concatenação de prefixo fixo de marca no `NOME_CLIFOR` (só em `p_RSV_...`).
- Leitura de tabela/vocabulário SAP `ZTBMM_FORNE_SOMA` (só em `PROC_HRG_...`).
- Sequencial adicional de lote via `LOG_INTEG_FORNECEDOR_REDMINE.LOTE` (só em `PROC_GS_INTEGRA_FORNECEDOR_REDMINE`).

**Aviso de proveniência, obrigatório sempre que este conhecimento for citado**: recorrência (mesmo Nível 1, ALTA confiança) **não equivale a "oficial"/"aprovado"**. Todas as 5 implementações lidas são integrações automatizadas de origem externa (parceiros, SAP, sistema de tickets) — **nenhuma foi confirmada como a rotina manual do Visual Linx usada por um operador humano na tela de cadastro**. O padrão de Nível 1 é a melhor evidência disponível sobre "como uma escrita via SQL deveria minimamente se comportar para ser compatível com o Linx", não confirmação de processo aprovado pela SOMA/Linx.

---

## 5. Exceções/anomalias explícitas (consolidação)

| Anomalia | Onde aparece | Classificação | Risco se copiado sem entender |
|---|---|---|---|
| Sequencial de cliente usado para papel de fornecedor | `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` | ESPECÍFICO/ANOMALIA | Um Adapter Linx que copiasse isso geraria códigos de fornecedor a partir do sequencial errado, colidindo potencialmente com codificação de clientes |
| Prefixo fixo de marca no `NOME_CLIFOR` (`AZCB-`) | `p_RSV_INTEGRACAO_CADASTRO_FORNECEDOR` | ESPECÍFICO | Prefixo pertence a uma integração de parceiro específica, não é convenção geral de nomenclatura do Linx |
| Vocabulário e tabelas SAP (`ZTBMM_FORNE_SOMA`, `MANDT`, `LIFNR`, `STCD1`) | `PROC_HRG_CADASTRA_ZTBMM_FORNE_SOMA` | ESPECÍFICO | Esse vocabulário nunca deve aparecer em nenhum domínio +Compras — pertence exclusivamente a um futuro Adapter Linx, se algum dia necessário |
| Vocabulário e fluxo de tickets Redmine (`ID_REDMINE`, sequencial de lote) | `PROC_GS_INTEGRA_FORNECEDOR_REDMINE` | ESPECÍFICO | Fluxo de importação em lote por ticket, não é o caminho de cadastro individual |
| Vocabulário "OBC" | `LX_GS_GERAR_ALTERAR_FORNECEDOR_OBC_LINX` | ESPECÍFICO | Nome de parceiro/plataforma não identificado em detalhe nesta rodada; não generalizar |

---

## 6. Triggers e efeitos colaterais

### 6.1 — Categorias descobertas em `CADASTRO_CLI_FOR` (11 triggers ativas, todas lidas por completo)

| Categoria | Trigger(s) | O que faz |
|---|---|---|
| Preenchimento automático | `LXI_CADASTRO_CLI_FOR`, `LXU_CADASTRO_CLI_FOR` | Preenche código de município IBGE via lookup cidade+UF (principal/cobrança/entrega); atualiza data de transferência |
| Validação/bloqueio de entrada | `LXI_ANM_CADASTRO_CLI_FOR` | Bloqueia INSERT com nome iniciando em espaço ou com caracteres especiais (RAISERROR+ROLLBACK) |
| Bloqueio condicional por parâmetro/permissão | `LXI_CADASTRO_CLI_FOR` (parâmetro `VALIDA_COD_IBGE_CADASTROS`), `LXU_ANM_CADASTRO_CLI_FOR` (bloqueia alteração de razão social/CNPJ/IE de filiais sem permissão dedicada) | Impede a operação se uma condição de negócio/permissão não for satisfeita |
| Auditoria completa (log antes/depois) | `GSI_CADASTRO_CLI_FOR_LOG`, `GSU_CADASTRO_CLI_FOR_LOG`, `GSD_CADASTRO_CLI_FOR_LOG` | Grava snapshot "antes/depois" de dezenas de colunas em tabela de log dedicada, para INSERT/UPDATE/DELETE |
| Integração fiscal | `LXI_CADASTRO_CLI_FOR` (chama procedure de integração PAF-ECF, se existir, após o insert) | Efeito colateral em subsistema fiscal, disparado automaticamente |
| Fila de ETL/replicação (mecanismo 1) | `LXI_ETL_CADASTRO_CLI_FOR`, `LXU_ETL_CADASTRO_CLI_FOR` | Enfileira a alteração para replicação externa, com auto-supressão quando a própria sessão de origem já é o processo de ETL (evita loop) |
| Fila de ETL/replicação (mecanismo 2, distinto) | `GSUI_WETL_CADASTRO_CLI_FOR` | Segunda fila de replicação paralela, sem a mesma lógica de auto-supressão observada no mecanismo 1 — identidade do consumidor não confirmada |
| Integração/bloqueio SAP | `GSU_SAP_CADASTRO_CLI_FOR` | Bloqueia UPDATE de dezenas de colunas para um subconjunto de registros marcados como integrados ao SAP, forçando que a alteração aconteça no SAP em vez do Linx |
| Integração com sistema de RH/terceiros | `LXI_ANM_CADASTRO_CLI_FOR` (mesma trigger da validação acima) | Após validar, insere dado derivado em uma tabela usada por outro sistema (RH/departamento pessoal) |
| Cascata para tabela especializada | `LXU_ANM_CADASTRO_CLI_FOR` | Propaga inativação para `CLIENTES_ATACADO` quando a flag `INATIVO` muda em um registro multi-papel; propaga alteração de e-mail para uma view de portal de boletos; audita mudanças de dados bancários em log dedicado |

### 6.2 — Mapa conceitual (template reutilizável)

```
Operação → Tabela principal → Trigger(s) → Validação/bloqueio → Colunas lidas/alteradas
   → Tabelas secundárias afetadas → Procedure/function chamada → Sistema externo envolvido
   → Efeito funcional (interpretação, sempre marcada Inferido até validação humana)
```

Aplicado ao domínio Fornecedor, este mapa já está preenchido (com Descoberto/Inferido separados corretamente) no documento-fonte `Discovery-Fornecedor-CNPJ-Linx-Compras.md`, seção 10-A — este snapshot preserva a estrutura do mapa como conhecimento GLOBAL reutilizável para qualquer domínio futuro, não repete os dados já detalhados lá.

### 6.3 — Princípio central (GLOBAL, aplicável a qualquer domínio Linx)

- **Título**: Um INSERT/UPDATE numa tabela Linx nunca deve ser assumido como tendo efeito apenas local
- **Categoria**: `RegraFuncional`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: confirmado na prática neste discovery — uma única operação de escrita em `CADASTRO_CLI_FOR` pode disparar, através de triggers `AFTER` padrão (que reagem a qualquer INSERT/UPDATE real, não só a inserts feitos manualmente pela tela), efeitos em: preenchimento automático de outros campos, bloqueio condicional, gravação de auditoria, chamada a procedure de integração fiscal, enfileiramento em duas filas de replicação distintas, bloqueio/integração SAP, e gravação em sistema de RH/terceiros. **Nenhuma dessas triggers exige nenhum passo adicional do chamador para disparar** — qualquer futuro Adapter Linx de escrita precisa assumir, por padrão, que a operação terá efeitos além da própria tabela, e mapear conscientemente cada trigger da tabela-alvo antes de escrever, nunca assumir "é só um INSERT simples".
- **Proveniência**: Descoberto (evidência direta no domínio Fornecedor) generalizado como Inferido (princípio aplicável a outros domínios, ainda não testado em nenhum outro)
- **Confiança**: ALTA para o domínio Fornecedor; MÉDIA como princípio geral (precisa se confirmar em outro domínio)
- **Evidência**: leitura completa das 11 triggers de `CADASTRO_CLI_FOR`, sessão de discovery READ-ONLY (12/08/2026)
- **Tags**: `triggers`, `efeitos-colaterais`, `principio-global`, `risco-de-escrita`
- **Dependências**: seção 6.1
- **Ator sugerido**: LinxErpSpecialist

---

## 7. Aprendizados arquiteturais derivados (de `Arquitetura-Fornecedor-CNPJ-Decisao.md`)

- **Título**: Fronteira de camadas Provider CNPJ → Adapter do Provider → Contrato Canônico → Domínio +Compras → Adapter Linx → contrato físico Linx
- **Categoria**: `HistoricoDecisao`
- **Escopo**: GLOBAL
- **Domínio**: ARQUITETURA DE FRONTEIRA
- **Conteúdo**: A arquitetura proposta (não implementada) para o fluxo de Fornecedor/CNPJ estabelece 5 camadas isoladas: (1) Provider externo de CNPJ (hoje BrasilAPI, trocável); (2) Adapter do Provider, que traduz o JSON específico do provider para (3) um Contrato Canônico de Consulta CNPJ, desacoplado de qualquer provider; (4) o domínio +Compras central (`Fornecedor.cs` e regras de negócio, revisão humana, persistência, proveniência, auditoria — nunca contaminado por detalhes de nenhuma das pontas); e (5) um futuro Adapter Linx, que traduziria o domínio +Compras para o contrato físico real do Linx (`CLIFOR`/`COD_CLIFOR`/`NOME_CLIFOR` via `LX_SEQUENCIAL`, INSERT em `CADASTRO_CLI_FOR`→`FORNECEDORES`, filas de ETL/WETL, vocabulário SAP/OBC/Redmine). **Princípio não-negociável**: nenhum detalhe físico do Linx (nomes de tabela/coluna, mecanismo de sequencial, vocabulário de integrações de terceiros) deve aparecer em `Fornecedor.cs`, no contrato canônico de CNPJ, ou em qualquer DTO/componente do domínio +Compras — pertencem exclusivamente ao Adapter Linx, cuja implementação real exige confirmação com especialista Visual Linx antes de qualquer escrita em produção.
- **Documento fiscal**: `DocumentoFiscal` (Value Object) é o conceito canônico do domínio +Compras — normalizado por dígitos puros, com validação de dígito verificador proposta. A peculiaridade do campo físico Linx `CGC_CPF` (que pode conter valores legados não numéricos) **pertence exclusivamente ao Adapter Linx**, nunca deve relaxar a normalização do Value Object canônico.
- **Endereço/contato**: o domínio +Compras modela intencionalmente **apenas um endereço principal e um contato**, mesmo sabendo que o Linx físico tem 3 blocos de endereço — essa é uma decisão consciente de manter o domínio simples até que exista requisito real documentado de múltiplos endereços; a tradução para os 3 blocos (se necessária) é responsabilidade exclusiva do futuro Adapter Linx.
- **Proveniência**: Validado (arquitetura formalmente proposta e documentada em relatório de decisão, ainda pendente de aprovação formal do PO como ADR)
- **Confiança**: ALTA (decisão arquitetural deliberada, não uma inferência)
- **Evidência**: `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md`, seções B, C, F, O
- **Tags**: `arquitetura-de-fronteira`, `provider-adapter-dominio`, `documento-fiscal-canonico`, `nao-contaminar-dominio`
- **Dependências**: todas as seções anteriores deste snapshot (é a síntese arquitetural que consome o conhecimento técnico Linx)
- **Ator sugerido**: LinxErpSpecialist (para a parte de fronteira conceitual) — mas nota-se que esta unidade específica é mais "conhecimento de arquitetura +Compras" do que "conhecimento Linx"; incluída aqui porque define o que o Adapter Linx NÃO deve fazer.

---

## 8. Contrato futuro do Adapter Linx (preservado, não implementado)

| Classificação | Item |
|---|---|
| **OBRIGATÓRIO** | Obter código via mecanismo real de geração Linx (melhor evidência atual: `LX_SEQUENCIAL`/`FORNECEDORES.CLIFOR`, Nível 1) |
| **OBRIGATÓRIO** | Gerar `NOME_CLIFOR` por sanitização de nome (sem espaço inicial, sem caracteres especiais — reforçado por trigger real de bloqueio) |
| **OBRIGATÓRIO** | Popular flags de papel Linx (`INDICA_FORNECEDOR` etc.) no momento da criação |
| **OBRIGATÓRIO** | Verificar existência prévia no lado Linx antes de decidir INSERT vs. UPDATE físico (regra distinta da verificação de duplicidade do +Compras) |
| **RECOMENDADO** | Transação com rollback ao escrever no Linx |
| **RECOMENDADO** | Popular o bloco de endereço físico Linx (replicando o endereço único do +Compras nos 3 blocos, se necessário) |
| **AINDA DESCONHECIDO** | Se o cadastro manual via tela Visual Linx segue o mesmo padrão das integrações automatizadas lidas, ou se existe rotina distinta |
| **AINDA DESCONHECIDO** | Quem consome as filas `LJ_ETL_REPOSITORIO`/`GS_WETL_REPOSITORIO` e se o Adapter deve suprimir ou permitir essa replicação |
| **NÃO ENTRA NO DOMÍNIO +COMPRAS** | Qualquer vocabulário/campo específico de integrações de terceiros (`grupo_conta`, `ORG_COMPRA`, `codigo_sap`, campos SAP, nomes de tabela Linx) |

**Gate obrigatório, preservado**: nenhuma escrita real do Adapter Linx deve ir para produção sem confirmação de um especialista Visual Linx sobre os itens "AINDA DESCONHECIDO" — o padrão de Nível 1 (seção 4) é a melhor evidência disponível, não um contrato oficialmente aprovado.

---

## 9. Desconhecidos preservados explicitamente

| Desconhecido | Por que importa | Como resolver no futuro |
|---|---|---|
| Existência de rotina distinta para a tela manual do Visual Linx | As 5 procedures lidas são todas integrações automatizadas (parceiro/SAP/Redmine); nenhuma foi confirmada como o processo usado por um operador humano | Ler mais candidatas em `sys.sql_modules` sem prefixo de integração de parceiro, e/ou confirmar diretamente com um especialista/operador Visual Linx |
| Identidade dos sistemas consumidores das filas `LJ_ETL_REPOSITORIO`/`GS_WETL_REPOSITORIO` | Sem saber quem lê essas filas, não se sabe se suprimir ou permitir a entrada de escritas futuras do +Compras | Investigação de discovery adicional (fora do escopo desta rodada) ou confirmação com time de integração/infra Linx |
| Critério definitivo de duplicidade do lado Linx (nome sanitizado vs. CNPJ vs. combinação) | As 5 implementações usam critérios diferentes — não há Nível 1 de confiança aqui, só Nível 2 | Precisa de decisão de negócio/confirmação com especialista antes do Adapter Linx |
| Regras finais de escrita do Adapter Linx (ordem exata, tratamento de erro, ponto de rollback cross-sistema) | Depende dos itens anteriores | Desenho de Work Order dedicada, após resolução dos desconhecidos acima |
| Necessidade de tratamento de dados legados (`CGC_CPF` não numérico no Linx) | O comentário original de `DocumentoFiscal.cs` cita essa preocupação, mas não foi confirmado quão frequente/relevante é na prática | Consultar volume real de registros com `CGC_CPF` não numérico no Linx físico (investigação futura, read-only) |
| Conteúdo completo de 6 procedures citadas por nome mas não lidas em profundidade (`LX_AZZ_GERAR_CLIENTE_ATAC_LINX`, `MIT_INTEGRA_ORO`, `MIT_INTEGRA_TRUNK`, `mit_integra_vintage`, `PROC_GS_INTEGRA_CLIENTES_ATACADO_REDMINE`, `LX_LGPD_PROC_CLIENTE`) | Poderiam aumentar ou contradizer a confiança dos níveis 1/2/3 da seção 4 | Leitura futura, se o domínio de Cliente/Filial vier a ser investigado com o mesmo rigor |

---

## 10. Playbook de discovery Linx reutilizável

Aplicável a qualquer domínio Linx futuro (Filiais, Itens, Pedidos, Clientes, Notas Fiscais, etc.), sempre em modo estritamente READ-ONLY:

1. Localizar candidatos a tabela principal via nome (`sys.tables`/`LX_CADE`) e via coluna característica do domínio (`sys.columns`/`LX_CADE_COLUNA`) — nunca assumir o nome da tabela pela nomenclatura de negócio (ex.: "CliFor" não é o nome real da tabela de Fornecedor).
2. Uma vez identificada a tabela candidata, listar todas as colunas (tipo, nulidade, identity) — checar explicitamente se há colunas de chave sem `IDENTITY` (sinal de geração externa de código).
3. Buscar todas as FKs de entrada e saída (`sys.foreign_keys`) para entender o quão central/acoplada a tabela é.
4. Verificar se a tabela é uma entidade-base multiuso: procurar colunas BIT que sugiram papéis, e localizar tabelas satélite cujo nome sugira especialização de papel — confirmar cada flag por USO REAL em triggers/procedures, nunca só pelo nome da coluna.
5. Mapear chaves e mecanismo de geração de código: se não houver `IDENTITY`, buscar `SEQUENCIAIS`/procedures de sequencial (`LX_SEQUENCIAL` ou equivalente) e ler a definição sem executar.
6. Listar e ler por completo todas as triggers da tabela principal (`sys.triggers` + `OBJECT_DEFINITION`) — nunca assumir efeito só pelo nome da trigger.
7. Buscar procedures/functions relacionadas via `sys.sql_modules` (busca por `INSERT INTO <tabela>`/`UPDATE <tabela>`, e por nomes de sequencial exatos encontrados no passo 5).
8. Ler views relevantes referenciadas pelas triggers/procedures, só o suficiente para entender o mecanismo, sem documentar o banco inteiro.
9. Identificar efeitos colaterais em sistemas externos (filas de ETL, bloqueios SAP, integrações de RH/terceiros) — sempre por evidência textual no código (nome de fila, mensagem de erro, chamada de procedure externa), nunca por suposição a partir do nome da trigger.
10. Comparar múltiplas implementações reais de escrita para a mesma tabela (nunca confiar em uma única procedure isolada) — classificar cada comportamento por contagem real de recorrência.
11. Separar padrão recorrente (Nível 1/2) de peculiaridade de uma única integração (Nível 3) — nunca copiar Nível 3 como se fosse regra geral.
12. Classificar a confiança de cada achado por contagem real (ex.: "4 de 5"), nunca por percentual estimado sem base.
13. Registrar explicitamente todo DESCONHECIDO (o que não foi possível confirmar), nunca preencher a lacuna com suposição.
14. Antes de qualquer implementação real de escrita, validar os achados de Nível 1 e os desconhecidos críticos com um especialista humano do sistema Linx — recorrência não é aprovação.

---

## 11. Nota sobre proveniência e trajetória de conhecimento vindo do PO

Alguns achados deste snapshot (notavelmente 2.1 — `CADASTRO_CLI_FOR` como entidade-base multiuso) foram originalmente levantados como **hipótese funcional informada pelo Product Owner** (com experiência prática em Visual Linx), e só depois **confrontados e confirmados por evidência técnica direta** (leitura de schema real + agregações estruturais, sem expor dados pessoais). Essa trajetória — "conhecimento funcional informado pelo PO → investigação técnica → confirmação ou refutação" — deve ser preservada explicitamente quando este conhecimento for convertido em `LinxKnowledgeEntry`, porque é exatamente o padrão de proveniência que a fundação O1.13.5 foi desenhada para capturar (campo `Fonte`, mais o histórico de versões via `NovaVersao`/`Promover`). Nenhuma hipótese do PO foi registrada como fato neste snapshot sem essa confirmação técnica associada — onde a confirmação não ocorreu (ex.: itens da seção 9), o item permanece explicitamente como DESCONHECIDO, nunca promovido silenciosamente.

---

## 12. GAP da fundação O1.13.5 — identificador real e sequência de ação futura

### Identificador real do GAP

**Não existe, no inventário de dívidas/GAPs já numerado do projeto (`docs/audits/O1.14-InventarioDividasEGaps.md`), um identificador DEB-/GAP- que corresponda exatamente a este bloqueio específico** ("não há infraestrutura local — banco/Docker/seed — para persistir efetivamente uma `LinxKnowledgeEntry` nesta sessão"). O item mais próximo já registrado é:

> **DEB-18** — "Evolução da busca textual/estruturada para embeddings/RAG é um ponto de extensão planejado, não implementado; ingestão dos +300 `obj_*.prg`/documentação Linx é fora de escopo; frontend administrativo de conhecimento não exigido" — classificado como **NÃO APLICÁVEL (arquitetura-alvo declarada, não dívida)**, escopo "Agents Linx / AI Factory" (`docs/audits/O1.14-InventarioDividasEGaps.md`).

DEB-18 é sobre evolução de busca/RAG, não sobre a ausência de infraestrutura local para o caminho de escrita já existente (`LinxKnowledgeRepository`/`LinxKnowledgeController`, via API+EF Core). **Este snapshot não inventa um novo identificador** — registra que o bloqueio específico encontrado nesta sessão de discovery (nenhum `docker-compose`, Docker não em execução, `appsettings.json` com placeholders sem `user-secrets` configurados localmente, nenhum seed/CLI alternativo) ainda não tem um DEB-/GAP- formalmente numerado no inventário canônico, e que **a formalização de um novo item (ou o ajuste de escopo de um item existente) é uma ação pendente para quem resolver este bloqueio**, não decidida por este snapshot.

### Sequência de ação futura, quando o GAP for resolvido

1. Ler este snapshot por completo.
2. Validar compatibilidade de cada unidade de conhecimento com o schema real de `LinxKnowledgeEntry` (campos `Especialista`/`Categoria`/`Assunto`/`Conteudo`/`Proveniencia`/`Fonte`/`Ator`/`UnidadeNegocioId`/`Tags`) — usar o mapeamento já indicado na seção "Como ler este documento".
3. Quebrar cada unidade em uma ou mais entradas reais de `LinxKnowledgeEntry` (uma unidade pode gerar mais de uma entrada, se cobrir mais de um `Assunto` distinto).
4. Preservar a proveniência exatamente como registrada aqui — nenhuma unidade nasce `Aprovado`; entradas marcadas ALTA confiança neste snapshot podem justificar nascer `Validado` em vez de `Descoberto`/`Inferido`, mas essa decisão cabe a quem fizer a ingestão, não é automática.
5. Persistir via o fluxo real já existente (`LinxKnowledgeController` → `LinxKnowledgeUseCases` → `LinxKnowledgeRepository`, contra um banco real).
6. Validar recuperação/busca/uso pelos Agents Especialistas (`LinxErpSpecialist`/`LinxDatabaseSpecialist`) — confirmar que uma consulta real recupera as entradas ingeridas.
7. **Não apagar este snapshot após a ingestão** — apenas atualizar o cabeçalho deste arquivo para `STATUS: INGESTÃO CONCLUÍDA`, com a data e uma referência (ex.: intervalo de IDs ou commit da migration/seed de ingestão) que permita rastrear onde o conhecimento foi efetivamente persistido.

---

## 13-A. Metodologia reutilizável — descoberta via código-fonte real da tela (TRANSACOES → SCX/SCT/PRG)

- **Título**: Playbook de descoberta de comportamento real de tela Visual Linx a partir de fontes locais
- **Categoria**: `FluxoErp`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: quando os fontes do Visual Linx estiverem disponíveis localmente (ex.: `docs/linxERP/Exclusivos.zip`, ignorado pelo git), a hierarquia de evidência preferencial passa a ser: (1) código real da tela padrão → (2) OBJ/customização associado → (3) schema/triggers/procedures do banco → (4) padrões recorrentes de integrações existentes → (5) validação com especialista humano → (6) inferência arquitetural. Fluxo de localização: identificar tabela/domínio → `SELECT * FROM TRANSACOES WHERE TABELA_PAI = '<TABELA>'` → obter `CONTROL_SISTEMA` → localizar `LX[CONTROL_SISTEMA].SCX`/`.SCT` (tela) e `obj_[CONTROL_SISTEMA].PRG`/`.FXP` (objeto de entrada/customização) no pacote de fontes → ler `.PRG` como fonte preferencial (texto plano); ler `.SCT` via extração de texto (é o memo da tela, contém SQL de views/cursors e métodos como texto) — nunca decompilar/modificar `.FXP`/`.SCX` binários. Sempre classificar cada achado como PADRÃO LINX vs. CUSTOMIZAÇÃO SOMA/AZZAS vs. BANCO/TRIGGER vs. INTEGRAÇÃO EXTERNA — nunca promover comportamento de um OBJ específico a regra universal do produto sem evidência adicional. Uma tela pode ter mais de uma transação relacionada à mesma tabela-pai; não escolher a primeira arbitrariamente — mapear finalidade de cada uma antes de decidir qual é a relevante.
- **Limitações**: o pacote de fontes disponibilizado localmente pode conter apenas customizações (OBJs de entrada) e não o framework/classe base do produto Linx — chamadas a métodos herdados (ex.: `l_desenhista_*`) revelam **quando e com quais parâmetros** algo é chamado, mas não necessariamente a implementação interna, que pode estar em um `.VCX`/`.PRG` do framework base não incluído no pacote. Registrar essa lacuna explicitamente como DESCONHECIDO em vez de inferir o comportamento interno.
- **Confiança**: ALTA (validado nesta sessão contra a tela real de Fornecedor, `CONTROL_SISTEMA=001016G1`, confirmado pelo Product Owner)
- **Evidência**: sessão de Gate Pré-B2.9, adendo de código-fonte real, `docs/architecture/Gate-PreB29-AdapterLinxFornecedor.md` seção 6-A
- **Tags**: `metodologia`, `discovery`, `visual-foxpro`, `transacoes`, `playbook`
- **Ator sugerido**: `LinxErpSpecialist`

### 13-A.1 — Achados de domínio Fornecedor confirmados via código real da tela (`001016G1`)

- **Título**: Regras de identidade e duplicidade de Fornecedor confirmadas por leitura direta da tela padrão
- **Categoria**: `RegraFuncional`
- **Escopo**: DOMÍNIO FORNECEDOR
- **Domínio**: DOMÍNIO FORNECEDOR
- **Conteúdo**: (a) `CLIFOR` de um fornecedor novo é gerado por `f_sequenciais('FORNECEDORES.CLIFOR', .t.)`, chamado apenas em modo Inclusão e apenas se ainda não gerado na sessão da tela — confirma `FORNECEDORES.CLIFOR` como sequencial oficial (a anomalia `CLIENTES_ATACADO.CLIFOR` de uma integração isolada não é um padrão alternativo válido); (b) `COD_CLIFOR` e `COD_FORNECEDOR` recebem exatamente o mesmo valor de `CLIFOR`, sem padding/transformação; (c) `NOME_CLIFOR` é o próprio valor do campo "Fornecedor" digitado pelo usuário (não deriva de razão social nem de sequencial), com sanitização de customização SOMA/AZZAS (maiúsculas, sem espaço inicial, sem caracteres especiais de uma lista fixa) — sem tratamento explícito de colisão de nome além da PK física; (d) critério de duplicidade primário e oficial da tela é `CGC_CPF` em `FORNECEDORES`, escopado por `EMPRESA`/grupo econômico via `CADASTRO_CLI_FOR_EMPRESA` — mesmo CNPJ na mesma empresa bloqueia, em outra empresa do grupo oferece reuso (vincular grupo econômico ao cadastro existente, sem criar novo `CADASTRO_CLI_FOR`); (e) a persistência real não passa por nenhuma das 5 procedures de integração conhecidas, nem por `TableUpdate()` automático de view (`SendUpdates=.F.` no cursor principal) — passa por uma classe base compartilhada (`l_desenhista_*`) cuja implementação interna está fora do pacote de fontes disponível; (f) evidência simétrica (pelo caminho de exclusão de papel) de que o modelo multiuso é ativamente gerenciado: remover o papel Fornecedor de um cadastro que também é Cliente/Filial/Representante só reseta a flag e remove de `FORNECEDORES`, preservando `CADASTRO_CLI_FOR` e os demais papéis.
- **Limitações**: não há evidência, no material disponível, de transação explícita (`BEGIN TRAN`) nem do caminho simétrico de *adicionar* um papel a um cadastro existente (só o de removê-lo foi encontrado); consumidores de filas ETL/WETL, comportamento sob concorrência de `LX_SEQUENCIAL` e estratégia de rollback cross-sistema permanecem desconhecidos — são comportamento de trigger/servidor, não alcançável por código de tela cliente VFP.
- **Confiança**: ALTA para (a)-(d) e (f) — leitura direta e literal do código real da tela padrão de Fornecedor; MÉDIA para (e), já que a implementação interna da classe base não foi lida.
- **Evidência**: `lx001016G1.SCX/SCT` + `obj_001016G1.PRG/FXP` (fonte local `docs/linxERP/Exclusivos.zip`, nunca versionada), lidos nesta sessão de Gate
- **Dependências**: depende da metodologia registrada em 13-A
- **Ator sugerido**: `LinxErpSpecialist`

---

## 13-B. Mecanismo genérico de persistência do framework Linx (`lx_class.vcx::l_salva`)

- **Título**: Rotina central `l_salva` da classe base — mecanismo real de transação/persistência de toda tela Visual Linx
- **Categoria**: `FluxoErp`
- **Escopo**: GLOBAL
- **Domínio**: GLOBAL LINX
- **Conteúdo**: localizado em `Classes/Linx_SQL_Fonte/Desenv/Lib/lx_class.vcx` (fonte local `docs/linxERP/linx_fonte.zip`, nunca versionada) o método genérico `l_salva`, herdado por todas as telas Visual Linx (incluindo a de Fornecedor, `001016G1`) — esta é a rotina central que investigações anteriores buscavam sem sucesso nas 5 procedures de integração. Estrutura real: hooks pré-salvamento (`l_desenhista_antes_salva`, `USR_SAVE_BEFORE`) → transação em duas camadas (`Begin Transaction` de buffer VFP + `data.connection.BeginTrans()` real no SQL Server, com isolamento ajustável para `READ COMMITTED` durante a escrita) → hooks de trigger antes → auditoria (`l_auditoria`) → gravação real via `objCursor.AcceptChanges(...)` (método nativo do `CursorAdapter` do VFP, um por cursor principal, todos dentro da mesma transação) → hooks de trigger depois → `CommitTrans()`/`End Transaction` em sucesso, ou `RollbackTrans()`+`RollBack` completo em qualquer falha em qualquer etapa → hooks pós-salvamento (`l_desenhista_apos_salva`, `USR_SAVE_AFTER`). Para a tela de Fornecedor, cujo `CursorAdapter` tem `Tables=CADASTRO_CLI_FOR,FORNECEDORES`, isso confirma por evidência direta (não mais inferência) que as duas tabelas são gravadas dentro da mesma transação de banco, com rollback total garantido pelo próprio framework em qualquer falha.
- **Limitações**: `f_sequenciais` (chamado pela tela de Fornecedor para obter `CLIFOR`) não está definido em `lx_class.vcx` — vive em biblioteca de funções globais não incluída nas fontes disponíveis; nenhuma referência a `LX_SEQUENCIAL` ou `SEQUENCIA_FORNECEDOR` foi encontrada nesta classe. Decisão do PO: irrelevante para o Adapter, que usa o mecanismo de banco diretamente, nunca a função VFP.
- **Confiança**: ALTA (leitura direta e literal do código-fonte real da classe base do produto)
- **Evidência**: `lx_class.vcx`/`.VCT`, fonte local `docs/linxERP/linx_fonte.zip`, lido nesta sessão de Gate (rodada de discovery final pré-B2.9)
- **Dependências**: complementa 13-A (metodologia) e 13-A.1 (achados da tela `001016G1`)
- **Ator sugerido**: `LinxErpSpecialist`

## 13-C. Decisões de produto aprovadas pelo Product Owner — domínio Fornecedor/BU/ERP

- **Título**: Regras de fronteira BU↔ERP, identidade de fornecedor e modelo multiuso aprovadas pelo PO
- **Categoria**: `HistoricoDecisao`
- **Escopo**: GLOBAL (itens 1–3, 6, 8, 10) e DOMÍNIO FORNECEDOR (itens 4–5, 7, 9)
- **Domínio**: ARQUITETURA DE FRONTEIRA / DOMÍNIO FORNECEDOR
- **Conteúdo**: decisões formalmente aprovadas pelo Product Owner nesta sessão, proveniência `APROVADO` (não inferidas, não derivadas de código): (1) BU é a fronteira de integração com ERP/banco — dentro de uma BU, cadastros são compartilhados entre marcas, marca não segrega fornecedor; (2) identidade do fornecedor = BU + CNPJ, nunca duplicado dentro da mesma BU; (3) `EMPRESA`/grupo econômico do Linx não pertence ao domínio +Compras — para a BU SOMA, tratado internamente como valor técnico fixo (`EMPRESA=1`) apenas se a persistência exigir, nunca exposto na UI nem replicado para Adapters futuros; (4) adicionar o papel Fornecedor a um `CADASTRO_CLI_FOR` existente nunca cria nova entidade-base — sempre `INDICA_FORNECEDOR=1` + `INSERT FORNECEDORES` se ainda não existir, preservando todos os papéis existentes; (5) o Adapter de Fornecedor nunca cria `FILIAIS` nem `CLIENTES_ATACADO`, mesmo quando o cadastro-base já possui esses papéis (apenas preserva); (6) transação essencial deve ser atômica com rollback total em falha, transações curtas, locks mínimos, nenhuma chamada externa dentro da transação; (7) `SEQUENCIA_FORNECEDOR` não é reconhecida como regra pelo PO e, sem evidência concreta adicional, é classificada como sem relevância para o contrato mínimo da B2.9; (8) ETL/WETL não pertencem ao contrato funcional da B2.9 — efeito colateral existente do ambiente, não implementado/replicado pelo Adapter; (9) e-mail comercial×fiscal é pendência de produto não bloqueante; (10) princípio de suficiência — dúvida só bloqueia se houver risco concreto de corrupção/duplicidade/perda de dados/inconsistência de papéis/transação; caso contrário, classificar como "validar em desenvolvimento/homologação".
- **Limitações**: nenhuma — são decisões de produto, não achados técnicos; prevalecem sobre qualquer inferência arquitetural anterior deste snapshot que as contradiga.
- **Confiança**: ALTA (decisão direta do Product Owner, registrada nesta sessão)
- **Evidência**: sessão de Gate Pré-B2.9, rodada final de discovery (adendo `linx_fonte.zip` + decisões do PO), `docs/architecture/Gate-PreB29-AdapterLinxFornecedor.md` seção 6-B
- **Ator sugerido**: `LinxErpSpecialist`

---

## 13. Nota sobre numeração de ADR

O documento `docs/audits/Arquitetura-Fornecedor-CNPJ-Decisao.md` menciona **"ADR-0020"** como o próximo número disponível em `.ai/DECISIONS.md`, com base no maior ADR existente identificado no momento daquela rodada (**ADR-0019**). **Essa numeração é provisória** — outras decisões podem ter sido registradas em `.ai/DECISIONS.md` entre aquela rodada e o momento em que a arquitetura de Fornecedor/CNPJ for formalmente aprovada. **Quem for formalizar essa decisão como ADR deve reabrir `.ai/DECISIONS.md`, confirmar o maior número real naquele momento, e usar o próximo disponível — nunca assumir "0020" sem reconfirmar.** Este snapshot não abre nem edita `.ai/DECISIONS.md`.
