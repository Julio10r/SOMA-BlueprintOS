# Contrato Funcional Preliminar — B3 (Item Fiscal / Materiais / Serviços)

## Status deste documento

**Preliminar, em construção, NÃO implementável ainda.** Consolida decisões funcionais explícitas do Product Owner tomadas durante o discovery da B3, cruzadas com evidência de código Linx (`docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md` e o discovery da tela `100126CS2`) e com o schema discovery em andamento no `SOMA_DESENV`. Este documento não substitui o discovery bruto nem antecipa o contrato funcional final exigido por `applications/mais-compras/docs/architecture/CadFormFactory.md` §3 — é o registro vivo do que já foi decidido/comprovado enquanto o discovery continua.

Cada item abaixo é rotulado:

- **DECISÃO DO PRODUCT OWNER** — decisão de produto, não deriva de evidência técnica.
- **EVIDÊNCIA DO ERP** — comprovado por leitura de código Visual Linx (SCX/SCT/PRG).
- **EVIDÊNCIA DO BANCO** — comprovado por `INFORMATION_SCHEMA` real via `LinxDatabaseSpecialist`/`ILinxSchemaDiscoveryReader`.
- **INFERÊNCIA** — interpretação ainda não confirmada.
- **GAP** — desconhecido explícito, não resolvido por suposição.
- **DEPENDÊNCIA FUNCIONAL FUTURA** — fora do escopo da B3, registrado para não ser esquecido.

## 1. Conceito de Item Fiscal — granularidade

**CORREÇÃO (2026-09-01, substitui a versão anterior deste documento)**: ~~"Item Fiscal deve ser genérico por natureza da compra"~~ foi **revogado**. A redação anterior presumia incorretamente que o +Compras deveria impor um nível de detalhamento (genérico) ao cadastro.

**DECISÃO DO PRODUCT OWNER (correta, 2026-09-01)**: a granularidade do Item Fiscal **é decisão de negócio da área de Compras**, não do +Compras enquanto sistema. O +Compras deve suportar qualquer nível de detalhamento que a área de Compras escolher operar — de um único item genérico ("Notebook") até itens específicos por marca/modelo ("MacBook Pro 14", "Dell Latitude 5450") — sem impor nem obrigar um nível específico.

Regras derivadas (todas DECISÃO DO PRODUCT OWNER):
- O +Compras **não deve** obrigar item genérico.
- O +Compras **não deve** obrigar item específico.
- O +Compras **não deve** obrigar marca/modelo.
- O +Compras **não deve** criar artificialmente um conceito de SKU.
- O +Compras **não deve** consolidar automaticamente itens distintos.
- O +Compras **não deve** fragmentar automaticamente um item em vários cadastros.
- A única restrição admitida é a que vier de **regra real do Linx comprovada em código/banco** — nunca de convenção observada nos dados atuais tratada como regra obrigatória sem evidência.

Essa decisão é **independente** de `ITEM_FISCAL_REF_FORNECEDOR` (seção 5): qualquer que seja a granularidade escolhida, o mecanismo de DE/PARA por fornecedor continua válido (um Item Fiscal — genérico ou específico — pode ter N referências de fornecedor). Também é independente de Conta Contábil/orçamento (seção 2/4): granularidade do catálogo e classificação contábil são responsabilidades distintas — cada Item Fiscal, qualquer que seja seu nível de detalhe, respeita sua própria configuração de Conta Contábil.

**Evidência Linx já verificada, sem limitação encontrada até agora**: `CADASTRO_ITEM_FISCAL.ITEM_DESCRICAO` é texto livre (`C(80)`), sem validação de unicidade por descrição nem exigência de vínculo com um SKU/modelo comercial específico (evidência: `docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md`, achado 1). Isso é compatível com qualquer granularidade — não é evidência a favor nem contra um nível específico. Se o schema discovery em andamento encontrar alguma **regra Linx comprovada** que limite essa liberdade, será registrado aqui como **REGRA LINX COMPROVADA** com a evidência.

## 2. Conta Contábil do Item Fiscal

