# Mapa do Portal Operacional +Compras

## Propósito

O Portal Operacional +Compras reúne a navegação e a identidade visual do produto, conforme a [ADR-0017](../../.ai/DECISIONS.md). O Dashboard é a página inicial: apresenta visão executiva, indicadores, integrações, alertas e atividades recentes, sem substituir os módulos operacionais.

## Mapa oficial

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

## Estados aprovados para a primeira versão visual

| Módulo | Estado alvo | Papel no portal |
|---|---|---|
| Dashboard | 🟡 Estrutura visual | Visão executiva, integrações, alertas e atividades recentes. |
| Fornecedores | 🟢 Funcional | Primeira vertical slice: lista, cadastro, edição, detalhes, sincronização ERP, histórico e auditoria. |
| Pedidos | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Cotações | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Negociações | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Contratos | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Indicadores | 🟡 Estrutura visual | Navegação e contexto visual até a entrega do domínio. |
| Agentes IA | ⚪ Planejado | Sem estrutura visual ou funcional até Work Order aprovada. |

Os estados acima definem a estratégia aprovada, não comprovam implementação no repositório. No estado atual, não há portal frontend implementado; apenas o contrato TypeScript inicial de fornecedor Linx existe. A alteração para `🟢 Funcional` requer evidência de código, APIs integradas, testes e aceite da Work Order correspondente.

## Roadmap visual

1. Construir a shell de navegação e a identidade visual conforme o Design System.
2. Implementar Fornecedores como primeira vertical slice sobre contratos oficiais do backend, acompanhando B2.1, B2.1.1, B2.1.2 e B2.2.
3. Evoluir os demais módulos de estrutura visual para funcionalidade conforme o roadmap de domínio.

Todo frontend utiliza o [AZZAS 2154 — GDT Design System](../design-system/README.md), consome somente APIs e DTOs oficiais e não duplica regras de negócio do backend.
