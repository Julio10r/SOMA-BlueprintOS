# Template: Escrever testes para uma funcionalidade

Uso: iniciar a criação de testes para uma funcionalidade existente ou recém-implementada.

## Antes de começar, consultar

- [../STANDARDS.md](../STANDARDS.md) §19 — prioridade de teste (Application, Domain, Integration, End-to-End)
- [../context/testing.md](../context/testing.md) — tipos de teste e organização
- [../context/definition-of-done.md](../context/definition-of-done.md) — "testes passam" como critério de conclusão

## Placeholders a preencher

- `{{modulo}}`
- `{{funcionalidade_ou_classe}}`
- `{{tipo_de_teste}}` (unitário / integração / arquitetura)
- `{{cenarios_de_sucesso}}`
- `{{cenarios_de_erro}}`
- `{{dependencias_externas}}` (para decidir mock vs. integração real)

## Prompt

Escreva testes `{{tipo_de_teste}}` para `{{funcionalidade_ou_classe}}` no módulo `{{modulo}}`.

Cenários de sucesso a cobrir: `{{cenarios_de_sucesso}}`.
Cenários de erro a cobrir: `{{cenarios_de_erro}}`.
Dependências externas envolvidas: `{{dependencias_externas}}` — use dublês de teste (mocks/stubs) para testes unitários; use a dependência real apenas em testes de integração.

Coloque os testes na pasta `Tests/` do módulo, seguindo a estrutura espelhada de Domain/Application/Infrastructure/Api conforme STANDARDS.md §5. Nomeie os métodos de teste descrevendo o comportamento esperado. Não altere o código de produção para "facilitar" o teste sem justificativa registrada.