**DECISÃO DO PRODUCT OWNER (2026-09-01)**: `CONTA_CONTABIL` do Item Fiscal deixa de ser tratada apenas como dado fiscal/contábil de uso posterior na Entrada Fiscal — passa a ser **atributo funcional relevante para o +Compras**, porque participa da validação orçamentária (seção 4).

**DECISÃO DO PRODUCT OWNER — obrigatoriedade (fecha a pergunta anteriormente em aberto, 2026-09-01)**: **`CONTA_CONTABIL` é obrigatória para todo Item Fiscal cadastrado/utilizável no +Compras**, mesmo que `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL` seja `NULLABLE` no banco Linx e a tela `100126CS2` permita salvar sem ela (evidência técnica, seção anterior — nunca revogada, apenas superada por regra de negócio mais restritiva). **LINX permite Item Fiscal sem Conta Contábil; +COMPRAS exige Conta Contábil.** Esta é regra de negócio do +Compras, não inferência nem GAP.

**DECISÃO DO PRODUCT OWNER — origem/importação**: Conta Contábil é cadastro de apoio **originado do Linx** (`CTB_CONTA_PLANO`, com propriedades em `PROP_CTB_CONTA_PLANO` — já observada na tela `100126CS2`, propriedade `00924` usada na regra de imobilizado). O +Compras **não cria Conta Contábil própria** — mantém um cadastro de apoio local alimentado por importação/sincronização a partir de `CTB_CONTA_PLANO`, e o usuário seleciona uma conta válida ao cadastrar o Item Fiscal (nunca digitação livre). Fluxo conceitual: `LINX CTB_CONTA_PLANO → importação/sincronização → +Compras (cadastro de apoio) → seleção no cadastro do Item Fiscal`.

**DECISÃO DO PRODUCT OWNER — herança**: Compras e Entradas **herdam** a Conta Contábil cadastrada no Item Fiscal — ao utilizar o Item Fiscal numa compra, `ITEM FISCAL.CONTA_CONTABIL → item da compra`; ao registrar a entrada correspondente, `ITEM FISCAL.CONTA_CONTABIL → item da entrada`. Essa regra de negócio **explica funcionalmente** (não ainda tecnicamente) por que existem colunas físicas próprias de `CONTA_CONTABIL` em `COMPRAS_CONSUMIVEL` e `ENTRADAS_ITEM` (evidência do banco, abaixo). **Não concluir ainda como o Linx realiza essa herança tecnicamente** — o mecanismo exato (incluindo o papel de `FX_CTB_BUSCA_CONTA_ITEM`, já identificado em `005109GS3`) é **ponto de discovery futuro**, a investigar quando o fluxo de Requisição/Pedido/Orçamento for estudado — não nesta etapa.

**GAP/REGRA A DEFINIR — alteração da conta do item**: se a Conta Contábil de um Item Fiscal já usado for alterada, o efeito sobre compras/entradas já existentes vs. novas compras/entradas **não está definido**. Não presumir atualização retroativa. Fica como regra a definir, não como fato.

**EVIDÊNCIA DO BANCO (comprovado nesta sessão via `ILinxSchemaDiscoveryReader`, `SOMA_DESENV`)**: `CTB_CONTA_PLANO` existe (38 colunas), com `CONTA_CONTABIL`/`DESC_CONTA` `NOT NULL` e `INATIVA bit NOT NULL` (status ativo/inativo real). `PROP_CTB_CONTA_PLANO` existe (5 colunas), estrutura compatível com o uso já visto na tela `100126CS2` para a propriedade `00924`. `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL` é `NULLABLE` no banco e a tela não a obriga — **evidência Linx, não contradita pela decisão de negócio acima, que é deliberadamente mais restritiva**. Detalhe completo em `docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md`, seção "Schema Discovery real".

