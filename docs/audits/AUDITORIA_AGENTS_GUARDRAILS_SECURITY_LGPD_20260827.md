# Auditoria Completa dos Agents e Guardrails do SOMA BlueprintOS

Data: 2026-08-27

Escopo: diagnostico da arquitetura atual de Agents, especialistas, runtime, guardrails, autorizacao humana, acessos a banco e encaixe futuro de um Security/LGPD Agent.

Restricoes desta etapa:

- Nenhum arquivo de codigo alterado.
- Nenhum agent criado.
- Nenhum commit.
- Nenhum push.

## 1. Resumo Executivo

A arquitetura atual possui tres camadas distintas:

1. **Runtime Agents reais no backend**
   - `EchoAgent`
   - `KnowledgeAgent`
   - `LinxErpSpecialistAgent`
   - `LinxDatabaseSpecialistAgent`

2. **Especialistas operacionais documentais/promptados**
   - WISE Agent
   - Showcase Agent
   - rotina diaria Linx/WISE

3. **Arquitetura-alvo conceitual**
   - Maestro, Planner, Task Protocol, Tool Gateway, AI Gateway, memoria avancada e orquestracao multi-agent aparecem majoritariamente em documentacao, nao como enforcement completo no runtime atual.

O backend ja possui RBAC real para endpoints HTTP do +Compras, autenticacao obrigatoria por fallback policy, policies por permissao, protecao CSRF em endpoints administrativos relevantes, rate limiting registrado e algumas protecoes de segredo. Entretanto, nao foi encontrado um Policy Engine transversal de IA, Tool Gateway, AI Gateway, interceptador SQL universal ou fluxo tecnico generico de aprovacao humana para acoes sensiveis propostas por Agents.

Os guardrails mais fortes hoje estao em:

- contratos limitados por codigo, como `LinxSchemaDiscoveryReader`, que so executa consultas fixas em `INFORMATION_SCHEMA`;
- RBAC tecnico do backend;
- workflow especifico do `LinxKnowledge`;
- script diario Linx/WISE, com validacoes e SQL fixo;
- runbooks e prompts operacionais, que sao fortes como processo, mas bypassaveis tecnicamente.

## 2. Fontes Auditadas

Arquivos e areas lidas/inspecionadas:

