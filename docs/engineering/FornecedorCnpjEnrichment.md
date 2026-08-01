# Fornecedor CNPJ Enrichment

## Objetivo

Definir a arquitetura inicial da capacidade B2.2 — Consulta CNPJ e Enriquecimento de Fornecedor.

A funcionalidade deve permitir que o usuário informe um `Cnpj_Cpf`, acione uma consulta externa e receba dados sugeridos para enriquecer o cadastro de fornecedor no +Compras. A consulta externa não substitui o cadastro e nunca deve atualizar o +Compras ou o ERP Linx sem confirmação humana.

**Estado B2.2.3:** concluída. O contrato, o resultado tipado, a auditoria persistida da consulta, o provider gratuito e o fluxo backend de comparação/aprovação/rejeição foram entregues; a consulta continua sendo sugestão revisável e não cria fornecedor automaticamente.

```text
Usuário informa Cnpj_Cpf
    ↓
Serviço de consulta externa
    ↓
Dados enriquecidos
    ↓
Comparação campo a campo
    ↓
Usuário aprova/rejeita campos
    ↓
Salva fornecedor +Compras
    ↓
Sincronização ERP
```

## Contrato implementado na B2.2.1

O contrato é independente do provedor externo e não contém SDK, URL, credencial ou tipo de resposta de uma API específica.

```text
+Compras
    ↓
ICnpjConsultaProvider
    ├── Provider gratuito (B2.2.2)
    ├── Provider pago (futuro)
    └── Serviço corporativo (futuro)
```

`ICnpjConsultaProvider` expõe `FonteConsulta` e `ConsultarAsync(string cnpjCpf, CancellationToken)`, retornando `ConsultaCnpjResultado`. O resultado contém identificação, situação cadastral, endereço, contato, dados adicionais, fonte, data, status e mensagem de erro. A situação cadastral aceita `Ativa`, `Baixada`, `Suspensa`, `Inapta` e `NaoEncontrada`.

Entrada mínima:

- `Cnpj_Cpf`: documento informado pelo usuário.
- `CorrelationId`: identificador de rastreabilidade da requisição.
- `RequestedBy`: identidade do usuário solicitante.

Saída esperada:

- status da consulta;
- dados sugeridos;
- fonte consultada;
- data/hora da consulta;
- mensagens de validação;
- erros normalizados, quando houver.

O retorno deve representar sugestão de dados, não comando de persistência.

## Fontes externas

O provider inicial implementado é `BrasilApiCnpjProvider`, em `BlueprintOS.Infrastructure.Integrations.CnpjConsulta`, usando a BrasilAPI pública para consulta de CNPJ.

Fluxo mantido:

```text
BrasilAPI
    ↓
BrasilApiCnpjProvider
    ↓
ConsultaCnpjResultado
    ↓
ConsultarCnpjFornecedorUseCase
    ↓
+Compras
```

Configuração:

```json
{
  "CnpjConsulta": {
    "Provider": "BrasilApi",
    "BaseUrl": "https://brasilapi.com.br/api/cnpj/v1/",
    "TimeoutSeconds": 10
  }
}
```

`BaseUrl` e `TimeoutSeconds` são externos ao código. `Provider` prepara a troca futura por `ProviderPago` ou `GovBr`; nesta sprint somente `BrasilApi` é registrado.

Critérios mínimos para escolha:

- cobertura para CNPJ brasileiro;
- termos de uso compatíveis com uso corporativo;
- disponibilidade e limites claros;
- SLA ou comportamento operacional documentado;
- modelo de autenticação compatível com secrets corporativos;
- possibilidade de auditoria da origem dos dados.

Limitações da fonte gratuita:

- disponibilidade e limites dependem da BrasilAPI;
- não há SLA corporativo contratado nesta sprint;
- não há cache local;
- falhas são normalizadas e não bloqueiam cadastro manual;
- uso produtivo definitivo ainda depende de validação jurídica/operacional.

## Campos retornados

Campos candidatos para sugestão:

- razão social;
- nome fantasia;
- situação cadastral;
- data de abertura;
- CNAE principal;
- CNAEs secundários;
- natureza jurídica;
- endereço;
- município;
- UF;
- CEP;
- telefone;
- e-mail;
- quadro societário, se permitido pelo provedor;
- data da última atualização da fonte.

Mapeamento BrasilAPI B2.2.2:

- `cnpj` → `Cnpj_Cpf`;
- `razao_social` → `RazaoSocial`;
- `nome_fantasia` → `NomeFantasia`;
- `descricao_situacao_cadastral` → `SituacaoCadastral`;
- `data_situacao_cadastral` → `DataSituacaoCadastral`;
- `cep`, `logradouro`, `numero`, `complemento`, `bairro`, `municipio`, `uf` → endereço;
- `email`, `ddd_telefone_1`/`ddd_telefone_2` → contato;
- `natureza_juridica`, `porte` → dados adicionais;
- `FonteConsulta` = `BrasilAPI`;
- `DataConsulta` = horário UTC da consulta.

Mapeamento para o +Compras:

- `Cnpj_Cpf` permanece o documento informado/validado.
- `RazaoSocial` pode receber sugestão externa.
- `NomeFantasia` deve respeitar a proteção operacional ERP definida na B2.1.2.
- Campos de domínio controlados pelo ERP Linx não devem ser sobrescritos por fonte externa.

## Fluxo de aprovação B2.2.3

O backend expõe o processo de validação antes de alterar o fornecedor:

