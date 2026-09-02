# B3 — Consolidação Final do Discovery (Item Fiscal / Cadastro e Integração de Itens)

## Status

**Discovery orientado por telas ENCERRADO** (2026-09-01, por decisão do Product Owner — não há mais telas a indicar nesta etapa). Este documento consolida `ContratoFuncionalPreliminar-B3-ItemFiscal.md`, `agents/knowledge/linx-item-fiscal-cadastro/` e `docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md` numa recomendação única de escopo/GO-NO-GO. **Ainda sem implementação.**

Telas investigadas: `100126CS2` (Cadastro de Item Fiscal), `004005GS` (Pedido de Consumíveis), `005109GS3` (Entrada de Nota Fiscal de Consumíveis). Tabelas comprovadas em `SOMA_DESENV`: `CADASTRO_ITEM_FISCAL`, `ITEM_FISCAL_REF_FORNECEDOR`, `MATERIAIS`, `PRODUTOS`, `COMPRAS`, `COMPRAS_CONSUMIVEL`, `ENTRADAS`, `ENTRADAS_ITEM`, `CTB_CONTA_PLANO`, `PROP_CTB_CONTA_PLANO`.

---

## 1. Modelo funcional recomendado

Um único cadastro no +Compras — **Item Fiscal** — espelhando o conceito real e comprovado de `CADASTRO_ITEM_FISCAL` no Linx:

- **Código**: identificador do item.
- **Descrição**: texto livre, granularidade decidida pela área de Compras (genérico ou específico — DECISÃO DO PO).
- **Unidade**: selecionada de cadastro de apoio (Linx `UNIDADES`).
- **Conta Contábil**: selecionada de cadastro de apoio (Linx `CTB_CONTA_PLANO`), **obrigatória no +Compras** mesmo sendo opcional no Linx (DECISÃO DO PO).
- **Situação (Ativo/Inativo)**: autoridade assimétrica (seção 6).
- **Referências por Fornecedor**: coleção filha (fornecedor, código do fornecedor), finalidade DE/PARA para XML NF-e/NFS-e futuro.

## 2. Material × Serviço — recomendação

**Recomendação: NÃO criar dois cadastros mestres separados ("Material" e "Serviço"). O cadastro único de Item Fiscal, já comprovado, é suficiente para o escopo desta B3.**

Fundamentação, direto da evidência (não da nomenclatura antiga do backlog):

- Nas três telas investigadas, **nenhuma tabela "SERVICOS" mestre foi encontrada** — o vocabulário de serviço já existe **dentro** de `CADASTRO_ITEM_FISCAL` (`CODIGO_SERVICO_REINF`, `CODIGO_SERVICO`), não numa tabela separada.
- `MATERIAIS` (109 colunas) e `PRODUTOS` (188 colunas) são cadastros reais no Linx, mas **não são o que o +Compras precisa** — são, respectivamente, o insumo de manufatura têxtil e o SKU comercial de varejo (griffe/coleção/grade/B2C), domínios completamente alheios ao processo de compra indireta. O Pedido de Consumíveis (`004005GS`) só aceita item vindo deles quando **explicitamente habilitado por flag** — o caminho padrão é `CADASTRO_ITEM_FISCAL`.
- A única diferenciação real e comprovada entre "produto/mercadoria" e "serviço" aparece na **Entrada Fiscal** (`005109GS3`), como uma **complementação fiscal da nota** (`ENTRADAS_COMPLEMENTO_NF_SERVICO`/`_PROCESSO` — retenção de imposto/REINF), não como um cadastro mestre diferente do item. Isso é característica do **processo fiscal da nota**, não do cadastro do item em si.

**O que isso implica para a B3**: o cadastro de Item Fiscal no +Compras deve ser único; a distinção "é um serviço para fins fiscais" pode, no futuro (fora da B3), precisar de um atributo/classificação no próprio Item Fiscal ou ser tratada apenas no momento da Entrada Fiscal — **não precisa nascer como cadastro separado agora**. Registrado como dependência futura não bloqueante (seção 8).

## 3. Campos mínimos do Item Fiscal no +Compras