**Achado incidental, registrado como ponto de discovery futuro (orçamento — fora do escopo da B3, não investigar agora)**: `CTB_CONTA_PLANO` já possui `INDICA_CTRL_ORCAMENTO`/`ANM_IND_ORCAMENTO`/`PREVISAO_DESPESA`/`RATEIO_CENTRO_CUSTO`. A diferença entre `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL`, `COMPRAS_CONSUMIVEL.CONTA_CONTABIL`, `ENTRADAS_ITEM.CONTA_CONTABIL` e `FX_CTB_BUSCA_CONTA_ITEM` **não será aprofundada agora** — fica registrada para quando estudarmos o fluxo de Requisição/Pedido/Orçamento.

**GAP remanescente (técnico, não de negócio)**: se `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL → CTB_CONTA_PLANO.CONTA_CONTABIL` é FK física ou apenas relação lógica (a ferramenta de schema discovery atual não expõe constraints/FK, só colunas); regras de elegibilidade de conta para uso em Item Fiscal; comportamento quando a conta é inativada. Ver seção 9.

## 3. Centro de Custo

**DECISÃO DO PRODUCT OWNER**: reaproveitar o modelo de Centro de Custo já existente no +Compras/Linx (Onda 1 já implementou Centro de Custo × Unidade de Alocação — `.ai/work-orders/completed/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md`). Nenhuma nova regra de Centro de Custo é criada por este contrato.

## 4. Validação orçamentária (binômio Centro de Custo × Conta Contábil) — DEPENDÊNCIA FUNCIONAL FUTURA, NÃO implementada na B3

**DECISÃO DO PRODUCT OWNER (2026-09-01, com ajuste na mesma data)**: o momento da validação de orçamento é quando o usuário, no fluxo de Requisição/Pedido (fora da B3):

1. seleciona o **Item Fiscal** que deseja comprar;
2. informa o **valor** da compra.

Fluxo conceitual (não implementado):

```
USUÁRIO SELECIONA ITEM FISCAL
        ↓
ITEM FISCAL.CONTA_CONTABIL + CENTRO DE CUSTO da requisição/compra
        ↓
BINÔMIO ORÇAMENTÁRIO (CENTRO DE CUSTO × CONTA CONTÁBIL)
        ↓
CONSULTA ORÇAMENTO DISPONÍVEL
        ↓
COMPARA COM O VALOR INFORMADO
        ↓
RESULTADO DA VALIDAÇÃO ORÇAMENTÁRIA
```

**O que a B3 precisa garantir** (dentro do escopo desta etapa): o contrato do Item Fiscal deve fornecer corretamente a `CONTA_CONTABIL` necessária para essa validação futura — nada além disso.

**Explicitamente fora da B3**: motor de orçamento, onde o orçamento é armazenado, representação do binômio, período/exercício, saldo disponível, realizado/comprometido, regras de bloqueio. Registrado como GAP a não investigar agora, a menos que apareça incidentalmente durante o discovery do Item Fiscal/Conta Contábil.

**Não presumir**: que todo binômio Centro de Custo × Conta Contábil possui orçamento válido — isso é uma verificação real a ser feita pelo motor de orçamento futuro, não uma garantia estrutural.

## 5. `ITEM_FISCAL_REF_FORNECEDOR` — DE/PARA fornecedor → Item Fiscal

**DECISÃO DO PRODUCT OWNER (2026-09-01)**: a finalidade da tabela no +Compras é suportar o DE/PARA entre a referência/código que o fornecedor usa para um item e o `CADASTRO_ITEM_FISCAL` interno correspondente. Cardinalidade de negócio: **1 Item Fiscal → N referências de fornecedor** (ex.: "Mouse sem fio" pode ter uma referência na Amazon e outra na Apple). Cada relacionamento deve identificar, no mínimo: fornecedor, item fiscal, referência/código do fornecedor.

**Uso futuro explícito**: processamento de XML de NF-e e, quando aplicável, NFS-e — o XML pode trazer o código do fornecedor, e o +Compras precisa resolver para o `CADASTRO_ITEM_FISCAL` interno correspondente. **Não investigar agora** o processamento de XML em si, exceto quando necessário para validar alguma estrutura da B3.

**Importante (correção de rota registrada nesta rodada)**: o discovery anterior (`docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md`, achado 6) comprovou que `004005GS` e `005109GS3` **não usam** `ITEM_FISCAL_REF_FORNECEDOR` diretamente — isso não invalida o uso futuro decidido acima; apenas significa que essas duas telas não são a evidência dessa finalidade. A finalidade é decisão de produto, não achado de tela.

