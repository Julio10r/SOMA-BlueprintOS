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

---

# Ambiente de Execução — Desenvolvimento Local (ADR-0018)

Status:
Aceito.

Decisão:
Desenvolvimento ocorre localmente no Mac com frontend React e API .NET. Persistência utiliza SQL Server corporativo acessível via VPN (`SOMA_DESENV`). Homologação futura será realizada em Windows Server/IIS.

Contexto:
Uma tentativa de publicar o frontend do Portal +Compras via n8n (com backend exposto temporariamente por túnel ngrok) foi revertida — o n8n só serve HTML como string única (sem suporte a pasta `dist/` com múltiplos assets) e não havia nenhum backend publicado além de localhost. Publicação via n8n/GCP fica registrada como opção futura de homologação/demonstração.

Ajustes decorrentes:
- `frontend/web/.env.example`: `VITE_API_BASE_URL` padrão ajustado para `http://localhost:8080` (Docker) com nota sobre `http://localhost:5262` (`dotnet run`).
- `frontend/web/vite.config.ts`: revertido para a forma original (sem `vite-plugin-singlefile`); proxy de dev aponta para `http://127.0.0.1:8080`.
- `backend/src/BlueprintOS.Api/appsettings.Development.json`: `Cors:AllowedOrigins` restrito a `http://localhost:5173` e `http://127.0.0.1:5173`.
- Removido header específico de bypass do ngrok em `supplierEnrichmentApi.ts` (não é mais necessário).
- Nenhuma regra de negócio foi alterada.

Ver ADR-0018 em `.ai/DECISIONS.md` para o detalhamento completo.

---

# Portal +Compras Frontend

Status:
Concluída tecnicamente no frontend (commit `8ee8f4e`, branch `feature/a13-procurement-vertical-slice`); validação de backend pendente.

Objetivo concluído:
Construir o portal visual +Compras (React, TypeScript, GDT Design System AZZAS 2154), transformando a tela funcional de fornecedor em uma experiência navegável, preservando a integração real já existente e sem simular funcionalidades ainda não implementadas nos demais módulos.

Evidências:

- Portal criado: shell de navegação (AppShell) e rotas React Router.
- Módulo Fornecedores funcional e integrado à API real: cadastro, consulta CNPJ, enriquecimento, aprovação e rejeição de divergências.
- Demais módulos (Pedidos, Negociações, Indicadores, Agentes IA, Configurações) implementados como telas demonstrativas, sem persistência simulada.
- Design System AZZAS 2154 / GDT aplicado (tokens, UI kit `portal-gdt`).
- Build frontend aprovado: `tsc` + `vite build`, 4/4 testes aprovados.
- Backend: `dotnet build`/`dotnet test` **não executados neste ciclo** por ausência do SDK .NET no ambiente de validação usado — pendente validação local antes de considerar a frente encerrada de ponta a ponta.
- Revisão manual de código confirmou (sem execução): endpoints `POST/GET /fornecedores`, `POST /fornecedores/consulta-cnpj`, `POST/{id}/enriquecimento-cnpj` (+ `/aprovar` e `/rejeitar`) batendo com as chamadas do frontend em `supplierEnrichmentApi.ts`; `Cnpj_Cpf` persistido como `varchar(14)` alfanumérico; `NomeFantasia` protegido e só atualizado quando origem é ERP; alerta de situação cadastral `Baixada/Suspensa/Inapta` presente em `ApprovalPanel.tsx` sem bloquear o fluxo; CORS configurável via `Cors:AllowedOrigins`, sem `AllowAnyOrigin` irrestrito e sem segredos hardcoded em `Program.cs`/`appsettings.Development.json`.

Documentação:

- `docs/work-orders/PortalMaisComprasFrontend.md`
- `docs/demo/PortalMaisComprasDemo.md` (novo — roteiro de demonstração executiva)

Transição:

- Próxima pendência: executar `dotnet build`/`dotnet test` localmente (ambiente com SDK .NET) para fechar a validação de backend desta frente.
- B3 permanece não iniciada.