- `.ai/CLAUDE.md`
- `.ai/context/`
- `.ai/prompts/`
- `.ai/AI_TEAM.md`
- `docs/agents/`
- `docs/operations/`
- `docs/backend/orchestration/Orchestration.md`
- `backend/src/BlueprintOS.Core/Agents/`
- `backend/src/BlueprintOS.Core/Workflows/`
- `backend/src/BlueprintOS.Application/Knowledge/Linx/`
- `backend/src/BlueprintOS.Domain/Knowledge/Linx/`
- `backend/src/BlueprintOS.Api/Authorization/`
- `backend/src/BlueprintOS.Api/Knowledge/LinxKnowledgeController.cs`
- `backend/src/BlueprintOS.Api/Program.cs`
- `backend/src/BlueprintOS.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `backend/src/BlueprintOS.Infrastructure/Integrations/ERP/Soma/LinxSchemaDiscoveryReader.cs`
- `scripts/linx_wise_daily_integration.py`
- `scripts/showcase_collector/`

Termos pesquisados:

- `Agent`
- `Specialist`
- `SpecialistAgent`
- `Policy`
- `Authorization`
- `Permission`
- `RBAC`
- `Guardrail`
- `Security`
- `Privacy`
- `LGPD`
- `Sensitive`
- `Approval`
- `Confirmation`
- `Risk`
- `Tool`
- `MCP`
- `Linx`
- `WISE`
- `Showcase`
- `INSERT`
- `UPDATE`
- `DELETE`
- `TRUNCATE`
- `DROP`
- `ALTER`
- `MERGE`
- `EXEC`
- `GRANT`
- `REVOKE`

## 3. Inventario de Agents e Especialistas

| Nome | Tipo | Localizacao principal | Escrita | Banco/sistema | Enforcement atual |
|---|---|---|---|---|---|
| `EchoAgent` | Runtime Agent diagnostico | `backend/src/BlueprintOS.Core/Agents/EchoAgent.cs` | Nenhuma | IA provider | Tecnico por ausencia de tools |
| `KnowledgeAgent` | Runtime Knowledge Agent | `backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs` | Nenhuma | `IKnowledgeService` / Markdown | Tecnico por contrato limitado |
| `LinxErpSpecialistAgent` | Domain Specialist runtime | `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs` | Leitura de knowledge | Base +Compras `LinxKnowledgeEntries` | Parcial: consulta knowledge e runtime IA |
| `LinxDatabaseSpecialistAgent` | Domain Specialist runtime | `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs` | Leitura de knowledge; schema discovery separado | +Compras e `SOMA_DESENV` via reader dedicado | Forte no schema reader; parcial no restante |
| WISE Agent | Operational Specialist / prompt-runbook | `.ai/context/wise-knowledge.md`, `.ai/prompts/consultar-wise.md`, `docs/operations/WiseAgentRunbook.md` | Padrao leitura; escrita so por excecao autorizada | SQL Server Linx via linked server WISE | Documental, salvo script diario |
| Showcase Agent | Operational Specialist / script | `.ai/context/showcase-knowledge.md`, `.ai/prompts/coletar-showcase.md`, `docs/operations/ShowcaseAgentRunbook.md`, `scripts/showcase_collector/` | Nenhuma no Showcase; escreve arquivos locais | API Showcase autenticada | Documental + scripts read-only |
| Maestro | Orchestrator conceitual | `.ai/AI_TEAM.md`, `.ai/context/runtime.md` | N/A | N/A | Nao implementado como runtime central |
| Planner | Orchestrator conceitual | `.ai/context/planner.md`, `.ai/context/runtime.md` | N/A | N/A | Nao implementado como engine completa |
| Negotiation strategy/memory | Orquestracao consultiva nao-agent | `docs/backend/orchestration/Orchestration.md`, `backend/src/BlueprintOS.Application/Procurement/Negotiations/` | Nenhuma critica | Memoria in-memory | Tecnico no vertical slice |

## 4. Fichas por Agent/Especialista

### 4.1 EchoAgent

**Tipo:** Runtime Agent diagnostico.

**Localizacao:**

- `backend/src/BlueprintOS.Core/Agents/EchoAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/BaseAgent.cs`
- `backend/src/BlueprintOS.Core/Agents/Contracts/IAgent.cs`

**Responsabilidade:** encaminhar a entrada diretamente ao `IAIRuntime` e devolver a resposta.

**Conhecimento:** nenhum conhecimento proprio.

**Ferramentas/acoes:** apenas chamada ao `IAIRuntime`.

**Bancos/sistemas acessados:** nenhum banco diretamente; usa provider de IA configurado.

**Capacidade de escrita:** nenhuma.

**Autorizacao atual:** nao possui fluxo proprio de autorizacao.

**Regras de seguranca existentes:** seguranca por simplicidade do contrato; sem tools, sem banco, sem escrita.

**Fontes canonicas:** codigo fonte do proprio agent.

**Relacao com outros agents:** agent base/diagnostico.

### 4.2 KnowledgeAgent

**Tipo:** Runtime Knowledge Agent.

**Localizacao:**

- `backend/src/BlueprintOS.Core/Agents/KnowledgeAgent.cs`
- `backend/src/BlueprintOS.Infrastructure/Services/KnowledgeService.cs`
- `backend/src/BlueprintOS.Infrastructure/Knowledge/MarkdownKnowledgeProvider.cs`

**Responsabilidade:** buscar conhecimento textual relevante via `IKnowledgeService`, montar prompt com contexto e enviar ao `IAIRuntime`.

**Conhecimento:** fontes Markdown configuradas via Knowledge provider.

**Ferramentas/acoes:** `IKnowledgeService.SearchAsync` e `IAIRuntime.ExecuteAsync`.

**Bancos/sistemas acessados:** nenhum banco diretamente.

**Capacidade de escrita:** nenhuma.

**Autorizacao atual:** nao possui gate proprio por operacao; depende de quem invoca o agent.

**Regras de seguranca existentes:** sem tools de escrita; risco residual de prompt injection se o conhecimento Markdown contiver texto malicioso, pois o agent base nao rotula tao fortemente quanto os Linx Specialists.

**Fontes canonicas:** `.ai/context/`, docs e Knowledge provider configurado.

**Relacao com outros agents:** base para RAG simples.

### 4.3 LinxErpSpecialistAgent

**Tipo:** Domain Specialist runtime.

**Localizacao:**

- `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs`
- `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxKnowledgeUseCases.cs`
- `backend/src/BlueprintOS.Domain/Knowledge/Linx/LinxEspecialista.cs`
- `backend/src/BlueprintOS.Api/Knowledge/LinxKnowledgeController.cs`

**Responsabilidade:** responder sobre regras, fluxos, entidades, comportamento e integracoes do ERP Visual Linx usando conhecimento persistido.

**Conhecimento:** `LinxKnowledgeEntries`, com categorias:

- `SchemaTabelaColuna`
- `RegraFuncional`
- `FluxoErp`
- `Integracao`
- `HistoricoDecisao`

**Ferramentas/acoes:** consulta `IBuscarConhecimentoUseCase`; chamada ao `IAIRuntime`.

**Bancos/sistemas acessados:** banco +Compras para base de conhecimento Linx. Nao acessa diretamente ERP Linx para executar operacoes.

**Capacidade de escrita:** leitura. A escrita em knowledge e feita por endpoints/use cases separados, nao pelo agent em si.

**Autorizacao atual:**

- GET de conhecimento exige autenticacao.
- Registrar/validar conhecimento exige `ConhecimentoLinx.Gerenciar`.
- Aprovar conhecimento exige `ConhecimentoLinx.Aprovar`.

**Regras de seguranca existentes:**

- conhecimento recuperado e inserido no prompt como dado rotulado, nao como instrucao;
- diferenca explicita entre `Validado/Aprovado`, `Descoberto` e `Inferido`;
- proibicao de fabricar resposta se nao houver evidencia recuperada;
- maquina de estado de proveniencia no dominio.

**Fontes canonicas:** banco de conhecimento Linx e docs/work orders associadas.

**Relacao com outros agents:** complementa `LinxDatabaseSpecialistAgent`; serve de fonte de interpretacao funcional para WISE/rotina Linx-WISE, mas nao executa integracao.

### 4.4 LinxDatabaseSpecialistAgent

**Tipo:** Domain Specialist runtime / Knowledge Agent especializado em schema.

**Localizacao:**

- `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs`
- `backend/src/BlueprintOS.Infrastructure/Integrations/ERP/Soma/LinxSchemaDiscoveryReader.cs`
- `backend/src/BlueprintOS.Infrastructure/Integrations/ERP/Contracts/ILinxSchemaDiscoveryReader.cs`

**Responsabilidade:** responder sobre estrutura do banco Visual Linx/SQL Server `SOMA_DESENV`, schema, tabelas, colunas, views/procedures conhecidas e relacionamentos.

**Conhecimento:** conhecimento persistido em `LinxKnowledgeEntries`; schema discovery separado usa `INFORMATION_SCHEMA`.

**Ferramentas/acoes:**

- consulta a knowledge persistido;
- schema discovery read-only por reader dedicado;
- chamada ao `IAIRuntime`.

**Bancos/sistemas acessados:**

- +Compras para knowledge;
- `SOMA_DESENV` via `LinxSchemaDiscoveryReader`, apenas metadados.

**Capacidade de escrita:** leitura.

**Autorizacao atual:** igual ao Linx Knowledge; schema reader e resolvido por DI, mas a seguranca principal dele e por contrato e SQL fixo.

**Regras de seguranca existentes:**

- `LinxSchemaDiscoveryReader` exige `InitialCatalog = SOMA_DESENV`;
- executa somente SQL fixo contra `INFORMATION_SCHEMA.TABLES` e `INFORMATION_SCHEMA.COLUMNS`;
- schema/tabela entram como parametros tipados;
- interface nao expoe SQL arbitrario;
- comentario declara explicitamente ausencia de `INSERT`, `UPDATE`, `DELETE`, `MERGE`, `ALTER`, `DROP`, `CREATE`, `GRANT`, `REVOKE`, `TRUNCATE`, `EXEC`.

**Fontes canonicas:** `LinxKnowledgeEntries`, `INFORMATION_SCHEMA`, `.ai/context/linx-wise-daily-integration.md`.

**Relacao com outros agents:** e a fronteira mais segura para conhecimento estrutural Linx; nao deve ser confundido com executor SQL operacional.

### 4.5 WISE Agent

**Tipo:** Operational Specialist / prompt-only specialist com runbook.

**Localizacao:**

- `.ai/context/wise-knowledge.md`
- `.ai/prompts/consultar-wise.md`
- `docs/operations/WiseAgentRunbook.md`

**Responsabilidade:** consultar e interpretar ambiente WISE: campanhas, saldo/estoque, estrutura `WS_*`, relacionamento Showcase-WISE e linked server `WISE_AZURE`.

**Conhecimento:**

- `WS_ESTOQUE_PRODUTOS`
- linked server `[WISE_AZURE].[SOMA_LINX].[dbo]`
- campanha `ID_CAMPANHA`
- `DT_EXCLUSAO IS NULL` como ativo
- relacionamento `PRODUTO + COR_PRODUTO`
- funcao `FN_CONSULTA_SALDO_WEB_WISE('%','%',NULL,NULL)` como fonte correta para escrita/saldo no fluxo diario

**Ferramentas/acoes:**

- consulta SQL direta via `.env`/`pyodbc` ou MCP SQL Server, quando disponivel;
- consulta Showcase via sessao autenticada como alternativa;
- nao possui implementacao runtime .NET propria.

**Bancos/sistemas acessados:**

- SQL Server de producao Linx `SRV-SOMADB`/`SOMA`;
- linked server WISE `[WISE_AZURE].[SOMA_LINX].[dbo]`;
- opcionalmente API Showcase autenticada.

**Capacidade de escrita:** padrao leitura. Escrita apenas por excecao/rotina autorizada.

**Autorizacao atual:**

- para consulta: gate de ambiente;
- para campanha: perguntar `ID_CAMPANHA` se nao informado;
- para escrita: redirecionar para runbook diario ou explicar proposta, mostrar registros afetados via `SELECT` previo e aguardar autorizacao explicita.

**Regras de seguranca existentes:**

- nunca imprimir senha, `.env`, connection string ou token;
- nunca escolher campanha sozinho;
- nunca executar automaticamente `INSERT`, `UPDATE`, `DELETE`, `TRUNCATE`, `MERGE`, `ALTER`, `DROP`, `CREATE` ou procedure de escrita;
- never executar procedures legadas automaticamente;
- runbook diario tem precedencia quando a tarefa for integracao real.

**Fontes canonicas:** `wise-knowledge.md` e `WiseAgentRunbook.md`; para escrita diaria, `linx-wise-daily-integration.md` e `LinxWiseDailyIntegrationRunbook.md` prevalecem.

**Relacao com outros agents:** recebe catalogo produto/cor do Showcase Agent; complementa Agent Linx, mas nao substitui validacao ERP.

### 4.6 Showcase Agent

**Tipo:** Operational Specialist / script-based specialist.

**Localizacao:**

- `.ai/context/showcase-knowledge.md`
- `.ai/prompts/coletar-showcase.md`
- `docs/operations/ShowcaseAgentRunbook.md`
- `scripts/showcase_collector/collect.js`
- `scripts/showcase_collector/enrich.js`
- `scripts/showcase_collector/build_excel.js`

**Responsabilidade:** acessar Showcase autenticado, detectar contexto da sessao, coletar catalogo, grade e fotos, gerar checkpoint e Excel, colaborar com WISE para saldo.

**Conhecimento:**

- API `https://wiseapi-gruposoma.azurewebsites.net/service.asmx/*`;
- endpoints `showcase`, `products`, `productColors`, `stock`;
- token `localStorage['0.soma|token']`;
- contexto de sessao: `brand_Id`, `company_Id`, `dept_Id`, `collection_Id`, `customer_Id`, `pricelist`, `payment`, `order_Id`;
- padrao de fotos no blob storage por marca detectada.