**EVIDÊNCIA DO BANCO (comprovado)**: `ITEM_FISCAL_REF_FORNECEDOR` existe, com exatamente 3 colunas — `FORNECEDOR`, `CODIGO_ITEM`, `CODIGO_ITEM_FORNECEDOR`, todas `varchar NOT NULL` — consistente com a chave `FORNECEDOR, CODIGO_ITEM` já vista na tela `100126CS2`. **GAP remanescente**: PK real não comprovada (ferramenta atual não expõe constraints), e como `FORNECEDOR` se relaciona exatamente com o cadastro de Fornecedor (mesmo domínio de `CADASTRO_CLI_FOR.NOME_CLIFOR`? não confirmado).

**EVIDÊNCIA técnica comprovada (VFP + banco)**: `ITEM_FISCAL_REF_FORNECEDOR` não tem coluna `DATA_PARA_TRANSFERENCIA` nem trigger própria, e o fluxo VFP da tela `100126CS2` (inclusão/edição/exclusão da referência) não altera o Item Fiscal pai — não existe mecanismo confiável de Last Write Wins para esta tabela (`docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md`, seções dedicadas).

**DECISÃO DO PRODUCT OWNER — resolução de conflito**: por não haver timestamp confiável, **não inventar mecanismo artificial de Last Write Wins** para `ITEM_FISCAL_REF_FORNECEDOR`. Em caso de divergência entre Linx e +Compras nas referências, aplica-se `ADR-0024` (`.ai/DECISIONS.md`) — **Linx prevalece** como fonte mandatória de resolução. Isso não impede o usuário do +Compras de cadastrar/manter referências normalmente; define apenas o desempate quando os dois lados divergem sem outra regra determinística.

**DECISÃO DO PRODUCT OWNER — unicidade de `ItemFiscal.Codigo` (homologação do Bloco 3, 2026-09-02)**: `ItemFiscal.Codigo` é único **GLOBALMENTE** no +Compras (não segmentado por Unidade de Negócio, marca ou outro contexto local) — objetivo é preservar correspondência inequívoca com `LINX.CADASTRO_ITEM_FISCAL.CODIGO_ITEM`; não deve existir mais de um Item Fiscal no +Compras representando o mesmo `CODIGO_ITEM` do Linx. Implementado como índice único global (`IX_ItensFiscais_Codigo`) — na data desta homologação (2026-09-02), mesma decisão então vigente para `Fornecedor.DocumentoFiscal` (`ADR-0023`). **Nota de atualização (04/09/2026, encerramento da Onda 2 — não reabre esta decisão de Item Fiscal):** a rodada arquitetural Multi-BU/Multi-ERP da Onda 2 normalizou `Fornecedor`/`FornecedorLinxVinculo` para unicidade composta por Unidade de Negócio (`UnidadeNegocioId + CnpjCpf`, não mais global) — ver `applications/mais-compras/docs/cadernos/Onda-2.md`, GAP "Fornecedor/CNPJ: unicidade global vs. fronteira de BU". A analogia com `Fornecedor.DocumentoFiscal` acima ficou desatualizada por esse motivo; `ItemFiscal.Codigo` permanece global por decisão de produto independente, não afetada por essa mudança.

**DECISÃO DO PRODUCT OWNER — unicidade de `(FornecedorId, CodigoItemFornecedor)` (autorização do Bloco 4, 2026-09-02)**: além da unicidade **comprovada** em Linx (`ITEM_FISCAL_REF_FORNECEDOR.KeyFieldList = FORNECEDOR, CODIGO_ITEM` — um fornecedor tem no máximo uma referência por Item Fiscal), o +Compras aplica uma segunda unicidade **não comprovada em Linx, explicitamente autorizada pelo Product Owner**: `(FornecedorId, CodigoItemFornecedor)` é único **GLOBALMENTE** — o mesmo fornecedor não pode usar o mesmo código para dois Itens Fiscais diferentes. Objetivo: garantir que o futuro DE/PARA reverso (Fornecedor + código usado pelo fornecedor → Item Fiscal, necessário para o processamento de XML NF-e/NFS-e) sempre resolva para um único Item Fiscal. Implementado como índice único global (`IX_ItensFiscaisReferenciasFornecedor_FornecedorId_CodigoItemFornecedor`). **Risco assumido conscientemente**: se o Linx tiver hoje uma duplicidade real nesse sentido (não comprovada, mas não descartada), uma sincronização futura (Bloco 5A) pode precisar resolver o conflito manualmente antes de importar.

