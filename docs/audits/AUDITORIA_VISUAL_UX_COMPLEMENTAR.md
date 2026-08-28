# Auditoria Visual/UX Complementar — +Compras (SOMA BlueprintOS)

Data: 2026-08-19
Autor: auditoria assistida por IA (persona: Senior Product Designer + UX Lead + especialista em Design Systems)
Escopo: exclusivamente design visual, UX, interação, hierarquia, consistência, usabilidade, densidade, responsividade e qualidade percebida. Não repete a auditoria funcional/técnica já registrada em `.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md`.
Método: inspeção visual real via Chrome DevTools (navegação, screenshot, snapshot de árvore de acessibilidade), tenant AZZAS 2154, usuário Julio Cesar, em 1440×900 e 1024×768. Todas as telas listadas no briefing foram abertas de fato no navegador.

---

## 1. Resumo executivo visual

O +Compras hoje é dois produtos visuais coexistindo sob o mesmo header. Um é maduro: a dupla Perfis/Usuários em Administração, com busca, filtro de status, tabela com cabeçalho fixo e padrão de três botões de ação (Visualizar/Editar/Ativar-Inativar) repetido de forma disciplinada. O outro é rascunho: Pedidos, Negociações, Indicadores, Agentes IA e Configurações são telas de um parágrafo e nada mais, sem qualquer elemento de produto (sem tabela vazia, sem CTA, sem estado "em construção" desenhado). Entre esses dois extremos há uma faixa de inconsistência real dentro da própria família "madura": Unidades de Negócio não tem busca nem filtro de status onde Perfis e Usuários têm; e a tela de Filiais apresenta uma tabela com sobreposição de texto visivelmente quebrada (ver Seção 19, VIS-P1-01).

O achado mais grave desta auditoria visual é estrutural, não estético: a tabela de Filiais renderiza 5.000 linhas de uma vez, sem paginação, sem virtualização, sem contagem de resultados. Isso não é um detalhe de polimento — é um problema de usabilidade que impede qualquer varredura (scanability) da tela e provavelmente degrada performance de rolagem em qualquer máquina.

A tela de Fornecedores — o fluxo mais crítico de negócio do produto — está com o card de consulta ocupando uma fração pequena da área útil, com "DETALHES TÉCNICOS" expostos ao usuário final por padrão, e a tela de Review não estava exibindo uma comparação real (a reconsulta externa estava indisponível no momento da auditoria), o que por si só é um estado de produção que precisa ser tratado como estado de UI de primeira classe, e hoje não está desenhado como tal — é texto corrido dentro do card, sem badge de estado visualmente destacado além de uma linha em caixa alta.

## 2. Primeira impressão do produto

Entrando pelo Dashboard, a impressão é de uma ferramenta interna B2B honesta, tipográfica, em preto/branco/verde com acentos mínimos — nada de gradientes, nada de ilustração, hierarquia baseada em peso de fonte e caixa alta para rótulos de seção. Isso comunica seriedade e é adequado ao contexto corporativo. O problema é que essa linguagem é aplicada com rigor desigual: no Dashboard e em Perfis/Usuários ela está bem executada; em Filiais ela quebra (texto sobreposto); nas telas de Compras (Pedidos/Negociações/Indicadores) ela simplesmente não existe além do H1 e uma frase.

O primeiro card do Dashboard ("FORNECEDORES CADASTRADOS: 3") convive com dois cards que mostram "--" e a palavra "Demo" como se fosse um valor de dado — isso é um indicador de placeholder vazando para produção sem tratamento visual de "métrica não disponível" (ex.: texto cinza "Ainda sem dados" versus um traço duplo que parece erro de carregamento).

## 3. Avaliação do Shell

Header: banda branca fixa, "AZZAS 2154 | +Compras" à esquerda, avatar circular preto com iniciais + nome + chevron à direita. Sóbrio e funcional. O menu do usuário (S14), ao abrir, mostra **apenas um item: "Sair"** — nenhuma opção de perfil, preferências, tenant switch ou ajuda. Isso é coerente com o estágio do produto, mas visualmente o menu parece "vazio demais" para o afordance de um chevron de menu (o usuário espera mais que uma opção).

Sidebar: 8 grupos (INÍCIO, FORNECEDORES, COMPRAS, GOVERNANÇA DE COMPRAS, ADMINISTRAÇÃO, AGENTES IA, sem rótulo de grupo, e Configurações solto no rodapé sem rótulo de grupo e sem separador visual forte — há apenas uma linha fina). ADMINISTRAÇÃO concentra 11 itens (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Unidades de Negócio, Configuração do ERP, Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações, Monitoramento) sob um único rótulo de grupo sem subagrupamento visual — a sidebar em 1440px chega a ~940px de altura de itens (ver S01/S04), forçando rolagem da própria navegação em telas mais baixas que 900px, o que é confirmado no teste 1024×768: a sidebar rola e os últimos itens de Administração (Feature Flags, Configuração de Notificações, Monitoramento) ficam fora do viewport inicial sem nenhuma pista visual de "há mais itens abaixo" (sem sombra de fade, sem indicador de scroll).

Item ativo: fundo preto sólido, texto branco, ícone branco — contraste forte e claro, sem ambiguidade (visto em Dashboard, Fornecedores, Perfis, Filiais). Ícones de grupo são consistentes em peso de linha (outline, ~20px) entre itens do mesmo grupo, mas há mistura de metáforas entre grupos (ex.: ícone de "camadas" para Unidades de Alocação vs. ícone de "mala" para Unidades de Negócio) sem um padrão de família visual que sinalize "isto tudo é Administração".