**Ferramentas/acoes:**

- Chrome DevTools MCP para sessao autenticada;
- Node.js scripts;
- chamadas HTTP GET/HEAD/fetch;
- escrita de arquivos locais de resultado.

**Bancos/sistemas acessados:** API Showcase/WiseCommerce; arquivos locais em `downloads/showcase_produtos`.

**Capacidade de escrita:** nenhuma no Showcase; escrita local controlada de arquivos.

**Autorizacao atual:**

- login manual do Product Owner;
- agent nunca preenche credenciais;
- se cair em login, parar e aguardar confirmacao;
- token apenas em memoria/env vars da execucao.

**Regras de seguranca existentes:**

- comportamento padrao somente leitura;
- nunca alterar pedido, carrinho, cadastro, configuracao ou usuario;
- nunca persistir token/cookie/senha em Git ou memoria permanente;
- se sessao expirar, parar e pedir novo login;
- cadencia controlada.

**Fontes canonicas:** `showcase-knowledge.md`, `ShowcaseAgentRunbook.md`, scripts validados.

**Relacao com outros agents:** fornece `PRODUTO + COR` ao WISE Agent; nao interpreta `WS_ESTOQUE_PRODUTOS`.

## 5. Agent Linx - Analise Detalhada

### 5.1 Escopo atual

