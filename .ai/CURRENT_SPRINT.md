# B2.1.2 - Implementação Modelo Canônico Fornecedor ERP Linx

Status:
Em implementação

Objetivo:
Implementar o alinhamento estrutural entre o ERP Linx e o modelo de fornecedor do +Compras conforme ADR-0016.

Fluxo:

```text
Linx ERP
    ↓
Contrato Canônico Fornecedor
    ↓
Banco +Compras
    ↓
API
    ↓
Frontend
```

Alterações realizadas:
- `Cnpj_Cpf` introduzido como documento fiscal compatível com `CGC_CPF`, com `varchar(14)` na persistência.
- `TipoPessoa` preservado para distinguir `PF`/`PJ`.
- `RazaoSocial` separado de `NomeFantasia`.
- `NomeFantasia` protegido contra alteração manual do +Compras; somente importação ERP altera esse campo.
- `Beneficiador` e `Licenciado` adicionados ao contrato canônico, entidade, banco, API/DTOs, sincronização e auditoria.
- Tabela `FornecedoresDominiosErp` criada para domínios controlados pelo Linx, com FK opcional em fornecedor para condição de pagamento, tipo e subtipo.
- Adaptador Linx passou a mapear `BENEFICIADOR` e `LICENCIADO`.
- Frontend inicial recebeu contrato TypeScript e validações de tamanho/tipo de documento sem listas fixas.

Migration:
- `202608010002_B212FornecedorLinxCanonicalModel`
- Aplicada no banco dev +Compras via `dotnet run --project backend/src/BlueprintOS.Api -- migrate`.

Validação:
- `dotnet build backend/BlueprintOS.sln --no-restore`: sucesso, 0 erros e 0 avisos.
- Testes unitários: 256 aprovados.
- Testes de integração: 4 aprovados.

Contexto herdado:
- A13 concluída.
- B1 concluída.
- B2.1 concluída.
- B2.1.1 concluída.
- B2.1.2 em implementação.
- B2.2 permanece Draft.
- B3 não iniciada.

Decisão de produto relacionada:
- A [ADR-0017](./DECISIONS.md) aprovou a estratégia do Portal Operacional +Compras: navegação e identidade visual completas desde a primeira versão visual, com evolução funcional incremental por domínio.
- Fornecedores é a primeira vertical slice funcional planejada; esta decisão não inicia o frontend nem altera o escopo técnico da B2.1.2.
