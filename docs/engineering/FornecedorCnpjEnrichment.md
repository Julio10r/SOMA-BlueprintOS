# Fornecedor CNPJ Enrichment

## Objetivo

Definir a arquitetura inicial da capacidade B2.2 — Consulta CNPJ e Enriquecimento de Fornecedor.

A funcionalidade deve permitir que o usuário informe um `Cnpj_Cpf`, acione uma consulta externa e receba dados sugeridos para enriquecer o cadastro de fornecedor no +Compras. A consulta externa não substitui o cadastro e nunca deve atualizar o +Compras ou o ERP Linx sem confirmação humana.

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

## Contrato

O contrato inicial deve ser independente do provedor externo.

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

Toda consulta deve gerar trilha auditável com:

- `CorrelationId`;
- usuário solicitante;
- documento consultado;
- provedor utilizado;
- timestamp da consulta;
- status e mensagens;
- campos sugeridos;
- campos aceitos ou rejeitados pelo usuário;
- snapshot antes/depois quando houver persistência.

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

- B2.2.1 — Contrato de consulta CNPJ.
- B2.2.2 — Integração API externa.
- B2.2.3 — Normalização de dados.
- B2.2.4 — Validação de fornecedor.
- B2.2.5 — Persistência e auditoria.