O Agent Linx possui dois papeis reais em codigo:

- `LinxErpSpecialistAgent`: conhecimento funcional/tecnico do ERP Visual Linx.
- `LinxDatabaseSpecialistAgent`: conhecimento estrutural do banco Visual Linx/SQL Server `SOMA_DESENV`.

O escopo atual e conhecimento e interpretacao. Nao foi encontrada capacidade runtime de executar SQL operacional arbitrario ou escrita autonoma no ERP.

### 5.2 Conhece somente integracoes ou banco SOMA/Linx amplo?

Ele conhece mais que integracoes, mas por meio de knowledge persistido e schema discovery controlado:

- regras funcionais;
- fluxo ERP;
- integracoes;
- historico de decisao;
- schema/tabela/coluna.

Entretanto, o acesso tecnico implementado para schema discovery e restrito ao `SOMA_DESENV` e a metadados `INFORMATION_SCHEMA`, nao aos dados de negocio.

### 5.3 Tabelas/dominios conhecidos

Pelos documentos e buscas, dominios/tabelas recorrentes incluem:

- `CADASTRO_CLI_FOR`
- `FORNECEDORES`
- `PRODUTOS`
- `PRODUTO_CORES`
- `PRODUTOS_PRECOS`
- `MB_PROD_EXTRA_WEB`
- `WS_ESTOQUE_PRODUTOS`
- `WS_PRODUTOS`
- `WS_PRODUTO_CORES`
- `WS_PRODUTOS_BARRA`
- `WS_PROP_PRODUTOS`
- `WS_PRODUTOS_PRECOS`
- `FN_CONSULTA_SALDO_WEB_WISE`
- `LX_SEQUENCIAL`

