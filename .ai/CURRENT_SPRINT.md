# B2.2.4 - Portal Cadastro Fornecedor Enriquecimento CNPJ

Status:
Em andamento.

Objetivo:
Disponibilizar primeira jornada funcional do portal +Compras para cadastro e enriquecimento inteligente de fornecedores.

Fluxo:

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
+Compras salva fornecedor
    ↓
Sincronização ERP
```

Regra central:
- A consulta externa não substitui o cadastro.
- A API externa é fonte de sugestão de dados.
- O usuário deve confirmar os dados antes da gravação no +Compras.
- Não haverá atualização automática do +Compras ou do ERP sem aprovação humana.
- A aprovação atualiza somente os campos aceitos e registra decisão por campo.
- `NomeFantasia` permanece protegido pela regra Linx e não é sobrescrito pela consulta CNPJ.

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
- B2.2.1 — Contrato de consulta CNPJ, concluída: provider desacoplado, resultado tipado, auditoria persistida e 260 testes unitários/4 de integração aprovados, sem API externa.
- B2.2.2 — Concluída: `BrasilApiCnpjProvider` implementado com BrasilAPI, configuração `CnpjConsulta`, timeout, cancelamento, normalização, auditoria via caso de uso e testes unitários.
- B2.2.3 — Concluída: comparação campo a campo, endpoints de análise/aprovação/rejeição, atualização seletiva, auditoria `FornecedorEnriquecimentoAnalise` e testes.
- B2.2.4 — Em andamento: primeira tela funcional `CadastroFornecedor` no portal +Compras, consulta CNPJ, exibição de dados, divergências, aprovação/rejeição e persistência no backend de fornecedores.
- B2.2.5 — Persistência e auditoria.

Contexto herdado:
- A13 concluída.
- B1 concluída.
- B2.1 concluída.
- B2.1.1 concluída.
- B2.1.2 concluída.
- B3 não iniciada.

Limites desta sprint:
- Não criar portal completo.
- Não alterar regras de domínio.
- Não assumir contratos pagos.
- Manter BrasilAPI como adaptador gratuito substituível por provider pago/GovBr.
