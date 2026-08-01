# Frontend do Portal Operacional +Compras

## Finalidade

Este documento orienta a futura implementação do frontend definido pela [ADR-0017](../../.ai/DECISIONS.md). O portal é uma interface operacional única para os domínios do +Compras; não é um produto separado nem substitui o backend como fonte de verdade.

## Estado atual

Não há portal frontend implementado. Existe somente o contrato TypeScript inicial de fornecedor Linx em `frontend/web/src/procurement/suppliers/linxSupplierContract.ts`. A arquitetura e o mapa descritos aqui são diretrizes para Work Orders aprovadas, não evidência de telas funcionais.

## Arquitetura alvo

- **Stack:** React e TypeScript, conforme [.ai/PROJECT.md](../../.ai/PROJECT.md).
- **Organização:** uma shell do portal, seguida de módulos de domínio e componentes compartilhados. A estrutura física será definida pela Work Order de implementação aprovada, sem criar uma arquitetura paralela ao Modular Monolith e à Clean Architecture existentes.
- **Comunicação:** clientes de API tipados consomem endpoints e DTOs oficiais. O frontend não acessa banco, ERP, adaptadores ou SDKs de infraestrutura.
- **Estado e regras:** validações de experiência podem orientar a entrada, mas regras de negócio, autorização, integração e decisões de sincronização permanecem no backend.
- **Erros:** apresentar mensagens sanitizadas e acionáveis; detalhes técnicos, credenciais, connection strings e exceções internas não são exibidos ao usuário.
- **Autenticação:** a integração definitiva será com Microsoft Entra ID quando Identity estiver aprovada e implementada. Até então, nenhuma tela deve assumir que possui autenticação corporativa concluída.

## Design System e componentes

Todo componente deve aplicar o [AZZAS 2154 — GDT Design System](../design-system/README.md). Consultar seus tokens, componentes, ícones, regras de acessibilidade e linguagem visual antes de criar qualquer interface.

Os componentes devem ser reutilizáveis quando representam um padrão visual comum e específicos do domínio quando representam uma operação de Procurement. Não duplicar componentes de formulário, feedback, estados vazios, tabelas, filtros, status ou auditoria sem necessidade comprovada.

## Evolução por domínio

Cada módulo evolui na sequência abaixo. Uma etapa não substitui a outra:

```text
Backend
  ↓
Contrato de API
  ↓
Frontend
  ↓
Experiência operacional
```

Fornecedores é a primeira vertical slice funcional planejada. Dashboard, Pedidos, Cotações, Negociações, Contratos e Indicadores poderão existir como estrutura visual enquanto suas capacidades de negócio são entregues; Agentes IA permanece planejado. Os estados oficiais estão em [PortalMapa.md](../product/PortalMapa.md).

## Checklist para implementação

Antes de iniciar uma Work Order frontend, ler `.ai/PROJECT.md`, `.ai/ARCHITECTURE.md`, `.ai/DECISIONS.md`, `.ai/CURRENT_SPRINT.md`, `docs/design-system/` e `docs/engineering/`. Confirmar o contrato de API, o domínio responsável, o estado do módulo no roadmap e os critérios de aceite da Work Order.
