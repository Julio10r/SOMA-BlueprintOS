# +Compras UX

## Introdução

Documenta wireframes, fluxo de navegação, comportamento das telas, componentes e experiência do usuário do +Compras. Descreve **como o usuário utiliza o sistema** — distinto de `+Compras Funcional` (o que o sistema faz) e da Arquitetura Técnica (como o sistema foi construído). Insumo direto do Mock navegável (ver `.ai/ROADMAP.md`, estratégia Frontend First).

## Responsabilidade

- Wireframes.
- Fluxo de navegação.
- Comportamento das telas.
- Componentes.
- Experiência do usuário.
- Cada seção de tela/módulo segue o [template UX oficial](./templates/UXTemplate.md), estendido com a seção **Componentes React previstos** ao final de cada tela (orientação de implementação da Onda 1).

## Público

Design, Produto, QA e Desenvolvimento.

## Nota de escopo desta atualização (O1.1)

Mesmo recorte de escopo do `ComprasFuncional.md`: apenas as telas da Onda 1 (Login, Seleção da Unidade de Negócio, Dashboard, Administração, Administração do Sistema, Configurações) recebem conteúdo. Fornecedores, Materiais, Serviços, Solicitações, Cotações, Negociação, Aprovação (transacional), Pedidos, Recebimento Fiscal, Pagamentos e Relatórios permanecem placeholder (Ondas 2 a 4). Ver as mesmas dúvidas de produto registradas em `ComprasFuncional.md`.

## Nota de escopo desta atualização (R1.1 — Revisão Arquitetural da Onda 1)

A ADR-0020 (`.ai/DECISIONS.md`) aprovou o corte definitivo entre Administração / Administração do Sistema / Configurações e acrescentou sub-telas de Administração: Gestão de Unidades de Alocação, Gestão de Filiais, Gestão de Centros de Custo (substituindo, no caso de Unidades de Alocação, o conceito informal de "Gestão de Empresas"), além de mover Workflow, Alçadas e Controle Orçamentário para `Administração` e reduzir `Configurações` a preferências pessoais (Conta, Preferências, Tema, Idioma). O modelo de segurança passa a ser RBAC exclusivo por perfil — nenhuma tela deve expor concessão de permissão individual a usuário. Os mapas de navegação abaixo foram atualizados para refletir essa decisão; nomes de tela seguem sempre "Gestão de X", nunca "Cadastro de X", para dados integrados do ERP.

## Design System obrigatório

Toda tela abaixo usa exclusivamente o **AZZAS 2154 — GDT Design System** (`resources/design-system/`):

- **Tokens:** `resources/design-system/colors_and_type.css` (cores, tipografia, raios, sombras, espaçamento) e `fonts.css` (Inter Tight, DM Sans, DM Mono).
- **Componentes de referência:** `resources/design-system/ui_kits/portal-gdt/` (`components.jsx`, `shell.jsx`) — kit clicável do GDT (não específico do +Compras, mas linguagem visual oficial obrigatória).
- **Paleta:** base bege quente `#F7F6F3` / branco `#FFFFFF` / bordas `#E2E0DB`, texto quase-preto `#1A1916`. Status semânticos em par cor+fundo pastel: azul = novo, laranja = avaliação, verde = aprovado, vermelho = rejeitado, roxo = aguardando.
- **Tipografia:** Inter Tight 700 (display/heros), DM Sans 300–600 (corpo, 13–14px dominante), DM Mono 300–500 (numérico, IDs, section titles em UPPERCASE com letter-spacing 0.07em).
- **Ícones:** SVG inline estilo Lucide/Feather, stroke 2px, `currentColor`, 14×14 ou 16×16px. Sem emoji, sem Heroicons/Material Icons/Font Awesome.
- **Cantos:** cards 12px (`--radius`), inputs/botões/badges grandes 8px (`--radius-sm`), tags pequenas 4px (`--radius-xs`), filtros/status/search/nav-badges pill 100px.
- **Header:** 56px (`--header-h`) para SHELL com tab-nav no cabeçalho; 64px (`--header-h-portal`) para portal standalone.
- **Sidebar:** três larguras (`240px` listas densas, `200px` avaliação, `220px` admin), escondida abaixo de 768px; variante **"Afirmativa"** (fundo `--accent` + texto branco no item ativo) é a recomendada para o contexto de Administração; variante **"Quieta"** (fundo `--bg` no ativo, sem inversão de cor) para telas operacionais densas.
- **Logo:** sempre `assets/logos/azzas-2154-mark-black.png`, 22px de altura; nunca substituir por SVG-texto ou monograma improvisado.
- **Voz e copy:** português brasileiro, tratamento "você", verbo no imperativo direto nos botões ("Aprovar", "Salvar", "Cancelar"), mensagens factuais sem exclamação decorativa nem emoji ("Demanda aprovada com sucesso." e não "🎉 Aprovado!").
- Qualquer necessidade visual sem componente equivalente no Design System é registrada como **GAP do Design System** (ver seção ao final deste documento), nunca inventada ad hoc.

## Índice

# Visão Geral

## Objetivo

Consolidar a experiência de navegação da Onda 1: login, seleção de contexto (`UnidadeNegocioId`), Dashboard e Administração — base sobre a qual os módulos operacionais das Ondas seguintes serão anexados ao AppShell.

## Wireframe

Referência visual de shell (não específica do +Compras, usada como padrão AZZAS 2154/GDT): `resources/design-system/ui_kits/portal-gdt/index.html`, `shell.jsx`. Estrutura: cabeçalho fixo com logo + navegação por abas (`nav-tab`) + user-chip com dropdown à direita; corpo com conteúdo do módulo ativo.

## Navegação

### Mapa de telas (Onda 1)

```
Bootstrap (somente enquanto não existir Administrador Sênior — ADR-0020, R1.2)
 └─ encerra permanentemente → Login

Login
 └─ Seleção da Unidade de Negócio (se o usuário tiver acesso a mais de uma)
     └─ Dashboard (home do AppShell)
         ├─ Administração
         │   ├─ Gestão de Unidades de Negócio
         │   ├─ Gestão de Unidades de Alocação
         │   ├─ Gestão de Filiais
         │   ├─ Gestão de Centros de Custo
         │   ├─ Gestão de Usuários
         │   ├─ Gestão de Perfis
         │   ├─ Workflow
         │   ├─ Alçadas
         │   ├─ Controle Orçamentário
         │   └─ Configuração ERP
         ├─ Administração do Sistema
         │   ├─ Identity Providers
         │   ├─ Feature Flags
         │   └─ Parâmetros
         └─ Configurações
             ├─ Conta
             ├─ Preferências
             ├─ Tema
             └─ Idioma
```

