# Design Review Consolidado Pós-Onda 1 — +Compras

> **Natureza deste documento: AUDITORIA / DIAGNÓSTICO.** Nenhum código, componente, rota, sidebar, CSS, endpoint ou migration foi alterado nesta etapa. A Onda 1 (41/41 entregáveis) não foi reaberta. A B2.9 (Adapter Linx) permanece tecnicamente concluída, com homologação manual transferida ao Gate Final. A Onda 2 não foi iniciada.
> Executor: Claude Code. Data: 17/08/2026.
> Este documento **consolida e estende** a auditoria anterior `docs/audits/DesignReview-Pos-Onda1-Auditoria-SOMA-vs-Compras.md` (11-12/08/2026), que permanece válida como evidência primária de comparação visual SOMA×+Compras. Não duplicamos o que já está lá — remetemos a ela e acrescentamos o que faltava: leitura de código completa, navegação visual end-to-end via Chrome DevTools MCP nesta sessão, auditoria de Administração/Governança/Configurações, dívida técnica de `ExcluirFornecedorUseCase`, revisão PT-BR e glossário, acessibilidade e responsividade.

---

## 1. Objetivo

Realizar o Design Review consolidado pós-Onda 1 do +Compras: auditoria completa de UX/UI, navegação, Design System, consistência entre telas administrativas, jornada de Fornecedores, PT-BR, acessibilidade e responsividade — sem implementar nenhuma correção. Produzir uma matriz de achados priorizada e uma proposta de lotes de correção (Work Orders) para decisão do Product Owner.

## 2. Escopo

Onda 1 completa (Administração + Fornecedores B1/B2.x + B2.9 Adapter Linx). Não inclui Pedidos/Negociações/Indicadores/Agentes IA em profundidade funcional (são mocks transparentes, fora do escopo real da Onda 1) — foram inspecionados apenas quanto à consistência visual e ao rótulo "Em desenvolvimento".

## 3. Governança inicial — confirmado

```
branch: main
git status --short: M .ai/dashboard/DASHBOARD_STATE.md   (pré-existente, não tocado)
origin/main...main: 0 0
```

Últimos commits confirmam B2.9 encerrada tecnicamente (`5d7d8b9`) e publicada em `origin/main`. Nenhuma ação destrutiva foi executada. `DASHBOARD_STATE.md` permanece com a alteração local pré-existente, intocada.

## 4. Fontes analisadas

- `.ai/DECISIONS.md` (ADRs 0001–0023, com destaque para ADR-0020 R2 — Vertical Slice obrigatório — e ADR-0023 — arquitetura canônica CNPJ e os 4 bugs históricos BUG-1..4).
- `.ai/PROJECT_STATE.md`, `.ai/CURRENT_SPRINT.md`, `.ai/BACKLOG.md`, `.ai/work-orders/README.md` (41/41 Work Orders da Onda 1 concluídas; nota de divergência textual sobre O1.6 registrada na seção 26).
- `docs/product/ComprasUX.md` (1242 linhas) — especificação UX obrigatória, referência ao Design System AZZAS 2154/GDT.
- `docs/frontend/Frontend.md`, `docs/demo/PortalMaisComprasDemo.md`, `docs/README.md`.
- `docs/audits/DesignReview-Pos-Onda1-Auditoria-SOMA-vs-Compras.md` — auditoria visual anterior (SOMA real × Design System documentado × +Compras real), tratada como evidência primária e não repetida aqui.
- `resources/design-system/` — tokens (`colors_and_type.css`), kit de referência `ui_kits/portal-gdt/` (`shell.jsx`, `components.jsx`).
- Código real: `frontend/web/src/**` (rotas, AppShell, sidebar, header, todos os slices administrativos, jornada de Fornecedores/CNPJ), `backend/src/BlueprintOS.Domain/Identity/**`, `backend/src/BlueprintOS.Application/Procurement/Suppliers/FornecedorUseCases.cs`, `backend/src/BlueprintOS.Infrastructure/Persistence/Repositories/FornecedorRepository.cs`.

## 5. Chrome DevTools MCP — utilizado com sucesso

Backend confirmado ativo em `http://127.0.0.1:5262` (instância pré-existente do usuário), frontend iniciado nesta sessão em `http://127.0.0.1:5173`. Login real de Development executado via fluxo OTP passwordless (e-mail corporativo + botão "Preencher código (Development)"), sessão autenticada como Julio Cesar. Nenhum bloqueio de Chrome ocorreu.

## 6. Inventário de telas (rotas reais, `frontend/web/src/core/AppRoutes.tsx`)

