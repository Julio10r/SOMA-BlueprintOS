# B2.1.2 - Validação Operacional e Sincronização de Fornecedores com ERP

Status:
Em execução.

Objetivo:
Implementar o primeiro fluxo operacional real do +Compras para consultar fornecedores no ERP `SOMA_DESENV` e sincronizar registros para o banco `MaisCompras`.

Escopo técnico:

- Camada desacoplada de integração ERP criada em `Infrastructure/Integrations/ERP`.
- Reader SOMA dedicado para leitura de fornecedores, sem SQL ERP em controllers.
- Fluxo `ERP -> DTO integração -> domínio -> Fornecedor MaisCompras -> persistência`.
- Endpoint operacional `GET /api/fornecedores/sincronizar-erp`.
- Resumo de execução com `consultados`, `incluidos`, `atualizados` e `semAlteracao`.
- Proteção de `NomeFantasia` preservada: somente origem `ERP` pode atualizar esse campo.
- Rastreabilidade mantida com `OrigemInformacao = ERP`, vínculo ERP e status de sincronização.

Validação planejada:

- `dotnet build backend/BlueprintOS.sln`
- `dotnet test backend/BlueprintOS.sln`
- `curl http://localhost:<porta>/api/fornecedores/sincronizar-erp`
- Validação de dados no banco `MaisCompras`

Observação operacional:

- Testes reais contra `SOMA_DESENV` e `MaisCompras` dependem de VPN, connection strings via secrets/variáveis de ambiente e API local em execução.

Documentação:

- `docs/engineering/FornecedorErpSynchronization.md`
