# Caderno — Onda 2

## Índice de convenções

Ver `README.md` nesta mesma pasta para o template e as regras de uso.

---

### ENCERRAMENTO FORMAL DA ONDA 2

- **Origem:** Encerramento documental da Onda 2, autorizado pelo Product Owner.
- **Assunto:** Registro formal de que a Onda 2 (Multi-BU/Multi-ERP + B3/Item Fiscal) está tecnicamente
  aprovada, commitada e publicada em `origin/main`.
- **Tipo:** Governança
- **Tratar em:** Somente documentação
- **Status:** **CONCLUÍDA / APROVADA**
- **Data técnica:** 04/09/2026
- **Commit oficial:** `d8dac6ed82aa0cfaa2222b4c56b6288fbc241a77` — "feat: complete Onda 2 B3 integration and
  multi-BU foundation" (`origin/main` sincronizado, 0 commits locais/remotos pendentes na data deste
  encerramento).
- **Resumo:** Este Caderno, do início ao fim, é o registro cronológico completo da rodada arquitetural
  Multi-BU/Multi-ERP e do fechamento técnico do B3/Item Fiscal. O estado final aprovado é:
  - **B3 (Item Fiscal — Blocos 1–4): APROVADO/HOMOLOGADO.** Discovery homologado; Bloco 1 (Conta Contábil),
    Bloco 2 (Unidade de Medida), Bloco 3 (Item Fiscal, domínio local/CRUD/RBAC) e Bloco 4 (Referências por
    Fornecedor) concluídos e homologados (ver `.ai/CURRENT_SPRINT.md`, "B3 — Item Fiscal", commit `c2365f5`).
  - **Onda 2 (Multi-BU/Multi-ERP): tecnicamente pronta e aprovada.** Todas as decisões arquiteturais e GAPs
    desta rodada estão registrados nas entradas acima (fronteira de dados por BU, integrações headless com
    BusinessUnit explícita, normalização de Fornecedor/CNPJ, `IntegrationOccurrence`/`LinxDatasetLoadState`,
    metadados de cadastro de apoio, classificação Multi-BU das entidades administrativas).
  - **Multi-BU normalizado para o Grupo Soma:** as 4 migrations desta rodada
    (`NormalizarMetadadosApoioPorUnidadeNegocioOnda2`, `NormalizarFornecedorPorUnidadeNegocioOnda2`,
    `NormalizarIntegrationOccurrencePorUnidadeNegocioOnda2`, `NormalizarLinxDatasetLoadStatePorUnidadeNegocioOnda2`)
    aplicadas com sucesso em `MAISCOMPRAS Development` (ver "Validação real em MAISCOMPRAS Development" acima)
    — zero perda de dado, backfill 100% correto, `dotnet ef migrations has-pending-model-changes` sem
    pendências.
  - **Bateria final de certificação B3:** ver "Bateria final de certificação B3 (04/09/2026)" acima —
    incremental normal PASS nos 5 datasets baselined; teste controlado real de ~101 Fornecedores em
    SOMA_DESENV (detecção → aplicação → restauração → detecção → aplicação) PASS; idempotência PASS;
    reconciliação de Fornecedor PASS (0 divergências); 2 defeitos reais encontrados e corrigidos (watermark
    sem fuso do servidor Linx; não-determinismo de RAW duplicado).
  - **Auditoria RAW determinística:** ver "GAP — auditoria RAW determinística" acima — todos os 5
    consumidores RAW→REFINED do B3 auditados; os 4 que ainda não tinham desempate por "mais recente" foram
    corrigidos (Cadastro de Apoio, Item Fiscal, Fornecedor Domínios, Item Fiscal Referência Fornecedor).
  - **Testes:** evolução registrada nesta rodada de 1.380 unitários + 30 integração (bateria final B3) para
    1.393 unitários + 30 integração (após a auditoria RAW determinística), todos aprovados, 0 falhas, sem
    regressão. `dotnet build`/`dotnet ef migrations has-pending-model-changes`: limpos em todas as etapas.
  - **Zero escrita Linx pelo pipeline 5A:** nenhum pipeline REFINED/LiveRead governado desta rodada escreveu
    no ERP Linx — apenas leitura (RAW/REFINED/reconciliação). O teste controlado de ~101 registros alterou
    dado real em `SOMA_DESENV` de forma deliberada e reversível (ver "Bateria final de certificação B3"
    acima), com restauração confirmada 101/101 antes do encerramento — não é escrita do pipeline, é validação
    controlada e auditada do próprio Gate.
  - **Zero LLM no happy path:** os pipelines de sincronização/RAW/REFINED/reconciliação desta Onda são
    determinísticos de ponta a ponta — nenhuma chamada a `IAIRuntime`/Agent/LLM no caminho de alto volume (ver
    regra formalizada em "Agent ≠ LLM", `agents/AGENT_CONTRACT.md`/`agents/docs/AIGovernance.md`).
  - **GAPs residuais:** classificados e **não bloqueadores** da Onda 2 — ver seção "Gaps Residuais" abaixo.
- **Decisão:** Encerramento formal autorizado pelo Product Owner nesta rodada documental. Nenhum código,
  migration, teste ou script foi alterado por este encerramento — apenas consolidação e fechamento do
  registro já produzido pelas entradas técnicas deste Caderno.

---

### Gaps residuais da Onda 2 (não bloqueadores)

- **Origem:** Consolidação do encerramento formal da Onda 2 (rodada documental).
- **Assunto:** Lista única dos gaps reais que permanecem abertos ao final da Onda 2 — nenhum deles impede o
  fechamento, todos têm gatilho explícito de quando devem ser tratados.
