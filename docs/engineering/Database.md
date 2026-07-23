# Banco de Dados

> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.

- **Versão:** 1.0.0
- **Gerado em:** 2026-07-23 03:43:24 UTC
- **Última atualização:** 2026-07-23

---

## Banco de dados

Nenhum schema de banco de dados definido até o momento.

O backend ainda não possui nenhum `DbContext` (EF Core) nem entidades persistentes.
A persistência atual dos módulos existentes (ex.: `Documentation`, `Knowledge`) é feita
em memória (`InMemoryDocumentationRepository`) ou em arquivos Markdown (ADRs, changelog),
adequado ao escopo das sprints entregues até aqui. Este documento será atualizado assim
que um `DbContext` real for introduzido no projeto.