Rodapé "Configurações" fica fora de qualquer grupo rotulado, sozinho, separado por uma linha — na prática funciona como um 9º grupo implícito sem cabeçalho, quebrando o padrão "toda entrada pertence a uma seção maiúscula" que rege o resto da sidebar.

## 4. Avaliação tela por tela

- **Dashboard** (S01): 4 KPI cards de mesma largura em grid única; 3 primeiros bons, 4º ("ALERTAS DE INTEGRAÇÃO: 0 / Nenhum alerta") redundante em conteúdo (o número e o texto dizem a mesma coisa duas vezes). Lista de "Cadastros recentes" usa cards largos (~1400px) com apenas 4 pares rótulo/valor dentro — desperdício de altura vertical por card (~230px de card para ~90px de conteúdo real).
- **Fornecedores inicial** (S02): H1 "Fornecedores" sem subtítulo de contexto de produto (a frase "Informe um CNPJ..." está estilizada como bullet de lista, não como descrição de página — inconsistente com outras telas administrativas que usam um parágrafo comum). Card de consulta ocupa uma fração pequena da largura útil da página (a área ao lado e abaixo do card fica vazia). "DETALHES TÉCNICOS" aparece como acordeão de mesma largura da página, visível a qualquer usuário autenticado, sem distinção de nível de acesso.
- **Fornecedores Review** (S03/S03b): estado observado foi "reconsulta indisponível" — a mensagem "RECONSULTA INDISPONÍVEL" aparece como rótulo pequeno em caixa alta sem cor de alerta (nem amarelo, nem laranja), seguida de texto explicativo longo dentro do corpo do card. Os botões Aceitar/Rejeitar aparecem desabilitados, cinza-claro sobre cinza-claro, com baixo contraste — comunicam "desabilitado" mas não comunicam "por quê" no próprio botão (só no texto acima). Acordeões de IDENTIFICAÇÃO / CNAE PRINCIPAL / ENDEREÇO / CONTATO funcionam, mas o card inteiro está claramente desenhado para o caminho feliz de divergência (com colunas "valor atual vs. valor novo"), e no caminho de falha de integração ele degrada para uma lista simples de campos — ou seja, o layout de comparação real (que é o propósito central da tela) não pôde ser confirmado neste teste porque a fonte externa estava indisponível; isso é registrado como não confirmado, não como ausente.
- **Perfis** (S04): tela de referência positiva da família Administração — H1, subtítulo de uma frase, card com header "Perfis cadastrados" + CTA "Novo perfil" no canto superior direito, busca + filtro de Status alinhados, tabela com 6 colunas. Altura de linha inconsistente: linhas com descrição longa (4 linhas de texto) ficam com ~220px de altura, linhas sem descrição ficam com ~90px — a grade perde a régua horizontal e a leitura vertical da coluna "AÇÕES" fica desalinhada linha a linha (visível em S04).
- **Usuários** (S05): mesmo esqueleto de Perfis, consistente.
- **Novo usuário** (S13): formulário de página inteira (não modal) com lista de ~19 checkboxes de Perfis e depois ~16 checkboxes de Centros de Custo em coluna única, sem colunas múltiplas, sem busca dentro da lista, sem contador de selecionados. Um aviso de "Filiais: vínculo será preparado em etapa futura" aparece como se fosse uma seção funcional (com título FILIAIS igual às outras), mas é só um aviso — visualmente indistinguível de uma seção real até se ler o texto.
- **Filiais** (S06): mesmo esqueleto de Perfis, mas com um defeito visual concreto: a coluna "DESCRIÇÃO +COMPRAS" mostra fragmentos de texto sobrepostos ("ição" flutuando acima de "+Compras" em cada linha) — indica um elemento de texto (provavelmente um placeholder ou tooltip) desalinhado atrás/sobre o conteúdo da célula. Ver Seção 19, VIS-P1-01. Adicionalmente, a tabela carrega 5.000 linhas sem paginação (Seção 19, VIS-P0-01).
- **Centros de Custo, Unidades de Alocação** (S07/S08): mesmo esqueleto validado por snapshot; consistentes.
- **Unidades de Negócio** (S08b): quebra o padrão da família — não há campo "Pesquisar" nem filtro de "Status"; a tabela tem apenas 4 colunas (Nome, Slug, Status, Ações) e 2 linhas de dados; a coluna de ações tem só 2 botões (Editar/Inativar) em vez de 3 (falta "Visualizar"). Isso é inconsistência estrutural, não apenas estética.
- **Regras de Workflow / Alçadas de Aprovação / Regras Orçamentárias** (S09/S09b/S09c): capturadas; layout de card único de "em construção/lista simples" — sem uma linguagem visual compartilhada explícita (nenhum selo, cor ou ícone de "grupo Governança") que diferencie essas três telas de uma tela genérica de Administração.
- **Configuração do ERP** (S10): capturada.
- **Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações, Monitoramento** (S15–S19): capturadas.
- **Pedidos, Negociações, Indicadores, Agentes IA, Configurações** (S20–S24): capturadas — todas seguem o padrão H1 + uma frase, sem qualquer elemento de produto (tabela, card, CTA, empty state ilustrado). Visualmente indistinguíveis entre si além do texto.

## 5. Avaliação especial de Fornecedores

Tela inicial: H1 "Fornecedores" sem descrição de propósito de produto acima do card (o texto presente é instrução de preenchimento, não uma descrição de tela, diferente do padrão "H1 + frase de propósito" usado em Administração). O card de consulta mede visualmente cerca de 45% da largura útil do conteúdo em 1440px — o restante da página fica vazio, sem uma segunda coluna de contexto (histórico de consultas recentes, atalhos, ou o próprio bloco "Cadastros recentes" que já existe no Dashboard e poderia ter sido reaproveitado aqui). "DETALHES TÉCNICOS" é um acordeão de largura total, visualmente idêntico em peso a um elemento de produto normal — não há badge "modo avançado" ou ícone de engrenagem que sinalize que é conteúdo de depuração; qualquer usuário de negócio clicará nele por curiosidade.