| # | Rota | Tela | Backend real? | Auditado visualmente | Principais achados |
|---|---|---|---|---|---|
| 1 | `/login` | Login OTP | Sim | Sim | OK — fluxo claro, atalho de dev sinalizado |
| 2 | `/` | Dashboard | Parcial (fornecedores real; pedidos/negociações mock rotulado) | Sim | PT-BR sem acento; "Demo" transparente |
| 3 | `/administracao/perfis` | Gestão de Perfis | Sim | Sim | Descrição "x" (dado de fixture); sem busca/filtro |
| 4 | `/administracao/usuarios` | Gestão de Usuários | Sim | Sim | Falta "Visualizar" comparado a outras (ok, é padrão próprio) |
| 5 | `/administracao/filiais` | Gestão de Filiais | Sim (metadados locais sobre ERP) | Sim | **Overflow de coluna confirmado** (P2) |
| 6 | `/administracao/centros-custo` | Gestão de Centros de Custo | Sim | Sim | Mesmo overflow em 2 colunas |
| 7 | `/administracao/unidades-alocacao` | Unidades de Alocação | Sim | Sim | Empty state ok |
| 8 | `/administracao/unidades-negocio` | Unidades de Negócio | Sim | Sim | Título "Cadastro de..." destoa de "Gestão de..." |
| 9 | `/administracao/configuracao-erp` | Configuração de ERP | Sim | Sim | `<select>` nativo não estilizado |
| 10 | `/administracao/identity-providers` | Identity Providers | Sim | Sim | `<select>` nativo não estilizado |
| 11 | `/administracao/parametros` | Parâmetros | Sim | Sim | Breadcrumb "ADMINISTRAÇÃO DO SISTEMA" (único caso) |
| 12 | `/administracao/feature-flags` | Feature Flags | Sim | Não (nav apenas) | — |
| 13 | `/administracao/configuracao-notificacao` | Configuração de Notificações | Sim | Não (nav apenas) | — |
| 14 | `/administracao/regras-workflow` | Regras de Workflow | Sim | Sim (nav) | Vive solta em "Administração" |
| 15 | `/administracao/alcadas-aprovacao` | Alçadas de Aprovação | Sim | Sim | Vive solta em "Administração" |
| 16 | `/administracao/regras-orcamentarias` | Regras Orçamentárias | Sim | Sim | Vive solta em "Administração" |
| 17 | `/administracao/monitoramento` | Monitor de Integrações | Sim | Sim | `<select>` nativo não estilizado |
| 18 | `/fornecedores` | Cadastro com enriquecimento CNPJ | Sim | Sim (fluxo completo testado) | "Identidade temporária de desenvolvimento", estados em inglês, tela de revisão sem dados visíveis |
| 19 | `/pedidos` | Pedidos | Mock rotulado | Sim | OK — banner transparente |
| 20 | `/negociacoes` | Negociações | Mock rotulado | Sim | OK — banner transparente |
| 21 | `/indicadores` | Indicadores | Mock rotulado | Não | — |
| 22 | `/agentes-ia` | Agentes IA | Mock rotulado | Não | — |
| 23 | `/configuracoes` | Configurações | Mock rotulado (somente leitura) | Sim | Workflow/Alçadas/Orçamento reais não aparecem aqui |

**22 rotas mapeadas via código, 17 auditadas visualmente nesta sessão** (mais 6 já cobertas na auditoria anterior). Cobertura total combinada: 19/23 telas com inspeção visual direta em pelo menos uma das duas sessões; Feature Flags, Configuração de Notificações, Indicadores e Agentes IA não foram abertas visualmente em nenhuma das duas rodadas — registrado como lacuna, não como "aprovado".

## 7. Resumo executivo

A Onda 1 entregou uma base funcional sólida e consistente (Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação/Negócio, Governança de Workflow/Alçadas/Orçamento, Monitoramento, Fornecedores) com paleta e tipografia já alinhadas ao Design System AZZAS 2154/GDT. Os problemas encontrados são, em sua maioria, de **estrutura de navegação** (sidebar plana, header sem identidade completa) e de **polimento** (acentuação, overflow de tabela, `<select>` nativo) — não de arquitetura de dados ou de segurança, com uma exceção relevante: `ExcluirFornecedorUseCase` usa exclusão física, divergindo do padrão de soft delete usado por todo o resto do domínio. Nenhum bug bloqueante de crash foi reproduzido nesta sessão (o bug histórico de `<StatusBadge>`/`.toLowerCase()` parece mitigado no fluxo real de CNPJ, mas o componente genérico que o causava ainda existe e pode ser reativado por um uso futuro incorreto).

## 8. Arquitetura atual de navegação

`frontend/web/src/core/AppShell.tsx` define um único array plano `navItems` com **21 itens** (Dashboard + 15 de Administração + Fornecedores/Pedidos/Negociações/Indicadores/Agentes IA/Configurações), renderizados sem nenhum agrupamento, título de seção, ícone ou colapsável. Confirmado em código (linhas ~20-43) e na tela (todas as capturas desta sessão). O item ativo usa a variante "Afirmativa" do Design System (fundo escuro + texto claro) corretamente — isso já está certo e não deve ser tocado.

## 9. Proposta de navegação (para decisão do PO — não implementado)

Confrontando os 21 itens reais com suas funções observadas, a organização por contexto de trabalho proposta é:

| Grupo proposto | Itens reais que entram |
|---|---|
| **Início** | Dashboard |
| **Fornecedores** | Fornecedores (cadastro/CNPJ) |
| **Compras** | Pedidos, Negociações, Indicadores (hoje mocks — grupo cresce na Onda 2) |
| **Governança de Compras** | Regras de Workflow, Alçadas de Aprovação, Regras Orçamentárias (decisão já aprovada pelo PO — ver seção 10) |
| **Administração** | Perfis, Usuários, Filiais, Centros de Custo, Unidades de Alocação, Unidades de Negócio, Monitoramento |
| **Configurações** (engrenagem, região inferior) | Configuração de ERP, Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações, tela `/configuracoes` |
| **Agentes IA** | Agentes IA — grupo próprio ou dentro de Compras (a decidir; funcionalidade ainda incipiente) |

Isto é proposta para o Design Review decidir, não implementação. Nomes de grupo e posição de "Agentes IA" precisam de confirmação do PO.

## 10. Governança de Compras — auditoria e proposta