| Campo | Origem | Obrigatório | Editável | Autoridade | Justificativa |
|---|---|---|---|---|---|
| Código | Linx `CODIGO_ITEM` | Sim | Depende da decisão de criação (GAP bloqueante, seção 9) | A definir | Chave funcional do item |
| Descrição | Linx `ITEM_DESCRICAO` | Sim | Depende da decisão de criação | A definir | Granularidade livre (decisão da área de Compras) |
| Unidade | Linx `UNIDADES` (apoio) | Sim | Não (seleção, nunca digitação) | Linx | Mesmo padrão de Conta Contábil — evita unidade inválida/inconsistente |
| Conta Contábil | Linx `CTB_CONTA_PLANO` (apoio) | Sim (regra +Compras, não do Linx) | Não (seleção) | Linx | Necessária para futura validação orçamentária (binômio CC×Conta) |
| Situação (Ativo/Inativo) | Linx `INATIVO` + local +Compras | Sim | Sim, com autoridade assimétrica | Linx (entrada) / +Compras (local) | Mesma regra já adotada em Fornecedores |
| Referências por Fornecedor | +Compras (com espelho lógico de `ITEM_FISCAL_REF_FORNECEDOR`) | Não (0..N) | Sim | +Compras | DE/PARA para XML NF-e/NFS-e futuro |
| Grupo/Categoria (opcional) | Linx `ITEM_FISCAL_GRUPO`/`CADASTRO_ITEM_GRUPO` | Não | Somente leitura, se incluído | Linx | Apoia busca/organização na listagem, sem inventar taxonomia nova |

**Deliberadamente fora da lista** (permanecem exclusivos do Linx, sem necessidade comprovada de o +Compras capturar/exibir): `CLASSIF_FISCAL`, `INDICADOR_CFOP`, `TIPO_ITEM_SPED`, `ID_CEST_NCM`, `CLASSE_IMOBILIZADO`/`SUBCLASSE_IMOBILIZADO`, `NATUREZA_RENDIMENTO`, `TRIBUT_ORIGEM`, rateio de centro de custo/filial no próprio Item Fiscal — são atributos fiscais/contábeis usados no processamento interno do Linx (Entrada Fiscal, SPED), não no processo de requisição/pedido do +Compras.

## 4. Cadastros de apoio necessários

- **Conta Contábil** (`CTB_CONTA_PLANO`) — comprovado em banco, `INATIVA` real, importado/sincronizado do Linx, nunca criado no +Compras.
- **Unidade** (`UNIDADES`) — **evidência do ERP** (tela `100126CS2` faz lookup `p_valida_coluna_tabela = UNIDADES`, com colunas `UNIDADE, DESC_UNIDADE, USO_MATERIAIS, USO_PRODUTOS`), mas **ainda não comprovado em banco** (não incluído no probe de schema desta rodada) — GAP não bloqueante, fácil de fechar numa próxima consulta.
- **Grupo/Categoria de Item Fiscal** (`CADASTRO_ITEM_GRUPO`) — apenas se o campo opcional da seção 3 for adotado; não investigado a fundo.

Nenhum outro cadastro de apoio foi identificado como necessário — não criar extras sem necessidade comprovada.

## 5. Matriz de autoridade consolidada

| Informação | Autoridade | Regra |
|---|---|---|
| Criação/edição do Item Fiscal | **COMPARTILHADA** (Linx + +Compras) | LWW via `DATA_PARA_TRANSFERENCIA` (comprovado); ambíguo → Linx prevalece (`ADR-0024`). **GAP bloqueante anterior FECHADO** |
| Inativação vinda do Linx | **LINX** | Propaga para +Compras (coerente com `ADR-0024`) |
| Inativação local no +Compras | **+COMPRAS** (local) | NÃO propaga para o Linx |
| Cadastrar/editar/inativar no +Compras | permissão/parâmetro do usuário | RBAC não implementado ainda |
| Conta Contábil | **LINX** (`CTB_CONTA_PLANO`) | Importada/sincronizada, nunca criada no +Compras; obrigatória no Item Fiscal (regra +Compras) |
| Unidade | **LINX** (`UNIDADES`, evidência ERP) | Selecionada, nunca digitada |
| Granularidade do Item Fiscal | **Área de Compras** | +Compras não impõe nível de detalhe |
| Referência por Fornecedor | +Compras consome/gerencia | Sem timestamp confiável; divergência → Linx prevalece (`ADR-0024`) |
| Conta Contábil em Compras/Entradas | Herdada do Item Fiscal | Mecanismo técnico exato = dependência futura |
| Orçamento (binômio CC × Conta) | Fora da B3 | Dependência funcional futura |
| Resolução de conflito/ambiguidade sem regra específica (qualquer campo/cadastro) | **LINX** (fallback transversal) | `ADR-0024`, não exclusivo da B3 |

## 6. Regras de sincronização

- **Linx → +Compras**: inativação é autoritativa (propaga); Conta Contábil e Unidade são importadas/sincronizadas como cadastros de apoio; em ambiguidade sem regra específica, Linx prevalece (`ADR-0024`).
- **+Compras → Linx**: inativação local não propaga. **Criação/edição do Item Fiscal**: autoridade compartilhada — ambos os lados podem escrever, resolvido por `DATA_PARA_TRANSFERENCIA` (Last Write Wins comprovado) com fallback Linx-prevalece para casos ambíguos.
- **`ITEM_FISCAL_REF_FORNECEDOR`**: sem mecanismo de Last Write Wins (sem timestamp, sem trigger, fluxo VFP não toca o Item Fiscal pai — comprovado) — divergência resolvida sempre por Linx prevalece, nunca por mecanismo artificial inventado.