Tela de Review: a arquitetura de informação (o que veio da consulta vs. o que já existe vs. o que será gravado vs. o que é editável) não pôde ser confirmada com um caso de divergência real, porque o ambiente de teste retornou "reconsulta externa indisponível" tanto para um CNPJ novo quanto para o único caminho testado. O que foi possível confirmar visualmente: o card usa acordeões por seção de dado (Identificação, CNAE, Endereço, Contato) com um único conjunto de valores (não duas colunas lado a lado de "atual" vs. "novo"), e os botões de decisão (Aceitar/Rejeitar) ficam ao final do card, desabilitados, com contraste baixo. Não é possível avaliar aqui a legibilidade do comparativo lado a lado porque essa camada visual não foi renderizada durante o teste — registrado como **não confirmado**, não como ausente.

## 6. Avaliação da família Administração

Comparação lado a lado confirma que Perfis, Usuários, Filiais e Centros de Custo/Unidades de Alocação compartilham o mesmo componente de "página de listagem" (header com breadcrumb textual em caixa alta, H1, subtítulo de uma frase, card com título de card + CTA primário no canto superior direito, linha de busca + filtro, tabela com cabeçalho em caixa alta e 3 botões de ação outline). Unidades de Negócio é a exceção: sem busca, sem filtro, sem terceiro botão de ação — parece ter sido construída antes do padrão se consolidar, ou deliberadamente simplificada por ter poucos registros, mas visualmente isso não é comunicado (nada avisa "esta lista não pode crescer, por isso não tem busca").

Modais/formulários dentro da família também não são uniformes: o cadastro de usuário (S13) é página inteira com lista longa de checkboxes de seleção múltipla sem paginação nem busca interna — em um tenant com 15 Centros de Custo isso já ocupa uma tela cheia de rolagem; com Filiais tendo 5.000 registros, se o mesmo padrão de checkbox único fosse usado ali, seria inviável.

## 7. Avaliação de Governança

Regras de Workflow, Alçadas de Aprovação e Regras Orçamentárias estão no mesmo grupo de sidebar ("GOVERNANÇA DE COMPRAS") mas, nas telas capturadas, não há nenhum elemento visual comum (cor de destaque, ícone de cabeçalho, selo) que sinalize ao usuário que essas três telas formam um conjunto conceitual distinto das telas de Administração pura. A única coesão hoje é a rotulagem do grupo na sidebar — o conteúdo de cada tela, quando aberto, parece indistinguível de uma tela genérica de Administração.

## 8. Avaliação de Compras

Pedidos, Negociações e Indicadores são telas de placeholder: H1 + uma frase, sem tabela vazia desenhada, sem CTA "Criar pedido", sem qualquer estado ilustrado de "módulo em construção". Para um módulo que dá nome ao produto ("+Compras"), a ausência total de qualquer elemento de produto nessas três telas é a maior lacuna de percepção de maturidade do sistema.

## 9. Avaliação de Configurações

A tela "Configurações" (rodapé da sidebar, fora de qualquer grupo rotulado) segue o mesmo padrão minimalista de placeholder das telas de Compras — H1 e frase, nada mais. Como é a única entrada de nível superior sem grupo, e também é a única tela de conteúdo mínimo fora do bloco "ainda não implementado" de Compras/Agentes IA, sua função real (o que ela deveria configurar, dado que já existem Parâmetros, Feature Flags e Configuração de Notificações dentro de Administração) não é comunicada visualmente — o usuário não tem como saber se "Configurações" é diferente de "Parâmetros" sem abrir as duas.

## 10. Hierarquia visual

Nas telas maduras (Dashboard, Perfis, Usuários, Filiais), a ordem de leitura funciona: breadcrumb pequeno → H1 grande em negrito → subtítulo cinza → card de ação. Isso corresponde à tarefa do usuário (entender onde está, depois agir). Nas telas de placeholder, a hierarquia "para" no H1 + frase — não há nada abaixo para guiar o olhar, o que é coerente com a ausência de conteúdo, mas produz uma sensação de tela quebrada/incompleta em vez de "em construção intencional".

Na tela de Review de Fornecedores, a hierarquia é ambígua: o aviso de estado ("RECONSULTA INDISPONÍVEL") tem o mesmo peso visual (caixa alta, cinza) que rótulos de seção como "IDENTIFICAÇÃO" — o usuário precisa ler o texto corrido para entender que aquele rótulo é um estado de erro/degradação, não uma seção de dado.

## 11. Aproveitamento do espaço

O padrão recorrente em 1440px é: sidebar fixa (~380px), conteúdo com margem generosa, e dentro do conteúdo, cards que não usam a largura disponível de forma proporcional à quantidade de informação. Exemplos concretos:
- Fornecedores inicial: card de ~480px dentro de uma área útil de ~1100px (~44% de ocupação horizontal), com o resto da página em branco.
- Dashboard "Cadastros recentes": cards de largura total (~1400px) contendo 4 pares rótulo/valor que ocupam menos de 1/6 da altura do card, com muito espaço em branco lateral entre os 4 blocos internos.
- Perfis/Usuários/Filiais: a tabela usa a largura total, mas a coluna "AÇÕES" reserva ~420px (3 botões de ~130px) para toda linha, mesmo quando duas colunas de dados (ex.: "Descrição" em Filiais) ficam comprimidas e truncadas ao lado.