**Decisão do PO já registrada no briefing**: Workflow, Alçadas e Regras Orçamentárias não devem ficar soltos em "Configurações". **Confirmado em código e em tela**: essas três telas hoje não estão em Configurações — estão soltas dentro do grupo "Administração" da sidebar plana, no mesmo nível visual que Perfis/Usuários/Filiais (breadcrumb "ADMINISTRAÇÃO" nas três). Isso não viola literalmente a decisão do PO (não estão em Configurações), mas também não a atende: elas estão misturadas com cadastros operacionais simples, sem destaque como capacidade de governança. Recomendação: criar o grupo "Governança de Compras" na sidebar (seção 9) e mover as três rotas para lá, sem alterar seus componentes internos.

## 11. Configurações / Engrenagem — auditoria e proposta

`/configuracoes` (`ConfiguracoesPage.tsx`) é uma tela **mockada, somente leitura** ("Em desenvolvimento: valores ilustrativos. A edição destes parâmetros ainda não está disponível nesta tela."), organizada em 3 blocos: Integração ERP, Consulta de CNPJ, Notificações. As telas com **implementação real** de configuração técnica (Configuração de ERP, Identity Providers, Parâmetros, Feature Flags, Configuração de Notificações) vivem soltas na sidebar principal, não dentro da engrenagem — ou seja, a engrenagem hoje é decorativa e a configuração real está em outro lugar. Recomendação: consolidar as 5 rotas técnicas reais como o conteúdo definitivo da engrenagem (substituindo o mock), mantendo Workflow/Alçadas/Orçamento fora dela (seção 10). O conceito de "Escopo Administrativo" (ADR-0022) não tem hoje nenhuma superfície visual própria — nem na engrenagem, nem no header — permanece como lacuna a decidir (ver seção 26).

## 12. Sidebar — auditoria detalhada

- 21 itens em lista plana, texto puro, **sem ícones** (nem sóbrios nem coloridos — a decisão de "ícones discretos, sem arco-íris" ainda não foi implementada, é ausência total, não excesso).
- Item ativo: correto (fundo escuro, texto claro — variante Afirmativa).
- Sem título de seção, sem indentação, sem estado de recolhimento.
- RBAC: itens são filtrados por `permissoesEfetivas` antes de renderizar (comentário no código confirma que a proteção real está no backend) — comportamento correto, não é apenas ocultação cosmética de botão.
- Comportamento em viewport menor: **falha**. Em 1024×768 a sidebar permanece com largura fixa e o conteúdo principal (tabelas) extrapola a viewport sem wrapper de scroll, quebrando o layout da página inteira (screenshot capturada, ver seção 23).
- Densidade/legibilidade: aceitável em 1440px; 21 itens exigem rolagem vertical da própria sidebar em telas de 800px de altura — não crítico, mas caminha para ficar pior conforme a Onda 2 adicionar itens.

## 13. Header / Identidade — auditoria detalhada

`AppShell.tsx`: header mostra `AZZAS 2154` (marca) + `+Compras` (produto) + **texto simples** "Julio Cesar" (não é botão, não abre dropdown, não tem avatar) + botão "Sair" isolado. Confirmado pela auditoria anterior (SOMA real usa `[avatar] Nome ▾` com popover de e-mail + badges de perfil) e pelo próprio Design System (`shell.jsx` já implementa `UserChip`/`user-dropdown` pronto para reuso, linhas ~159-186). Não há breadcrumbs no header (breadcrumb aparece como rótulo de seção acima do `<h1>` de cada página — ex. "ADMINISTRAÇÃO", "+COMPRAS" — funciona como pseudo-breadcrumb estático de nível único). Sem redundância grave com a sidebar. **Achado adicional confirmado nesta sessão**: dentro do fluxo de Fornecedores, o campo "USUARIO" do painel de estado exibe literalmente **"Identidade temporária de desenvolvimento"** — o texto residual mencionado no briefing como dívida cosmética esperada após autenticação real ainda está presente e visível ao usuário autenticado (`Julio Cesar` está logado, mas o painel de CNPJ mostra a identidade de dev, não o nome do usuário real).

## 14. Administração — consistência entre telas

Perfis, Usuários, Filiais, Centros de Custo e Unidades de Alocação compartilham o mesmo esqueleto (breadcrumb → `<h1>` "Gestão de X" → subtítulo explicativo → card com toolbar de busca+filtro (quando ERP) → tabela → ações Visualizar/Editar/Inativar). Isso já forma uma família visual coerente. Inconsistências pontuais encontradas:
- **Título**: "Gestão de Perfis/Usuários/Filiais/Centros de Custo" vs. "**Cadastro** de Unidades de Negócio" (única tela com verbo diferente) vs. "Unidades de Alocação **cadastradas**" (card interno usa "cadastradas" em vez de "integradas"/"gerenciadas").
- **Breadcrumb**: "ADMINISTRAÇÃO" em quase todas, mas "**ADMINISTRAÇÃO DO SISTEMA**" em Parâmetros — único caso com esse rótulo estendido.
- **Ações da tabela**: Usuários/Perfis/Filiais/Centros de Custo têm Visualizar+Editar+Inativar; Unidades de Negócio tem apenas Editar+Inativar (sem Visualizar) — pode ser intencional (entidade mais simples), mas não está documentado como decisão.
- **Busca/filtro**: presente em Filiais e Centros de Custo (dados de ERP, com campo de busca + filtro de status) e em Unidades de Alocação (busca simples); **ausente** em Perfis e Usuários (achado já registrado na auditoria anterior, confirmado novamente aqui).
- Todas usam corretamente Ativar/Inativar (nunca "excluir") — alinhado ao princípio de não usar DELETE físico como operação de negócio, **exceto Fornecedor** (seção 25).

## 15. Fornecedores — jornada de CNPJ, auditada em fluxo real

