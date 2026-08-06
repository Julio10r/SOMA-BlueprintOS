# DOCUMENTATION STRATEGY

Toda documentação deve responder às necessidades de um público específico.

Existem seis documentos oficiais: os três históricos (Executive Report, Product Blueprint, Documentação Técnica) e três criados na preparação da Onda 1 do MVP 1.0 sob `docs/product/` (+Compras Funcional, +Compras UX, +Compras Data Model).

---

# Executive Report

Público

Diretoria

Objetivo

Mostrar a evolução semanal do projeto e o roadmap do MVP 1.0 por Ondas (ver `.ai/ROADMAP.md`).

Atualização

Toda sprint.

---

# Product Blueprint

Público

Cliente

Objetivo

Explicar o +Compras.

Atualização

Sempre que houver evolução funcional.

---

# +Compras Funcional

Público

Negócio, Produto, QA e Desenvolvimento

Objetivo

Especificação funcional oficial do sistema. Descreve **o que o sistema faz**. Toda funcionalidade nasce primeiro neste documento, antes de qualquer implementação. Não é documentação técnica nem de arquitetura — é documentação de negócio, referência única compartilhada entre negócio, produto, QA e desenvolvimento.

Atualização

Sempre que uma funcionalidade for especificada ou evoluir — precede a criação do Mock navegável (ver `.ai/ROADMAP.md`, estratégia Frontend First).

Arquivo: [`docs/product/ComprasFuncional.md`](../docs/product/ComprasFuncional.md). Estrutura criada na preparação da Onda 1, com índice inicial por módulo e placeholders — nenhum conteúdo funcional real foi escrito ainda; cada seção segue o [template oficial de tela](../docs/product/templates/TelaTemplate.md).

---

# +Compras UX

Público

Design, Produto, QA e Desenvolvimento

Objetivo

Wireframes, navegação, componentes, comportamento visual e jornada do usuário. Descreve **como o usuário utiliza o sistema** — distinto de +Compras Funcional (o que o sistema faz) e da Arquitetura Técnica (como o sistema foi construído).

Atualização

Sempre que a UX de uma funcionalidade for definida ou evoluir — insumo direto do Mock navegável (ver `.ai/ROADMAP.md`, estratégia Frontend First).

Arquivo: [`docs/product/ComprasUX.md`](../docs/product/ComprasUX.md). Mesmo tratamento do +Compras Funcional — estrutura e índice criados, sem conteúdo de tela; segue o [template UX oficial](../docs/product/templates/UXTemplate.md).

---

# +Compras Data Model

Público

Desenvolvimento, Arquitetura e QA

Objetivo

Acompanhar a evolução funcional do modelo de dados do +Compras, módulo a módulo: entidade, tabela +Compras, tabela ERP, relacionamentos, integrações e observações — sem inventar tabelas antes de sua especificação real.

Atualização

Sempre que um módulo tiver seu modelo de dados definido durante a implementação de uma Onda.

Arquivo: [`docs/product/ComprasDataModel.md`](../docs/product/ComprasDataModel.md). Estrutura criada na preparação da Onda 1, seguindo o [template de modelo de dados oficial](../docs/product/templates/DataModelTemplate.md).

---

# Documentação Técnica (docs/README.md)

Público

Desenvolvedores

Objetivo

Facilitar onboarding e desenvolvimento. A partir da ADR-0019, a documentação técnica não vive mais em um único Engineering Handbook — está organizada por domínio em `docs/` (arquitetura, backend, frontend, banco, agentes, operações, testes, releases), indexada por `docs/README.md`.

Atualização

Contínua, por domínio — ver a regra de atualização em `docs/README.md`.

---

# Regras

Não duplicar conteúdo.

Não criar documentação sem propósito.

Preferir documentos curtos.

Utilizar diagramas apenas quando agregarem valor.

Cada documento deve responder às perguntas do seu público.