Sub-telas aprovadas pela ADR-0020 ainda sem wireframe detalhado nesta revisão (existem apenas como item de menu): `Administração > Notificações`; `Administração do Sistema > Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs, Saúde`.

### Menu completo do Portal (estado alvo, todas as Ondas)

Mapa oficial do Portal Operacional +Compras (ADR-0017, `docs/frontend/Frontend.md`), com o estado de cada módulo nesta atualização:

```
+Compras
├── Dashboard                              🟡 estrutura visual
├── Administração                          🟢 nesta Onda (Unidades de Negócio, Unidades de Alocação, Filiais, Centros de Custo, Usuários, Perfis, Workflow, Alçadas, Controle Orçamentário, Configuração ERP)
├── Administração do Sistema               🟢 nesta Onda (Identity Providers, Feature Flags, Parâmetros)
├── Configurações                          🟡 estrutura visual (Conta, Preferências, Tema, Idioma — ADR-0020, sem wireframe detalhado ainda)
├── Fornecedores                           🟢 funcional (implementado fora de sequência — Onda 2 formal)
│   ├── Lista
│   ├── Cadastro
│   ├── Detalhes
│   ├── Sincronização ERP
│   └── Auditoria
├── Pedidos                                🟡 estrutura visual (Onda 3)
├── Cotações                               🟡 estrutura visual (Onda 3)
├── Negociações                            🟡 estrutura visual (Onda 3)
├── Contratos                              🟡 estrutura visual (fora do MVP 1.0 — ver Versão 1.1)
├── Indicadores                            🟡 estrutura visual (Onda 3/4)
└── Agentes IA                             ⚪ planejado (sem estrutura visual ou funcional)
```

Legenda: 🟢 funcional, 🟡 estrutura visual, ⚪ planejado. A mudança de estado exige evidência de código, API integrada, testes e aceite de Work Order (`docs/frontend/Frontend.md`).

### Menu visível nesta Onda

O menu do AppShell nesta Onda exibe apenas: **Dashboard**, **Administração**, **Administração do Sistema**, **Configurações** — e, quando o usuário tiver perfil compatível, **Fornecedores** (já funcional). Os módulos ainda 🟡/⚪ aparecem no menu apenas se o Product Owner decidir manter a visão de roadmap completa (ver `fluxo_compras_indiretas_html.html`/ADR-0017: "estrutura visual demonstrativa, sem funcionalidades falsas") — **GAP/decisão de UX:** confirmar se módulos 🟡/⚪ ficam visíveis no menu como "em breve" ou ficam ocultos até terem ao menos estrutura visual.

### Fluxo de login

```
1. Tela de Login: e-mail corporativo
2. Validação de domínio + envio de código (ou redirecionamento ao Identity Provider federado, quando Entra ID estiver ativo)
3. Tela de verificação: código de 6 dígitos
4. Sucesso → contexto UnidadeNegocioId definido automaticamente (uma Unidade) ou tela de Seleção da Unidade de Negócio (mais de uma)
5. Dashboard
```

### Seleção de Unidade de Negócio

Quando o usuário tem acesso a mais de uma Unidade de Negócio, a seleção ocorre em tela dedicada, imediatamente após o login e antes do Dashboard. A partir da seleção, `UnidadeNegocioId` fica disponível em toda a sessão (refletido na URL: `/soma`, `/reserva`, `/hering`, `/arezzo`) e determina ERP, Workflow, Aprovação, Controle Orçamentário e Identity Provider ativos.

### Contexto `UnidadeNegocioId`

- Sempre presente na URL após a seleção (`https://maiscompras.somagrupo.com.br/{unidade}`).
- Exibido de forma persistente no cabeçalho do AppShell (ex.: junto ao user-chip ou como rótulo ao lado do logo) para que o usuário nunca perca a noção de em qual Unidade de Negócio está operando — **GAP/decisão de UX:** o local exato (rótulo no header vs. seletor persistente) não está definido; ver dúvida de produto equivalente em `ComprasFuncional.md`.
- Trocar de Unidade de Negócio (quando o usuário tiver acesso a mais de uma) deve recarregar visualmente todo o contexto dependente e informar claramente que a troca ocorreu.

### Perfis simulados (para navegação do Mock)

Para o Mock navegável poder demonstrar visibilidade de menu por perfil sem depender de Identity/backend reais, recomenda-se simular localmente (sem persistência), a exemplo do padrão já usado no kit de referência (`data.js`/`shell.jsx`, filtragem de `modules` por `perfis`):

| Perfil simulado | Módulos visíveis no Mock |
|---|---|
| Administrador | Dashboard, Administração, Administração do Sistema, Configurações, Fornecedores |
| Comprador | Dashboard, Fornecedores, (demais módulos 🟡/⚪ conforme decisão de menu) |
| Aprovador | Dashboard, (módulos de aprovação quando existirem — Onda 3) |
| Solicitante | Dashboard, (módulos de solicitação quando existirem — Onda 3) |

**GAP/dúvida de produto:** catálogo definitivo de perfis ainda não aprovado (ver `ComprasFuncional.md`, dúvida 5); esta tabela é hipótese de trabalho para o Mock, não especificação.

### Jornada do administrador

```
Login → Seleção de UN (se aplicável) → Dashboard
  → Administração → cadastra Unidade de Negócio (se nova) → cadastra Usuários → cria Perfis → associa Permissões
  → Administração do Sistema → configura Identity Provider → configura ERP da Unidade → (opcional) ativa Feature Flags → ajusta Parâmetros
  → Configurações → configura Workflow → configura Alçadas de Aprovação → configura Controle Orçamentário
  → Unidade de Negócio pronta para a Onda 2 (Cadastros)
```

### Jornada do usuário comum

```
Login → Seleção de UN (se aplicável) → Dashboard
  → (nesta Onda, sem módulo operacional próprio — aguarda Onda 2/3)
```

## Componentes

Ver cada tela.

## Estados da Tela

Ver cada tela.

## Responsividade