Testado ao vivo: consulta de CNPJ já cadastrado (Amazon Serviços de Varejo, fixture existente). Observações:
- O painel de estado técnico (`ESTADO`, `FONTE`, `DATA/HORA`, `USUARIO`, `CORRELATIONID`) expõe a state machine técnica diretamente ao usuário, incluindo valores em **inglês** ("Idle", "Consulting", "Review") misturados com rótulos em português — viola diretamente o princípio do briefing de que "o usuário não precisa conhecer a state machine técnica". `CorrelationId` e `Estado` bruto são informação de suporte/debug, não de produto; deveriam estar escondidos por padrão (ex. atrás de um "Detalhes técnicos" colapsável) ou traduzidos para linguagem de usuário ("Consultando...", "Em revisão...").
- Ao consultar um CNPJ já cadastrado sem possibilidade de reconsulta externa, a tela mostra a mensagem "Fornecedor já cadastrado... revise os dados atuais" e habilita os botões **Aceitar/Rejeitar**, mas **nenhum dado do fornecedor é exibido para revisão** — o usuário é convidado a decidir sobre algo que não vê. Isto é uma lacuna de UX no estado "Review" quando a fonte externa está indisponível (não testado o caminho de sucesso completo de reconsulta, que não pôde ser exercitado sem acesso à BrasilAPI real neste ambiente).
- Nenhum erro de console/rede foi observado nesta consulta — o crash histórico (`value.toLowerCase is not a function` em `<StatusBadge>`, ADR-0023 BUG-2) não se repetiu no fluxo real de situação cadastral, que hoje usa o componente dedicado `SituacaoCadastralBadge` (mapeamento fixo por enum, sem `.toLowerCase()`). Porém o componente genérico `shared/components/StatusBadge.tsx` (usado por Administração para Ativo/Inativo) **ainda contém** `` `status status-${value.toLowerCase()}` `` na linha 19 — o mesmo padrão de risco permanece latente, isolado apenas por convenção de uso (o comentário do próprio arquivo alerta que a situação cadastral do CNPJ não deve usar este componente). Não é um bug reproduzido, é um risco de regressão documentado no próprio código.
- `NovoFornecedorPanel.tsx` (painel de revisão para CNPJ novo, sem Fornecedor existente): 11 campos de formulário, **nenhum com atributo `name`** (têm `id` apenas em alguns casos, associação por `<label>` envolvente presente) — dívida de acessibilidade confirmada, consistente com o item já conhecido do briefing. Rótulos dos campos usam nomes de propriedade coladas em CamelCase diretamente como texto visível: **"RazaoSocial"**, **"NomeFantasia"** em vez de "Razão Social"/"Nome Fantasia" — achado novo de PT-BR/rotulagem, não estava registrado na auditoria anterior.

## 16. Tabelas

Padrão comum (Filiais, Centros de Custo, Perfis, Usuários): cabeçalho em uppercase, linhas com padding generoso, badge de status colorido, coluna de ações à direita com botões outline. Problemas confirmados:
- **Overflow crítico em Filiais e Centros de Custo**: a coluna "Descrição +Compras" (texto vazio renderiza `"Sem descricao +Compras"` via `FilialTable.tsx:35`) tem largura fixa insuficiente para o texto do empty-state, que quebra em duas linhas e **visualmente sobrepõe a coluna de Status ao lado** (confirmado em screenshot, dois locais diferentes — Filiais e Centros de Custo, este último também na coluna "Unidade de Alocação Padrão"). Isso não é um problema pontual de dado de exemplo, é um bug sistemático de layout de tabela.
- **Responsividade**: nenhuma tabela de Administração usa `overflow-x: auto` num wrapper próprio; em viewport de notebook (1024px) a página inteira (não só a tabela) extrapola a largura da viewport, incluindo o header — falha de responsividade confirmada (seção 23).
- Nenhuma tabela de Administração tem paginação (aceitável no volume atual de poucos registros, mas não escala).
- Busca/filtro ausente em Perfis e Usuários (ver seção 14).

## 17. Formulários

- Padrão de rótulo com `<span>` + `<input>` dentro de `<label>` (associação implícita correta).
- `NovoFornecedorPanel` usa rótulos crus de propriedade (seção 15) — precisa de revisão de texto.
- Diferenciação editável/readonly existe via classes `field-editable`/`field-readonly` no CSS global (`styles.css`), mas não foi possível confirmar visualmente neste ambiente qual campo específico usa cada uma sem abrir o modo de edição de cada entidade individualmente — não incluído no escopo desta rodada por tempo, registrar como verificação pendente.
- `<select>` nativo do navegador (sem estilização do Design System) confirmado em: Configuração de ERP, Identity Providers, Monitoramento (filtro de Status), Alçadas de Aprovação, Regras Orçamentárias — mais abrangente do que o único caso ("Monitoramento") registrado na auditoria anterior; é um padrão recorrente em qualquer tela que usa seletor de "Unidade de Negócio".

## 18. Modais / Confirmações

Não foi possível abrir um modal de confirmação de Inativar nesta sessão (ação destrutiva-reversível real sobre dado de fixture, evitada deliberadamente para não alterar estado do ambiente de forma não solicitada). O nome do componente `ConfirmToggleAtivoUsuarioModal.tsx` e o texto documentado em `ComprasUX.md` ("Tem certeza que deseja inativar {entidade}? Essa ação não pode ser desfeita.") indicam que o padrão existe e é nomeado corretamente; a frase documentada, porém, é o tipo de confirmação genérica que o item 16 do briefing pede para evitar quando a consequência pode ser explicitada (ex.: "usuários vinculados perderão acesso imediatamente"). Não verificado ao vivo — registrar como pendência de verificação funcional, não como achado confirmado.