- **Tipo:** Governança
- **Tratar em:** Ver "Tratar em" individual de cada item.
- **Status:** Registrado
- **Resumo:**
  1. **`IDatasetLoadGate`/`ToolGateway` (LiveRead governado) ainda não é Multi-BU-aware** — ver GAP residual 1
     em "GAP — `DatasetLoadState`/`IntegrationOccurrence` sem dimensão de BU" acima. **Tratar em:** antes de
     uma segunda BU real executar `linx.fornecedores.snapshot` por esse caminho.
  2. **`RawLinxFornecedorSnapshotExecucao` ainda sem `UnidadeNegocioId`** — ver GAP residual 2 na mesma
     entrada. **Tratar em:** antes do onboarding operacional de uma segunda BU no mesmo dataset.
  3. **`ConfiguracaoErp` existe por BU, mas os `Soma*Readers` ainda não a consomem** — ver "GAP —
     `ConfiguracaoErp` (O1.11) ainda não é consumida pelos leitores ERP reais" acima. Dívida arquitetural de
     evolução Multi-ERP, sem prazo atrelado ao fechamento da Onda 2.
  4. **Bloco 5B (+Compras → Linx) intencionalmente não iniciado** — depende de validação com especialista
     Visual Linx; não bloqueou o B3 nem a Onda 2 (ver "Bloco 5B ... não bloqueia o fechamento funcional da
     Onda 2" acima).
  5. **Ressalva de Item Fiscal — criação ERP de Item Fiscal novo** comprovada por testes automatizados/código,
     não por execução real end-to-end (ambiguidade observada na formatação `CodigoErp` RAW × domínio).
     Registrada como ressalva, sem bloqueio retroativo do B3/Onda 2.

  Nenhum destes 5 pontos é um bloqueador retroativo do Gate B3 ou do fechamento da Onda 2 — todos foram
  avaliados pelo Product Owner e classificados como dívida a tratar no gatilho descrito, não antes.

---

### Toda dado funcional do +Compras pertence a uma Unidade de Negócio

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Unidade de Negócio é fronteira de dados, não apenas atributo de exibição.
- **Tipo:** Arquitetura
- **Tratar em:** Nesta onda (fundação já parcialmente existente desde O1.11/ADR-0022; extensão a entidades
  ainda não cobertas é o gap desta rodada — ver "Classificação Multi-BU das entidades administrativas" abaixo).
- **Status:** Decidido
- **Resumo:** Fornecedores, vínculos ERP, tipos/subtipos de fornecedor, condições de pagamento, Filiais,
  Centros de Custo, Contas Contábeis, Unidades de Medida, Itens Fiscais, referências Item Fiscal × Fornecedor,
  configurações, metadados locais, Descrição +Compras, status locais, regras orçamentárias, alçadas, vínculos
  operacionais e datasets RAW/REFINED — todos pertencem conceitualmente a uma BU. Nada disso é global só
  porque hoje só existe uma BU operacional (Grupo Soma).
- **Decisão:** Confirmada pelo Product Owner nesta rodada. A fundação técnica (`UnidadeNegocio`,
  `EscopoAdministrativoUnidadeNegocio`, ADR-0022) já existe desde a Onda 1 (O1.11) para o eixo
  administrativo/RBAC; esta rodada estende o princípio a todo o eixo de dados operacionais/integrados.

---

### Integrações headless recebem BusinessUnit explicitamente, nunca inferida do usuário

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Um pipeline ERP (ex.: sincronização Item Fiscal Linx → +Compras) não tem usuário logado; a
  Unidade de Negócio da execução vem do contexto explícito do disparo (job, execução agendada, chamada
  administrativa), nunca de heurística.
- **Tipo:** Arquitetura
- **Tratar em:** Nesta onda — é a regra que resolve conceitualmente o GAP de `ItemFiscal.CriarDeErp` exigindo
  `UnidadeNegocioId`.
- **Status:** Implementado para Item Fiscal (03/09/2026) — padrão a replicar nos demais pipelines (ver GAPs
  abaixo).
- **Resumo:** Se uma execução não tiver uma BusinessUnit válida/configurada, o pipeline deve falhar fechado
  (fail closed) antes de ler ou escrever qualquer dado de domínio — nunca assumir um default, nunca inferir
  a partir de `ICurrentIdentity`. A mesma regra vale para qualquer outra entidade integrada que precise de
  `UnidadeNegocioId` no futuro (não é regra exclusiva de Item Fiscal).
- **Decisão:** Confirmada pelo Product Owner nesta rodada. Implementado para Item Fiscal:
  `ItensFiscaisRefinedCliHandler` exige `--business-unit <slug>`, resolve contra `UnidadeNegocio` real
  (`IUnidadeNegocioRepository.ObterPorSlugAsync`, adicionado ao contrato existente) e falha fechado
  (`BUSINESS_UNIT_REQUIRED`/`BUSINESS_UNIT_NOT_FOUND`/`BUSINESS_UNIT_INATIVA`, exit code 1) antes de instanciar
  o use case. `ProcessarItensFiscaisRawParaDominioUseCase.ExecutarAsync` recebe `Guid unidadeNegocioId`
  obrigatório e lança `ArgumentException` se `Guid.Empty` (defesa em profundidade). Padrão pronto para
  replicar em `CadastroApoioRefinedCliHandler`/`RefinedFornecedorCliHandler`/
  `ItensFiscaisReferenciasFornecedorRefinedCliHandler` quando os GAPs de `DatasetLoadState`/
  `IntegrationOccurrence`/Fornecedor forem implementados (mesma mecânica de resolução, só o `unidadeNegocioId`
  muda de "não usado" para "usado").

---

### Grupo Soma/Linx é implementação inicial de referência, não regra universal do +Compras

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Nenhuma decisão funcional específica da Grupo Soma (ex.: layout de tabela Linx, regra de
  trigger, particularidade de campo) deve ser promovida a regra global do +Compras sem evidência de que
  outra BU/ERP a exige também.
- **Tipo:** Arquitetura / Governança
- **Tratar em:** Nesta onda (vigora desde já como princípio de revisão de código) e Encerramento do Projeto
  (validação plena só é possível com uma segunda BU real).
- **Status:** Decidido
- **Resumo:** Contratos canônicos do +Compras (Fornecedor, Filial, CentroCusto, ContaContabil, UnidadeMedida,
  ItemFiscal etc.) são conhecimento de negócio, independente de ERP. Camada de ERP Adapter/Capabilities
  conhece tabelas/APIs/schemas/regras técnicas de um ERP específico (hoje só Linx). Camada de Business Unit
  Profile guarda config/particularidades de uma BU específica (hoje só Grupo Soma). Ver detalhe completo em
  `Encerramento-Projeto.md`, entrada "FactoryBU.New / onboarding Multi-BU e Multi-ERP".
- **Decisão:** Confirmada pelo Product Owner nesta rodada.

---

### Verificação do Bloco 5A em progresso (não implementado por esta rodada)

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026). Trabalho paralelo não commitado
  de outra sessão, em andamento no momento desta rodada.
- **Assunto:** Auditoria read-only do estado do Bloco 5A (sincronização Linx → +Compras de Item Fiscal /
  `FornecedorLinxVinculo` / RAW-REFINED / `DatasetLoadState`) em relação às regras de BU-awareness acima.
- **Tipo:** Técnico
- **Tratar em:** Nesta onda — a ser fechado por quem estiver conduzindo o Bloco 5A; esta rodada apenas leu o
  estado (autorizado pelo Product Owner a não editar esses arquivos, para não colidir com trabalho não
  commitado em andamento).
- **Status:** Em análise
- **Resumo:** Achados objetivos da auditoria read-only (03/09/2026):
  - `ItemFiscal.CriarDeErp` (Domain) já recebe `UnidadeNegocioId` como `Guid` obrigatório, sem qualquer
    inferência a partir de usuário/sessão dentro do Domain — correto por construção.
  - O pipeline headless real (`ProcessarItensFiscaisRawParaDominioUseCase`) **ainda não chama**
    `CriarDeErp` em nenhum lugar: itens Linx novos (sem `ItemFiscal` local correspondente) são
    deliberadamente **rejeitados** com uma ocorrência `Warning`
    (`ITEM_FISCAL_NOVO_SEM_UNIDADE_NEGOCIO_RESOLVIVEL`), com o próprio código documentando isso como "GAP
    arquitetural, aguardando decisão do PO". Ou seja: a regra "não inferir BU do usuário" já é respeitada
    (fail-safe por omissão), mas a criação de fato (receber a BU explícita da execução e chamar
    `CriarDeErp`) ainda não foi implementada — exatamente o próximo passo que a decisão desta rodada
    (seção acima, "Integrações headless recebem BusinessUnit explicitamente") autoriza a desbloquear.
  - `FornecedorLinxVinculo` (Domain, novo/em progresso): identidade lógica é `ErpSistema + CodigoErp`,
    **sem** `UnidadeNegocioId`. Alinhado ao índice do `Fornecedor` (ver GAP "Fornecedor/CNPJ" abaixo).
  - `LinxDatasetLoadState`: chave é só `Dataset` (string), sem `UnidadeNegocioId` — duas BUs rodando o
    mesmo nome de dataset colidiriam de estado (watermark/baseline compartilhados).
  - `IntegrationOccurrence`: identidade/dedup é `(ExecutionId, Dataset, Stage, Code, OriginRecordKey)`, sem
    `UnidadeNegocioId` — isolamento entre execuções existe via `ExecutionId`, não via BU explícita.
  - `Fornecedor.Cnpj_Cpf`: índice único **global**, com comentário de código deliberado ("Fornecedor é
    corporativo, não pertence a um usuário [...] `BusinessUnit` descartado por ausência de evidência de
    necessidade"). Ver GAP dedicado abaixo — é mudança de regra já homologada, não uma omissão.
- **Decisão:** Nenhuma alteração de código feita nesta rodada nesses arquivos (autorizado pelo Product Owner
  a não colidir com o Bloco 5A em progresso). Os 4 pontos acima (ItemFiscal via pipeline, DatasetLoadState,
  IntegrationOccurrence, Fornecedor/CNPJ) ficam registrados como GAPs formais abaixo.

---

### GAP — Fornecedor/CNPJ: unicidade global vs. fronteira de BU

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026), auditoria read-only do Bloco 5A.
  **Decisão do Product Owner registrada em 03/09/2026 (mesma data, rodada seguinte).**