- Sidebar (variantes "Afirmativa"/"Quieta") escondida abaixo de 768px em todas as telas de Administração/Configurações; navegação nesse breakpoint deve migrar para menu compacto (padrão a definir com o Design System — **GAP:** o kit de referência não documenta o comportamento mobile da sidebar além de "escondida").
- Header da SHELL (56px) e header de portal standalone (64px) mantêm-se fixos em todas as larguras.
- Tabelas de listagem (Unidades de Negócio, Unidades de Alocação, Filiais, Centros de Custo, Usuários, Perfis, Identity Providers, Feature Flags, Parâmetros, Workflow, Alçadas, Regras Orçamentárias) usam o componente `component-table.html` do Design System, com rolagem horizontal em telas estreitas.

## Validações Visuais

Ver cada tela.

## Observações

Nenhuma.

---

# Login

## Objetivo

Prover a experiência de **Login Passwordless via OTP por e-mail** — mecanismo oficial da Onda 1 (ADR-0020, revisão R1.2) — visualmente alinhada ao AZZAS 2154/GDT, com dois passos: e-mail corporativo e código de verificação, ou redirecionamento federado quando o Identity Provider da Unidade de Negócio for Entra ID (coexistindo com o OTP como Identity Providers alternativos da mesma Unidade).

## Wireframe

Referência direta (padrão de mercado do GDT, reaproveitável tal como é): `resources/design-system/ui_kits/portal-gdt/shell.jsx` → `AuthScreen`, `AuthEmailStep`, `AuthOtpStep`. Estrutura: card centralizado (`auth-card`) com logo + divisor + subtítulo institucional; passo 1 com campo de e-mail; passo 2 com `OtpInput` de 6 células e contador de reenvio.

## Navegação

- Passo 1 (e-mail) → Passo 2 (código) → autenticado → Seleção de UN ou Dashboard.
- "Usar outro e-mail" retorna do Passo 2 ao Passo 1, limpando o código.
- Sem navegação lateral (tela pré-autenticação, sem AppShell).

## Componentes

| Componente do Design System | Uso nesta tela |
|---|---|
| `Logo` | Cabeçalho do card de login. |
| `Eyebrow` | Rótulo "Passo 1 · E-mail" / "Passo 2 · Código". |
| Campo de texto (`field-input`) | E-mail corporativo. |
| `OtpInput` | Código de 6 dígitos, com suporte a colar o código completo. |
| `NoticeBox` (kind="info") | Aviso de validade do código (15 minutos). |
| `Button` (variant="primary") | "Continuar" / "Verificar e entrar", com estado `loading`. |
| Link de texto (`resend-btn`, `auth-back`) | "Reenviar código" (com contador) e "Usar outro e-mail". |

## Estados da Tela

| Estado | Comportamento |
|---|---|
| Loading | Botão principal exibe spinner (`btn-loading`) e é desabilitado durante o envio do e-mail/validação do código. |
| Vazio | Não aplicável (formulário sempre tem os campos visíveis). |
| Sucesso | Transição automática para Seleção de UN/Dashboard; nenhuma mensagem de sucesso adicional é necessária (a navegação já comunica o sucesso). |
| Erro | Mensagem inline sob o campo (`auth-error`): e-mail inválido, domínio não autorizado, código incompleto/incorreto. |

## Responsividade

Card de login centralizado com largura máxima fixa; em telas estreitas, ocupa a largura disponível com padding lateral — sem sidebar, sem tabela, comportamento já contemplado pelo componente de referência.

## Validações Visuais

- E-mail: formato válido (`@` e `.`) e domínio autorizado pela Unidade de Negócio/Identity Provider — mensagem: "Informe um e-mail corporativo válido." / "Apenas e-mails {domínio} são autorizados."
- Código: exatamente 6 dígitos antes de habilitar "Verificar e entrar"; auto-submit ao completar os 6 dígitos.
- Mensagens seguem a voz do Design System: factuais, sem emoji, tratamento "você" implícito.

## Observações

O componente de referência é do kit genérico GDT (não específico do +Compras) — reaproveitável como está, ajustando apenas textos institucionais ("Portal +Compras" no lugar de "Gestão de Demandas de Tecnologia") e o domínio de e-mail (`@somagrupo.com.br` ou o domínio de cada Unidade de Negócio). A implementação desta tela exige revisão do Agente Engenheiro de Segurança Sênior antes e validação de segurança depois (ADR-0020) — ver `ComprasFuncional.md`.

## Componentes React previstos

- Page (`LoginPage`)
- AuthCard
- AuthEmailStep
- AuthOtpStep
- OtpInput
- NoticeBox
- Button

---

# Bootstrap

## Objetivo

Prover a experiência de inicialização do +Compras quando não existir nenhum Administrador Sênior cadastrado (ADR-0020, R1.2), substituindo a tela de Login enquanto essa condição existir.

## Wireframe

Sem componente de referência direto no kit GDT (fluxo de inicialização não existe no kit genérico). Recomenda-se um assistente em etapas (wizard) reaproveitando o mesmo card centralizado do Login: passo 1 (dados da primeira Unidade de Negócio), passo 2 (dados do primeiro Administrador Sênior), passo 3 (confirmação e encerramento do Bootstrap).

## Navegação

- Passo 1 (Unidade de Negócio) → Passo 2 (Administrador Sênior) → Passo 3 (confirmação) → Bootstrap encerrado → redireciona para `Login`.
- Sem opção de "voltar" após a confirmação final (o encerramento é permanente, ADR-0020).
- Sem navegação lateral (tela pré-autenticação, sem AppShell); acessível apenas enquanto não existir Administrador Sênior.

## Componentes

| Componente do Design System | Uso nesta tela |
|---|---|
| `Logo` | Cabeçalho do card, mesma identidade do Login. |
| `component-status-stepper.html` | Indicador de progresso do wizard (3 passos). |
| Campo de texto (`field-input`) | Identificador/nome da Unidade de Negócio, nome/e-mail do Administrador Sênior. |
| `NoticeBox` (kind="warn") | Aviso de que a conclusão do Bootstrap é permanente e não pode ser desfeita. |
| `Button` (variant="primary") | "Continuar" / "Concluir Bootstrap", com estado `loading`. |

## Estados da Tela

