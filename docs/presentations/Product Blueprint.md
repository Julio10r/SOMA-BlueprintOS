# Product Blueprint — Conteúdo da Apresentação

> Conteúdo e narrativa da apresentação, a partir de `docs/Product Blueprint.md` (fonte aprovada, público Cliente).
> Público: Cliente, Stakeholders de Compras, Sponsors do +Compras.
> Fontes: `docs/Product Blueprint.md`.
> Segue exatamente o mesmo padrão visual e estrutural do `Executive Report.md` — 11 slides, mesma sequência de masters do Design System.

---

## Slide 1 — Capa

**Objetivo:** apresentar o produto e situar a audiência antes de qualquer detalhe.

**Conteúdo:**
- +Compras
- Product Blueprint
- O produto de Inteligência Artificial para Compras, construído sobre o BlueprintOS

**Speaker Notes:**
O +Compras é o primeiro produto construído sobre a plataforma BlueprintOS — este documento explica o que ele é, que problema resolve e para onde vai.

---

## Slide 2 — Visão Geral

**Objetivo:** entregar, em poucos segundos, o essencial do produto.

**Conteúdo:**
- O que é: produto de IA para automação e apoio à área de Compras, combinando agentes especializados, memória de negociação e conhecimento organizacional.
- Problema que resolve: torna explícito e reutilizável o conhecimento de negociação hoje disperso e dependente de cada comprador.
- Como funciona: um Agente de IA (Comprador Sênior) consome essa memória e conhecimento para recomendar estratégias de negociação.

**Speaker Notes:**
O produto apoia o comprador — não o substitui. Essa é a mensagem central de todo o material.

---

## Slide 3 — Problema Resolvido

**Objetivo:** justificar por que o +Compras existe.

**Conteúdo:**
- Áreas de Compras lidam com grande volume de negociações e histórico disperso de fornecedores.
- Decisões dependem fortemente da experiência individual do comprador.
- O conhecimento organizacional se perde ou não é reutilizado entre negociações.

**Speaker Notes:**
O +Compras existe para tornar esse conhecimento explícito, consistente e reutilizável.

---

## Slide 4 — Objetivos

**Objetivo:** apresentar os compromissos que orientam o produto.

**Conteúdo:**
- Apoiar decisões: recomendações de negociação baseadas em histórico real de fornecedores.
- Reter conhecimento: políticas, processos e aprendizados organizacionais preservados e reutilizáveis.
- Reduzir tempo: menos tempo reunindo informação dispersa antes de uma negociação.

**Speaker Notes:**
O produto evolui de forma incremental, sempre resolvendo um problema real do comprador.

---

## Slide 5 — Arquitetura Simplificada

**Objetivo:** situar, sem jargão técnico, como o produto funciona.

**Conteúdo:**
- Comprador → Agente de IA (Comprador Sênior) → Recomendação de Estratégia de Negociação.
- A recomendação é alimentada pela Memória de Negociação (histórico de fornecedores, score) e pela Base de Conhecimento Organizacional.

**Speaker Notes:**
O +Compras roda sobre o BlueprintOS, que fornece o runtime de agentes, a memória e o motor de conhecimento como serviços compartilhados — reutilizáveis por futuros produtos da SOMA.

---

## Slide 6 — Funcionalidades

**Objetivo:** separar, com clareza, o que já existe do que está planejado.

**Conteúdo:**
- Implementadas hoje: motor de estratégia de negociação, memória de negociação, Agente Comprador Sênior, base de conhecimento organizacional.
- Planejadas: automação de compras de ponta a ponta (módulo Procurement), integração com ERP, portal para o comprador.

**Speaker Notes:**
A fundação de IA já está construída; o portal e a automação de processo são os próximos módulos.

---

## Slide 7 — Jornada do Usuário

**Objetivo:** mostrar como o comprador usa o produto, do início ao fim.

**Conteúdo:**
- Abrir negociação com um fornecedor.
- Consultar histórico do fornecedor (preços, relacionamento, negociações anteriores).
- Receber recomendação de estratégia, com justificativa.
- Registrar o resultado, alimentando a memória para as próximas negociações.

**Speaker Notes:**
Hoje, a recuperação de histórico e a recomendação já existem como capacidade de IA; a abertura da negociação e o registro do resultado dependem do portal, ainda no roadmap.

---

## Slide 8 — Roadmap

**Objetivo:** apresentar a evolução planejada do produto.

**Conteúdo:**
- Fundação — base arquitetural e de documentação da plataforma (em andamento).
- Conhecimento e Automação — conhecimento organizacional e agentes de IA (iniciada); módulo de Procurement e integração com ERPs.
- Escala — painéis de indicadores e operação em escala.

**Speaker Notes:**
Da fundação técnica à automação completa, cada fase entrega a base para a seguinte.

---

## Slide 9 — Benefícios

**Objetivo:** mostrar o impacto esperado para o cliente.

**Conteúdo:**
- Decisões de negociação apoiadas por dados reais, não apenas por memória individual.
- Conhecimento organizacional preservado e reutilizável entre compradores.
- Plataforma (BlueprintOS) pensada para crescer com o produto, sem retrabalho arquitetural.

**Speaker Notes:**
Cada benefício já tem evidência concreta hoje, na capacidade de IA em produção.

---

## Slide 10 — Perguntas Frequentes

**Objetivo:** responder, de forma direta, as dúvidas mais comuns do cliente.

**Conteúdo:**
- O +Compras apoia a decisão do comprador — não a substitui.
- O produto está em construção: as capacidades de IA já existem; portal e automação de processo seguem no roadmap.
- Segurança de dados de fornecedores é requisito de escopo da plataforma (autenticação, autorização, LGPD), no módulo de Identidade planejado.

**Speaker Notes:**
Transparência sobre o estágio atual gera confiança — nada aqui é prometido além do que está documentado no roadmap.

---

## Slide 11 — Conclusão

**Objetivo:** encerrar reforçando o valor já entregue e a visão de futuro.

**Conteúdo:**
- O +Compras já apoia o comprador sênior hoje, com motor de negociação e memória de fornecedores.
- É o primeiro produto construído sobre o BlueprintOS — a base para os próximos módulos e produtos de IA da SOMA.

**Speaker Notes:**
Termina em fato, não em promessa: o valor de hoje é a prova da visão de amanhã.