### 5.4 Existe `LinxSpecialistAgents.cs`?

Sim:

- `backend/src/BlueprintOS.Application/Knowledge/Linx/LinxSpecialistAgents.cs`

### 5.5 Acesso de leitura/escrita

- Agents Linx: leitura de knowledge + IA.
- `LinxSchemaDiscoveryReader`: leitura de metadados em `SOMA_DESENV`.
- Endpoints Linx Knowledge: escrita controlada na base +Compras para registrar/promover conhecimento.

Nao ha executor SQL generico do Agent Linx.

### 5.6 Protecoes

Protecoes tecnicas reais:

- SQL fixo e parametrizado no schema reader;
- bloqueio de banco diferente de `SOMA_DESENV`;
- interface sem metodo para SQL arbitrario;
- RBAC para registrar/validar/aprovar knowledge;
- maquina de estado de proveniencia;
- prevencao de conflito com conteudo ja validado/aprovado;
- prompt dos Linx Specialists trata knowledge como dado, nao instrucao.

Protecoes documentais:

- proibicao de SQL autonomo de escrita;
- limites definidos em work orders e docs.

### 5.7 Barreira contra DELETE/TRUNCATE/DROP/UPDATE perigoso

No `LinxSchemaDiscoveryReader`, a barreira e enforcement real: nao existe caminho de entrada para comandos arbitrarios e o SQL emitido e fixo.

Fora desse reader, a protecao e parcial/documental. Scripts, pyodbc, MCP SQL ou credenciais SQL com permissao podem bypassar o modelo se usados diretamente.

## 6. WISE Agent - Analise Detalhada

### 6.1 Escopo

Consulta e interpretacao do ambiente WISE:

- campanhas;
- saldo/estoque;
- tabelas `WS_*`;
- linked server `WISE_AZURE`;
- relacionamento Showcase-WISE;
- leitura por `ID_CAMPANHA` e `DT_EXCLUSAO IS NULL`.

### 6.2 Seguranca

Regras principais:

- comportamento padrao somente leitura;
- gate de ambiente com `SELECT @@SERVERNAME, DB_NAME()`;
- seguir apenas se `SRV-SOMADB` e `SOMA`;
- nunca imprimir senha, `.env`, connection string ou token;
- nunca escolher `ID_CAMPANHA` sozinho.

### 6.3 Regra somente leitura

A regra somente leitura existe em runbook/prompt/contexto, nao como middleware tecnico central.

### 6.4 Excecoes para integracao

Quando a tarefa for escrita/sincronizacao diaria, o WISE Agent deve ceder precedencia ao runbook diario:

- `.ai/context/linx-wise-daily-integration.md`
- `docs/operations/LinxWiseDailyIntegrationRunbook.md`

### 6.5 Precedencia do runbook diario

Para o gatilho `Executar integracao diaria Linx/WISE desta planilha`, o runbook diario e fonte de verdade. WISE Agent e complementar.

### 6.6 Como escritas sao autorizadas hoje

Autorizacao atual para escrita:

- documental/operacional: Product Owner informa campanha e aprova escopo;
- script diario executa SQL fixo com validacoes;
- fora do runbook: deve haver proposta, `SELECT` previo e autorizacao explicita.

Nao ha objeto tecnico de autorizacao assinado ou policy central.

## 7. Showcase Agent - Analise Detalhada

### 7.1 Autenticacao

Login manual pelo Product Owner. O agent deve parar se cair na tela de login e nunca preencher credenciais, OTP ou MFA.

### 7.2 Tokens temporarios

Token JWT obtido de `localStorage['0.soma|token']` apos login. O token deve ser usado apenas durante a execucao e nunca salvo em Git, logs ou memoria permanente.

### 7.3 Leitura

O script usa endpoints de leitura/consulta da API Showcase e sondagem/baixar fotos. O uso operacional e read-only.

### 7.4 Capacidade de chamada de API

O script `collect.js` faz chamadas HTTP ao host API configurado, com Bearer token. Tambem faz `HEAD` e download de imagens.

### 7.5 Protecoes contra escrita

Protecoes atuais:

- script implementado apenas com chamadas de leitura;
- runbook proibe alterar pedido, carrinho, cadastro ou configuracao;
- ausencia de endpoints de escrita no script validado.

Nao ha gateway externo impedindo alguem de editar o script ou usar token manualmente para chamar endpoint de escrita.

### 7.6 Riscos de exposicao

Riscos principais:

- token em variavel de ambiente pode vazar em shell history/logs se usado incorretamente;
- arquivos locais podem conter dados comerciais sensiveis;
- fotos/catalogo podem ser exportacao material;
- nao ha DLP/transient secret scanner no fluxo.

## 8. +Compras / Runtime

### 8.1 O que existe

Existe:

- autenticacao obrigatoria por `FallbackPolicy` global;
- handlers de autenticacao por ambiente;
- RBAC real por permissions claims;
- policies geradas a partir de `PermissaoCatalogo`;
- `PermissaoAuthorizationHandler`;
- escopo administrativo por Unidade de Negocio em areas administrativas;
- CSRF em endpoints administrativos;
- rate limiting registrado;
- security headers;
- Data Protection para segredos tecnicos;
- endpoints Linx Knowledge com permissoes especificas;
- orquestracao consultiva de negociacao via Controller -> Application Use Case -> Strategy/Memory.

### 8.2 O que nao foi encontrado como implementacao central

Nao foi encontrado:

- Policy Engine de IA;
- Tool Gateway;
- AI Gateway;
- middleware que classifique operacoes de agents como GREEN/YELLOW/RED;
- trilha generica de aprovacao para tools;
- registro de autorizacao humana com escopo imutavel;
- interceptador SQL universal;
- runtime central com Maestro implementado como componente obrigatorio para todos os agents;
- Planner completo executando Task Protocol real.

### 8.3 O que e planejado/documental

Maestro, Planner, protocolo de tasks, AI Factory ampla, memoria avancada, observabilidade de agentes e Tool Gateway aparecem como arquitetura alvo ou fundamentos conceituais nos docs, mas nao como enforcement transversal completo.

## 9. Seguranca de Banco

### 9.1 Comandos perigosos

Comandos avaliados:

- `UPDATE`
- `DELETE`
- `TRUNCATE`
- `DROP`
- `ALTER`
- `MERGE`
- `EXEC`
- `GRANT`
- `REVOKE`

### 9.2 Classificacao das protecoes

| Area | Protecao | Tipo |
|---|---|---|
| `LinxSchemaDiscoveryReader` | SQL fixo em `INFORMATION_SCHEMA`; sem SQL arbitrario; banco `SOMA_DESENV` obrigatorio | Enforcement tecnico |
| Linx Specialists | Sem tool de escrita SQL; consulta knowledge | Enforcement tecnico parcial |
| Linx Knowledge endpoints | RBAC + CSRF + maquina de estado de proveniencia | Enforcement tecnico |
| WISE Agent runbook | Proibe DML/DDL/procedures automaticas | Documental |
| Script diario Linx/WISE | SQL fixo, filtros, gates, validacao, rollback parcial | Enforcement tecnico especifico do fluxo |
| MCP/pyodbc/manual SQL | Depende do operador e da permissao SQL | Depende de comportamento/permissao |
| Backend +Compras EF | RBAC em endpoints; repositories escrevem via use cases | Enforcement tecnico por API, nao por agent policy |

### 9.3 Conclusao

Nao existe bloqueio universal contra SQL perigoso. Existem barreiras fortes em componentes especificos, mas scripts externos e acesso SQL direto continuam dependendo de processo, credenciais e comportamento do operador/agent.

## 10. Dados Pessoais / LGPD

### 10.1 Regras encontradas

Em `.ai/context/security.md`:

- classificacao de dados pessoais;
- minimizacao;
- retencao;
- acesso e auditoria;
- protecao de segredos.

Em ADRs/docs:

- QSA nao persistido por minimizacao;
- BrasilAPI/CNPJ com preocupacao de snapshot bruto e retencao;
- uso de Data Protection para segredos;
- proibicao de imprimir segredos, tokens e connection strings;
- Showcase proibe persistir tokens/cookies.

### 10.2 Dados pessoais mencionados no repo

Foram encontrados contextos envolvendo:

- CPF/CNPJ via `CGC_CPF`, `DocumentoFiscal`, CNPJ/CPF;
- e-mail de usuario/autenticacao;
- dados de fornecedor;
- dados de cliente Showcase/WISE;
- tokens/cookies/segredos;
- possiveis dados comerciais exportados em planilhas.

### 10.3 Protecoes existentes

Protecoes tecnicas/parciais:

- Data Protection para segredos de configuracao;
- historico/expurgo de snapshot bruto de consulta CNPJ previsto/implementado como use case;
- RBAC para acesso administrativo;
- identidade autenticada nos endpoints;
- regras de minimizacao em decisao arquitetural.

Protecoes documentais:

- nao imprimir tokens/senhas/connection strings;
- nao persistir token Showcase;
- minimizacao LGPD;
- retencao por dominio.

### 10.4 Lacunas

Nao foi encontrado enforcement transversal para:

- mascaramento automatico de CPF/e-mail/telefone;
- anonimização central;
- DLP em prompts;
- bloqueio de exportacao massiva;
- classificacao formal campo-a-campo no dominio inteiro;
- politica automatica para envio de dados pessoais a modelos externos;
- policy de redacao de logs contendo PII;
- Security/LGPD Agent avaliando prompt/tool/output.

## 11. Autorizacao Humana

### 11.1 Padroes existentes