## 12. Botões e ações

Padrão dominante: CTA primário = preto sólido, texto branco, sem ícone (ex.: "Novo perfil", "Consultar CNPJ", "Salvar"). Ações de tabela = outline cinza-claro, mesmo peso visual entre si (Visualizar/Editar/Ativar-Inativar), sem hierarquia entre elas — "Inativar" (ação com consequência) tem exatamente o mesmo estilo visual que "Visualizar" (ação neutra), sem cor de alerta, sem agrupamento visual que separe leitura de escrita de estado. Em Filiais, o texto do botão de estado é mais longo ("Inativar no +Compras" em vez de "Inativar"), quebrando o alinhamento horizontal da coluna de ações em relação às outras telas da mesma família. Não há, em nenhuma tela observada, um botão com estilo "destrutivo" (vermelho) — mesmo ações de inativação usam o outline neutro, o que é discutível dado que a auditoria funcional já registrou exclusão física real em Parâmetros (fora do escopo visual, mas relevante como referência de risco).

## 13. Formulários

O formulário de "Novo usuário" (S13) é o mais representativo capturado: campos "Nome" e "E-mail" com label acima, borda cinza fina, altura padrão (~40px) — coerentes com os inputs de busca das telas de listagem. O problema visual está nas listas de seleção: 19 checkboxes de Perfis e ~16 de Centros de Custo em coluna única, sem agrupamento em grade de 2–3 colunas, sem busca/filtro interno, sem indicação de quantos itens já estão marcados. O aviso "Filiais... etapa futura" reaproveita o mesmo componente de "cartão de seção" (título em caixa alta) usado pelas seções funcionais acima, tornando-se visualmente indistinguível de uma seção real até a leitura do texto — isso é hierarquia de informação ausente, não apenas estética.

## 14. Tabelas

Cabeçalho consistente (caixa alta, cinza, fundo levemente diferenciado) nas telas da família Administração. Densidade inconsistente: linhas sem descrição longa ficam compactas (~90px), linhas com descrição de 4 linhas ficam ~220px — a grade não tem altura de linha fixa, o que quebra a varredura vertical (o olho não consegue "escanear" a coluna Status ou Ações em linha reta). Em Filiais, além da altura variável, há sobreposição de texto na coluna "Descrição +Compras" (ver Seção 19). A ausência de paginação em Filiais (5.000 linhas) é o problema mais grave de tabela encontrado nesta auditoria. Alinhamento de colunas numéricas/status não segue um padrão de alinhamento à direita para números — "Permissões" e "Usuários vinculados" em Perfis estão alinhados à esquerda, como texto, o que dificulta comparação visual rápida de quantidades entre linhas.

## 15. Cards

Os cards usados em Dashboard, Fornecedores e nas telas de Administração são majoritariamente decorativos no sentido de agrupamento visual (borda + sombra leve + canto arredondado), mas cumprem função semântica real quando contêm um formulário ou uma tabela completa (ex.: card "Consultar fornecedor", card "Perfis cadastrados"). No Dashboard, os cards de "Cadastros recentes" são agrupamento semântico fraco: cada card tem 4 sub-blocos com fundo levemente cinza que parecem "mini-cards dentro do card", criando uma hierarquia de aninhamento (card > sub-card) que não se repete em nenhuma outra tela do produto.

## 16. Modais

Nenhum modal verdadeiro (overlay sobre a tela, com fundo escurecido) foi encontrado nas telas navegadas — os fluxos de criação/edição (Novo perfil, Novo usuário) são páginas de rota própria, não modais. Isso é uma decisão de arquitetura de interação válida, mas contradiz a expectativa do briefing de auditoria de que exista ao menos um modal real; o achado é: **o produto não usa modais para CRUD, usa navegação de página cheia**. Isso deveria ser uma decisão deliberada e documentada de design system, não uma ausência incidental — não há evidência visual de que seja deliberada (nenhuma tela usa disclosure inline ou side panel como alternativa consciente).

## 17. Responsividade

Testado em 1440×900 e 1024×768 em Dashboard, Fornecedores, Perfis, Usuários, Filiais, Centros de Custo, Regras de Workflow e Configuração do ERP.

- A sidebar não colapsa nem se torna gaveta (drawer) em 1024×768 — permanece fixa com a mesma largura (~380px), consumindo ~37% da largura da viewport nessa resolução, contra ~26% em 1440px. Isso reduz proporcionalmente ainda mais o espaço de conteúdo exatamente na resolução em que o conteúdo já é mais apertado.
- Em Filiais e Perfis a 1024px, a tabela não reflui em cards nem prioriza colunas — ela mantém as mesmas 6–7 colunas, forçando compressão de texto e, em Filiais, piora a legibilidade da coluna "Descrição +Compras" já comprometida pela sobreposição de texto descrita na Seção 19.
- Nenhuma tela testada exibiu barra de rolagem horizontal na página inteira (o scroll horizontal, quando necessário, parece ficar contido dentro do card de tabela), o que é o comportamento correto; porém a ausência de qualquer sinalização visual de "esta tabela rola para o lado" (sombra de borda, gradiente) não pôde ser confirmada como presente nas capturas.
- Nenhum breakpoint intermediário de reorganização do grid de KPIs do Dashboard foi observado entre 1440 e 1024 — os 4 cards continuam em uma única linha até 1024px, ficando visualmente mais espremidos, mas sem quebrar layout.

## 18. Consistência cross-screen