| Estado | Comportamento |
|---|---|
| Loading | Botão principal exibe spinner durante a criação da Unidade/Administrador. |
| Vazio | Não aplicável (formulário sempre tem os campos visíveis). |
| Sucesso | Transição automática para `Login` após confirmação; mensagem factual de que o Bootstrap foi concluído. |
| Erro | Mensagem inline sob o campo com falha de validação; erro de persistência exibido em `NoticeBox` (kind="error"). |

## Responsividade

Card/wizard centralizado com largura máxima fixa, mesmo padrão do Login; etapas empilhadas verticalmente em telas estreitas.

## Validações Visuais

- Identificador da Unidade de Negócio único; campos obrigatórios de Unidade e Administrador Sênior validados antes de avançar cada passo.
- Confirmação explícita obrigatória no passo 3 (ex.: checkbox ou botão dedicado) antes de habilitar "Concluir Bootstrap", dado o caráter permanente da ação.

## Observações

Esta tela não deve ser acessível quando já existir qualquer Administrador Sênior cadastrado — o frontend deve verificar essa condição antes de renderizar o Bootstrap em vez do Login. **GAP de Design System:** não existe componente de referência pronto para wizard/assistente em etapas no kit GDT atual — recomenda-se reaproveitar `component-status-stepper.html` combinado ao card de Login, sem introduzir um padrão visual novo. A implementação desta tela exige revisão do Agente Engenheiro de Segurança Sênior antes e validação de segurança depois (ADR-0020) — ver `ComprasFuncional.md`.

## Componentes React previstos

- Page (`BootstrapPage`)
- BootstrapWizard
- StepIndicator
- Form
- NoticeBox
- Button

---

# Seleção da Unidade de Negócio

## Objetivo

Permitir a escolha explícita da Unidade de Negócio quando o usuário autenticado tiver acesso a mais de uma, antes de entrar no Dashboard.

## Wireframe

Sem componente de referência direto no kit GDT (contexto multiempresa não existe no kit genérico). Recomenda-se card centralizado similar ao de Login, listando as Unidades como itens selecionáveis (cards ou lista com estado hover/active, seguindo o padrão de card do Design System: borda 1px `--border`, radius 12px, hover com `--border-hover` + sombra 1 + `translateY(-1px)`).

## Navegação

- Exibida apenas quando o usuário tiver mais de uma Unidade de Negócio vinculada; senão, o sistema pula direto para o Dashboard.
- Seleção → Dashboard (com `UnidadeNegocioId` definido).

## Componentes

| Componente do Design System | Uso nesta tela |
|---|---|
| `Logo` | Cabeçalho, mesma identidade do Login. |
| Card de seleção (padrão `component-cards.html`) | Um card por Unidade de Negócio disponível. |
| `Button` (variant="primary") | "Continuar" após seleção. |

## Estados da Tela

| Estado | Comportamento |
|---|---|
| Loading | Skeleton dos cards de Unidade enquanto a lista de Unidades do usuário é carregada (padrão `component-loading-states.html`). |
| Vazio | Não deveria ocorrer para um usuário autenticado (todo usuário tem ao menos uma Unidade); se ocorrer, exibir mensagem de erro de configuração e orientar contato com o Administrador. |
| Sucesso | Navegação imediata ao Dashboard após seleção. |
| Erro | Mensagem factual caso a definição de contexto falhe (ex.: "Não foi possível definir a Unidade de Negócio. Tente novamente."). |

## Responsividade

Cards em grade responsiva (`auto-fit`), uma coluna em telas estreitas.

## Validações Visuais

Seleção obrigatória antes de habilitar "Continuar" (quando não for seleção automática ao clique no próprio card).

## Observações

**GAP de Design System:** não existe componente de referência pronto para "seletor de tenant/empresa" no kit GDT atual — recomenda-se criar um card de seleção reaproveitando os tokens existentes (cores, radius, sombra), sem introduzir um padrão visual novo.

## Componentes React previstos

- Page (`SelecaoUnidadeNegocioPage`)
- UnidadeNegocioCard
- Button

---

# Dashboard

## Objetivo

Página inicial do Portal após autenticação/seleção de Unidade de Negócio: visão executiva estrutural, sem dados de negócio ainda reais nesta Onda.

## Wireframe

Referência direta de estrutura de boas-vindas: `shell.jsx` → `WelcomeScreen` (saudação + atalhos para até 4 módulos). Para a versão "home" definitiva do Dashboard (com indicadores), a referência visual de KPIs/cards é `preview/component-kpi-hero.html`, `preview/component-stats.html` e `preview/component-summary-strip.html` do Design System — usados aqui apenas como estrutura visual (sem dados reais), conforme a regra de não simular funcionalidade inexistente.

## Navegação

Home do AppShell; a partir daqui o usuário navega para Administração, Administração do Sistema, Configurações (e Fornecedores, já funcional).

## Componentes

| Componente do Design System | Uso nesta tela |
|---|---|
| `WelcomeScreen`/saudação | "Bem-vindo, {primeiro nome}". |
| Atalhos de módulo (`welcome-module-btn`) | Acesso rápido aos módulos disponíveis para o perfil do usuário. |
| Cards de estrutura visual (KPI/stat, sem dados reais) | Indicação honesta de roadmap para módulos 🟡/⚪. |

## Estados da Tela

| Estado | Comportamento |
|---|---|
| Loading | Skeleton dos cards estruturais durante o carregamento inicial do shell. |
| Vazio | Estado normal e esperado nesta Onda: nenhum dado de negócio real ainda existe; a tela deve comunicar isso de forma honesta ("Os indicadores de compras aparecem aqui a partir dos próximos módulos."), nunca com placeholder que pareça dado real. |
| Sucesso | Não aplicável (tela de leitura, sem ação de escrita). |
| Erro | Mensagem factual em caso de falha ao carregar módulos disponíveis para o perfil. |

## Responsividade

Grade de atalhos/cards reflui para coluna única em telas estreitas.

## Validações Visuais

Nenhuma (tela sem formulário).

## Observações

Nenhum dado fictício de negócio deve aparecer nesta tela nesta Onda (mesma regra de `docs/frontend/Frontend.md`/ADR-0017 aplicada aos demais módulos demonstrativos).

## Componentes React previstos

- Page (`DashboardPage`)
- WelcomeHeader
- ModuleShortcutGrid
- EmptyStateCard

---

# Administração

## Objetivo