| Padrao | Onde aparece | Tipo |
|---|---|---|
| Decisao final humana em negociacao | `NegotiationRecommendationResponse.humanDecisionRequired` | Tecnico no response, mas consultivo |
| Product Owner informa `ID_CAMPANHA` | WISE/Linx-WISE runbooks | Documental/operacional |
| Login manual Showcase | Showcase runbook | Operacional |
| Escrita WISE fora do diario exige proposta + SELECT + autorizacao | WISE runbook | Documental |
| Linx Knowledge aprovar exige `ConhecimentoLinx.Aprovar` | API/RBAC | Enforcement tecnico |
| Registrar/validar Linx Knowledge exige `ConhecimentoLinx.Gerenciar` | API/RBAC | Enforcement tecnico |
| RBAC por perfil | backend Identity | Enforcement tecnico |
| Commit/push approval | workflow do projeto | Documental/operacional |
| Preview/releitura/validacao pos-escrita | script Linx-WISE | Enforcement especifico do fluxo |

### 11.2 Conclusao

Ha autorizacao humana real em alguns fluxos, mas nao existe um mecanismo central que associe uma aprovacao especifica a uma acao especifica de agent/tool com ambiente, tabela, campos, filtro, quantidade e finalidade. Esse e o principal gap para o modelo futuro desejado.

## 12. Arquitetura Atual Real

### 12.1 Fluxo backend comum

```text
Usuario/API
-> ASP.NET Core endpoint
-> Authentication + Authorization/RBAC
-> Application Use Case
-> Repository/Adapter/Service
-> SQL Server +Compras ou ERP adapter
```

### 12.2 Runtime agents

```text
Chamador interno/teste
-> IAgent.ExecuteAsync
-> IAIRuntime
-> IAIProvider/OpenAI
```

### 12.3 Knowledge Agent

```text
IAgent
-> IKnowledgeService
-> MarkdownKnowledgeProvider
-> IAIRuntime
```

### 12.4 Linx Specialists

```text
IAgent
-> IBuscarConhecimentoUseCase
-> ILinxKnowledgeRepository
-> BlueprintOSDbContext
-> IAIRuntime
```

### 12.5 Schema discovery Linx

```text
Chamador autorizado/interno
-> ILinxSchemaDiscoveryReader
-> SQL fixo INFORMATION_SCHEMA
-> SOMA_DESENV
```

### 12.6 Operacional WISE/Showcase

```text
Usuario/Product Owner
-> Prompt/runbook
-> script/MCP/pyodbc/Chrome
-> SQL/API externa
-> relatorio/planilha/arquivos locais
```

## 13. Pontos de Bypass e Riscos

1. Guardrails WISE/Showcase sao fortes como processo, mas bypassaveis por execucao manual de SQL/script.
2. Nao ha interceptacao central de tool calls.
3. Nao ha parser/classificador SQL transversal.
4. Tokens Showcase em env vars podem vazar se comandos/logs forem descuidados.
5. A rotina diaria autoriza `UPDATE` por desenho, mas a autorizacao nao e representada como objeto tecnico assinado/registrado com escopo imutavel.
6. RBAC protege endpoints HTTP do +Compras, nao acoes fora do backend.
7. Scripts externos podem evoluir sem passar por RBAC/API.
8. Prompts e runbooks dependem da disciplina do agent/operador.
9. Exportacoes locais podem conter dados pessoais/comerciais sem DLP.
10. Nao ha um catalogo tecnico central de dados pessoais por campo/tabela.

## 14. Alternativas para Security/LGPD Agent

### Alternativa 1: Policy Engine central antes de Tool Execution

Fluxo:

```text
Usuario
-> Orquestrador/Agent de dominio
-> Agent propoe ToolCall/ActionProposal
-> Security/LGPD Policy Engine classifica risco
-> Autorizacao humana quando necessario
-> Tool Gateway executa
-> Auditoria
```

**Vantagens:**

- Melhor enforcement real.
- Cria ponto unico para GREEN/YELLOW/RED.
- Permite autorizacao especifica por ambiente, sistema, tabela, operacao, campos, filtro, quantidade e finalidade.
- Reduz bypass se toda tool obrigatoriamente passar pelo gateway.

**Limitacoes:**

- Maior impacto de arquitetura.
- Exige modelar `ToolCall`, `ActionProposal`, `PolicyDecision`, `Approval`.
- Exige migrar ou envolver scripts externos.

**Enforcement:** real, se o Tool Gateway for obrigatorio.

**Risco de bypass:** baixo dentro do runtime; alto para scripts/MCP fora do gateway enquanto nao forem migrados.

**Compatibilidade:** boa com agents futuros; exige adaptacao dos existentes.

### Alternativa 2: Security/LGPD Agent como revisor consultivo no Maestro

Fluxo:

```text
Usuario
-> Maestro
-> Agent de dominio monta plano
-> Security/LGPD Agent revisa plano
-> Humano aprova se necessario
-> Execucao por fluxo existente
```

**Vantagens:**

