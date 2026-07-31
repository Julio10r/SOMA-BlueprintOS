# decisions.md

O log canônico de ADRs do projeto é [.ai/DECISIONS.md](../DECISIONS.md). Este arquivo permanece reservado para memória operacional e não substitui ADRs.

## A10 — Adoção da Política de Autonomia da IA

Foi adotada oficialmente `AI_AUTONOMY_POLICY.md`, definindo autonomia total para melhorias internas compatíveis, proposta obrigatória para mudanças estruturais e proibição de decisões estratégicas sem aprovação humana.

## A12 — Catálogo oficial de oito fases e 56 Work Orders

Foi adotado o catálogo estratégico de oito fases e 56 Work Orders. A especificação não aprova execução nem altera fatos históricos; status Completed permanece condicionado a evidência de código, testes ou Git.

## ADR-0011 — Identidade temporária para desenvolvimento

Foi aprovada uma identidade temporária exclusivamente em `Development` para permitir persistência e vínculo de autoria de fornecedores antes do Entra ID. A camada de negócio dependerá de contrato substituível; produção continua bloqueada até H1/H2.

## ADR-0013 — Evolução operacional e inteligente do +Compras

Foi aceita a construção incremental: fluxos operacionais completos e manuais de fornecedores, itens e pedidos precedem inteligência avançada. O portal é a interface do +Compras; agentes atuam assistivamente por casos de uso e decisões críticas continuam sob confirmação humana.