## 6. Sincronização Linx → +Compras (inativação) e autoridade de criação/edição

**DECISÃO DO PRODUCT OWNER — inativação**: mesma regra de autoridade já adotada em Fornecedores (`CadFormFactory.md`, exemplo didático de assimetria) — **Linx inativa Item → +Compras deve inativar o item correspondente** (autoridade do Linx, propagação Linx→+Compras). **+Compras inativa Item localmente → NÃO propaga para o Linx** (inativação local permanece local). Coerente com `ADR-0024`. Regra registrada no contrato; **não implementar ainda**.

**DECISÃO DO PRODUCT OWNER — autoridade de criação/edição (fecha o GAP bloqueante anterior, 2026-09-01)**: `CADASTRO_ITEM_FISCAL` tem **autoridade operacional compartilhada** entre Linx e +Compras — ambos podem cadastrar/editar/inativar. Resolução de conflito em duas camadas:

1. **Quando houver timestamps comparáveis**: prevalece a alteração mais recente, usando `DATA_PARA_TRANSFERENCIA` como referência (mecanismo comprovado: `DEFAULT GETDATE()` + trigger `LXUDT_CADASTRO_ITEM_FISCAL`, incondicional em qualquer UPDATE — ver seção evidência técnica abaixo).
2. **Quando surgir situação ambígua que o Last Write Wins não resolva com segurança**: aplica-se `ADR-0024` — **Linx prevalece**.

**EVIDÊNCIA técnica que sustenta esta decisão**: `docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md` (seção `DATA_PARA_TRANSFERENCIA`) comprova o mecanismo exato via `sys.triggers`/`OBJECT_DEFINITION` (leitura read-only): `DEFAULT GETDATE()` na inclusão + trigger `LXUDT_CADASTRO_ITEM_FISCAL` forçando `GETDATE()` em qualquer UPDATE, salvo se o próprio UPDATE já tiver setado a coluna (caveat de implementação: um futuro Adapter +Compras nunca deve setar `DATA_PARA_TRANSFERENCIA` explicitamente).

## 7. Permissões / parâmetros do usuário

**DECISÃO DO PRODUCT OWNER**: cadastrar, editar e inativar Item Fiscal continuam dependendo de permissões/parâmetros do usuário — não presumir que todo usuário com acesso ao cadastro pode executar todas as três operações. O contrato funcional final deverá prever controle separado por ação, seguindo o modelo de autorização já existente no +Compras (RBAC). **Não implementar RBAC agora** — apenas a necessidade funcional está registrada.

## 8. Matriz de autoridade preliminar

