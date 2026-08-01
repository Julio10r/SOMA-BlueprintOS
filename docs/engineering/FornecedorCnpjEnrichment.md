# Fornecedor CNPJ Enrichment

## Objetivo

Definir a arquitetura inicial da capacidade B2.2 — Consulta CNPJ e Enriquecimento de Fornecedor.

A funcionalidade deve permitir que o usuário informe um `Cnpj_Cpf`, acione uma consulta externa e receba dados sugeridos para enriquecer o cadastro de fornecedor no +Compras. A consulta externa não substitui o cadastro e nunca deve atualizar o +Compras ou o ERP Linx sem confirmação humana.

**Estado B2.2.1:** concluída. O contrato, o resultado tipado, a auditoria persistida e os testes unitários foram entregues; a B2.2 continua em andamento e não possui provider externo configurado.

```text
Usuário informa Cnpj_Cpf
    ↓
Serviço de consulta externa
    ↓
Dados enriquecidos
    ↓
Usuário valida
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

A fonte externa ainda deve ser selecionada e autorizada.

Critérios mínimos para escolha:

- cobertura para CNPJ brasileiro;
- termos de uso compatíveis com uso corporativo;
- disponibilidade e limites claros;
- SLA ou comportamento operacional documentado;
- modelo de autenticação compatível com secrets corporativos;
- possibilidade de auditoria da origem dos dados.

Fontes públicas ou gratuitas podem ser usadas apenas como referência técnica durante a arquitetura. Contratos pagos dependem de aprovação específica.

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

Mapeamento para o +Compras:

- `Cnpj_Cpf` permanece o documento informado/validado.
- `RazaoSocial` pode receber sugestão externa.
- `NomeFantasia` deve respeitar a proteção operacional ERP definida na B2.1.2.
- Campos de domínio controlados pelo ERP Linx não devem ser sobrescritos por fonte externa.

## Regras de Aceite e Rejeição

Aceite:

- documento válido para consulta;
- provedor respondeu com status válido;
- dados retornados são apresentados como sugestão;
- usuário confirma explicitamente os campos a aplicar;
- persistência registra origem, data e responsável.

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

Campos aceitos/rejeitados e snapshots antes/depois pertencem à futura confirmação e persistência do fornecedor; não são produzidos pela consulta de sugestão.

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
- B2.2.2 — Integração API externa.
- B2.2.3 — Normalização de dados.
- B2.2.4 — Validação de fornecedor.
- B2.2.5 — Persistência e auditoria.
