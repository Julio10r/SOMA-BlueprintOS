# context/observability.md

> Escopo: métricas, logs, tracing e auditabilidade de eventos, aplicáveis tanto a módulos de negócio quanto à execução de agentes (Runtime). Complementa STANDARDS.md §12 (ILogger) — não repetido aqui.

## Logs

STANDARDS.md §12 já exige uso obrigatório de `ILogger` (nunca `Console.WriteLine`). Como complemento:

- Logs devem ser **estruturados** (campos nomeados, não apenas mensagens de texto livre), permitindo consulta e agregação posterior.
- Todo log relevante a uma execução de agente ou tarefa deve incluir o identificador da tarefa/Task Packet correspondente (ver WORKFLOW.md §7).
- Nunca registrar dados pessoais sensíveis em log sem necessidade (ver [security.md](./security.md), princípio de minimização).

## Métricas

Cada módulo deve expor métricas mínimas de operação: contagem de requisições, taxa de erro, latência de operações críticas. Para o módulo Agents/Runtime, adicionalmente:

- número de execuções de agente por tipo/agente;
- taxa de sucesso/falha por agente;
- tempo de execução por tarefa.

## Tracing

Operações que atravessam múltiplos módulos (via Contracts) ou múltiplos agentes (via Tasks) devem propagar um **correlation ID** único por Task Packet, permitindo reconstruir o caminho completo de uma requisição através do sistema — desde a entrada até o resultado final, mesmo quando o caminho envolve mais de um módulo ou mais de um agente.

Tracing distribuído é especialmente importante no BlueprintOS porque a arquitetura de Modular Monolith está desenhada para eventualmente permitir extração em microsserviços (ARCHITECTURE.md §13) — correlation IDs consistentes desde já evitam retrabalho nessa transição.

## Eventos

Domain Events já são um padrão obrigatório (ARCHITECTURE.md §11), usados para propagar efeitos de negócio. Para observabilidade, todo Domain Event relevante deve também:

- ser registrado em log estruturado no momento em que é publicado e quando é tratado;
- carregar timestamp e identificador de correlação, permitindo auditoria de "o que aconteceu e quando" independentemente do uso de negócio do evento.

Isto é, Domain Events servem tanto à lógica de negócio quanto à trilha de auditoria — as duas finalidades não são excludentes.

Ver também [security.md](./security.md) para auditoria de acesso a dados pessoais.
