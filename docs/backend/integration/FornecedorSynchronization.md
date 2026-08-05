# Sincronização de fornecedores — B2.1

## Arquitetura

O caso de uso depende apenas de contratos da Application. `IErpFornecedorAdapterResolver` seleciona o adaptador autorizado para a BU; `SomaDesenvolErpFornecedorAdapter` é a implementação inicial isolada em Infrastructure. O domínio não conhece tabelas, colunas ou connection strings do ERP.

O vínculo externo é `BusinessUnit + ErpSistema + ErpFornecedorId`, com índice único filtrado. O contrato canônico cobre identificação, endereço, contato, dados fiscais/bancários/comerciais, classificação e indicadores de fornecimento. A tabela `FornecedoresSincronizacoes` é append-only e registra direção, origem/destino, decisão temporal, timestamps originais/normalizados, antes/depois, hashes, tentativa, duração, correlação e erro sanitizado. Não existe exclusão automática.

## Operação

- `POST /api/fornecedores/sincronizar` importa por identificador ERP ou exporta por `FornecedorId`.
- `POST /api/fornecedores/sincronizar/lote` exporta uma lista controlada, limitada a 100 itens.
- `OperacaoFornecedor.Inativar` realiza inativação lógica e pode ser reexecutada sem duplicar fornecedor ou escrita.
- A comparação temporal normaliza ambos os lados para `America/Sao_Paulo`, com precisão até o segundo. ERP mais recente vence; +Compras mais recente vence; empate divergente favorece +Compras; empate igual não escreve.
- A repetição consulta o vínculo externo e mantém o mesmo fornecedor; quando os dados não mudaram, não repete a atualização.
- Cancelamento propaga `CancellationToken`; comandos SQL têm timeout configurável em `ErpIntegration:TimeoutSeconds`.

## Configuração

`ConnectionStrings:MaisComprasConnection` é o banco próprio. `ConnectionStrings:ErpConnection` deve apontar exclusivamente para `SOMA_DESENV`. A BU autoriza seu ERP em `ErpIntegration:BusinessUnits:{BU}:ErpSistema`; o schema/tabela inicial usa `ErpIntegration:SomaDesenvol:Schema` e `Table`.

Credenciais devem permanecer em User Secrets ou variáveis de ambiente. Logs registram operação, ERP e status, nunca connection strings, senhas ou CNPJ.

## Validação operacional

1. Aplicar a migration no +Compras com `dotnet run --project backend/src/BlueprintOS.Api -- migrate`, após autorização operacional.
2. Consultar um fornecedor fictício existente no ERP e chamar o endpoint na direção `ErpParaMaisCompras` duas vezes; conferir uma linha em `Fornecedores` e duas tentativas auditadas, sem duplicidade.
3. Criar no +Compras um fornecedor de teste com identificador rastreável, exportar, consultar o ERP e atualizar somente um campo corporativo permitido; exportar novamente.
4. Guardar a correlação das respostas, IDs externos e consultas de conferência. Não remover o registro de teste sem registrar o procedimento.

A reabertura de 01/08/2026 foi concluída com o fornecedor fictício +Compras `8a86809e-b123-493d-8bb7-b855527e98a1`/ERP `900001`: importação, exportação, alteração de CNPJ, inativação nos dois sentidos, reexecução idempotente e auditoria (15 eventos, 0 falhas). A migration complementar `202608010001_B21CanonicalSupplierSynchronization` foi aplicada somente no +Compras dev; a procedure `LX_AZZ_GERAR_FORNECEDOR_LINX` não é chamada. A B2.1 está concluída, incluindo os cenários temporal e de empate previstos no contrato canônico.

## Evidência final da validação Linx — 01/08/2026

O adaptador Linx da BU usa exclusivamente o mecanismo oficial, dentro da transação de criação:

```sql
DECLARE @CLIFOR CHAR(6);
EXEC LX_SEQUENCIAL
    @TABELA_COLUNA = 'FORNECEDORES.CLIFOR',
    @EMPRESA = 1,
    @SEQUENCIA = @CLIFOR OUTPUT,
    @UPDATE_SEQUENCIAL = 1;
SELECT @CLIFOR AS CLIFOR;
```

As criações reais retornaram `315501` (reconciliado após confirmação remota), `315502` e `315503` (execuções concorrentes). Cada código foi confirmado em `FORNECEDORES.COD_FORNECEDOR/CLIFOR` e `CADASTRO_CLI_FOR.COD_CLIFOR/CLIFOR`; atualizações e inativações reutilizaram o mesmo vínculo.

O campo efetivo de transferência identificado foi `CADASTRO_CLI_FOR.DATA_PARA_TRANSFERENCIA`, preenchido junto ao cadastro e consultado com `FORNECEDORES.DATA_PARA_TRANSFERENCIA` como espelho/fallback. O adaptador normaliza para `America/Sao_Paulo`, preserva a origem na auditoria e compara até o segundo.

O registro inválido `00000*` não foi excluído. Foi inativado no ERP e no +Compras pela correlação `b21-invalid-clifor-inactivate-final-erp`; a sonda final confirmou `INATIVO=True` e o código permaneceu preservado para histórico.

