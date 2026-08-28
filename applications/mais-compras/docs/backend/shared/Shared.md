# Shared

`BlueprintOS.Shared` reúne utilitários e tipos compartilhados entre camadas e módulos — não contém regra de negócio de nenhum domínio específico.

- **Result Pattern** — tratamento de erro por retorno tipado em vez de exceções para fluxos de negócio esperados (ADR-0004); usado em toda a camada Application. Nunca usar `throw Exception()` genérico para fluxos de negócio previsíveis.

Convenções gerais de código válidas para todos os módulos (ver [`.ai/STANDARDS.md`](../../../.ai/STANDARDS.md) para o guia completo):

| Item | Convenção |
|---|---|
| Idioma do código | Inglês |
| Idioma da documentação | Português |
| Classes / Métodos / Propriedades | PascalCase |
| Interfaces | Prefixo `I` |
| Campos privados | `_camelCase` |
| Variáveis | camelCase |
| Comentários | Evitar; código deve ser autoexplicativo |
| Tratamento de erro | Result Pattern; nunca `throw Exception()` genérico |
| Logging | `ILogger`; nunca `Console.WriteLine()` |
| Tamanho de método | Até ~30 linhas |
| Tamanho de classe | Até ~300 linhas |

Proibido: `#region`, Service Locator, classes estáticas para regra de negócio, SQL concatenado, dependências cíclicas.