Prover navegação e layout consistentes para as sub-telas de governança organizacional, cadastros integrados do ERP e motores de regra de negócio da Unidade (ADR-0020): Gestão de Unidades de Negócio, Gestão de Unidades de Alocação, Gestão de Filiais, Gestão de Centros de Custo, Gestão de Usuários, Gestão de Perfis, Workflow, Alçadas, Controle Orçamentário, Configuração ERP.

## Wireframe

Sidebar variante **"Afirmativa"** (`220px`, item ativo com fundo `--accent` e texto branco) + área de conteúdo com listagem em tabela e ação "Novo" no canto superior direito — padrão `preview/component-sidebar-variants.html` + `preview/component-table.html`.

## Navegação

```
Administração (sidebar)
├── Gestão de Unidades de Negócio    → Lista → Criar/Editar (modal ou tela dedicada)
├── Gestão de Unidades de Alocação   → Lista → Criar/Editar (origem +Compras) / Ativar-Inativar (origem ERP)
├── Gestão de Filiais                → Lista (somente leitura, origem ERP) → Ativar/Inativar
├── Gestão de Centros de Custo       → Lista (somente leitura, origem ERP) → Ativar/Inativar → Vincular Unidades de Alocação
├── Gestão de Usuários               → Lista → Criar/Editar
├── Gestão de Perfis                 → Lista → Criar/Editar → Associar Permissões do catálogo
├── Workflow                         → Lista → Criar/Editar regra
├── Alçadas                          → Lista → Criar/Editar alçada
├── Controle Orçamentário            → Lista → Criar/Editar regra
└── Configuração ERP                 → Selecionar Unidade → Configurar
```

Não existe, em nenhuma sub-tela, ação de "conceder permissão individual ao usuário" — o modelo é RBAC exclusivo por perfil (ADR-0020); a única forma de dar acesso diferente é associar outro perfil.

## Componentes

| Componente do Design System | Uso |
|---|---|
| Sidebar "Afirmativa" | Navegação entre as sub-telas de Administração. |
| `component-table.html` | Listagens (Unidades de Negócio, Unidades de Alocação, Filiais, Centros de Custo, Usuários, Perfis, Workflow, Alçadas, Controle Orçamentário). |
| `component-filters.html` | Filtro por status (Ativo/Inativo) e busca textual. |
| `component-badges-status.html` | Badge de status Ativo/Inativo. |
| `component-badges-meta.html` | Badge distinguindo origem ERP vs. +Compras (Unidades de Alocação) e indicando "somente leitura ERP" em Filiais/Centros de Custo. |
| `Button` (variant="primary", icon="plus") | Ação "Novo" (quando a origem permitir criação). |
| `component-modal-toast.html` / Drawer | Formulário de criação/edição (modal para operações rápidas, drawer para formulários mais longos como Usuário com múltiplos vínculos, ou vínculo Centro de Custo × Unidade de Alocação). |

## Estados da Tela

| Estado | Comportamento |
|---|---|
| Loading | Skeleton de linhas de tabela (`component-loading-states.html`). |
| Vazio | "Nenhum resultado — Não foram encontrados registros com os filtros selecionados." (voz do Design System) para listas filtradas; para a primeira utilização (nenhum registro cadastrado), CTA de criação em destaque. |
| Sucesso | Toast factual: "{Entidade} salva com sucesso." / "{Entidade} inativada com sucesso." |
| Erro | Toast de erro (`toast error`) com mensagem sanitizada; erros de validação de campo aparecem inline no formulário. |

## Responsividade

Sidebar oculta abaixo de 768px; navegação migra para menu compacto (**GAP** — ver "Visão Geral > Responsividade").

## Validações Visuais

- Campos obrigatórios marcados e validados antes do envio (ex.: e-mail de Usuário, identificador de Unidade de Negócio).
- Confirmação explícita antes de Inativar (modal: "Tem certeza que deseja inativar {entidade}? Essa ação não pode ser desfeita." — padrão de copy do Design System).

## Observações

Nenhuma.

## Componentes React previstos

- Page (`AdministracaoPage`)
- AdminSidebar
- Table
- Filter
- Toolbar
- Modal / Drawer
- Form
- Badge
- Toast

## Gestão de Unidades de Negócio

### Objetivo

Listar e cadastrar Unidades de Negócio.

### Wireframe

Tabela (identificador, nome, ERP associado, status) + formulário em modal/drawer.

### Navegação

Lista → Novo/Editar → Salvar → volta à lista.

### Componentes

`component-table.html`, `component-badges-status.html`, `component-form-selectors.html` (seleção de ERP associado), `Button`.

### Estados da Tela

Loading (skeleton), Vazio ("Nenhuma Unidade de Negócio cadastrada."), Sucesso (toast), Erro (toast + inline).

### Responsividade

Tabela com rolagem horizontal em telas estreitas.

### Validações Visuais

Identificador único (mensagem inline se já existir); campos obrigatórios destacados.

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Modal
- Form
- StatusBadge

## Gestão de Unidades de Alocação

### Objetivo

Listar Unidades de Alocação (origem ERP ou +Compras), cadastrar as de origem +Compras e ativar/inativar as de origem ERP.

### Wireframe

Tabela (identificador, tipo, origem, descrição ERP, descrição +Compras, status) + formulário em modal/drawer, com campos de origem ERP somente leitura quando aplicável.

### Navegação

Lista → Novo (somente origem +Compras) / Editar Descrição +Compras (qualquer origem) → Salvar.

### Componentes

`component-table.html`, `component-badges-meta.html` (badge de origem ERP/+Compras e de Tipo), `component-form-selectors.html` (Tipo, Unidade de Negócio), `Button`.

### Estados da Tela

Loading (skeleton), Vazio ("Nenhuma Unidade de Alocação cadastrada."), Sucesso (toast), Erro (toast + inline).

### Responsividade

Tabela com rolagem horizontal em telas estreitas.

### Validações Visuais

Campo de origem ERP nunca editável; identificador único; campos obrigatórios destacados.

### Observações

Substitui "Gestão de Empresas" (ADR-0020); esse nome não deve aparecer em nenhum wireframe ou componente.

### Componentes React previstos

- Table
- Modal
- Form
- StatusBadge
- OriginBadge

## Gestão de Filiais

### Objetivo

Listar Filiais sincronizadas do ERP e permitir ativação/inativação no +Compras.

### Wireframe

Tabela (Código CliFor, Nome CliFor — somente leitura, Descrição +Compras, status) + edição inline ou modal simples para Descrição +Compras e toggle de status.

### Navegação

