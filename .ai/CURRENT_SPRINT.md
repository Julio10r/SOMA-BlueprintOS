# B2.1.2 - Alinhamento Estrutural ERP Linx x +Compras

Status:
Em andamento

Objetivo:
Comparar e alinhar o contrato estrutural de fornecedores entre Linx e +Compras.

Fluxo avaliado:

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

Escopo desta etapa:
- Diagnóstico de tipos, tamanhos, nullable, collation, validações e limitações operacionais.
- Levantamento das tabelas Linx `CADASTRO_CLI_FOR`, `FORNECEDORES` e `PROP_FORNECEDORES`.
- Comparação com `FornecedorCanonico`, agregado `Fornecedor`, DTOs, API e tabela `Fornecedores`.
- Registro de divergências e recomendações em `docs/engineering/B21.2-EstruturaFornecedorERP.md`.

Fora de escopo:
- Alteração de código.
- Criação de migration.
- Alteração de banco.
- Alteração frontend.
- Alteração de arquitetura.

Contexto herdado:
- A13 concluída.
- B1 concluída.
- B2.1 concluída.
- B2.1.1 concluída.
- B2.1.2 iniciada em 01/08/2026.
- B2.2 permanece Draft.
- B3 não iniciada.

Evidência inicial:
- Branch: `feature/a13-procurement-vertical-slice`.
- Repositório limpo no início da etapa.
- Acesso direto ao SQL Server `SOMA_DESENV` tentou consulta somente leitura via `sqlcmd`, mas falhou por timeout de rede; o diagnóstico inicial usa evidências versionadas da B2.1/B2.1.1 e contratos locais.
