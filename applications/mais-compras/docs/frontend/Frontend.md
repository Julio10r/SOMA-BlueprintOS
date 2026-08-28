# Frontend do Portal Operacional +Compras

## Finalidade

Este documento orienta a futura implementação do frontend definido pela [ADR-0017](../../.ai/DECISIONS.md). O portal é uma interface operacional única para os domínios do +Compras; não é um produto separado nem substitui o backend como fonte de verdade.

## Estado atual

Há um portal parcialmente implementado: o módulo Fornecedores está conectado à API real (cadastro, consulta/enriquecimento de CNPJ, aprovação/rejeição), com o Design System AZZAS 2154/GDT aplicado; os demais módulos (Dashboard, Pedidos, Negociações, Indicadores, Agentes IA, Configurações) existem como telas demonstrativas sem persistência real, conforme a Work Order `.ai/work-orders/active/PortalMaisComprasFrontend.md`. Para o estado operacional comprovado (build/testes), ver `.ai/PROJECT_STATE.md` e `.ai/BACKLOG.md` — este documento não reproduz esse estado, apenas a arquitetura do frontend.

## Arquitetura alvo

- **Stack:** React e TypeScript, conforme [.ai/PROJECT.md](../../.ai/PROJECT.md).
- **Organização:** uma shell do portal, seguida de módulos de domínio e componentes compartilhados. A estrutura física será definida pela Work Order de implementação aprovada, sem criar uma arquitetura paralela ao Modular Monolith e à Clean Architecture existentes.
- **Comunicação:** clientes de API tipados consomem endpoints e DTOs oficiais. O frontend não acessa banco, ERP, adaptadores ou SDKs de infraestrutura.
- **Estado e regras:** validações de experiência podem orientar a entrada, mas regras de negócio, autorização, integração e decisões de sincronização permanecem no backend.
- **Erros:** apresentar mensagens sanitizadas e acionáveis; detalhes técnicos, credenciais, connection strings e exceções internas não são exibidos ao usuário.
- **Autenticação:** a integração definitiva será com Microsoft Entra ID quando Identity estiver aprovada e implementada. Até então, nenhuma tela deve assumir que possui autenticação corporativa concluída.

## Design System e componentes

Todo componente deve aplicar o [AZZAS 2154 — GDT Design System](../../resources/design-system/README.md). Consultar seus tokens, componentes, ícones, regras de acessibilidade e linguagem visual antes de criar qualquer interface.

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

Fornecedores é a primeira vertical slice funcional. Dashboard, Pedidos, Cotações, Negociações, Contratos e Indicadores existem hoje como estrutura visual demonstrativa enquanto suas capacidades de negócio são entregues; Agentes IA permanece planejado.

## Mapa do Portal Operacional +Compras

Navegação e identidade visual do produto, conforme a ADR-0017. O Dashboard é a página inicial: apresenta visão executiva, indicadores, integrações, alertas e atividades recentes, sem substituir os módulos operacionais.

```text
+Compras
├── Dashboard
├── Fornecedores
│   ├── Lista
│   ├── Cadastro
│   ├── Detalhes
│   ├── Sincronização ERP
│   └── Auditoria
├── Pedidos
├── Cotações
├── Negociações
├── Contratos
├── Indicadores
└── Agentes IA
```

| Módulo | Estado alvo | Papel no portal |
|---|---|---|
| Dashboard | 🟡 Estrutura visual | Visão executiva, integrações, alertas e atividades recentes. |
| Fornecedores | 🟢 Funcional | Vertical slice: lista, cadastro, edição, detalhes, sincronização ERP, histórico e auditoria. |
| Pedidos | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Cotações | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Negociações | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Contratos | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Indicadores | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Agentes IA | ⚪ Planejado | Sem estrutura visual ou funcional até Work Order aprovada. |

A mudança de um módulo para `🟢 Funcional` exige evidência de código, APIs integradas, testes e aceite da Work Order correspondente — este documento não substitui essa evidência, apenas descreve a arquitetura de navegação alvo.

## Checklist para implementação

Antes de iniciar uma Work Order frontend, ler `.ai/PROJECT.md`, `.ai/ARCHITECTURE.md`, `.ai/DECISIONS.md`, `.ai/CURRENT_SPRINT.md`, `resources/design-system/` e os documentos de domínio em `docs/backend/`. Confirmar o contrato de API, o domínio responsável, o estado do módulo no roadmap e os critérios de aceite da Work Order.