## 19. Status e Badges

- `SituacaoCadastralBadge` — específico para situação cadastral CNPJ, mapeamento fixo, correto (não usa `.toLowerCase()`), alinhado à decisão do briefing.
- `StatusBadge` genérico (`shared/components/StatusBadge.tsx`) — usado para Ativo/Inativo (tone="situacao") e Pendente/Aceito/Rejeitado (tone="decisao"). Semântica de cor consistente com o Design System (verde=aprovado/ativo, vermelho=rejeitado/inativo, âmbar=pendente). Risco de tipo latente já descrito na seção 15.
- Não foi encontrado componente `StatusSincronizacao`; o equivalente real é `StatusExecucaoBadge` (Monitoramento de sincronizações).
- Nenhuma duplicidade grosseira de variantes encontrada; poucos badges no total (Ativo/Inativo, Pendente/Aceito/Rejeitado, situação cadastral, execução).

## 20. Estados da interface

| Estado | Coberto onde | Lacuna |
|---|---|---|
| Inicial | Todas as telas administrativas | — |
| Loading | Não observado explicitamente nesta sessão (respostas rápidas em ambiente local) | Verificar com throttling de rede — não feito |
| Sucesso | Consulta de CNPJ já cadastrado | — |
| Vazio | Unidades de Alocação, Parâmetros — bons empty states textuais | — |
| Erro | Dashboard (falha ao carregar fornecedores), Fornecedores (reconsulta indisponível) | Mensagens sem acentuação (seção 21) |
| Sem permissão | Não testado (usuário logado é Administrador; não foi trocado de perfil) | Pendência de verificação |
| Dados parciais | Fornecedores (revisão sem reconsulta) | Falta exibir os dados existentes no estado Review (seção 15) |
| Operação pendente/concluída | Perfis/Usuários (status Ativo/Inativo) | — |

## 21. RBAC visual

Filtragem de itens de sidebar por `permissoesEfetivas` confirmada em código, com comentário explícito de que essa é proteção de UX, não de segurança (o backend é a autoridade real, endpoints usam `RequireAuthorization`/`RbacPolicies`). Não foi possível, no tempo desta sessão, logar com um segundo perfil de permissão restrita para observar visualmente ocultação de menu/ação — registrar como verificação pendente, não como aprovado.

## 22. Revisão gramatical PT-BR

Achado sistemático confirmado em toda a aplicação: **ausência de acentuação** em títulos, subtítulos e mensagens — não é um problema pontual, aparece em praticamente toda tela auditada. Exemplos coletados nesta sessão (evidência direta de tela/código, não amostra da auditoria anterior):

| Local | Texto atual | Forma correta |
|---|---|---|
| Sidebar/títulos | "Usuarios", "Configuracoes", "Negociacoes", "Alcadas de Aprovacao", "Regras Orcamentarias" | Usuários, Configurações, Negociações, Alçadas de Aprovação, Regras Orçamentárias |
| Dashboard | "Nao foi possivel carregar o resumo de fornecedores." | Não foi possível carregar o resumo de fornecedores. |
| Filiais/Centros de Custo | "Filiais sao dados mestres do ERP. O +Compras nao cria nem altera..." | Filiais são dados mestres do ERP. O +Compras não cria nem altera... |
| Unidades de Negócio | "Nao ha exclusao fisica — apenas Ativar/Inativar." | Não há exclusão física — apenas Ativar/Inativar. |
| Monitoramento | "Reaproveita integralmente a infraestrutura real de sincronizacao... apenas consulta, nenhum motor novo." | ...sincronização... |
| Fornecedores (painel de estado) | "Idle" / "Consulting" / "Review" (em inglês) | Ocioso / Consultando / Em revisão (ou ocultar do usuário final) |
| NovoFornecedorPanel | "RazaoSocial", "NomeFantasia" (rótulos colados) | Razão Social, Nome Fantasia |
| Negociações | "Recomendacoes e acompanhamento de negociacoes com fornecedores." | Recomendações e acompanhamento de negociações com fornecedores. |

Não parece ser limitação de encoding (acentos aparecem corretamente em alguns lugares, ex. nomes de fornecedores reais "SÃO PAULO / SP" no Dashboard) — é ausência deliberada ou negligenciada na escrita de strings de UI/backend, portanto corrigível sem risco técnico.

## 23. Acessibilidade

- **`NovoFornecedorPanel.tsx`**: confirmado nesta sessão — nenhum dos 11 `<input>` tem atributo `name`; associação a `<label>` é implícita (envolvente), o que atende ao mínimo de rótulo acessível, mas não ao preenchimento automático de formulário nem a testes automatizados por `name`. Dívida confirmada, extensão mapeada (11 campos, 1 arquivo).
- Campo de CNPJ na tela principal de consulta (`CnpjSearch`) tem `id="cnpjCpf"` mas também sem `name`.
- Navegação por teclado não testada exaustivamente (fora do tempo desta rodada); os links de sidebar são `<a>`/`NavLink` nativos, portanto navegáveis por Tab por padrão.
- Contraste: paleta bege/preto do Design System tem contraste alto por design; nenhum problema de contraste observado nas capturas.
- `<select>` nativos (seção 17) são navegáveis por teclado nativamente — não é um problema de acessibilidade, é um problema de consistência visual.

## 24. Responsividade

