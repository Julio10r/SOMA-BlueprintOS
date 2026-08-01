# B2.2 - Enriquecimento Inteligente de Fornecedor

Status:
Concluída.

Objetivo concluído:
Criar a capacidade de consultar dados externos por `Cnpj_Cpf`, tratar o retorno como sugestão revisável, comparar com o fornecedor do +Compras, aprovar ou rejeitar divergências e registrar auditoria antes de qualquer persistência seletiva.

Evidências:

- Consulta CNPJ implementada.
- Provider externo `BrasilApiCnpjProvider` criado.
- Auditoria de consulta criada em `FornecedoresCnpjConsultas`.
- Aprovação/rejeição de enriquecimento criada.
- Auditoria de decisões criada em `FornecedoresEnriquecimentoAnalises`.
- Portal fornecedor funcional criado com `CadastroFornecedor`.
- Testes aprovados.
- Commits:
  - `5a6aab8`
  - `234906c`
  - `32c9971`

Fluxo entregue:

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
+Compras persiste alterações aceitas
```

Regra central preservada:

- A consulta externa não substitui o cadastro.
- A API externa é fonte de sugestão de dados.
- O usuário deve confirmar os dados antes da gravação no +Compras.
- Não há atualização automática do +Compras ou do ERP sem aprovação humana.
- A aprovação atualiza somente os campos aceitos e registra decisão por campo.
- `NomeFantasia` permanece protegido pela regra Linx e não é sobrescrito pela consulta CNPJ.

Documentação:

- `docs/engineering/FornecedorCnpjEnrichment.md`
- `docs/work-orders/PortalMaisComprasFrontend.md`

Transição:

- Próxima frente: Portal +Compras Frontend.
- Executor planejado: Claude Code.
- B3 permanece não iniciada.