- **Assunto:** Identidade funcional de Fornecedor passa a ser `UnidadeNegocioId + CnpjCpf` — 1 CNPJ/CPF = 1
  Fornecedor **dentro** da Unidade de Negócio, não corporativamente entre BUs. `Grupo Soma/CNPJ X` e
  `Reserva/CNPJ X` podem existir como Fornecedores independentes, cada um com seus próprios vínculos ERP,
  Principal, tipos/subtipos, condições, metadados, Descrição +Compras, status e relacionamentos — sem
  compartilhamento automático de dado entre BUs. `FornecedorLinxVinculo` também deixa de ter identidade
  apenas `ErpSistema + CodigoErp`: passa a contemplar a Business Unit.
- **Tipo:** Arquitetura / Funcional
- **Tratar em:** Nesta onda.
- **Status:** Implementado (04/09/2026).
- **Resumo:** É normalização arquitetural para multi-BU, **não reabertura funcional** do cadastro Grupo
  Soma — o Gate de Fornecedores já homologado (01/09/2026) permanece válido para Grupo Soma, sem regressão.
  Implementado de ponta a ponta: `Fornecedor.UnidadeNegocioId` (imutável, guard fail-closed nos 3
  construtores) e `FornecedorLinxVinculo.UnidadeNegocioId` (Domain); índices únicos compostos
  `(UnidadeNegocioId, Cnpj_Cpf)`, `(UnidadeNegocioId, ErpSistema, ErpFornecedorId)` e
  `(UnidadeNegocioId, ErpSistema, CodigoErp)` (Infrastructure/Persistence/Configurations);
  `IFornecedorRepository`/`IFornecedorLinxVinculoRepository` e as 2 implementações passam a exigir
  `unidadeNegocioId` em toda busca/existência; `ContextoBuFornecedor.Resolver(RequestIdentity)` resolve a BU
  para os casos de uso interativos (`Cadastrar`/`Atualizar`/`Inativar`/`AlterarStatus`/`Obter`/`Pesquisar`/
  `PesquisarPaginado`), com BU divergente tratada como "não encontrado" (isolamento, nunca vazamento de
  existência entre BUs); os pipelines headless (`SincronizarFornecedoresErpUseCase`,
  `ProcessarFornecedoresRawParaDominioUseCase`, `GarantirFornecedorNoErpUseCase`,
  `BackfillFornecedorLinxVinculosUseCase`, `VincularFornecedorDominiosErpUseCase`,
  `SincronizarFornecedorDominiosErpUseCase`) recebem `Guid unidadeNegocioId` explícito e fail-closed (mesmo
  padrão do Item Fiscal); `SincronizarFornecedorUseCase` resolve a BU pelo slug informado no DTO via
  `IUnidadeNegocioRepository`. Migration `NormalizarFornecedorPorUnidadeNegocioOnda2`
  (04/09/2026): backfill seguro (coluna nullable → `UPDATE` a partir da única Unidade de Negócio real existente
  no banco de destino, com `THROW` fail-closed se a contagem de BUs ≠ 1, nunca GUID hardcoded → `AlterColumn`
  NOT NULL → índices/PK), substituindo manualmente o `defaultValue: Guid.Empty` que o scaffold do EF gera por
  padrão para coluna Guid não-nula nova (inseguro para backfill). Testes: `FornecedorRepositoryIntegrationTests`
  reescrito com Grupo Soma/Reserva reais e 4 cenários Multi-BU novos (mesmo CNPJ em 2 BUs coexiste; duplicata
  na mesma BU rejeitada; Fornecedor de uma BU nunca aparece em query da outra; edição/inativação em uma BU
  nunca afeta a outra) — a checagem de unicidade usa inspeção de metadado do modelo EF
  (`context.Model.FindEntityType(...).GetIndexes()`), pois o provider InMemory usado em testes não impõe
  `HasIndex().IsUnique()` em runtime (a imposição real em SQL Server já foi comprovada no Work Order B2.9);
  demais suites (`FornecedorUseCasesTests`, `GarantirFornecedorNoErpUseCaseTests`,
  `SincronizarFornecedorUseCaseTests`, `SincronizarFornecedoresErpUseCaseTests`,
  `FornecedorLinxVinculoUseCasesTests`, `FornecedorLinxVinculoModelTests`, `FornecedorEnriquecimentoUseCasesTests`,
  `SincronizarItemFiscalReferenciasFornecedorErpUseCaseTests`) atualizadas para as novas assinaturas, sem
  perda de cobertura. `dotnet ef migrations has-pending-model-changes`: sem pendências.
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026; implementação concluída e testada em 04/09/2026.

---

### GAP — `DatasetLoadState`/`IntegrationOccurrence` sem dimensão de BU

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERp (03/09/2026), auditoria read-only do Bloco 5A.
  **Decisão do Product Owner registrada em 03/09/2026: confirmado como consequência obrigatória.**
- **Assunto:** Estado de carga (`LinxDatasetLoadState`, chave só por `Dataset`) e ocorrências de integração
  (`IntegrationOccurrence`, chave por `ExecutionId+Dataset+Stage+Code+OriginRecordKey`) não incluem
  `UnidadeNegocioId`. Seguro hoje (só existe Grupo Soma); torna-se colisão real assim que uma segunda BU
  rodar os mesmos nomes de dataset.
- **Tipo:** Técnico
- **Tratar em:** Nesta onda.
- **Status:** Implementado (04/09/2026) para `IntegrationOccurrence` e para `LinxDatasetLoadState` nos 3
  pipelines REFINED que o consomem diretamente via `DbContext`. **Permanece deliberadamente NÃO implementado**
  (por decisão de escopo, não por esquecimento) na cadeia de Governança de IA
  (`IDatasetLoadGate`/`LinxDatasetSnapshotReadAdapter`/`ToolGateway`) — ver GAP residual abaixo.
