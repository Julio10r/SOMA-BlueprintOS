# .empty/ — Quarentena de reorganização física

Esta pasta **não é fonte canônica de nada**. É um contêiner temporário criado
durante a reorganização física do repositório (ver
`docs/repository/RepositoryReorganization-Final.md`) para itens identificados
como obsoletos, duplicados ou saída operacional acumulada, mas que não foram
apagados definitivamente por prudência.

Regras:

- Nada aqui deve ser referenciado por código, scripts, testes, `agent.yaml`
  ou documentação viva. Se você encontrar uma referência ativa a algo dentro
  de `.empty/`, isso é um bug de reorganização — corrija a referência ou
  restaure o item ao seu lugar de origem, não ajuste `.empty/` para acomodar.
- Remoção definitiva de qualquer item aqui exige revisão humana explícita —
  não apague nada desta pasta "de passagem" sem confirmar com o dono da área
  de origem (ver `QUARANTINE_MANIFEST.md` para o item e seu contexto).
- Novos itens só devem ser adicionados aqui via decisão explícita e
  documentada (não é uma lixeira de conveniência do dia a dia).

Ver `.empty/QUARANTINE_MANIFEST.md` para o inventário item a item, com
justificativa, possível substituto e recomendação de segurança de remoção.
