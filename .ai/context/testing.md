# context/testing.md

> Escopo: complementa STANDARDS.md §19 (Testes) com organização e cobertura. Convenções de nomenclatura de pastas seguem STANDARDS.md §5 (`Tests/` por módulo) — não repetidas aqui.

## Tipos de teste

- **Testes unitários** — cobrem Domain e Application isoladamente, sem dependências externas (banco, rede). São a maior parte da suíte de testes de um módulo.
- **Testes de integração** — validam Infrastructure real (EF Core, banco de dados, chamadas a Contracts de outros módulos) em ambiente controlado.
- **Testes de arquitetura** — validam automaticamente as regras de dependência entre camadas definidas em ARCHITECTURE.md §8-9 (ex.: Domain não referencia Application; nenhum módulo acessa Infrastructure de outro módulo). Recomenda-se adotar ferramentas de teste de arquitetura (ex.: bibliotecas de análise de dependências entre assemblies) à medida que os módulos forem criados — nenhuma ferramenta específica está adotada ainda no projeto.

## Prioridade de cobertura

Segue a prioridade já definida em STANDARDS.md §19 (Application, Domain, Integration, End-to-End). Como meta prática:

- Domain e Application: cobertura alta é esperada, dado que concentram regra de negócio e são os pontos de maior retorno por teste.
- Infrastructure e Api: cobertura via testes de integração e smoke test (ver WORKFLOW.md §11), com menor densidade de testes unitários.

Não há um número fixo de cobertura mínima definido para o projeto; a prioridade por camada acima é o critério até que uma meta numérica seja formalmente adotada via ADR.

## Organização

Cada módulo possui sua própria pasta `Tests/` (STANDARDS.md §5), espelhando a estrutura de Domain/Application/Infrastructure/Api sendo testada. Nomeação de classes e métodos de teste segue as convenções gerais de STANDARDS.md §4, com o nome do método de teste descrevendo comportamento esperado (ex.: `CreatePurchaseOrder_ComDadosValidos_DeveRetornarSucesso`).

## Sequência obrigatória

A sequência de execução de testes dentro do fluxo de entrega (Build → Testes Unitários → Integração → Smoke Test → Aceite) já está definida em WORKFLOW.md §11 e não é repetida aqui. Nenhuma etapa dessa sequência pode ser ignorada antes de um merge.

Ver também [definition-of-done.md](./definition-of-done.md) — "testes passam" é um item obrigatório do DoD canônico.