- Menor impacto inicial.
- Pode reutilizar runbooks e contexto atual.
- Rapido para padronizar criterios.

**Limitacoes:**

- Nao bloqueia tecnicamente execucao se alguem bypassar o Maestro.
- Continua dependente do comportamento do agent/operador.

**Enforcement:** majoritariamente documental/consultivo.

**Risco de bypass:** medio/alto.

**Compatibilidade:** alta com arquitetura atual, pois exige menos mudancas.

### Alternativa 3: Adaptadores protegidos por dominio

Fluxo:

```text
Agent/Use Case
-> Adapter Linx/WISE/Showcase
-> Policy local obrigatoria
-> Execucao
-> Auditoria
```

**Vantagens:**

- Encaixa bem na arquitetura de adapters/use cases existente.
- Permite hardening incremental por dominio.
- Bom para Linx/WISE, onde riscos sao muito especificos.

**Limitacoes:**

- Pode duplicar regras se nao houver nucleo comum.
- Scripts externos continuam fora.
- Menos elegante para agents genericos.

**Enforcement:** real dentro de cada adapter instrumentado.

**Risco de bypass:** medio, se existirem rotas paralelas.

**Compatibilidade:** boa com o backend atual.

## 15. Recomendacao Arquitetural

Recomenda-se combinar Alternativa 1 com Alternativa 3:

1. Criar um contrato central de acao sensivel:

```text
ActionProposal
- ambiente
- sistema
- recurso/tabela
- operacao
- campos
- filtro
- quantidade prevista
- finalidade
- dados pessoais envolvidos
- reversibilidade
- origem/runbook
- agente solicitante
```

2. Criar um Policy Engine deterministico que classifique:

- GREEN: leitura, schema, metadata, analise.
- YELLOW: insert controlado, update contextualizado e delimitado, escrita prevista em runbook aprovado.
- RED: update sem contexto, update sem where, delete, truncate, drop, alter destrutivo, merge amplo, grant/revoke, exec/procedure desconhecida, exportacao massiva de dados pessoais, exposicao de segredo.

3. Criar um Tool Gateway que seja obrigatorio para toda acao agent-driven.

4. Criar modelo de aprovacao especifica:

```text
Ambiente: Producao
Sistema: SOMA/Linx
Tabela: PRODUTOS
Operacao: UPDATE
Campo: ENVIA_ATACADO_INTERNET
Registros previstos: 417
Filtro: conjunto validado da planilha
Finalidade: integracao diaria
Dados pessoais: nao
Reversivel: sim/nao
```

5. Migrar ou envolver scripts externos, comecando por `linx_wise_daily_integration.py`, para emitir `ActionProposal` antes de escrita.

6. Manter o Security/LGPD Agent como especialista de interpretacao e recomendacao, mas nao como a unica barreira. A barreira de seguranca deve estar em codigo.

## 16. Arquivos Provavelmente Impactados em Implementacao Futura

Possiveis arquivos/areas:

- `backend/src/BlueprintOS.Core/Agents/*`
- `backend/src/BlueprintOS.Core/Workflows/*`
- `backend/src/BlueprintOS.Application/Knowledge/Linx/*`
- `backend/src/BlueprintOS.Application/*`
- `backend/src/BlueprintOS.Domain/*`
- `backend/src/BlueprintOS.Infrastructure/DependencyInjection/ServiceCollectionExtensions.cs`
- `backend/src/BlueprintOS.Api/Program.cs`
- novos contratos para:
  - `ActionProposal`
  - `PolicyDecision`
  - `ToolGateway`
  - `ApprovalRequest`
  - `ApprovalGrant`
  - `SensitiveDataClassification`
- `scripts/linx_wise_daily_integration.py`
- `scripts/showcase_collector/*`, se forem incorporados ao gateway
- `.ai/context/security.md`
- `.ai/context/agents.md`
- `.ai/context/runtime.md`
- `docs/operations/*Runbook.md`
- `docs/agents/Agents.md`
- `docs/agents/AgentsCatalog.html`

## 17. Conclusao

O SOMA BlueprintOS ja possui uma boa base de seguranca aplicacional no backend +Compras: autenticacao obrigatoria, RBAC real, permissoes centralizadas, endpoints protegidos, CSRF em areas administrativas e alguns mecanismos de protecao de segredo.

Para Agents, entretanto, a seguranca ainda esta dividida entre:

- contratos tecnicos limitados em componentes especificos;
- runbooks/prompt discipline;
- permissao SQL do usuario/operador;
- scripts com validacoes proprias.

O Security/LGPD Agent nao deveria nascer como apenas mais um prompt. Ele deve se encaixar como parte de uma camada transversal de policy e tool execution, com enforcement real. O papel ideal do agent especialista e interpretar risco, contexto e LGPD; a decisao bloqueante deve ser aplicada por Policy Engine/Tool Gateway/adapters protegidos.