- **Resumo:** `IntegrationOccurrence.UnidadeNegocioId` implementado (Domain: parâmetro fail-closed em
  `Registrar(...)`; índice de dedupe migrado para `(UnidadeNegocioId, ExecutionId, Dataset, Stage, Code,
  OriginRecordKey)`); os 5 call sites de produção (`ProcessarFornecedoresRawParaDominioUseCase`,
  `ProcessarItensFiscaisReferenciasFornecedorRawParaDominioUseCase`, `VincularFornecedorDominiosErpUseCase`,
  `SincronizarFornecedorDominiosErpUseCase`, `ProcessarCadastroApoioRawParaDominioUseCase`) já recebiam
  `unidadeNegocioId` explícito (threading feito junto com Fornecedor/Item Fiscal) e passaram a propagá-lo
  também para `Registrar`. Migration `NormalizarIntegrationOccurrencePorUnidadeNegocioOnda2` (04/09/2026),
  mesma disciplina de backfill seguro (nunca `Guid.Empty`, `THROW` fail-closed se a BU real não puder ser
  resolvida de forma inequívoca). `LinxDatasetLoadState`: chave composta `(UnidadeNegocioId, Dataset)`
  implementada no Domain (`Novo(Guid unidadeNegocioId, string dataset)`, fail-closed) e na configuração EF
  (`HasKey`); migration `NormalizarLinxDatasetLoadStatePorUnidadeNegocioOnda2` (04/09/2026, mesmo padrão
  drop-PK → coluna nullable → backfill seguro → NOT NULL → recriar PK composta). Escopo entregue
  deliberadamente restrito aos 3 pipelines REFINED que leem/gravam `LinxDatasetLoadState` diretamente via
  `DbContext` (Item Fiscal, Cadastro de Apoio genérico, Fornecedor) — **zero alteração em
  `BlueprintOS.Core`**, portanto zero risco novo sobre `ToolGateway`/`ActionProposal`/prevenção de reuso de
  aprovação, que têm suíte adversarial própria e não foram tocados nesta rodada. Testes:
  `IntegrationOccurrenceTests` (+2: `Registrar_Exige_UnidadeNegocioId`,
  `Mesma_Chave_De_Dedupe_Em_BUs_Diferentes_Nao_Colide`); `LinxDatasetLoadStateMultiBuTests` (novo,
  persistência EF real provando que 2 BUs executando o mesmo dataset nunca compartilham bootstrap/baseline/
  watermark); `ToolGatewayLiveReadTests`/`LinxDatasetLoadStateAvancarWatermarkTests`/
  `LinxDatasetLoadStateGateTests` ajustados apenas para passar um `Guid.NewGuid()` a `Novo(...)` — nenhuma
  asserção de Governança de IA mudou. `dotnet ef migrations has-pending-model-changes`: sem pendências para
  nenhuma das 3 migrations desta rodada (Fornecedor, IntegrationOccurrence, LinxDatasetLoadState) compostas.
- **GAP residual 1 — `IDatasetLoadGate`/`ToolGateway` (LiveRead governado) permanece single-BU-safe, não
  Multi-BU-aware:** `LinxDatasetLoadStateGate` (`Infrastructure/Integrations/ERP/Soma`) implementa
  `IDatasetLoadGate` (`BlueprintOS.Core.AI.Governance.Contracts`), consumido por `LinxDatasetSnapshotReadAdapter`
  (`BlueprintOS.Core`) — a capability governada de LiveRead (`linx-dataset-snapshot-read`, B3/Bloco 5A.9) que
  passa pelo `ToolGateway`/`ActionProposal`. Propagar `unidadeNegocioId` por essa cadeia até o motor de
  Governança de IA exigiria tocar `ToolGateway.cs`/`GovernedWriteModels.cs` — decisão deliberada de NÃO fazer
  isso nesta rodada, por ser o subsistema de maior criticidade de segurança do projeto (hash de payload para
  prevenção de reuso de aprovação, suíte de testes adversariais própria), tangencial ao fechamento da Onda 2
  (relevante hoje só para o dataset `linx.fornecedores.snapshot`, sem urgência com uma única BU em produção),
  e sem necessidade real ainda (nenhuma segunda BU executa esse caminho hoje). **Tratar em:** só quando existir
  necessidade real de uma segunda BU executar `linx.fornecedores.snapshot` via LiveRead governado; decisão
  explícita do Product Owner recomendada antes de tocar `BlueprintOS.Core.AI.Governance`.
- **GAP residual 2 — identidade de execução RAW (`RawLinxFornecedorSnapshotExecucao`) ainda não inclui
  Business Unit — achado NOVO desta rodada (04/09/2026):** `RawLinxFornecedorSnapshotExecucao` (a execução
  RAW compartilhada como cabeçalho pelos pipelines de Fornecedor/Item Fiscal/Cadastro de Apoio/Referências
  Fiscais) não tem `UnidadeNegocioId`. Confirmado por leitura direta do código: a resolução da "execução Full
  completa mais recente" usada para homologar baseline (`ProcessarFornecedoresRawParaDominioUseCase.cs:79-82`,
  mesmo padrão nos demais processadores REFINED) é `context.RawLinxFornecedoresSnapshotExecucoes.Where(e =>
  e.Dataset == dataset && e.Completa).OrderByDescending(e => e.ConcluidoEm).FirstOrDefaultAsync()` — **sem
  filtro de BU**. Hoje seguro (só existe Grupo Soma); no dia em que uma segunda BU executar o mesmo `Dataset`,
  essa query pode selecionar a execução RAW de uma BU como baseline/candidata de outra BU — exatamente a
  colisão que a decisão do PO de "execução = BusinessUnit + Dataset + ERP Configuration + Execution Mode +
  ExecutionId, nunca colidir entre BUs" pretende impedir. Este piso (identidade da execução RAW em si, e as
  tabelas `RawLinx*Snapshot`/`RawFiliaisSnapshot`/etc. que ela referencia) **não foi normalizado nesta
  rodada** — é escopo real, não coberto pelas 3 migrations já aplicadas, e requer tocar o mesmo caminho de
  escrita RAW usado pelo LiveRead governado (GAP residual 1 acima), portanto tem o mesmo tipo de acoplamento
  com `BlueprintOS.Core` a avaliar com cuidado antes de implementar. **Tratar em:** antes de qualquer execução
  real com uma segunda Business Unit sobre os mesmos datasets Linx — não bloqueia o fechamento da Onda 2 com
  Grupo Soma como única BU ativa, mas deve ser resolvido antes de onboarding real de uma 2ª BU (FactoryBU.New).
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026 (regra); `IntegrationOccurrence` e
  `LinxDatasetLoadState` (escopo REFINED) implementados e testados em 04/09/2026; os 2 GAPs residuais acima
  ficam registrados explicitamente para decisão/priorização futura do Product Owner.

### Validação real em MAISCOMPRAS Development (04/09/2026)

- **Origem:** Fechamento da rodada Onda 2 — Multi-BU/Multi-ERP. Mecanismo de conexão já autorizado
  (`ConnectionStrings:MaisComprasConnection` via `dotnet user-secrets`, projeto `BlueprintOS.Api`, per
  `agents/DATABASE_CONNECTION_POLICY.md` §3.1/§4) localizado e utilizado — nenhuma credencial nova foi
  solicitada, gravada, logada ou exibida.