Testado em 1440px (padrão), 1280px e 1024px×768px.
- Em 1440px e 1280px: layout estável, sem cortes.
- **Em 1024×768 (notebook menor): falha confirmada.** A tabela de Filiais (e por extensão qualquer tela de Administração com tabela larga) não está envolvida por um contêiner com `overflow-x: auto` — o conteúdo extrapola a viewport e a **página inteira** passa a exigir rolagem horizontal do browser, non apenas a tabela. Isso é uma violação direta do critério do briefing ("não pode haver... conteúdo inacessível") em telas de uso corporativo comum (notebook 13"/14" a 1024-1366px de largura efetiva). Sidebar não colapsa nem se adapta abaixo de 768px, conforme o próprio Design System já especifica que deveria ocultar-se — comportamento não verificado abaixo de 768px nesta sessão (não testado explicitamente), mas o comportamento em 1024px já demonstra que o problema aparece antes do breakpoint documentado.

## 25. Design System

Confirma-se: a referência oficial é **AZZAS 2154 / GDT**, documentada em `docs/product/ComprasUX.md` e materializada em `resources/design-system/` (tokens `colors_and_type.css`, kit de referência `ui_kits/portal-gdt/`). O frontend real importa os tokens corretamente (`styles.css:1`). Nenhum Design System paralelo foi criado. Desvios classificados:

| Uso | Classificação |
|---|---|
| Paleta bege/preto, botão preto como CTA primário | Segue o DS |
| Sidebar variante "Afirmativa" no item ativo | Segue o DS |
| Badges de status coloridos com semântica verde/vermelho/âmbar | Segue o DS |
| Header sem `user-chip`/dropdown (componente já existe no kit de referência) | Desvia sem necessidade — componente pronto não foi reaproveitado |
| Sidebar sem agrupamento semântico por seção | Desvia sem necessidade — o próprio kit GDT já demonstra o padrão de agrupamento |
| `<select>` nativo em vez de combobox estilizado do DS | Desvia sem necessidade |
| Tabelas sem busca/filtro/paginação padronizada | Parcialmente alinhado — DS referencia `component-table.html`, não plenamente adotado |

## 26. Dívidas técnicas pré-Gate

### 26.1 `ExcluirFornecedorUseCase` — exclusão física (P0/P1 técnico, não visual)

Confirmado em código: `backend/src/BlueprintOS.Application/Procurement/Suppliers/FornecedorUseCases.cs` (linhas ~119-127) implementa `ExcluirFornecedorUseCase.ExecuteAsync`, que chama `IFornecedorRepository.ExcluirAsync`, implementado em `backend/src/BlueprintOS.Infrastructure/Persistence/Repositories/FornecedorRepository.cs` (linhas ~13-14) como:
```csharp
context.Fornecedores.Remove(fornecedor);
await context.SaveChangesAsync(cancellationToken);
```
Isso é `DELETE` físico real no banco, exposto via `DELETE /fornecedores/{id}` (`FornecedoresController.cs`). **Todas as demais entidades do domínio auditadas usam inativação lógica**: `Usuario.Ativar/Inativar` (`StatusUsuario`), `Perfil.Ativar/Inativar`, `UnidadeAlocacao.Ativar/Inativar`, `UnidadeNegocio.Ativar/Inativar` (flag `Ativa`) — nenhuma delas expõe exclusão física. `Fornecedor` já possui campo `Status` (string "Ativo"/"Inativo") mas o Use Case de exclusão não o utiliza — usa remoção física em vez de setar o status. Não há documentação encontrada que justifique esse desvio para Fornecedor especificamente. **Registrado como dívida técnica obrigatória pré-Gate Manual, conforme instrução do PO — não corrigido nesta tarefa.**

### 26.2 Divergência textual O1.6 (BAIXA severidade, não bloqueia)

`.ai/work-orders/README.md` descreve "O1.6 — Gestão de Usuários (Backend Real)" como candidata em `backlog/`, mas o arquivo físico está em `completed/` e `CURRENT_SPRINT.md` registra abertura e encerramento formal em 11/08/2026. É uma inconsistência de documentação de gestão de projeto, não de produto — sinalizada aqui por transparência, fora do escopo de correção deste Design Review.

## 27. Outras dívidas (classificadas fora de "ajustes de layout")

| ID | Categoria | Descrição |
|---|---|---|
| DT-1 | DÍVIDA TÉCNICA | `ExcluirFornecedorUseCase` — exclusão física (seção 26.1) |
| DT-2 | DECISÃO DE PRODUTO | Nenhuma superfície visual para "Escopo Administrativo" (ADR-0022) — precisa de decisão de onde expor |
| DT-3 | ACESSIBILIDADE | `NovoFornecedorPanel` e `CnpjSearch` sem atributo `name` nos inputs |
| DT-4 | BUG FUNCIONAL (risco latente) | `StatusBadge` genérico ainda usa `.toLowerCase()` sobre valor não garantidamente string (linha 19) — mitigado por convenção, não por tipo |
| DT-5 | DADO/CONTRATO | Painel de estado de Fornecedores expõe `CorrelationId` e estados de state machine em inglês diretamente ao usuário final |
| DT-6 | DECISÃO DE PRODUTO | Estado "Review" sem reconsulta disponível não exibe os dados do fornecedor a serem revisados antes de habilitar Aceitar/Rejeitar |

---

## 28. Matriz completa de achados

