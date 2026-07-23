# context/coding-standards.md

> Escopo: complemento a [STANDARDS.md](../STANDARDS.md), que é o documento canônico de padrões de engenharia. Este arquivo adiciona os princípios SOLID e orientações sobre comentários de documentação, que STANDARDS.md aplica implicitamente mas não nomeia.

## Princípios SOLID

STANDARDS.md já define regras concretas (nomenclatura, pastas, tamanho de classe/método) que são a implementação prática destes princípios. Aqui explicamos o "porquê" por trás delas.

- **SRP (Responsabilidade Única):** cada classe resolve um único motivo de mudança. É a base da regra "uma classe, uma responsabilidade" e do limite de ~300 linhas por classe (STANDARDS.md §5, §9).
- **OCP (Aberto/Fechado):** classes devem ser extensíveis sem modificação, tipicamente via interfaces e composição. Reflete-se na obrigatoriedade de interfaces `I`-prefixadas e injeção de dependência (STANDARDS.md §4, §14).
- **LSP (Substituição de Liskov):** qualquer implementação de uma interface deve poder substituir outra sem quebrar o contrato. Relevante especialmente para implementações de Contracts entre módulos (ARCHITECTURE.md §9).
- **ISP (Segregação de Interfaces):** interfaces devem ser pequenas e específicas ao consumidor, evitando interfaces "gigantes" — equivalente, em nível de contrato, à proibição de God Classes (STANDARDS.md §24).
- **DIP (Inversão de Dependência):** camadas de alto nível dependem de abstrações, não de implementações concretas. É exatamente a regra de dependência entre camadas definida em ARCHITECTURE.md §8 (Domain não depende de nada; Application depende de abstrações).

## Comentários de documentação (XML doc comments)

STANDARDS.md §10 desencoraja comentários em geral, priorizando código autoexplicativo. Isso não se aplica à documentação de superfícies públicas. Comentários XML (`///` em C#) são recomendados quando:

- o membro faz parte de um **Contract** consumido por outro módulo (ARCHITECTURE.md §9);
- o membro é parte da **API pública** de uma biblioteca em BuildingBlocks;
- o comportamento não é óbvio a partir da assinatura (ex.: pré-condições, exceções lançadas, efeitos colaterais esperados).

Comentários XML não substituem nomes e abstrações claras — são um complemento para consumidores externos ao módulo, não para lógica interna.
