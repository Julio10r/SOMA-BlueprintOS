# Template: Refatorar código existente

Uso: iniciar uma refatoração de código sem alterar comportamento externo.

## Antes de começar, consultar

- [../STANDARDS.md](../STANDARDS.md) — convenções de nome, tamanho de classe/método, código proibido
- [../context/coding-standards.md](../context/coding-standards.md) — princípios SOLID a aplicar
- [../ARCHITECTURE.md](../ARCHITECTURE.md) — garantir que a refatoração não viole regras de dependência entre camadas/módulos

## Placeholders a preencher

- `{{modulo}}`
- `{{arquivo_ou_classe}}`
- `{{motivo_da_refatoracao}}` (ex.: classe grande demais, duplicação, baixa legibilidade)
- `{{comportamento_a_preservar}}`
- `{{testes_existentes}}` (cobrem o comportamento atual? sim/não)

## Prompt

Refatore `{{arquivo_ou_classe}}` no módulo `{{modulo}}`.

Motivo: `{{motivo_da_refatoracao}}`.

Comportamento externo que deve ser preservado integralmente: `{{comportamento_a_preservar}}`.

Testes existentes cobrindo este comportamento: `{{testes_existentes}}` — se não houver, crie testes de caracterização antes de refatorar.

Não altere escopo além do necessário para a refatoração. Não altere arquitetura. Verifique se a refatoração viola algum limite de STANDARDS.md (tamanho de classe/método, código proibido) antes de finalizar. Ao final, confirme que os testes existentes ainda passam sem alteração de asserts.