| ID | Tela/Rota | Categoria | Descrição | Severidade | Bloqueia Gate? | Arquivos envolvidos |
|---|---|---|---|---|---|---|
| DR-01 | Sidebar (global) | Navegação | Sidebar plana com 21 itens, sem agrupamento por contexto | P2 | Não | `frontend/web/src/core/AppShell.tsx` |
| DR-02 | Header (global) | Navegação/Design | Header sem `[avatar] Nome ▾`/dropdown; componente de referência já existe no DS | P2 | Não | `AppShell.tsx`; `resources/design-system/ui_kits/portal-gdt/shell.jsx` |
| DR-03 | Administração (Workflow/Alçadas/Orçamento) | Navegação | Vivem soltas em "Administração", não em "Governança de Compras" (decisão do PO ainda não refletida) | P2 | Não | `AppShell.tsx` (navItems) |
| DR-04 | Configurações (`/configuracoes`) | Navegação | Engrenagem é mock somente-leitura; configuração real (ERP, IdP, Parâmetros, Feature Flags, Notificações) está fora dela | P2 | Não | `settings/pages/ConfiguracoesPage.tsx` |
| DR-05 | Filiais / Centros de Custo | Tabela/Layout | Overflow de texto em coluna "Descrição +Compras" sobrepõe coluna de Status | P2 | Não | `administration/branches/components/FilialTable.tsx:35`; `administration/cost-centers/components/CentroCustoTable.tsx` |
| DR-06 | Administração (tabelas larguras fixas) | Responsividade | Em 1024px de largura, página inteira extrapola viewport (sem `overflow-x` no wrapper) | P1 | Sim (uso corporativo em notebook) | Tabelas de `administration/*` (CSS global `styles.css`) |
| DR-07 | Perfis, Usuários | Tabela | Sem busca/filtro/paginação | P3 | Não | `administration/profiles`, `administration/users` |
| DR-08 | Config ERP, IdP, Monitoramento, Alçadas, Regras Orçamentárias | Formulário | `<select>` nativo não estilizado (padrão recorrente, não caso único) | P3 | Não | múltiplos slices de `administration/*` |
| DR-09 | Fornecedores (`/fornecedores`) | Terminologia/Dados | Painel de estado expõe `CorrelationId` e estados de state machine em inglês ("Idle"/"Consulting"/"Review") | P2 | Não | `procurement/suppliers/components/CadastroFornecedor.tsx` |
| DR-10 | Fornecedores (`/fornecedores`) | UX | Estado "Review" sem reconsulta disponível habilita Aceitar/Rejeitar sem exibir os dados a revisar | P1 | Sim | `procurement/suppliers/components/CadastroFornecedor.tsx`/`SupplierComparison.tsx` |
| DR-11 | Fornecedores (header do painel) | Cosmético/Identidade | Texto residual "Identidade temporária de desenvolvimento" exibido a usuário autenticado real | P3 | Não | `procurement/suppliers` (painel de estado) |
| DR-12 | `NovoFornecedorPanel` | Acessibilidade | 11 inputs sem atributo `name` | P3 | Não | `procurement/suppliers/components/NovoFornecedorPanel.tsx` |
| DR-13 | `NovoFornecedorPanel` | PT-BR/Rotulagem | Rótulos "RazaoSocial"/"NomeFantasia" colados, sem espaço/acento | P2 | Não | `NovoFornecedorPanel.tsx:66,70` |
| DR-14 | `shared/components/StatusBadge.tsx` | Bug funcional (risco latente) | `.toLowerCase()` sobre valor não garantidamente string, mitigado só por convenção de uso | P2 | Sim (risco de regressão) | `shared/components/StatusBadge.tsx:19` |
| DR-15 | Toda a aplicação | PT-BR | Ausência sistemática de acentuação em títulos, subtítulos e mensagens | P3 | Não | Amplo — ver seção 22 |
| DR-16 | Unidades de Negócio | Consistência | Título "Cadastro de..." destoa do padrão "Gestão de..."; falta botão Visualizar | P3 | Não | `administration/business-units` |
| DR-17 | Parâmetros | Consistência | Breadcrumb único "ADMINISTRAÇÃO DO SISTEMA" (todas as demais usam "ADMINISTRAÇÃO") | P3 | Não | `administration/parameters` |
| DR-18 | `ExcluirFornecedorUseCase` | Dívida técnica (dado) | Exclusão física via `DbSet.Remove`, divergente do padrão de soft delete do domínio | P1 | **Sim — obrigatório pré-Gate (instrução explícita do PO)** | `FornecedorUseCases.cs:119-127`; `FornecedorRepository.cs:13-14` |
| DR-19 | Perfis (dados de fixture) | Dado/Qualidade | Descrições "x" em perfis de teste (Pos-hardening, Verificação pos-hardening) vazando para ambiente demonstrável | P3 | Não | Dado de seed, não é bug de código |
| DR-20 | RBAC visual | Verificação pendente | Não testado com perfil de permissão restrita nesta sessão | — (pendência) | A confirmar | — |
| DR-21 | Modais de confirmação | Verificação pendente | Não exercitado ao vivo; texto documentado é genérico ("Tem certeza?") | — (pendência) | A confirmar | `ConfirmToggleAtivoUsuarioModal.tsx` e afins |
| DR-22 | Feature Flags, Config. Notificações, Indicadores, Agentes IA | Cobertura | Não inspecionadas visualmente em nenhuma das duas rodadas de auditoria | — (lacuna de cobertura) | Não | — |

## 29. Priorização

- **P0**: nenhum achado desta rodada atinge P0 (nenhuma perda de dado em produção observada; nenhuma falha de segurança visual identificada — backend permanece autoridade RBAC).
- **P1** (bloqueiam Gate Manual): DR-06 (responsividade quebrando layout), DR-10 (Aceitar/Rejeitar sem dados visíveis), DR-18 (exclusão física de Fornecedor — bloqueio explícito determinado pelo PO no briefing).
- **P2**: DR-01, DR-02, DR-03, DR-04, DR-05, DR-09, DR-13, DR-14 (risco latente).
- **P3**: DR-07, DR-08, DR-11, DR-12, DR-15, DR-16, DR-17, DR-19.
- **Pendências de verificação** (não classificadas por severidade, precisam de rodada dedicada): DR-20, DR-21, DR-22.