- **Achado crítico corrigido antes da aplicação:** a tabela `UnidadesNegocio` real tem **2** linhas (`Grupo
  Soma`/`grupo-soma` e uma fixture `BU Teste Gate 41`/`bu-teste-gate-41`), não 1. As 3 migrations desta
  rodada (Fornecedor, IntegrationOccurrence, LinxDatasetLoadState) assumiam originalmente "exatamente 1
  Unidade de Negócio cadastrada" para resolver Grupo Soma — premissa falsa neste ambiente, que teria
  disparado o `THROW` fail-closed em vez de aplicar corretamente. Corrigido nas 3 migrations para resolver
  por `WHERE [Slug] = N'grupo-soma'` (nunca por contagem/`TOP 1` da tabela inteira) antes de qualquer
  aplicação real — exatamente o tipo de verificação de segurança do backfill exigida antes de tocar dado
  real.
- **Migrations aplicadas** (`dotnet ef database update`, MAISCOMPRAS Development, `SRV-SOMA-DEV`): as 4
  pendentes — `NormalizarMetadadosApoioPorUnidadeNegocioOnda2`,
  `NormalizarFornecedorPorUnidadeNegocioOnda2`, `NormalizarIntegrationOccurrencePorUnidadeNegocioOnda2`,
  `NormalizarLinxDatasetLoadStatePorUnidadeNegocioOnda2` — aplicadas com sucesso sobre o schema do Bloco 5A
  já presente no banco (todas as migrations anteriores já estavam aplicadas).
- **Validação pós-migration (evidência objetiva, `CONFIRMED_IN_DEVELOPMENT`):**
  - Contagens de linhas idênticas antes/depois — zero perda de dado: `Fornecedores` 73.449,
    `FornecedorLinxVinculos` 76.419, `IntegrationOccurrences` 9.667, `LinxDatasetLoadState` 5.
  - Backfill 100% correto: 0 linhas com `UnidadeNegocioId` nulo, 0 linhas atribuídas a uma BU diferente de
    Grupo Soma, nas 4 tabelas.
  - `UnidadesNegocio` permanece com 2 linhas — nenhuma BU nova foi criada; `BU Teste Gate 41` permanece com
    zero Fornecedores/LoadState (nenhum dado atribuído por engano à fixture).
  - Índices únicos compostos confirmados criados em produção do schema:
    `IX_Fornecedores_UnidadeNegocioId_Cnpj_Cpf`, `IX_Fornecedores_UnidadeNegocioId_ErpSistema_ErpFornecedorId`,
    `IX_FornecedorLinxVinculos_UnidadeNegocioId_ErpSistema_CodigoErp`,
    `IX_IntegrationOccurrences_Dedup` (`UnidadeNegocioId, ExecutionId, Dataset, Stage, Code,
    OriginRecordKey`, filtrado, único).
  - `dotnet ef migrations has-pending-model-changes` contra o banco real: sem pendências.
- **SOMA_DESENV (Linx):** zero leituras e zero escritas nesta etapa — não foi necessário, já que as tabelas
  normalizadas pertencem exclusivamente a `MAISCOMPRAS`.
- **Status:** Concluído e validado (04/09/2026).

---

### Bateria final de certificação B3 (04/09/2026) — 2 defeitos reais encontrados e corrigidos, execução real contra SOMA_DESENV

- **Origem:** Bateria final de certificação técnica do B3/Onda 2, autorizada pelo Product Owner (checkpoint
  Multi-BU já aprovado). Execução real (não simulada) de LiveRead governado + REFINED contra
  `SOMA_DESENV`/`MAISCOMPRAS Development`, incluindo um teste controlado e reversível de inativação em 101
  Fornecedores reais (registro completo BEFORE/candidatos em
  `scratchpad/{before_snapshot_fornecedores.txt,clifor_in_list_quoted.txt}` desta sessão).
- **Pré-flight:** build limpo, `has-pending-model-changes` sem pendências (local e contra o banco real),
  nenhuma execução RAW `EmAndamento` (nenhuma linha com `ConcluidoEm IS NULL`), Grupo Soma resolvida por
  `Slug` real (`376BDC4C-...`), watermarks/baselines dos 5 datasets já homologados (Fornecedores, Filiais,
  Centros de Custo, Contas Contábeis, Itens Fiscais — todos `CargaFullInicialValidada=1`/
  `IncrementalLiberado=1`); `linx.unidades-medida.snapshot` sem `LinxDatasetLoadState` **por design**
  (dataset exclusivamente Full, não exige a máquina de baseline — ver doc-comment de
  `ProcessarCadastroApoioRawParaDominioUseCase`).
- **Incremental normal (happy path):** executado e aprovado, real, contra SOMA_DESENV, para os 5 datasets
  baselined (Fornecedores, Centros de Custo, Contas Contábeis, Filiais, Itens Fiscais) — 0 mudanças
  detectadas em todos (esperado: nada mudou no Linx desde o último watermark), `LiveReadCompleted` em todos.
- **DEFEITO REAL #1 — comparação de watermark ignorava o fuso do servidor Linx:** o teste controlado (101
  Fornecedores inativados de forma reversível em SOMA_DESENV, ver abaixo) expôs que um INCREMENTAL executado
  minutos depois da alteração real reportava `rowsRead=0` — a mudança não era detectada. Causa raiz
  confirmada por evidência real (`GETDATE()`/`GETUTCDATE()` do servidor Linx): `DATA_PARA_TRANSFERENCIA` é
  estampada pela trigger via `GETDATE()` (hora LOCAL do servidor, UTC-3, sem fuso), mas o código comparava
  esse valor diretamente contra `watermark.UtcDateTime` — subestimando sistematicamente qualquer mudança
  recente por um valor igual ao offset local×UTC (~3h para SRV-SOMA-DEV). Corrigido em
  `SomaLinxDatasetBulkReader.StreamAsync` (`ResolveWatermarkNoFusoDoServidorOrigemAsync`): o offset é
  resolvido em runtime, direto do servidor de origem (`DATEDIFF(SECOND, GETDATE(), GETUTCDATE())`), nunca
  hardcoded — corrige os 5 datasets que usam este leitor genérico, não só Fornecedores. Reteste real: mesmo
  INCREMENTAL após a correção detectou `rowsRead=101`, exatamente o número real alterado. Build/testes:
  1.380 unitários + 30 integração, 0 falhas, sem regressão.