| Elemento | Dashboard | Fornecedores | Perfis | Usuários | Filiais | Centros de Custo | Unid. Alocação | Unid. Negócio | Governança (3 telas) | Compras (3 telas) | Config./Agentes IA |
|---|---|---|---|---|---|---|---|---|---|---|---|
| H1 + descrição de propósito | CONSISTENTE | PARCIAL (descrição em formato de bullet) | CONSISTENTE | CONSISTENTE | CONSISTENTE | CONSISTENTE (por amostragem) | CONSISTENTE (por amostragem) | CONSISTENTE | CONSISTENTE | PARCIAL (frase única, sem propósito de produto) | PARCIAL |
| CTA primário no card | — | Consultar CNPJ | Novo perfil | Novo usuário | — (sem CTA de criação, dado mestre do ERP) | não confirmado visualmente em detalhe | não confirmado | Nova unidade de negócio | não confirmado | ausente | ausente |
| Busca | — | CNPJ/CPF (é a própria função) | Sim | Sim | Sim | não confirmado em detalhe | não confirmado | **AUSENTE** | não confirmado | ausente | ausente |
| Filtro de status | — | — | Sim | Sim | Sim | não confirmado | não confirmado | **AUSENTE** | não confirmado | ausente | ausente |
| Tabela com 3 ações (Visualizar/Editar/Ativar-Inativar) | — | — | Sim | Sim | Sim (texto do botão mais longo) | não confirmado | não confirmado | **2 ações apenas** | não confirmado | ausente | ausente |
| Altura de linha de tabela fixa | — | — | INCONSISTENTE (varia com descrição) | não confirmado | INCONSISTENTE | não confirmado | não confirmado | CONSISTENTE (poucas linhas) | — | — | — |
| Modal para criar/editar | — | — | NÃO USA (página própria) | NÃO USA | NÃO USA | não confirmado | não confirmado | não confirmado | não confirmado | — | — |
| Empty state desenhado | Sim (parcial, "Nenhum alerta") | Sim ("Consulta é somente leitura...") | não observado | não observado | não observado | não observado | não observado | não observado | não observado | **AUSENTE** | **AUSENTE** |
| Selo/cor de agrupamento de módulo | — | — | — | — | — | — | — | — | **AUSENTE** | **AUSENTE** | — |

Legenda: "não confirmado" significa que a tela foi aberta e capturada, mas o detalhe específico não foi verificado a fundo nesta rodada (evidência disponível nos screenshots correspondentes para conferência).

## 19. Achados VIS-P0/P1/P2/P3

**VIS-P0-01 — Tabela de Filiais sem paginação, 5.000 linhas renderizadas de uma vez.**
Tela: Filiais, 1440×900 e 1024×768. Descrição: a árvore de acessibilidade da página capturou 5.000 botões "Visualizar" — ou seja, 5.000 linhas de tabela no DOM simultaneamente, sem nenhum controle de paginação, "carregar mais" ou virtualização visível. Impacto: rolagem pesada, tempo de carregamento inicial elevado, impossibilidade prática de varredura visual da lista, risco de a página travar em máquinas mais fracas. Evidência: S06 (screenshot mostra apenas as 5 primeiras linhas visíveis; a contagem de 5.000 veio da árvore de acessibilidade da mesma navegação). Sem referência de design system aplicável — é ausência de padrão de paginação em tabela grande.

**VIS-P1-01 — Sobreposição/corrupção visual de texto na coluna "Descrição +Compras" da tabela de Filiais.**
Tela: Filiais, 1440×900. Descrição: cada linha da coluna mostra um fragmento de texto ("ição") flutuando visualmente acima/atrás do texto "+Compras" da célula abaixo, sugerindo um elemento de posicionamento absoluto mal ajustado (possível tooltip, placeholder de campo editável ou texto de coluna anterior "vazando"). Impacto: reduz confiança na qualidade dos dados exibidos e na maturidade visual da tela; usuário pode interpretar como bug de dado, não de layout. Evidência: S06 (visível nas 5 primeiras linhas capturadas).

**VIS-P1-02 — "DETALHES TÉCNICOS" exposto por padrão a usuários de negócio na tela inicial de Fornecedores.**
Tela: Fornecedores (consulta), 1440×900. Descrição: acordeão de largura total, mesmo peso visual de qualquer outro elemento de card, sem badge de "modo avançado/depuração" nem controle de visibilidade por perfil. Impacto: usuários não técnicos podem abrir e se deparar com informação de payload/API não destinada a eles, e/ou o time de produto perde a chance de usar aquele espaço para conteúdo de negócio. Evidência: S02.

**VIS-P2-01 — Altura de linha inconsistente nas tabelas de Perfis e Filiais.**
Tela: Perfis (S04), Filiais (S06). Descrição: linhas com texto de descrição longo ocupam ~220px, linhas sem descrição ~90px, sem altura mínima/máxima padronizada nem truncamento com "ver mais". Impacto: quebra a varredura vertical da coluna Status/Ações. Evidência: S04, S06.

**VIS-P2-02 — Unidades de Negócio quebra o padrão estrutural da família Administração.**
Tela: Unidades de Negócio. Descrição: ausência de campo de busca, ausência de filtro de Status, e apenas 2 botões de ação (Editar/Inativar) em vez dos 3 padrão (falta Visualizar). Impacto: usuário que aprendeu o padrão nas outras 5 telas de Administração encontra uma tela "incompleta" sem aviso. Evidência: snapshot de acessibilidade da rota `/administracao/unidades-negocio`.

**VIS-P2-03 — Botões de ação de tabela sem hierarquia ou cor de risco.**
Telas: toda a família Administração. Descrição: "Inativar"/"Ativar" usam o mesmo estilo outline neutro de "Visualizar", sem cor de alerta (âmbar/laranja) nem agrupamento separado do botão de leitura. Impacto: reduz a sinalização de que uma ação muda estado de forma consequente. Evidência: S04, S05, S06.

