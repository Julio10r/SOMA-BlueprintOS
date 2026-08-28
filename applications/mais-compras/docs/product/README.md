# Product

## Objetivo

Esta área reúne toda a documentação funcional do sistema +Compras. Ela complementa a Arquitetura Técnica — não descreve implementação, descreve o produto.

## Documentos

### [ComprasFuncional.md](./ComprasFuncional.md)

Especificação funcional oficial do sistema. Descreve:

- regras de negócio;
- funcionalidades;
- fluxos;
- comportamento esperado.

### [ComprasUX.md](./ComprasUX.md)

Documentação de UX. Descreve:

- navegação;
- wireframes;
- componentes;
- experiência do usuário.

### [ComprasDataModel.md](./ComprasDataModel.md)

Acompanhamento funcional do modelo de dados. Descreve:

- entidades;
- relacionamento funcional;
- integração ERP;
- evolução do banco.

### [templates/](./templates/)

Modelos oficiais utilizados durante a especificação das funcionalidades.

## Posicionamento documental

`docs/product/` não substitui `docs/architecture/`, `docs/backend/`, `docs/database/` ou `docs/frontend/`. Ele complementa essas áreas.

Arquitetura responde: "Como o sistema foi construído."

Product responde: "Como o sistema funciona."

## Ciclo oficial

Toda funcionalidade segue obrigatoriamente:

```
+Compras Funcional
  ↓
+Compras UX
  ↓
Mock
  ↓
Blueprint Banco
  ↓
APIs
  ↓
Integrações
  ↓
Implementação
  ↓
Testes
  ↓
Homologação
```

## Regras

- Nenhum conteúdo fictício.
- Toda funcionalidade nasce primeiro nesta área.
- Documentação funcional evolui junto com o produto.
- Arquitetura técnica permanece separada.
- Evitar duplicação de conteúdo.