- **DEFEITO REAL #2 — não-determinismo na aplicação de domínio sob RAW com linhas duplicadas (Incremental
  append-only):** após o Defeito #1 corrigido, o REFINED apply real de Fornecedores reportou reconciliação
  **reprovada** (vínculos ativos no domínio divergindo do esperado pelo RAW por exatamente 101 — depois por
  77, em execuções sucessivas idênticas). Causa raiz: `RAW_LinxFornecedoresSnapshot` sob Incremental é
  append-only (nunca trunca — só Full trunca); o mesmo `CodigoFornecedor` pode aparecer 2x (linha antiga +
  recém-anexada). `FornecedorRefinedProjector.Projetar` iterava TODAS as linhas do grupo sem escolher a mais
  recente por `CodigoFornecedor` — quem "vencia" dependia da ordem de retorno do banco (sem `ORDER BY`, não
  determinística), podendo reaplicar o estado ANTIGO por cima do novo. Agravante: as 2 colunas do watermark
  híbrido são independentes (COALESCE prioriza `CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA`) — uma mudança
  isolada em `FORNECEDORES` (como a deste teste) não altera `UltimaAlteracao`, gerando empate exato entre a
  linha antiga e a nova. Corrigido deduplicando `grupo` por `CodigoFornecedor` (maior `UltimaAlteracao`,
  desempate por maior `Id` — RAW só cresce sob Incremental, então Id mais alto = linha mais recente) antes do
  loop de decisão de vínculo; a mesma correção foi aplicada à reconciliação
  (`ProcessarFornecedoresRawParaDominioUseCase.ReconciliarAsync`), que tinha o mesmo problema isoladamente.
  Build/testes: 1.380 unitários + 30 integração, 0 falhas, sem regressão. **Severidade: real e potencialmente
  silenciosa em produção** — qualquer dataset que reutilize `SomaLinxDatasetBulkReader`/RAW append-only e seja
  reprocessado mais de uma vez sem um novo Full poderia sofrer o mesmo não-determinismo; apenas o caminho de
  Fornecedor foi corrigido nesta rodada (é o único com reconciliação formal hoje). **Recomendação registrada:**
  avaliar se os demais consumidores de RAW append-only (Cadastro de Apoio, Item Fiscal, Referências, Domínios)
  têm o mesmo risco — não investigado nesta rodada por escopo (nenhum deles reprocessou RAW duplicado durante
  esta bateria).
- **Teste controlado (~101 registros), design e execução real:** candidatos = 101 `CodigoErp` reais de
  `FornecedorLinxVinculos` (Grupo Soma, ativos, não-Principal, `DataParaTransferencia` mais antiga — menor
  risco de colidir com edição concorrente). Snapshot BEFORE completo capturado em SOMA_DESENV antes de
  qualquer escrita. Alteração: `UPDATE FORNECEDORES SET INATIVO=1` para os 101 `CLIFOR` (nenhuma alteração de
  schema/trigger/índice/procedure). Verificado: 101/101 afetados, trigger estampou `DATA_PARA_TRANSFERENCIA`
  automaticamente (confirma trigger `LXU_FORNECEDORES` ativa). Detecção via INCREMENTAL real (pós-Defeito #1
  corrigido): 101/101 detectados. Reconciliação real (pós-Defeito #2 corrigido): **aprovada**, exatamente os
  números originais (71.137 vínculos ativos, 76.411 vínculos totais, 0 divergências).
- **Interrupção de conectividade transitória (04/09/2026, durante a validação final):** a conexão com
  `MAISCOMPRAS Development`/`SRV-SOMA-DEV` ficou indisponível por alguns minutos no meio do reteste (TCP
  alcançável via `nc`, mas login SQL não completava — padrão de servidor DEV sob carga momentânea, não de VPN
  caída). Nenhuma ação destrutiva foi tentada durante a instabilidade; aguardada a normalização (monitor
  ativo) antes de qualquer nova escrita.
- **Restauração:** confirmada de ponta a ponta. SOMA_DESENV: 101/101 `CLIFOR` restaurados para `INATIVO=0`
  (idêntico ao snapshot BEFORE). Propagação real via Incremental (101/101 detectados novamente) + REFINED
  apply: reconciliação aprovada, números batendo exatamente com o baseline original. Domínio confirmado por
  leitura direta: 101/101 `FornecedorLinxVinculos` de volta a `InativoFornecedores=0`. Contagens finais
  idênticas às de antes de todo o teste: `Fornecedores` 73.449, `FornecedorLinxVinculos` 76.419 — zero perda,
  zero duplicação.
- **Idempotência:** REFINED apply reexecutado uma segunda vez sem nenhuma nova leitura do Linx — resultado
  byte-a-byte idêntico ao anterior (mesmos `ativosEsperados/ativosReaisNoDominio` 71.137,
  `principaisEsperados/ReaisNoDominio` 69.169, 0 divergências, mesma contagem de ocorrências persistidas
  nesta execução). Nenhuma duplicação de Fornecedor/vínculo; `IntegrationOccurrences` cresce por execução por
  desenho (auditoria por `ExecutionId`, não deduplicação entre execuções distintas) — comportamento correto,
  não um defeito.
- **Status:** Concluída. 2 defeitos reais encontrados, corrigidos e retestados; teste controlado completo
  (BEFORE → alteração → detecção → aplicação → restauração → detecção → aplicação → idempotência), tudo PASS.

---

### GAP — auditoria RAW determinística: todos os consumidores de RAW verificados e corrigidos (04/09/2026)

- **Origem:** Consequência direta do Defeito Real #2 encontrado e corrigido na bateria final de certificação
  B3 (04/09/2026) — ver seção acima. Auditoria dedicada solicitada pelo Product Owner após aprovação do B3,
  cobrindo TODOS os demais consumidores RAW→REFINED do B3.
- **Assunto:** O Defeito #2 (linhas duplicadas por chave, sem desempate explícito por "mais recente") foi
  auditado em todos os 5 processadores RAW→REFINED existentes no B3. Achado: **os outros 4 também estavam
  vulneráveis**, cada um de forma ligeiramente diferente — nenhuma correção foi copiada cegamente; cada uma
  respeita a chave e os timestamps reais do próprio dataset.
- **Tipo:** Técnico
- **Status:** **RESOLVIDO (04/09/2026).**
- **Resumo por consumidor:**
  1. **Fornecedor** (`FornecedorRefinedProjector`) — já corrigido na bateria anterior (ver Defeito Real #2
     acima). Chave: `CodigoFornecedor`. RAW duplicável: sim, Incremental (append-only). Critério: maior
     `UltimaAlteracao`, desempate por maior `Id`.
  2. **Cadastro de Apoio** (`CadastroApoioRefinedProjector` — Contas Contábeis, Centros de Custo, Filiais;
     Unidades de Medida é Full-only) — **VULNERÁVEL, corrigido.** Chave: `CodigoErp` (trim). RAW duplicável:
     sim, Incremental. Particularidade: o projetor já tratava colisão de códigos DISTINTOS que convergem
     após `Trim()` (dado sujo real, ex. `"1.000310   "` vs `"   1.000310"`) como ambiguidade — mas tratava
     IGUALMENTE uma duplicata EXATA do mesmo código (RAW append-only), quando na verdade essa categoria não é
     ambígua e tem "mais recente" claro. Corrigido deduplicando por código EXATO (maior `UltimaAlteracao`,
     desempate por maior `Id`) antes da checagem de ambiguidade por Trim — preserva a ambiguidade real,
     resolve a duplicata espúria. 4 testes novos (2 variações de ordem + desempate por Id + regressão da
     ambiguidade real).
  3. **Item Fiscal** (`ItemFiscalRefinedProjector`) — **VULNERÁVEL, corrigido.** Chave: `CodigoErp` (trim).
     RAW duplicável: sim, Incremental. Não tinha NENHUM agrupamento por chave (pior que Fornecedor: duplicata
     no caminho Insert colide com o índice único de `Codigo`, não só produz resultado não determinístico no
     Update). Corrigido com o mesmo critério (maior `UltimaAlteracao`, desempate por maior `Id`). 3 testes
     novos.
  4. **Fornecedor Domínios** (`FornecedorDominioErpRefinedProjector`) — **VULNERÁVEL, corrigido.** Chave:
     `(TipoDominio, CodigoErp)`. Dataset é Full apenas (RAW sempre truncado — "volume pequeno, sem
     necessidade de incremental"), então não acumula ENTRE execuções, mas a MESMA leitura pode conter 2
     linhas para a mesma chave (dado sujo na origem: `FORNECEDOR_TIPOS`/`FORNECEDOR_SUBTIPO`/
     `COND_ENT_PGTOS`). Corrigido com o mesmo critério (maior `UltimaAlteracao`, desempate por maior `Id`).
     3 testes novos.
  5. **Item Fiscal Referência Fornecedor** (`ItemFiscalReferenciaFornecedorRefinedProjector`) —
     **VULNERÁVEL, corrigido com critério DIFERENTE.** Chave: `(ItemFiscalId, FornecedorId)` e
     `(FornecedorId, CodigoItemFornecedor)`. Dataset é Full apenas (mesmo motivo do item 4, não acumula entre
     execuções), mas a MESMA leitura pode ter 2 linhas para a mesma chave lógica. **Esta tabela não tem
     nenhum campo de timestamp confiável** (`ADR-0024`, já documentado no próprio código) — por isso NÃO se
     aplicou "mais recente vence": inventar um critério sem dado real violaria a instrução explícita do PO
     ("se não houver informação suficiente, não invente regra"). Corrigido estendendo a MESMA filosofia já
     usada pelo resto do projetor (nunca escolhe arbitrariamente em ambiguidade — vira conflito, exige
     verificação manual) para também comparar decisões dentro do mesmo lote, não só contra o domínio
     pré-existente: qualquer chave com 2+ linhas resolvidas neste lote vira
     `ITEM_FISCAL_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA`/`CODIGO_ITEM_FORNECEDOR_DUPLICADO_NA_MESMA_LEITURA`,
     nenhuma das linhas envolvidas gera decisão (nunca "a primeira processada", que ainda seria arbitrário/
     dependente da ordem de enumeração do banco). 3 testes novos.
- **Build/testes:** 1.393 unitários (+13) + 30 integração, 0 falhas. `dotnet ef migrations
  has-pending-model-changes`: sem pendências (nenhuma mudança de modelo EF nesta auditoria — só
  `BlueprintOS.Application`/`BlueprintOS.Infrastructure`, camada de projeção pura).
- **Riscos residuais:** nenhum consumidor RAW→REFINED do B3 ficou sem auditoria — os 5 existentes
  (Fornecedor, Cadastro de Apoio, Item Fiscal, Fornecedor Domínios, Item Fiscal Referência Fornecedor) foram
  todos verificados e, onde vulneráveis, corrigidos. Não bloqueia o fechamento da Onda 2.
- **Decisão:** Confirmada pelo Product Owner (auditoria solicitada e concluída em 04/09/2026).

---

### GAP — `ConfiguracaoErp` (O1.11) ainda não é consumida pelos leitores ERP reais

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026), investigação read-only da
  fundação Multi-BU existente. **Decisão do Product Owner registrada em 03/09/2026: a Business Unit
  determina sua `ConfiguracaoErp`; Grupo Soma/Linx não deve ser hardcoded como arquitetura global; os
  Soma*Readers atuais que ignoram `ConfiguracaoErp` são dívida de integração multi-BU, a evoluir a estrutura
  existente — nunca criar arquitetura paralela.**
- **Assunto:** O cadastro administrativo `ConfiguracaoErp` (Domain/Identity, O1.11) existe, é 1:1 com
  `UnidadeNegocio`, tem segredo cifrado por `IDataProtector` com propósito dedicado e API administrativa
  completa (`ConfiguracaoErpController`) — mas nenhum leitor ERP real (`SomaFornecedorReader`,
  `SomaItemFiscalReader`, `SomaFilialReader` etc.) o consome hoje. Esses leitores resolvem sua própria
  conexão por outro mecanismo (fora do escopo desta investigação), não por `ConfiguracaoErp`.
- **Tipo:** Arquitetura / Integração
- **Tratar em:** Próxima onda ou Onda específica de Multi-ERP real (só passa a ser urgente quando existir um
  segundo ERP/BU real a configurar) — não implementada por esta rodada (leitores ERP reais estão sob
  reescrita ativa pelo Bloco 5A nesta mesma data).
- **Status:** Decidido — dívida técnica reconhecida, sem prazo de implementação atrelado ao fechamento da
  Onda 2 (não é bloqueante enquanto só existe Grupo Soma/Linx)
- **Resumo:** É o gap central que impede a "Configuração ERP administrável pelo Admin Master" (seção 4 da
  rodada) de ser mais do que um cadastro — hoje é fundação correta, mas sem efeito real sobre qual banco/ERP
  um pipeline de fato lê. Reaproveitar (não recriar) `ConfiguracaoErp`/`ISegredoProtector` quando essa
  integração for feita. Ver `MultiBU-MultiErp-Arquitetura.md` para detalhe.
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026 — evoluir a estrutura existente, nunca criar uma
  segunda arquitetura de configuração ERP.

---

### GAP — padronização dos 4 metadados de cadastro de apoio por `UnidadeNegocioId + CodigoErp`

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026), classificação BU-aware das
  entidades administrativas. **Decisão do Product Owner registrada em 03/09/2026.**
