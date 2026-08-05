# Comando `[atualizar tudo]`

Quando o usuário enviar o comando exato `[atualizar tudo]`, o agente deve atualizar a documentação viva do projeto com base no estado atual e evidências verificáveis.

## Escopo mínimo

1. Verificar o estado do repositório, a sprint atual, backlog, Work Orders, histórico e validações aplicáveis.
2. Atualizar os documentos canônicos afetados em `.ai/` e os artefatos publicados em `docs/`, sem editar manualmente arquivos declarados como gerados automaticamente quando houver gerador ou fluxo de publicação disponível.
3. Atualizar o relatório gerencial e seus artefatos de processo em `resources/presentations/`, quando o estado executivo tiver mudado.
4. Preservar a taxonomia de evidências: Implementado, Parcial, Planejado, Não iniciado e Não comprovado.
5. Executar as validações proporcionais à alteração e registrar limites, divergências ou itens que exijam aprovação humana.

## Limites

- O comando não autoriza criar ou iniciar Work Orders, modificar escopo, roadmap ou decisões de produto sem aprovação explícita.
- A atualização deve refletir fatos comprováveis; não pode transformar planejamento em entrega.
