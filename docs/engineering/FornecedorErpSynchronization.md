# Sincronização de Fornecedores ERP SOMA

## Objetivo

Implementar o primeiro fluxo operacional real do +Compras para ler fornecedores no ERP `SOMA_DESENV` e persistir novos registros ou atualizações no banco da aplicação `MaisCompras`.

## Arquitetura

O SQL do ERP fica isolado na infraestrutura:

```text
Infrastructure
└── Integrations
    └── ERP
        ├── Contracts
        │   └── IFornecedorErpReader.cs
        └── Soma
            └── SomaFornecedorReader.cs
```

Fluxo:

```text
SOMA_DESENV
  ↓
FornecedorErpIntegracaoDto
  ↓
FornecedorCanonico
  ↓
Fornecedor (+Compras)
  ↓
Entity Framework Core / MaisCompras
```

## Endpoint

```http
GET /api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=100
```

Resposta:

```json
{
  "consultados": 100,
  "incluidos": 10,
  "atualizados": 5,
  "semAlteracao": 85,
  "businessUnit": "DEFAULT",
  "erpSistema": "SOMA_DESENV",
  "correlationId": "..."
}
```

## Regras

- `NomeFantasia` só é sobrescrito por dados com origem `ERP`.
- Registros sincronizados recebem `OrigemInformacao = ERP`.
- Vínculo ERP é mantido por `BusinessUnit`, `ErpSistema` e `ErpFornecedorId`.
- Connection strings não ficam no código; usar `ConnectionStrings:ErpConnection` e `ConnectionStrings:MaisComprasConnection` via user-secrets ou variáveis de ambiente.

## Validação

Com VPN ativa e SQL Server corporativo acessível:

```bash
dotnet build backend/BlueprintOS.sln
dotnet test backend/BlueprintOS.sln
curl "http://localhost:5262/api/fornecedores/sincronizar-erp?businessUnit=DEFAULT&limite=100"
```

Depois validar no banco `MaisCompras`:

- novos fornecedores inseridos;
- existentes atualizados;
- `OrigemInformacao = ERP`;
- `ErpSistema = SOMA_DESENV`;
- `NomeFantasia` preservado para alterações não originadas do ERP.

Os testes de integração reais dependem de VPN e das connection strings configuradas fora do repositório.