- **Assunto:** Os quatro cadastros de apoio (Conta Contábil, Unidade de Medida, Centro de Custo, Filial)
  passam a ter identidade local `UnidadeNegocioId + CodigoErp`. `FilialMetadado` já está mais próxima desse
  desenho (já tem índice composto). `ContaContabilMetadado`, `UnidadeMedidaMetadado` e `CentroCustoMetadado`
  hoje têm índice único **global** por `CodigoErp` e deixam de depender dessa unicidade global — o mesmo
  código ERP pode existir em BUs diferentes sem compartilhar metadado (ex.: `Grupo Soma/código 001` e
  `Reserva/código 001` são contextos independentes).
- **Tipo:** Arquitetura
- **Tratar em:** Nesta onda.
- **Status:** Implementado (03/09/2026).
- **Resumo:** Preservado o comportamento lazy já homologado: ausência de metadado local continua **BY
  DESIGN** (nenhuma mudança na regra "nunca criar em massa"). Implementado: os 3 índices únicos globais
  (`ContaContabilMetadado`, `UnidadeMedidaMetadado`, `CentroCustoMetadado`) migrados para
  `(UnidadeNegocioId, CodigoErp)`, alinhados a `FilialMetadado` — migration `NormalizarMetadadosApoioPorUnidadeNegocioOnda2`
  (drop/create de índice puro, sem backfill necessário: a coluna `UnidadeNegocioId` já existia e já estava
  populada nos 3). O achado real desta implementação: `ICentroCustoMetadadoRepository.ObterPorCodigoErpGlobalAsync`
  (usada por `CentroCustoVinculoValidator` — dívida O1.6-L2 — e por `CentroCustoMetadadoResolver`/
  `AtualizarMetadadoCentroCustoUseCase`) dependia da unicidade global para detectar/rejeitar vínculo
  cross-BU; removida do contrato e das 3 chamadas — agora cada Unidade de Negócio ancora seu próprio
  metadado independentemente, sem rejeitar nem depender do metadado de outra BU (exatamente a regra desta
  decisão). 6 testes atualizados/substituídos para refletir o novo comportamento (contextos independentes em
  vez de rejeição cross-BU) em `CentroCustoVinculoValidatorTests`, `FilialCentroCustoUseCasesTests` (2
  testes) e `CentroCustoUnidadeAlocacaoUseCasesTests`. Nenhum comportamento de `ContaContabilMetadado`/
  `UnidadeMedidaMetadado` precisou de ajuste além do índice (não tinham consulta "global" equivalente).
  Backend: 1376 unitários + 26 integração, 0 falhas; `dotnet ef migrations has-pending-model-changes`: sem
  pendências.
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026 — direção escolhida foi alinhar os 3 aos moldes
  de `FilialMetadado`, não o inverso.