Lista → Ativar/Inativar (toggle inline) → Editar Descrição +Compras (modal) → Salvar.

### Componentes

`component-table.html`, toggle/switch, `component-badges-status.html`, `NoticeBox` (kind="info") explicando que dados de origem ERP não são editáveis.

### Estados da Tela

Loading (skeleton), Vazio ("Nenhuma Filial sincronizada."), Sucesso (toast), Erro.

### Responsividade

Tabela com rolagem horizontal.

### Validações Visuais

Código/Nome CliFor sempre somente leitura; nenhuma ação de criação exposta (dado é integrado do ERP).

### Observações

Nome oficial da tela é "Gestão de Filiais"; "Cadastro de Filiais" não deve ser usado (ADR-0020).

### Componentes React previstos

- Table
- Toggle
- Modal
- StatusBadge
- NoticeBox

## Gestão de Centros de Custo

### Objetivo

Listar Centros de Custo sincronizados do ERP, ativar/inativar no +Compras e vincular Unidades de Alocação permitidas.

### Wireframe

Tabela (Código ERP, Descrição ERP — somente leitura, Descrição +Compras, status, Unidades de Alocação vinculadas) + drawer para gerenciar o vínculo N:N com Unidades de Alocação (multi-seleção + indicação de padrão).

### Navegação

Lista → Ativar/Inativar (toggle inline) → Editar Descrição +Compras → Gerenciar Unidades de Alocação (drawer) → Selecionar padrão → Salvar.

### Componentes

`component-table.html`, toggle/switch, `component-form-selectors.html` (multi-seleção de Unidade de Alocação), `component-badges-meta.html` (chip de Unidade de Alocação padrão), `NoticeBox`.

### Estados da Tela

Loading (skeleton), Vazio ("Nenhum Centro de Custo sincronizado."), Sucesso (toast), Erro.

### Responsividade

Drawer de vínculo ocupa largura total em telas estreitas.

### Validações Visuais

Código/Descrição ERP sempre somente leitura; Unidade de Alocação padrão deve pertencer ao conjunto de Unidades permitidas selecionadas.

### Observações

Nome oficial da tela é "Gestão de Centros de Custo"; "Cadastro de Centros de Custo" não deve ser usado (ADR-0020). A autorização de acesso de usuário a Centros de Custo é gerenciada em `Gestão de Usuários`, não aqui.

### Componentes React previstos

- Table
- Toggle
- Drawer
- MultiSelect
- Chip
- NoticeBox

## Gestão de Usuários

### Objetivo

Listar e cadastrar usuários, com vínculo a Unidade(s) de Negócio, Perfil(is) e Centro(s) de Custo autorizado(s).

### Wireframe

Tabela (nome, e-mail, Unidades vinculadas, Perfis vinculados, status) + drawer de formulário (mais longo, por isso drawer em vez de modal), com seção adicional de Centros de Custo autorizados.

### Navegação

Lista → Novo/Editar (drawer) → seleção múltipla de Unidades, Perfis e Centros de Custo autorizados → Salvar.

### Componentes

`component-table.html`, `component-form-selectors.html` (multi-seleção de Unidade de Negócio, Perfil e Centro de Custo), `component-badges-meta.html` (chips de Unidades/Perfis/Centros de Custo na listagem), `Button`.

### Estados da Tela

Loading, Vazio ("Nenhum usuário cadastrado."), Sucesso, Erro — mesmo padrão da Administração.

### Responsividade

Drawer ocupa largura total em telas estreitas.

### Validações Visuais

E-mail com formato válido e único; ao menos uma Unidade de Negócio e um Perfil selecionados antes de salvar.

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Drawer
- Form
- MultiSelect
- Chip

## Gestão de Perfis

### Objetivo

Listar e cadastrar perfis (RBAC exclusivo por perfil, ADR-0020), associando permissões do catálogo do sistema. `Permissões` deixa de ser sub-tela própria — o catálogo é exibido dentro desta mesma tela.

### Wireframe

Tabela (nome, descrição, quantidade de permissões, status) + drawer com lista de permissões selecionáveis (checkbox agrupado por módulo — Administração, Administração do Sistema, Fornecedores etc.).

### Navegação

Lista → Novo/Editar (drawer) → selecionar permissões do catálogo, agrupadas por módulo → Salvar.

### Componentes

`component-table.html`, checkbox group por módulo (padrão de formulário do Design System), `Button`.

### Estados da Tela

Loading, Vazio ("Nenhum perfil cadastrado."), Sucesso, Erro.

### Responsividade

Lista de permissões com rolagem interna no drawer em telas estreitas.

### Validações Visuais

Ao menos uma permissão selecionada antes de salvar.

### Observações

Catálogo de perfis é pendência de produto (ver `ComprasFuncional.md`); a mecânica RBAC (perfil como única unidade de concessão de acesso, sem exceção individual) está decidida (ADR-0020). Nenhuma tela do Portal deve oferecer "conceder permissão direta ao usuário".

### Componentes React previstos

- Table
- Drawer
- CheckboxGroup
- Form

## Configuração ERP

### Objetivo

Registrar o ERP associado a cada Unidade de Negócio e seus parâmetros de conexão/mapeamento. A partir da revisão R1.1 (ADR-0020), esta sub-tela pertence a `Administração`, não mais a `Administração do Sistema`.

### Wireframe

Formulário por Unidade de Negócio (seleção de sistema ERP + parâmetros de conexão mascarados) — sem tabela de mapeamento de domínios nesta Onda (conteúdo populado a partir da Onda 2, conforme ADR-0016).

### Navegação

Selecionar Unidade de Negócio → Configurar → Salvar.

### Componentes

`component-form-selectors.html` (sistema ERP), campo mascarado para parâmetros de conexão, `NoticeBox` (kind="warn") para alertar que a integração efetiva só ocorre na Onda 4.

### Estados da Tela

Loading, Vazio ("Nenhum ERP configurado para esta Unidade de Negócio."), Sucesso, Erro.

### Responsividade

Formulário em coluna única em telas estreitas.

### Validações Visuais

Parâmetros obrigatórios conforme o sistema ERP selecionado.

### Observações

Nenhuma leitura/escrita real no ERP ocorre a partir desta tela nesta Onda.

### Componentes React previstos

- Form
- SecretField
- NoticeBox

## Workflow

### Objetivo

