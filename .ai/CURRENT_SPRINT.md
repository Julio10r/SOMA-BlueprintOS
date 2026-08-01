# B2.2 - Consulta CNPJ e Enriquecimento de Fornecedor

Status:
Em andamento

Objetivo:
Criar capacidade arquitetural para consultar dados externos de fornecedor a partir do `Cnpj_Cpf` informado pelo usuário, tratando o retorno como sugestão de enriquecimento revisável antes da persistência no +Compras e antes de qualquer sincronização ERP.

Fluxo:

```text
Usuário informa Cnpj_Cpf
    ↓
Serviço de consulta externa
    ↓
Dados enriquecidos
    ↓
Usuário valida
    ↓
+Compras salva fornecedor
    ↓
Sincronização ERP
```

Regra central:
- A consulta externa não substitui o cadastro.
- A API externa é fonte de sugestão de dados.
- O usuário deve confirmar os dados antes da gravação no +Compras.
- Não haverá atualização automática do +Compras ou do ERP sem aprovação humana.

Fluxo permitido:

```text
API externa
    ↓
Sugestão de dados
    ↓
Usuário confirma
    ↓
+Compras
```

Fluxo proibido:

```text
API externa
    ↓
Atualização automática sem aprovação
```

Documentação inicial:
- `docs/engineering/FornecedorCnpjEnrichment.md`

Backlog B2.2:
- B2.2.1 — Contrato de consulta CNPJ.
- B2.2.2 — Integração API externa.
- B2.2.3 — Normalização de dados.
- B2.2.4 — Validação de fornecedor.
- B2.2.5 — Persistência e auditoria.

Contexto herdado:
- A13 concluída.
- B1 concluída.
- B2.1 concluída.
- B2.1.1 concluída.
- B2.1.2 concluída.
- B3 não iniciada.

Limites desta sprint:
- Não criar frontend ou telas.
- Não implementar chamadas definitivas para fornecedor externo.
- Não assumir contratos pagos.
- Consolidar primeiro a arquitetura e o contrato operacional.
