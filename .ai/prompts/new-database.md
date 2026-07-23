# Template: Criar tabela/migração de banco

Uso: iniciar a criação de uma nova tabela ou alteração de schema via migração EF Core.

## Antes de começar, consultar

- [../ARCHITECTURE.md](../ARCHITECTURE.md) — regras de Infrastructure e banco de dados (§8, §10)
- [../STANDARDS.md](../STANDARDS.md) — ORM oficial, SQL parametrizado (§16)
- [../context/security.md](../context/security.md) — classificação de dados pessoais (LGPD), se a tabela armazenar dados de usuário

## Placeholders a preencher

- `{{modulo}}`
- `{{nome_da_entidade}}`
- `{{campos}}` (nome, tipo, obrigatoriedade)
- `{{relacionamentos}}`
- `{{indices_necessarios}}`
- `{{contem_dados_pessoais}}` (sim/não, e quais campos)

## Prompt

No módulo `{{modulo}}`, crie a entidade `{{nome_da_entidade}}` com os campos: `{{campos}}`.

Relacionamentos: `{{relacionamentos}}`.

Índices necessários: `{{indices_necessarios}}`.

Dados pessoais envolvidos: `{{contem_dados_pessoais}}` — se sim, classifique os campos conforme context/security.md.

Gere a migração EF Core correspondente. Nunca altere dados manualmente em produção. Migrações devem ser versionadas e revisadas antes do merge. Nenhuma entidade interna de um módulo deve ser acessada diretamente por outro módulo — exponha apenas via Contracts, se necessário.