**VIS-P2-04 — Cards de "Cadastros recentes" no Dashboard com aninhamento visual não replicado em nenhuma outra tela.**
Tela: Dashboard. Descrição: cada card externo contém 4 sub-blocos com fundo cinza claro, um padrão de "card dentro de card" único no produto. Impacto: introduz uma linguagem visual nova sem reaproveitamento, aumentando a variedade de padrões que o usuário precisa aprender. Evidência: S01.

**VIS-P2-05 — Governança de Compras sem identidade visual de grupo.**
Telas: Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias. Descrição: nenhuma cor, ícone de cabeçalho ou selo comum diferencia essas telas de uma tela genérica de Administração, apesar de estarem em um grupo de sidebar próprio. Impacto: usuário não percebe visualmente que está numa família conceitual diferente (governança vs. cadastro). Evidência: S09, S09b, S09c.

**VIS-P3-01 — Telas de Compras/Agentes IA/Configurações sem qualquer elemento visual de produto.**
Telas: Pedidos, Negociações, Indicadores, Agentes IA, Configurações. Descrição: H1 + uma frase, sem tabela vazia, sem CTA, sem estado "em construção" desenhado. Impacto: baixo para uso (não há uso ainda), mas alto para percepção de maturidade em qualquer demonstração ou onboarding de novo usuário. Evidência: S20–S24.

**VIS-P3-02 — Card de KPI "Alertas de Integração" redundante em conteúdo.**
Tela: Dashboard. Descrição: valor "0" seguido do texto "Nenhum alerta" — repete a mesma informação duas vezes dentro do mesmo card. Impacto: polimento apenas. Evidência: S01.

**VIS-P3-03 — Alinhamento à esquerda de colunas numéricas em tabelas (Permissões, Usuários vinculados).**
Tela: Perfis. Descrição: valores numéricos alinhados como texto comum, dificultando comparação vertical rápida. Impacto: polimento. Evidência: S04.

## 20. Achados UX-P0/P1/P2/P3

**UX-P1-01 — Estado de degradação da integração externa em Fornecedores não tem tratamento visual de destaque.**
Tela: Fornecedores (Review). Descrição: "RECONSULTA INDISPONÍVEL" aparece como rótulo em caixa alta cinza, com o mesmo peso visual dos rótulos de seção normais (IDENTIFICAÇÃO, ENDEREÇO), obrigando o usuário a ler o parágrafo abaixo para entender que é um estado degradado, não uma seção de dado. Impacto: usuário pode não perceber, em uma leitura rápida, que a divergência não foi calculada e que os botões estão desabilitados por causa disso. Evidência: S03.

**UX-P1-02 — Formulário de "Novo usuário" usa lista de checkbox única e longa sem busca interna, para uma operação que cresce (Centros de Custo).**
Tela: `/administracao/usuarios/novo`. Descrição: 19 checkboxes de Perfis + 16 de Centros de Custo em coluna única, sem filtro, sem contador de selecionados, sem colunas múltiplas. Impacto: usabilidade cai proporcionalmente ao crescimento do tenant; já é um formulário longo com poucos registros de teste. Evidência: S13.

**UX-P2-01 — Sidebar não colapsável nem em drawer, ocupando ~37% da viewport em 1024×768.**
Telas: todas, resolução 1024×768. Descrição: a sidebar mantém largura fixa (~380px) nas duas resoluções testadas, sem opção de recolher. Impacto: em notebooks corporativos comuns (1366×768, 1280×800), a área útil de conteúdo fica desproporcionalmente reduzida. Evidência: S11, S12, S25–S30.

**UX-P2-02 — Ausência de indicação de "há mais itens" quando a sidebar rola em telas baixas.**
Resolução: 1024×768. Descrição: os últimos itens de Administração ficam fora do viewport sem sombra/gradiente de corte. Impacto: usuário pode não descobrir que Feature Flags/Configuração de Notificações/Monitoramento existem, se não rolar a navegação por conta própria. Evidência: inferência a partir de S11/S26 combinada com a altura de sidebar medida em S01/S04.

**UX-P2-03 — "Configurações" no rodapé da sidebar sem grupo, e sem diferenciação clara de propósito frente a Parâmetros/Feature Flags/Configuração de Notificações.**
Tela: Configurações + sidebar geral. Descrição: a existência de 4 telas com nome semanticamente próximo de "configuração" (Configurações, Parâmetros, Feature Flags, Configuração de Notificações) sem nenhuma explicação visual da diferença entre elas. Impacto: usuário não sabe onde procurar algo antes de abrir todas. Evidência: sidebar em S01 e conteúdo mínimo de S24.

**UX-P3-01 — Menu do usuário reduzido a uma única opção ("Sair").**
Tela: header, todas as páginas. Descrição: o afordance de chevron/menu sugere mais opções do que existe. Impacto: baixo, mas gera expectativa não correspondida (perfil, preferências, troca de tenant). Evidência: S14.

## 21. Matriz de notas 0–10