Configurar regras de workflow (etapas, condições, responsáveis). A partir da revisão R1.1 (ADR-0020), esta sub-tela pertence a `Administração`, não mais a `Configurações`.

### Wireframe

Tabela de regras + editor de etapas em drawer, usando um componente de etapas sequenciais (referência: `component-status-stepper.html`/`component-step-progress.html`) para representar visualmente a sequência configurada.

### Navegação

Lista → Nova/Editar regra → Definir etapas e condições → Salvar.

### Componentes

`component-table.html`, `component-status-stepper.html` (editor/preview de etapas), `component-form-selectors.html` (condições).

### Estados da Tela

Loading, Vazio ("Nenhuma regra de workflow cadastrada."), Sucesso, Erro.

### Responsividade

Editor de etapas em coluna única (etapas verticais) em telas estreitas.

### Validações Visuais

Ao menos uma etapa definida antes de salvar; **GAP de produto:** catálogo definitivo de etapas ainda não aprovado (ver `ComprasFuncional.md`).

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Drawer
- StepBuilder
- Form

## Alçadas

### Objetivo

Configurar alçadas de aprovação (critério, nível, aprovador). A partir da revisão R1.1 (ADR-0020), esta sub-tela pertence a `Administração`, não mais a `Configurações`, e é renomeada de "Aprovação" para **"Alçadas"** para não ser confundida com o fluxo transacional de Aprovação da Onda 3.

### Wireframe

Tabela de alçadas + drawer de formulário com critério + lista ordenável de níveis de aprovação.

### Navegação

Lista → Nova/Editar alçada → Definir critério e níveis → Salvar.

### Componentes

`component-table.html`, lista ordenável de níveis (drag-and-drop ou reordenação por botões — **GAP:** kit de referência não documenta um componente de lista ordenável; avaliar reordenação simples por botões "subir/descer" como alternativa sem componente novo), `component-form-selectors.html` (aprovador/perfil).

### Estados da Tela

Loading, Vazio ("Nenhuma alçada cadastrada."), Sucesso, Erro.

### Responsividade

Formulário em coluna única em telas estreitas.

### Validações Visuais

Ao menos um critério e um nível de aprovação definidos antes de salvar.

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Drawer
- Form
- OrderableList (ou botões de reordenação)

## Controle Orçamentário

### Objetivo

Configurar regras orçamentárias (centro de custo, categoria, período, limite, comportamento em estouro). A partir da revisão R1.1 (ADR-0020), esta sub-tela pertence a `Administração`, não mais a `Configurações`.

### Wireframe

Tabela de regras + drawer de formulário, com seleção de Centro de Custo a partir de `Administração > Gestão de Centros de Custo`.

### Navegação

Lista → Nova/Editar regra → Definir limite e comportamento em estouro → Salvar.

### Componentes

`component-table.html`, `component-form-selectors.html` (centro de custo, categoria, comportamento em estouro), campo monetário formatado.

### Estados da Tela

Loading, Vazio ("Nenhuma regra orçamentária cadastrada."), Sucesso, Erro.

### Responsividade

Formulário em coluna única em telas estreitas.

### Validações Visuais

Limite orçamentário deve ser um valor monetário positivo; comportamento em estouro obrigatório.

### Observações

Fonte de verdade do saldo orçamentário é pendência de produto (ver `ComprasFuncional.md`).

### Componentes React previstos

- Table
- Drawer
- Form
- CurrencyInput

---

# Fornecedores

Fora do escopo desta atualização (Onda 2). Ver nota em `ComprasFuncional.md`.

# Materiais

Fora do escopo desta atualização (Onda 2).

# Serviços

Fora do escopo desta atualização (Onda 2).

# Solicitações

Fora do escopo desta atualização (Onda 3).

# Cotações

Fora do escopo desta atualização (Onda 3).

# Negociação

Fora do escopo desta atualização (Onda 3).

# Aprovação

Fluxo transacional fora do escopo desta atualização (Onda 3). Configuração de alçadas em `Administração > Alçadas` (a partir da revisão R1.1/ADR-0020), nesta mesma Onda 1.

# Pedidos

Fora do escopo desta atualização (Onda 3).

# Recebimento Fiscal

Fora do escopo desta atualização (Onda 4).

# Pagamentos

Fora do escopo desta atualização (Onda 4).

# Relatórios

Onda não classificada nesta leitura — ver dúvida de produto em `ComprasFuncional.md`.

---

# Administração do Sistema

## Objetivo

Prover navegação e layout para as sub-telas técnicas e de observabilidade operacional (ADR-0020): Identity Providers, Feature Flags, Parâmetros. `Integrações`, `Monitor`, `Filas`, `Reprocessamentos`, `Auditoria`, `Logs` e `Saúde` foram aprovadas como itens de menu por esta ADR, mas ainda não têm wireframe detalhado — **PENDÊNCIA** para a próxima revisão.

## Wireframe

Mesmo padrão de sidebar "Afirmativa" + tabela da Administração, para manter consistência visual entre as duas áreas administrativas.

## Navegação

```
Administração do Sistema (sidebar)
├── Identity Providers
├── Feature Flags
├── Parâmetros
├── Integrações          (sem wireframe detalhado — PENDÊNCIA)
├── Monitor               (sem wireframe detalhado — PENDÊNCIA)
├── Filas                 (sem wireframe detalhado — PENDÊNCIA)
├── Reprocessamentos      (sem wireframe detalhado — PENDÊNCIA)
├── Auditoria             (sem wireframe detalhado — PENDÊNCIA)
├── Logs                  (sem wireframe detalhado — PENDÊNCIA)
└── Saúde                 (sem wireframe detalhado — PENDÊNCIA)
```

`Configuração ERP` deixou de pertencer a esta seção a partir da revisão R1.1 (ADR-0020) — ver `Administração > Configuração ERP`.

## Componentes

Mesmos componentes de `Administração` (Table, Filter, Modal/Drawer, Form, Badge, Toast).

## Estados da Tela

Mesmo padrão de Loading/Vazio/Sucesso/Erro das demais telas administrativas.

## Responsividade

Mesmo padrão de `Administração`.

## Validações Visuais

Campos sensíveis (credenciais de Identity Provider, parâmetros de conexão ERP) nunca exibem valor salvo em texto claro — apenas indicação de "configurado"/"não configurado", com opção de substituir o valor.

## Observações

Nenhuma.

## Componentes React previstos