| Informação | Autoridade | Observação |
|---|---|---|
| Item Fiscal — criação/edição | **COMPARTILHADA** (Linx + +Compras) | LWW via `DATA_PARA_TRANSFERENCIA` (comprovado); ambíguo → Linx prevalece (`ADR-0024`) — GAP bloqueante anterior FECHADO (DECISÃO DO PO) |
| Item Fiscal — inativação vinda do Linx | **LINX** | propaga para +Compras (DECISÃO DO PO, coerente com `ADR-0024`) |
| Item Fiscal — inativação local no +Compras | **+COMPRAS** (local) | NÃO propaga para o Linx (DECISÃO DO PO) |
| Cadastrar/editar/inativar Item Fiscal no +Compras | permissão/parâmetro do usuário | necessidade registrada, RBAC não implementado (DECISÃO DO PO) |
| Referência do item por fornecedor (`ITEM_FISCAL_REF_FORNECEDOR`) | +Compras consome, Linx é a fonte estrutural | sem timestamp confiável comprovado; divergência → Linx prevalece (`ADR-0024`); finalidade DE/PARA para XML NF-e/NFS-e futuro (DECISÃO DO PO) |
| Resolução de conflito/ambiguidade sem regra específica (qualquer campo) | **LINX** (fallback) | `ADR-0024` — princípio transversal Linx↔+Compras, não exclusivo da B3 |
| Granularidade do Item Fiscal | livre, decisão da área de Compras (genérico ou específico) | +Compras não impõe nível (DECISÃO DO PO, corrigida em 2026-09-01); só limita se houver regra Linx comprovada |
| Conta Contábil | **LINX** (cadastro de apoio, `CTB_CONTA_PLANO`) | +Compras não cria conta própria; importa/sincroniza e oferece seleção (DECISÃO DO PO) |
| Obrigatoriedade de Conta Contábil no Item Fiscal | **+COMPRAS** exige, mesmo Linx permitindo nula | regra de negócio mais restritiva que o Linx (DECISÃO DO PO, fecha pergunta anterior) |
| Conta Contábil em Compras/Entradas | herdada do Item Fiscal | mecanismo técnico exato = ponto de discovery futuro (DECISÃO DO PO) |
| Centro de Custo | modelo já existente +Compras/Linx | nenhuma regra nova |
| Orçamento (binômio CC × Conta Contábil) | fora da B3 | dependência funcional futura (Requisição/Pedido) |

## 9. O que ainda precisa ser comprovado tecnicamente (GAPs abertos)

Resolvidos nesta rodada (`docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md`, seção "Schema Discovery real"): existência e estrutura de coluna de `CADASTRO_ITEM_FISCAL`, `ITEM_FISCAL_REF_FORNECEDOR`, `MATERIAIS`, `PRODUTOS`, `COMPRAS`, `COMPRAS_CONSUMIVEL`, `ENTRADAS`, `ENTRADAS_ITEM`, `CTB_CONTA_PLANO`, `PROP_CTB_CONTA_PLANO`; obrigatoriedade real (nullability) de `CONTA_CONTABIL` em `CADASTRO_ITEM_FISCAL` (nullable, não obrigatória); status ativo/inativo real de `CTB_CONTA_PLANO` (`INATIVA`) e de `CADASTRO_ITEM_FISCAL`/`MATERIAIS`/`PRODUTOS` (`INATIVO`).

Ainda em aberto (técnico, não de negócio — obrigatoriedade de Conta Contábil já foi decidida, seção 2):
- FK física vs. relação lógica entre `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL` e `CTB_CONTA_PLANO.CONTA_CONTABIL` (ferramenta atual não expõe constraints/FK/índices — limitação da ferramenta, não do Linx).
- PK real de `ITEM_FISCAL_REF_FORNECEDOR` (mesma limitação de ferramenta).
- Regras de conta elegível para uso em Item Fiscal, comportamento quando a conta é inativada.
- Efeito da alteração da Conta Contábil de um Item Fiscal já usado sobre compras/entradas existentes vs. novas (regra a definir, sem presunção de retroatividade — seção 2).
- Cadastro mestre de Serviço (nenhuma tela de Serviço analisada ainda).

**Pontos de discovery futuro** (registrados, não investigar agora — fora do escopo da B3 conforme instrução do Product Owner):
- Mecanismo técnico exato de herança de `CONTA_CONTABIL` do Item Fiscal para `COMPRAS_CONSUMIVEL`/`ENTRADAS_ITEM`, incluindo o papel de `FX_CTB_BUSCA_CONTA_ITEM` — a investigar junto ao fluxo de Requisição/Pedido/Orçamento.
- Validação orçamentária real (binômio Centro de Custo × Conta Contábil) — seção 4.

## 10. Explicitamente fora da B3

Motor de orçamento, Requisição, Pedido, RBAC real, sincronização/inativação implementada, processamento de XML NF-e/NFS-e, cadastro próprio de Conta Contábil no +Compras.