## 30. Proposta de execução (lotes/Work Orders — não criados nesta tarefa)

Mantendo a disciplina de separar ESTRUTURA de DESIGN:

- **DR.1 — Estrutura de navegação e contextos** (ESTRUTURA): agrupar sidebar por contexto (seção 9), mover Workflow/Alçadas/Orçamento para "Governança de Compras" (DR-03), consolidar Configurações como engrenagem real (DR-04). Depende de decisão do PO sobre nomes de grupo.
- **DR.2 — Design de navegação e header** (DESIGN): implementar `[avatar] Nome ▾` reaproveitando `shell.jsx` do kit GDT (DR-02), remover/traduzir estado técnico exposto em Fornecedores (DR-09), remover texto residual de identidade de dev (DR-11).
- **DR.3 — Padronização de telas administrativas** (DESIGN): corrigir overflow de tabela (DR-05), corrigir responsividade em notebook (DR-06), padronizar `<select>` estilizado (DR-08), adicionar busca/filtro em Perfis/Usuários (DR-07), alinhar título/breadcrumb de Unidades de Negócio e Parâmetros (DR-16, DR-17).
- **DR.4 — Fornecedores/Review** (DESIGN + pequeno funcional): exibir dados no estado Review sem reconsulta (DR-10), revisar rótulos de `NovoFornecedorPanel` (DR-13), adicionar `name` aos inputs (DR-12), isolar/blindar `StatusBadge` genérico (DR-14).
- **DR.5 — PT-BR e acessibilidade** (DESIGN): restaurar acentuação sistemática (DR-15), revisar glossário (seção 31).
- **DR.6 — Dívidas técnicas pré-Gate** (ESTRUTURA/dado, fora de Design puro): substituir exclusão física de Fornecedor por inativação lógica (DR-18) — **obrigatório antes do Gate Final**, conforme instrução direta do PO.
- **DR.7 — Verificações pendentes** (curta, antes de fechar o Design Review): RBAC visual com perfil restrito (DR-20), modal de confirmação real (DR-21), abrir Feature Flags/Config. Notificações/Indicadores/Agentes IA (DR-22).
- **DR.8 — Regressão consolidada**: após DR.1–DR.7, suíte completa + nova passada visual rápida.

Ordem recomendada: DR.6 (dívida de dado, isolada e não depende de UI) pode correr em paralelo desde já; DR.1 antes de DR.2/DR.3/DR.4 (mudar estrutura antes de polir dentro dela); DR.5 e DR.7 podem ser paralelos a qualquer momento; DR.8 é sempre o último passo antes do CRUD/E2E manual do PO.

## 31. Glossário proposto da UI

| Termo canônico | Sinônimos/variações encontradas a eliminar |
|---|---|
| Usuário | "Usuario" (sem acento) |
| Configuração / Configurações | "Configuracao(oes)" (sem acento) |
| Centro de Custo | (nenhuma variação de nome encontrada — apenas falta de acento: "Centros de Custo") |
| Unidade de Negócio | "BU" aparece em dado de seed ("BU Teste Gate 41") — aceitável como nome próprio de teste, mas a UI deve preferir "Unidade de Negócio" no texto de produto |
| Unidade de Alocação | "Unidades de Alocacao" (sem acento) |
| Fornecedor | Nenhum uso de "Supplier" encontrado na UI (apenas em nomes internos de arquivo/tipo, ex. `SupplierCard`, fora da superfície visível) |
| Perfil | Consistente |
| Permissão | Consistente ("Permissoes" sem acento na coluna de tabela) |
| Situação Cadastral | Consistente, bem isolado em `SituacaoCadastralBadge` |
| Razão Social / Nome Fantasia | "RazaoSocial"/"NomeFantasia" colados em `NovoFornecedorPanel` — corrigir |
| Ativo / Inativo | Consistente em todas as entidades administrativas |
| Governança de Compras | Termo ainda não presente na UI — a introduzir na Onda DR.1 |

Termos técnicos internos que não aparecem para o usuário (ex. `CorrelationId`, nomes de classe C#, `Vertical Slice`) não foram traduzidos — corretamente fora do escopo de tradução de UI, mas `CorrelationId` e o `Estado` bruto da state machine **estão** vazando para a superfície visível (DR-09) e deveriam deixar de estar.

## 32. Console/Network — observações

Nenhuma exceção, 404, 403 inesperado ou 500 foi observado no console durante a navegação desta sessão (login, Dashboard, todas as telas de Administração visitadas, consulta de CNPJ de fornecedor já cadastrado). Isso é evidência de que o crash histórico de `<StatusBadge>` (ADR-0023 BUG-2) não se reproduz no caminho testado — mas o caminho de "CNPJ novo com reconsulta externa bem-sucedida" não foi exercitado nesta sessão (seria necessário acesso real à BrasilAPI ou um mock específico), portanto **não é uma confirmação completa de que o bug está eliminado**, apenas de que o caminho testado está limpo.

---

## 33. Confirmações de governança final

```
git status --short:        M .ai/dashboard/DASHBOARD_STATE.md   (preservado, não tocado)
origin/main...main:        0  0   (sem alterações desde o início)
Arquivo novo desta tarefa: docs/architecture/Design-Review-Consolidado-Pos-Onda1.md
```

Nenhum código, CSS, rota, componente, endpoint ou migration foi alterado. Nenhuma correção foi implementada. Nenhuma Work Order de implementação foi aberta.