- Page (`AdministracaoSistemaPage`)
- AdminSidebar
- Table
- Modal / Drawer
- Form
- SecretField (campo mascarado)

## Identity Providers

### Objetivo

Cadastrar Identity Providers por Unidade de Negócio.

### Wireframe

Tabela (Unidade de Negócio, tipo de provider, domínio(s), status) + formulário com campos sensíveis mascarados.

### Navegação

Lista → Novo/Editar → Salvar.

### Componentes

`component-table.html`, campo mascarado (padrão `component-inputs.html` com variante password/secret), `NoticeBox` para alertas de configuração incompleta.

### Estados da Tela

Loading, Vazio ("Nenhum Identity Provider configurado para esta Unidade de Negócio."), Sucesso, Erro (ex.: falha ao validar configuração, se houver ação de teste de conexão).

### Responsividade

Mesmo padrão das demais tabelas.

### Validações Visuais

Domínio de e-mail em formato válido; campos obrigatórios conforme o tipo de provider selecionado.

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Form
- SecretField
- NoticeBox

## Feature Flags

### Objetivo

Ativar/desativar funcionalidades por Unidade de Negócio.

### Wireframe

Tabela (nome da flag, descrição, Unidades ativas, status) com toggle inline por linha.

### Navegação

Lista → Toggle por Unidade de Negócio (sem tela de detalhe, ação direta na linha).

### Componentes

`component-table.html`, toggle/switch (padrão de formulário do Design System), `component-badges-status.html`.

### Estados da Tela

Loading, Vazio ("Nenhuma feature flag disponível." — esperado nesta Onda, catálogo populado por módulos futuros), Sucesso (toast ao alternar), Erro.

### Responsividade

Tabela com rolagem horizontal.

### Validações Visuais

Confirmação antes de desativar uma flag em produção — **GAP/decisão de UX:** confirmar se a Onda 1 distingue ambientes (dev/homologação/produção) nesta tela; hoje o projeto opera só em Desenvolvimento Local (ADR-0018).

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Toggle
- StatusBadge

## Parâmetros

### Objetivo

Editar parâmetros de sistema.

### Wireframe

Tabela (chave, valor, descrição, Unidade de Negócio quando aplicável) com edição inline ou em modal.

### Navegação

Lista → Editar valor → Salvar.

### Componentes

`component-table.html`, campo de edição inline ou modal simples, `NoticeBox` explicando o efeito do parâmetro ao lado do campo.

### Estados da Tela

Loading, Vazio ("Nenhum parâmetro cadastrado." — esperado nesta Onda), Sucesso, Erro.

### Responsividade

Tabela com rolagem horizontal.

### Validações Visuais

Tipo de valor validado conforme o parâmetro (texto, número, booleano).

### Observações

Nenhuma.

### Componentes React previstos

- Table
- Modal
- Form

---

# Configurações

## Objetivo

Prover navegação e layout para as preferências pessoais do usuário autenticado (ADR-0020): Conta, Preferências, Tema, Idioma. Não confundir com os motores de regra de negócio (Workflow, Alçadas, Controle Orçamentário), que passam a `Administração` a partir da revisão R1.1.

## Wireframe

Sem wireframe detalhado nesta revisão — sub-telas aprovadas apenas como item de menu pela ADR-0020. **PENDÊNCIA** para a próxima revisão.

## Navegação

```
Configurações (sidebar ou menu do user-chip)
├── Conta            (sem wireframe detalhado — PENDÊNCIA)
├── Preferências      (sem wireframe detalhado — PENDÊNCIA)
├── Tema              (sem wireframe detalhado — PENDÊNCIA)
└── Idioma            (sem wireframe detalhado — PENDÊNCIA)
```

## Componentes

A definir na próxima revisão.

## Estados da Tela

A definir na próxima revisão.

## Responsividade

A definir na próxima revisão.

## Validações Visuais

A definir na próxima revisão.

## Observações

Esta seção existe, nesta revisão, apenas como item de índice aprovado pela ADR-0020; sua especificação de UX completa é trabalho pendente de uma próxima revisão, não desta.

## Componentes React previstos

A definir na próxima revisão.

---


# IA

## Objetivo

Nenhuma tela nesta Onda; módulo "Agentes IA" aparece no menu, quando exibido, apenas como ⚪ Planejado.

## Wireframe

Se exibido no menu, usar o mesmo padrão de `TabPlaceholder` do kit de referência: mensagem honesta de que o conteúdo entra em Onda futura, sem simular funcionalidade.

## Navegação

Não aplicável (sem sub-telas).

## Componentes

`TabPlaceholder` (ou equivalente) — mensagem factual de roadmap.

## Estados da Tela

Único estado: placeholder informativo.

## Responsividade

Não aplicável.

## Validações Visuais

Não aplicável.

## Observações

Nenhuma.

## Componentes React previstos

- TabPlaceholder

---

# Glossário

Ver `ComprasFuncional.md` (glossário único, não duplicado aqui).

---

# GAPs do Design System (para avaliação futura)

1. **Seletor de Unidade de Negócio/tenant:** não existe componente de referência pronto no kit GDT atual para seleção de empresa/tenant multiempresa — recomenda-se compor com os tokens de card existentes, sem criar padrão visual novo.
2. **Lista ordenável (drag-and-drop) para níveis de alçada de aprovação:** não documentada no kit de referência; avaliar reordenação por botões como alternativa mínima antes de introduzir uma biblioteca de drag-and-drop.
3. **Comportamento responsivo da sidebar abaixo de 768px:** o Design System documenta que a sidebar fica "escondida", mas não documenta o padrão de navegação que a substitui (menu hambúrguer, tabs, etc.) — decisão de UX pendente antes da Onda 1.2.
4. **Rótulo persistente de `UnidadeNegocioId` no header:** não há um componente/padrão documentado para exibir o contexto de Unidade de Negócio ativo de forma persistente — recomenda-se avaliar extensão do `UserChip`/header ou um badge dedicado, seguindo os tokens existentes.
5. **Wizard/assistente em etapas para o Bootstrap:** não existe componente de referência pronto no kit GDT para um fluxo de inicialização em múltiplos passos — recomenda-se combinar `component-status-stepper.html` com o card centralizado já usado no Login, sem criar padrão visual novo.

---

# Dúvidas de produto

Ver `ComprasFuncional.md` (dúvidas únicas, não duplicadas aqui).