---

### `Usuario.Email` permanece único globalmente — confirmado, com evolução conceitual futura

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026), classificação BU-aware.
  **Decisão do Product Owner registrada em 03/09/2026: confirma o desenho atual e formaliza o modelo
  conceitual de evolução futura.**
- **Assunto:** `Usuario.Email` continua único **globalmente** — a identidade da pessoa é única no +Compras,
  não por BU. Modelo conceitual formalizado para evolução futura (não implementado nesta rodada): Usuário
  global → autorizações para N Business Units → uma Business Unit ativa na sessão → dados integralmente
  isolados pela Business Unit ativa. Nunca duplicar usuário só porque ele acessa duas BUs; a
  sessão/contexto deve impedir vazamento entre BUs.
- **Tipo:** Funcional / Arquitetura
- **Tratar em:** Próxima onda ou Encerramento do Projeto — o modelo hoje é single-BU-por-usuário
  (`Usuario.UnidadeNegocioId` único, claim de sessão única); a extensão para N autorizações de BU por
  usuário só se torna necessária com uma segunda BU real e usuários reais compartilhados entre BUs. Não é
  um bloqueio para o fechamento da Onda 2.
- **Status:** Decidido
- **Resumo:** Não é um gap nem uma correção — é a confirmação explícita de uma decisão de produto já tomada
  e testada (`Usuario.Email` global), junto com o registro do modelo conceitual que evoluirá isso no
  futuro, para que a evolução não seja confundida com "duplicar Usuário por BU" quando chegar a hora.
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026.

---

### Bloco 5B (+Compras → Linx) não bloqueia o fechamento funcional da Onda 2

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026). Decisão do Product Owner.
- **Assunto:** Bloco 5B permanece bloqueado até validação específica com especialista Visual Linx, mesma
  decisão anterior do Product Owner (mesmo padrão já aplicado à B2.9/Adapter Linx). Não deve ser iniciado
  agora apenas para fechar a Onda 2.
- **Tipo:** Governança
- **Tratar em:** Onda específica (quando a validação com o especialista Visual Linx estiver agendada) —
  não bloqueia o Gate B3 nem o fechamento funcional atual da Onda 2.
- **Status:** Decidido
- **Resumo:** Confirma que o caminho crítico da Onda 2 passa por: Bloco 5A estabilizar → normalização
  Multi-BU (GAPs acima) → Bloco 6 (Gate técnico/homologação final da B3) → bateria final da Onda 2 —
  **sem** depender de 5B.
- **Decisão:** Confirmada pelo Product Owner em 03/09/2026.

---

### Classificação Multi-BU das entidades administrativas (fora do escopo do Bloco 5A)

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Classificação (1–5, ver `.ai/CLAUDE.md`/prompt desta rodada) de entidades administrativas que
  não fazem parte do trabalho paralelo do Bloco 5A: `ContaContabilMetadado`, `UnidadeMedidaMetadado`,
  `CentroCustoMetadado`, `FilialMetadado`, RBAC (`Perfil`/`Permissao`/`UsuarioPerfil`), `Usuario`/
  `UsuarioCentroCusto`, Regras Orçamentárias, Alçadas, Workflow, sessão/OTP, Identity Providers, Configuração
  de Notificações.
- **Tipo:** Arquitetura
- **Tratar em:** Nesta onda (itens classificados como "1 — deve ser BU-aware agora" foram corrigidos nesta
  rodada, quando seguros e sem colisão com o trabalho paralelo; os demais foram apenas classificados).
- **Status:** Implementado (classificação concluída; nenhuma alteração de código necessária além dos dois
  GAPs já registrados acima — `FilialMetadado` e `Usuario.Email`)
- **Resumo:** Auditoria read-only (03/09/2026) de 12 entidades/grupos administrativos, taxonomia 1–5:

  | Entidade | Classificação | Evidência resumida |
  |---|---|---|
  | ContaContabilMetadado | 3 — global legítimo | `UnidadeNegocioId` presente; índice único global por `CodigoErp`, documentado (cadastro de apoio compartilhado) |
  | UnidadeMedidaMetadado | 3 — global legítimo | idem |
  | CentroCustoMetadado | 3 — global legítimo | idem; índice global usado ativamente como âncora anti-cross-BU do vínculo Usuário×Centro de Custo |
  | FilialMetadado | **4 — precisa decisão do PO** | único dos 4 metadados com índice composto por BU, sem justificativa documentada (GAP dedicado acima) |
  | Perfil/Permissao/PerfilPermissao/UsuarioPerfil | 2 — já BU-aware | `Perfil` com índice composto `(UnidadeNegocioId, Nome)`; `Permissao` catálogo global correto (ADR-0020); junções herdam BU |
  | Usuario/UsuarioCentroCusto | 2 (BU-anchor) / 3 (Email global, decisão documentada) | `UnidadeNegocioId` presente em `Usuario`; `UsuarioCentroCusto` valida BU indiretamente via `CentroCustoMetadado` |
  | RegrasOrcamentarias | 2 — já BU-aware | `UnidadeNegocioId` no domínio + índice + controller com path/`EscopoUnidadeNegocioPathFilter` |
  | Alçadas (AlcadaAprovacao) | 2 — já BU-aware | idem |
  | Workflow (RegraWorkflow) | 2 — já BU-aware | idem |
  | CodigoVerificacaoOtp | 3 — global legítimo | mecanismo de autenticação de pessoa, não de BU |
  | SessaoAutenticacao | 2 — já BU-aware | `UnidadeNegocioId` herdado do usuário, usado nas claims de autorização |
  | IdentityProvider | 2 — já BU-aware | confirma padrão de `PROJECT_STATE.md` (path + `EscopoUnidadeNegocioPathFilter`) |
  | ConfiguracaoNotificacoes | 2 — já BU-aware | índice único 1:1 por BU |

  Achados adicionais fora da lista original (mesmo padrão de risco): `FeatureFlag.Nome` único global —
  legítimo (catálogo de produto, N:N por BU via `FeatureFlagUnidadeNegocio`); `Parametro` tem dois índices
  únicos coexistindo (`(Chave, UnidadeNegocioId)` e um também global só por `Chave`) — vale confirmar se
  ambos são intencionais numa revisão futura, não investigado a fundo nesta rodada.

  **Nenhuma entidade desta lista se enquadrou em classificação 1 (crítico, falta BU-awareness).** A fundação
  administrativa da Onda 1 (O1.11/ADR-0022) já cobre corretamente quase todo o eixo administrativo — os
  gaps reais desta rodada estão concentrados no eixo de dados operacionais/integrados (Fornecedor, Item
  Fiscal, RAW/REFINED, datasets), registrados nos GAPs acima.