| Área | Nota | Justificativa objetiva |
|---|---|---|
| Shell (header + sidebar) | 6 | Header limpo e funcional; sidebar com hierarquia de grupo/item/ativo bem resolvida visualmente, mas sem colapso responsivo, com 11 itens sob um único rótulo "ADMINISTRAÇÃO" sem subagrupamento, e "Configurações" órfã fora de qualquer grupo. |
| Fornecedores | 5 | Fluxo central do produto com card de consulta subutilizando a largura da página, "Detalhes técnicos" exposto sem controle, e o estado de comparação real (Review com divergência) não pôde ser confirmado por indisponibilidade da integração externa no momento do teste — nota reflete o que foi possível observar, não um veredito sobre a tela completa. |
| Administração | 6 | Perfis/Usuários/Filiais/Centros de Custo/Unidades de Alocação compartilham um padrão sólido e repetido; Unidades de Negócio quebra esse padrão (sem busca, sem filtro, 2 ações em vez de 3); Filiais tem defeito visual concreto (sobreposição de texto) e a tabela sem paginação de 5.000 linhas é o achado mais grave de toda a auditoria.
| Compras (Pedidos/Negociações/Indicadores) | 1 | Três telas do módulo que dá nome ao produto sem nenhum elemento de UI além de H1 e uma frase — nenhuma tabela, nenhum CTA, nenhum estado vazio desenhado. |
| Governança | 4 | Telas abertas e navegáveis, mas sem qualquer elemento visual que comunique que Regras de Workflow, Alçadas de Aprovação e Regras Orçamentárias formam uma família conceitual distinta de Administração. |
| Configurações | 2 | Tela de rodapé com H1 e uma frase, sem diferenciação de propósito frente a outras 3 telas de nome semelhante dentro de Administração. |
| Modais/Formulários | 5 | O único formulário completo observado (Novo usuário) tem inputs de texto corretos e consistentes, mas a lista de seleção múltipla (Perfis, Centros de Custo) não escala e o produto não usa modais reais para CRUD em nenhum fluxo testado. |
| Tabelas | 4 | Cabeçalho e busca/filtro consistentes na família madura, mas altura de linha instável, coluna de ações desproporcionalmente larga, ausência total de paginação em Filiais (5.000 linhas) e defeito de sobreposição de texto na mesma tela. |
| Responsividade | 5 | Nenhum layout quebrado (sem scroll horizontal de página, sem elementos cortados de forma ilegível) nas 8 combinações testadas, mas a sidebar fixa não se adapta a 1024×768, consumindo proporção maior da tela justamente onde o espaço é mais escasso, sem indicação de rolagem oculta. |
| Consistência geral | 5 | Alta consistência dentro da família "madura" de Administração (5 de 6 telas), mas a 6ª (Unidades de Negócio) quebra o padrão, Governança e Compras não compartilham nenhuma assinatura visual com o restante, e um defeito de renderização real foi encontrado em produção de teste (Filiais). |

## 22. Screenshot Manifest

Todos os arquivos estão em `.ai/audit-visual-screenshots/` (caminhos relativos ao repositório):

- S01-dashboard-1440.png — Dashboard, 1440×900, página completa
- S02-fornecedores-inicial-1440.png — Fornecedores, estado inicial de consulta, 1440×900
- S03-fornecedores-review-1440.png — Fornecedores, Review (estado "reconsulta indisponível"), 1440×900
- S03b-fornecedores-review-detalhestecnicos-1440.png — Fornecedores, Review com acordeão Endereço e Detalhes Técnicos expandidos
- S04-perfis-1440.png — Perfis, listagem, 1440×900
- S04b-perfil-detalhe-1440.png — Perfil, tela de detalhe/visualização
- S05-usuarios-1440.png — Usuários, listagem, 1440×900
- S06-filiais-1440.png — Filiais, listagem (evidência de defeito de sobreposição de texto e ausência de paginação), 1440×900
- S07-centros-custo-1440.png — Centros de Custo, 1440×900
- S08-unidades-alocacao-1440.png — Unidades de Alocação, 1440×900
- S08b-unidades-negocio-1440.png — Unidades de Negócio (evidência de quebra de padrão estrutural), 1440×900
- S09-regras-workflow-1440.png — Regras de Workflow, 1440×900
- S09b-alcadas-aprovacao-1440.png — Alçadas de Aprovação, 1440×900
- S09c-regras-orcamentarias-1440.png — Regras Orçamentárias, 1440×900
- S10-configuracao-erp-1440.png — Configuração do ERP, 1440×900
- S11-perfis-1024.png — Perfis, 1024×768
- S12-fornecedores-1024.png — Fornecedores, 1024×768
- S13-usuario-novo-formulario-1440.png — Formulário "Novo usuário" (evidência de lista longa de checkboxes)
- S14-menu-usuario-1440.png — Menu do usuário aberto (apenas "Sair")
- S15-identity-providers-1440.png — Identity Providers, 1440×900
- S16-parametros-1440.png — Parâmetros, 1440×900
- S17-feature-flags-1440.png — Feature Flags, 1440×900
- S18-config-notificacao-1440.png — Configuração de Notificações, 1440×900
- S19-monitoramento-1440.png — Monitoramento, 1440×900
- S20-pedidos-1440.png — Pedidos (placeholder), 1440×900
- S21-negociacoes-1440.png — Negociações (placeholder), 1440×900
- S22-indicadores-1440.png — Indicadores (placeholder), 1440×900
- S23-agentes-ia-1440.png — Agentes IA (placeholder), 1440×900
- S24-configuracoes-1440.png — Configurações (placeholder), 1440×900
- S25-dashboard-1024.png — Dashboard, 1024×768
- S26-usuarios-1024.png — Usuários, 1024×768
- S27-filiais-1024.png — Filiais, 1024×768
- S28-centros-custo-1024.png — Centros de Custo, 1024×768
- S29-regras-workflow-1024.png — Regras de Workflow, 1024×768
- S30-configuracao-erp-1024.png — Configuração do ERP, 1024×768

## 23. As 10 maiores fragilidades visuais/UX atuais

