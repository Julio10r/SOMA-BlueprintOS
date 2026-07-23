# context/git-workflow.md

> Escopo: fluxo local de Git. Convenções de branch e formato de commit já estão definidas em STANDARDS.md §20-22 — não repetidas aqui.

## Fluxo local padrão

O fluxo oficial para qualquer alteração local neste projeto é:

```bash
git add .
git commit
git push
```

Isso deve ocorrer sempre em uma branch de feature/correção (`feature/`, `bugfix/`, `hotfix/`, `release/` — ver STANDARDS.md §20), nunca diretamente na `main`. Ou seja, o fluxo `add → commit → push` descreve a mecânica local do dia a dia, e a regra "nunca desenvolver diretamente na main" (STANDARDS.md §20) continua sendo o limite dentro do qual esse fluxo acontece.

## Sequência completa esperada

1. Criar ou atualizar a branch de trabalho a partir da `main`, com o prefixo correto.
2. `git add .`
3. `git commit` com mensagem no formato `tipo: descrição` (STANDARDS.md §21).
4. `git push` para a branch remota.
5. Abrir Pull Request seguindo o checklist de STANDARDS.md §22.

Nenhuma etapa deste fluxo substitui a revisão e aprovação exigidas em WORKFLOW.md §10 e §12 antes do merge.
