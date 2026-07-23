# Template: Criar novo endpoint/API

Uso: iniciar a criação de um novo endpoint de API em um módulo existente.

## Antes de começar, consultar

- [../ARCHITECTURE.md](../ARCHITECTURE.md) — regras da camada Api (§8) e comunicação entre módulos (§9)
- [../STANDARDS.md](../STANDARDS.md) — convenções de API, DTOs, versionamento (§17)
- [../context/security.md](../context/security.md) — autenticação, autorização e rate limiting
- [../context/coding-standards.md](../context/coding-standards.md) — princípios SOLID aplicáveis ao design do endpoint

## Placeholders a preencher

- `{{modulo}}`
- `{{nome_do_endpoint}}`
- `{{verbo_http}}` (GET/POST/PUT/DELETE)
- `{{rota}}`
- `{{objetivo}}`
- `{{dto_entrada}}`
- `{{dto_saida}}`
- `{{regras_de_autorizacao}}`
- `{{casos_de_erro_esperados}}`

## Prompt

Crie um endpoint `{{verbo_http}} {{rota}}` no módulo `{{modulo}}`, com o objetivo de `{{objetivo}}`.

DTO de entrada: `{{dto_entrada}}`. DTO de saída: `{{dto_saida}}`. Nunca exponha entidades de domínio diretamente.

Regras de autorização: `{{regras_de_autorizacao}}`.

Casos de erro esperados e como devem ser tratados (Result Pattern, nunca `throw Exception()` genérico): `{{casos_de_erro_esperados}}`.

A lógica de negócio deve residir em Application (Command/Query + Handler), nunca no Controller/endpoint. Valide entrada com FluentValidation. Adicione testes cobrindo o caso de sucesso e os casos de erro listados.