1. Tabela de Filiais sem paginação, com 5.000 linhas no DOM de uma vez (VIS-P0-01).
2. Defeito visual de sobreposição de texto na coluna "Descrição +Compras" de Filiais (VIS-P1-01).
3. Três telas centrais do módulo "Compras" (Pedidos, Negociações, Indicadores) sem qualquer elemento de produto além de texto.
4. "Detalhes técnicos" exposto por padrão, sem controle de visibilidade, na tela mais usada do fluxo de Fornecedores.
5. Unidades de Negócio quebra o padrão estrutural (busca, filtro, número de ações) do restante da família Administração.
6. Sidebar fixa não responsiva, consumindo proporção maior da tela em 1024×768, sem opção de colapso.
7. Estado de degradação de integração externa em Fornecedores (Review) sem tratamento visual de destaque/cor de alerta.
8. Formulário de "Novo usuário" com lista de checkbox única e longa, sem busca interna, para dados que crescem (Centros de Custo, potencialmente Filiais).
9. Altura de linha inconsistente nas tabelas de Perfis e Filiais, quebrando a varredura vertical.
10. Ausência de qualquer assinatura visual comum entre as três telas de Governança de Compras.

## 24. As 10 melhores decisões visuais/UX atuais

1. Linguagem tipográfica sóbria e consistente (peso de fonte + caixa alta para rótulos), adequada ao contexto corporativo, sem decoração supérflua.
2. Estado ativo da navegação (fundo preto sólido + texto branco) com contraste inequívoco em todas as telas testadas.
3. Padrão replicado de "página de listagem" (H1 + subtítulo + card com CTA + busca/filtro + tabela) em 5 das 6 telas centrais de Administração.
4. Aviso explícito de que Perfis/Usuários seguem um modelo "sem permissão individual" — comunicado tanto no subtítulo da página quanto no formulário de criação, reforçando a regra de negócio visualmente.
5. Uso de acordeões para organizar seções de dado longas na tela de Review de Fornecedores (Identificação, CNAE, Endereço, Contato), evitando um formulário monolítico.
6. Rótulo textual explícito de "Consulta é somente leitura — nenhum fornecedor é criado nesta etapa" junto ao campo de busca de Fornecedores, reduzindo ambiguidade sobre o efeito da ação.
7. Indicação textual clara de que Filiais são dados mestres do ERP e não podem ser criadas/alteradas localmente, comunicada no subtítulo da própria tela.
8. Paleta reduzida (preto, branco, cinza, um verde de "Ativo" e um vermelho de "Inativo") aplicada de forma consistente aos badges de status em toda a família Administração.
9. Header institucional simples (tenant + produto + usuário) sem ruído visual, adequado a um produto multi-tenant B2B.
10. Mensagens de estado vazio/indisponível escritas em linguagem natural e específica ("Nenhum alerta", "Não foi possível obter dados atualizados da fonte externa agora") em vez de códigos de erro genéricos.

## 25. O que mais separa o +Compras atual de um produto corporativo visualmente maduro

O que separa hoje o +Compras de um produto corporativo maduro não é a paleta nem a tipografia — essas já estão em nível aceitável e consistente onde foram aplicadas. É a **desigualdade de investimento entre módulos**: o mesmo produto tem uma família de telas (Administração, com exceção de Unidades de Negócio) desenhada com disciplina de padrão repetível, e ao lado dela um módulo inteiro (Compras: Pedidos, Negociações, Indicadores) que ainda não tem nenhuma decisão de UI tomada, o que cria um produto visualmente bipolar do ponto de vista de quem navega pela sidebar de cima a baixo. Um produto corporativo maduro trataria mesmo um placeholder como uma decisão de design (um estado "em construção" com ícone, texto e talvez uma prévia do que vai existir), não como uma ausência.

Em segundo lugar, a maturidade real de um sistema administrativo se mede pela capacidade de lidar com volume — e a tabela de Filiais com 5.000 linhas sem paginação é evidência direta de que o padrão de listagem foi desenhado e testado com poucos registros, não com a escala real do ERP integrado. Isso é o oposto de "produto corporativo maduro": é um padrão de UI que só funciona em ambiente de demonstração.

Por fim, falta uma camada de identidade visual de "família de módulo" acima do nível de tela individual — cores, selos ou ícones de cabeçalho que digam "isto é Governança", "isto é Administração", "isto é Compras" de forma perceptível sem ler o rótulo da sidebar. Hoje essa informação existe só na estrutura de navegação, não se repete dentro do conteúdo da própria tela.

---

## Confirmação final de estado do repositório

Reexecutado ao final da auditoria:
- `git status --short`: apenas `.ai/dashboard/DASHBOARD_STATE.md` (modificado, pré-existente, não tocado por esta auditoria), `.ai/AUDITORIA_COMPRAS_ESTADO_ATUAL.md` (novo, da auditoria anterior, não tocado) e `.ai/audit-visual-screenshots/` + este próprio relatório (novos, produzidos por esta auditoria).
- `git rev-list --left-right --count origin/main...main`: `0 14` — inalterado em relação ao estado inicial registrado no briefing.
- Nenhum comando destrutivo, commit, push ou alteração de código/CSS/texto de produto foi executado.

## Veredito final

**AUDITORIA VISUAL/UX COMPLEMENTAR CONCLUÍDA — AGUARDANDO AVALIAÇÃO DO PRODUCT OWNER**

Ressalva pontual registrada na Seção 5: o comparativo lado a lado da tela de Review de Fornecedores (dado divergente real) não pôde ser renderizado durante o teste porque a integração externa de reconsulta de CNPJ estava indisponível no momento da auditoria — a tela foi aberta e inspecionada visualmente no estado disponível (dados já cadastrados, sem divergência calculada), e esse estado foi auditado e registrado como tal, não presumido.