```text
Fornecedor existente
    ↓
Consulta CNPJ externa
    ↓
ConsultaCnpjResultado
    ↓
Comparação campo a campo
    ↓
Divergências pendentes
    ↓
Aprovação ou rejeição por usuário
    ↓
Atualização somente dos campos aceitos
    ↓
FornecedorEnriquecimentoAnalise
```

Endpoints adicionados:

- `POST /fornecedores/{id}/enriquecimento-cnpj`: compara o fornecedor atual com um `ConsultaCnpjResultado` e retorna divergências/alertas.
- `POST /fornecedores/{id}/enriquecimento-cnpj/aprovar`: registra decisão `Aceito` e aplica somente os campos aprovados.
- `POST /fornecedores/{id}/enriquecimento-cnpj/rejeitar`: registra decisão `Rejeitado` sem alterar o fornecedor.

O corpo recebe `Consulta`, `ConsultaId`, `BusinessUnit`, `ErpSistema`, `CorrelationId` e, nas decisões, a lista `Campos`. Quando `Campos` vem vazia, a decisão é aplicada às divergências retornadas pela análise.

Modelo de divergência:

- `Campo`;
- `ValorAtual`;
- `ValorSugerido`;
- `Origem` = `ConsultaCnpj`;
- `StatusDecisao`: `Pendente`, `Aceito` ou `Rejeitado`.

## Regras por campo B2.2.3

Campos comparados inicialmente:

- Identificação: `RazaoSocial`, `NomeFantasia` e consistência de `Cnpj_Cpf`.
- Endereço: `Cep`, `Logradouro`, `Numero`, `Complemento`, `Bairro`, `Cidade`, `Estado`.
- Contatos: `Email`, `Telefone`.
- Situação cadastral: `SituacaoCadastral`, `DataSituacaoCadastral` como informação/alerta.

Regras:

- `Cnpj_Cpf` nunca é atualizado pela consulta CNPJ; divergência gera alerta de consistência.
- `RazaoSocial` pode ser atualizada somente após aprovação.
- `NomeFantasia` é protegido por regra Linx (`NomeFantasia = NOME_CLIFOR = FORNECEDOR`) e não é alterado pela consulta CNPJ, mesmo se aprovado; a decisão é auditada.
- Endereço e contatos podem ser atualizados somente quando aprovados.
- Situação cadastral não bloqueia cadastro; situações como `Baixada`, `Suspensa` e `Inapta` geram alerta informativo.

## Regras de Aceite e Rejeição

Aceite:

- documento válido para consulta;
- provedor respondeu com status válido;
- dados retornados são apresentados como sugestão;
- usuário confirma explicitamente os campos a aplicar;
- persistência registra origem, data, responsável, decisão e `CorrelationId`.

Rejeição:

- documento inválido;
- documento não encontrado;
- provedor indisponível;
- retorno incompleto para o objetivo mínimo;
- divergência crítica que exige revisão manual;
- tentativa de atualizar cadastro sem confirmação humana.

## Auditoria

Toda consulta realizada pelo caso de uso gera `FornecedorCnpjConsultaHistorico`, persistido em `FornecedoresCnpjConsultas`, com:

- `CorrelationId`;
- usuário solicitante;
- documento consultado;
- provedor utilizado;
- timestamp da consulta;
- status e mensagens;
- resultado da consulta e mensagem de erro normalizada;
- `BusinessUnit` e `ErpSistema` opcional para futuras configurações por BU.

As decisões de campo geram `FornecedorEnriquecimentoAnalise`, persistido em `FornecedoresEnriquecimentoAnalises`, com:

- `FornecedorId`;
- `Cnpj_Cpf`;
- `ConsultaId`;
- `Campo`;
- `ValorAnterior`;
- `ValorNovo`;
- `Decisao`;
- `Usuario`;
- `DataHora`;
- `CorrelationId`;
- `BusinessUnit`;
- `ErpSistema`;
- `Fonte`.

Essa trilha responde quem aprovou ou rejeitou cada alteração, quando, em qual BU/ERP e com qual correlação.

Dados sensíveis devem ser registrados com mascaramento quando aplicável.

## Tratamento de Erro

Erros devem ser normalizados por categoria:

- documento inválido;
- documento não encontrado;
- provedor indisponível;
- timeout;
- autenticação/autorização do provedor;
- limite de uso excedido;
- resposta inválida;
- erro inesperado.

Falha na consulta externa não deve bloquear o cadastro manual de fornecedor.

O provider B2.2.2 trata:

- CNPJ inválido;
- CNPJ não encontrado (`404`);
- indisponibilidade externa;
- timeout configurável;
- cancelamento por `CancellationToken`;
- erro de comunicação;
- resposta inválida.

Erros técnicos brutos não são repassados ao usuário; o retorno usa mensagens normalizadas em `ConsultaCnpjResultado.MensagemErro`.

## Futuro Modelo de Licença/API

A B2.2 deve manter o provedor externo atrás de uma interface própria para permitir troca futura.

Antes de uso produtivo, será necessário definir:

- provedor aprovado;
- contrato comercial ou jurídico;
- limites de uso;
- política de cache;
- retenção de dados;
- classificação LGPD;
- gestão de secrets;
- monitoramento de consumo;
- plano de fallback operacional.

## Backlog B2.2

- B2.2.1 — Concluída: contrato de consulta CNPJ, resultado tipado e auditoria persistida, sem provider externo.
- B2.2.2 — Concluída: provider BrasilAPI, configuração externa, timeout, cancelamento, normalização e testes.
- B2.2.3 — Concluída: comparação campo a campo, aprovação/rejeição, atualização seletiva, proteção `NomeFantasia`/Linx e auditoria de decisões.
- B2.2.4 — Validação de fornecedor.
- B2.2.5 — Persistência e auditoria complementar.