## 7. Dependências funcionais futuras (não bloqueiam a B3)

- Validação orçamentária real (binômio Centro de Custo × Conta Contábil) — motor, armazenamento, período/exercício.
- Mecanismo técnico exato de herança de Conta Contábil em `COMPRAS_CONSUMIVEL`/`ENTRADAS_ITEM`, incluindo `FX_CTB_BUSCA_CONTA_ITEM`.
- Processamento de XML de NF-e/NFS-e e uso operacional de `ITEM_FISCAL_REF_FORNECEDOR` nesse fluxo.
- Comportamento fiscal específico de Serviço na Entrada Fiscal (`ENTRADAS_COMPLEMENTO_NF_SERVICO`/`_PROCESSO`).
- Efeito de alteração de Conta Contábil de um Item Fiscal já usado sobre compras/entradas existentes (regra a definir, sem presunção de retroatividade).
- Comprovação em banco de `UNIDADES`.

## 8. GAPs

### Bloqueantes para iniciar a implementação da B3

**Nenhum.** O único GAP bloqueante identificado na consolidação anterior (autoridade de criação/edição do Item Fiscal) foi fechado pela decisão do Product Owner de autoridade compartilhada + `ADR-0024` (Linx prevalece como fallback transversal de conflito/ambiguidade).

### Não bloqueantes / futuros

- PK/FK física de `ITEM_FISCAL_REF_FORNECEDOR` e de `CADASTRO_ITEM_FISCAL.CONTA_CONTABIL → CTB_CONTA_PLANO` (limitação da ferramenta de schema discovery, não do Linx).
- Estrutura real em banco de `UNIDADES`.
- Regras de conta elegível / comportamento de conta inativada.
- Todos os itens da seção 7.

## 9. Aderência ao CadFormFactory

| Item do checklist | Situação |
|---|---|
| Discovery funcional realizado | ✅ Concluído (3 telas) |
| Linx ERP Agent consultado | ✅ |
| Linx Database Agent consultado | ✅ (schema real comprovado) |
| Evidências registradas | ✅ `docs/audits/Discovery-ItemFiscal-Pedido-EntradaFiscal-Consumiveis.md` |
| Matriz de autoridade definida | ✅ Completa — criação/edição compartilhada + fallback `ADR-0024` |
| Contrato funcional definido | ✅ `ContratoFuncionalPreliminar-B3-ItemFiscal.md` |
| Layout padrão / Design System | Não iniciado (etapa posterior ao GO) |
| RBAC frontend/backend | Necessidade registrada, não implementada |
| Validação frontend/backend | Não iniciada |
| Duplicidade e identidade | Resolvida em princípio pela mesma autoridade compartilhada/`ADR-0024`; detalhamento operacional fica para a implementação |
| Ativação/inativação | ✅ Definida (assimétrica) |
| +Compras↔ERP validado | Não aplicável ainda (sem implementação) |
| Falha de integração testada | Não aplicável ainda |

### Nota de UX — organização por abas (autorizado pelo Product Owner)

Quando a implementação for autorizada, o layout da tela de Item Fiscal **não precisa ser um formulário único** — a evidência real da própria tela Linx `100126CS2` já usa exatamente essa separação (`Page1 "Item"` / `Page2 "Referencia Fornecedor"`), o que reforça a proposta de duas abas:

- **Dados Gerais**: código, descrição, unidade, situação, conta contábil.
- **Referências por Fornecedor**: gerenciamento da coleção fornecedor × código do fornecedor.

Consistente com o padrão visual já usado em Fornecedores (múltiplas abas) e com o Design System — decisão de UX a confirmar na etapa de Mock navegável, não nesta etapa de discovery.

## 10. Recomendação final

**GO PARA VALIDAÇÃO FINAL DO PRODUCT OWNER — nenhum GAP bloqueante restante.**

O que fechou nesta rodada: autoridade de criação/edição do Item Fiscal (compartilhada, com Last Write Wins via `DATA_PARA_TRANSFERENCIA` comprovado tecnicamente até a trigger exata), autoridade de `ITEM_FISCAL_REF_FORNECEDOR` (sem timestamp confiável — Linx prevalece em divergência), e o princípio transversal de fallback Linx↔+Compras (`ADR-0024`, registrado canonicamente em `.ai/DECISIONS.md`, referenciado por `CadFormFactory.md`). Combinado com o que já estava fechado (conceito de Item Fiscal, Material×Serviço, campos mínimos, cadastros de apoio, inativação, conta contábil), a matriz de autoridade está completa.

**Isto ainda NÃO autoriza implementação** — é a sinalização de que o discovery está pronto para ser validado pelo Product Owner como base da futura implementação da B3.