O teste automatizado de concorrência foi aprovado junto com a execução real simultânea: dois códigos diferentes, nenhum vínculo duplicado e nenhuma geração por `MAX(CLIFOR)+1`. Os CLIFORs `315501`, `315502`, `315503` e `315505` foram confirmados em `FORNECEDORES` e `CADASTRO_CLI_FOR`. O build terminou sem avisos; a suíte possui 250 testes unitários e 3 de integração aprovados. A B2.1 está concluída.

## B2.1.1 — Mapeamento canônico ERP → +Compras

O adaptador `SomaDesenvolErpFornecedorAdapter` consulta `FORNECEDORES` em conjunto com `CADASTRO_CLI_FOR` e devolve `ErpFornecedorDto.DadosCanonicos`, sem expor tabelas do ERP à Application. O mapeamento cobre:

| Origem Linx | Contrato canônico |
|---|---|
| `RAZAO_SOCIAL`, `NOME_CLIFOR`, `CGC_CPF`, `PJ_PF`, `RG_IE` e inscrição municipal disponível | razão social, nome fantasia, documento, tipo de pessoa, inscrições estadual e municipal |
| `CEP`, `ENDERECO`, `NUMERO`, `COMPLEMENTO`, `BAIRRO`, `CIDADE`, `UF`, `PAIS`, `COD_MUNICIPIO_IBGE` | endereço |
| `DDD1`, `TELEFONE1`, `EMAIL`, `EMAIL_NFE` | DDD, telefone, e-mail e e-mail fiscal |
| `BANCO`, `CC_AGENCIA`, `CC_CONTA` | dados bancários |
| `CONDICAO_PGTO`, `TIPO`, `SUBTIPO_FORNECEDOR`, `CTB_CONTA_CONTABIL` | condição de pagamento, tipo/subtipo de fornecedor e conta contábil |
| `FORNECE_MATERIAIS`, `FORNECE_MAT_CONSUMO`, `FORNECE_OUTROS`, `FORNECE_PROD_ACAB` | materiais, consumo, serviços e produtos |
| `TIPO_TRIBUTACAO`, `INDICADOR_FISCAL_TERCEIRO`, `ATIVIDADE_SIMPLES_NACIONAL` | dados fiscais |

Validação real: fornecedor fictício ERP `315504`, correlação `b21-1-1-importacao-completa-315504-v2`. Foram persistidos razão social, nome fantasia, endereço, contatos, banco/agência/conta, condição de pagamento, indicadores, regime fiscal, Simples Nacional e hash no registro +Compras `0a89dfbd-a6db-42eb-b0d6-413400a8a268`. A repetição `b21-1-1-importacao-completa-315504-idempotente` retornou `NenhumaAlteracao` e preservou `Versao=4`.

O CNPJ `21855705000160` foi importado com cidade e UF e, em nova execução, retornou `NenhumaAlteracao`, confirmando o mapeamento canônico e a idempotência da B2.1.1.

O ambiente ERP possui FKs para classificação (`TIPO`/`SUBTIPO_FORNECEDOR`); não foram inventados valores para forçar o teste. Quando preenchidos por dados válidos do ERP, esses campos são lidos pelo mesmo mapeamento. Nenhuma migration adicional foi necessária.

## B2.1.2 — Modelo canônico integrado ao Linx

A ADR-0016 foi implementada na estrutura de fornecedor do +Compras.

Mudanças principais:

| Área | Implementação |
|---|---|
| Documento fiscal | `Cnpj_Cpf` substitui o conceito persistente de `Cnpj`, com coluna `varchar(14)` e compatibilidade com `CADASTRO_CLI_FOR.CGC_CPF`. O contrato legado `Cnpj` continua aceito na API e é normalizado para manter compatibilidade. |
| Tipo de pessoa | `TipoPessoa` permanece no modelo e distingue `PF`/`PJ`; validações de formato ficam na API/frontend. |
| Nomes | `RazaoSocial` representa `CADASTRO_CLI_FOR.RAZAO_SOCIAL`; `NomeFantasia` representa `CADASTRO_CLI_FOR.NOME_CLIFOR`/`FORNECEDORES.FORNECEDOR`. `NomeFantasia` só é alterado quando a origem do contrato canônico é `ERP`. |
| Flags Linx | `Beneficiador` e `Licenciado` foram adicionados ao contrato canônico, agregado, banco, DTOs, sincronização e snapshot de auditoria. |
| Domínios ERP | `FornecedoresDominiosErp` armazena domínios sincronizados por `Tipo`, `CodigoERP`, `Descricao`, `BusinessUnit`, `ErpSistema`, `Status` e timestamps. `Fornecedores` possui FKs opcionais para condição de pagamento, tipo e subtipo. |
| Frontend | `frontend/web/src/procurement/suppliers/linxSupplierContract.ts` define contrato e validações iniciais sem listas fixas de domínio. |

Migration aplicada no +Compras dev:

```text
202608010002_B212FornecedorLinxCanonicalModel
```

Validação técnica:
- `dotnet build backend/BlueprintOS.sln --no-restore`: sucesso, 0 erros e 0 avisos.
- Testes unitários: 256 aprovados.
- Testes de integração: 4 aprovados.

Limite pendente: a estrutura de domínios ERP foi criada, mas a homologação operacional de sincronização dos domínios reais Linx ainda precisa ser executada com acesso ao `SOMA_DESENV`.
