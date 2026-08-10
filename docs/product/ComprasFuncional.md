# +Compras Funcional

## Objetivo do documento

Especificação funcional oficial do sistema +Compras. Descreve **o que o sistema faz**, em linguagem de negócio. Toda funcionalidade nasce primeiro aqui, antes de qualquer Mock navegável, UX, banco, API ou implementação (ver `.ai/ROADMAP.md`, estratégia Frontend First).

## Responsabilidade

- Não é documentação técnica.
- Não é documentação de arquitetura.
- É documentação de negócio: referência funcional única compartilhada entre negócio, produto, QA e desenvolvimento.
- Cada seção de tela/módulo segue o [template oficial](./templates/TelaTemplate.md).

## Público-alvo

Negócio, Produto, QA e Desenvolvimento.

## Regras de manutenção

- Atualizado sempre que uma funcionalidade for especificada ou evoluir — precede a criação do Mock navegável.
- Não duplica conteúdo de `+Compras UX` (como o usuário utiliza o sistema) nem da Arquitetura Técnica (como o sistema foi construído) — ver `.ai/DOCUMENTATION_STRATEGY.md`.
- Nenhum conteúdo é escrito antes da especificação real da funcionalidade correspondente.

## Nota de escopo desta atualização (O1.1 — Consolidação Funcional do +Compras)

Esta atualização documenta exclusivamente as telas da **Onda 1 — Fundação Funcional** (`.ai/ROADMAP.md`): frontend navegável, Administração completa e estrutura de Login/Seleção de Unidade de Negócio/Dashboard. Fornecedores, Materiais, Serviços, Solicitações, Cotações, Negociação, Aprovação (fluxo transacional de compra), Pedidos, Recebimento Fiscal, Pagamentos e Relatórios pertencem às Ondas 2 a 4 (`.ai/BACKLOG.md`, seção "Reclassificação oficial") e permanecem como placeholder, mesmo que partes de Fornecedores já existam tecnicamente implementadas fora de sequência (ver B1/B2/B2.1/B2.2 e a Work Order ativa `PortalMaisComprasFrontend.md`). Essa divergência entre implementação real e sequência formal de Ondas está registrada como **dúvida de produto** ao final deste documento.

## Nota de escopo desta atualização (R1.1 — Revisão Arquitetural da Onda 1)

A revisão arquitetural R1.1 (ADR-0020, `.ai/DECISIONS.md`) aprovou formalmente o corte entre as três seções do índice, substituindo a distribuição provisória registrada pela O1.1. A distribuição vigente é:

| Seção do índice | Sub-telas alocadas | Critério aplicado |
|---|---|---|
| Administração | Gestão de Unidades de Negócio, Gestão de Unidades de Alocação, Gestão de Filiais, Gestão de Centros de Custo, Gestão de Usuários, Gestão de Perfis, Workflow, Alçadas, Controle Orçamentário, Configuração ERP, Notificações | Governança organizacional, de acesso e motores de regra de negócio da Unidade de Negócio |
| Administração do Sistema | Identity Providers, Feature Flags, Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs, Saúde | Configuração técnica/infraestrutural e observabilidade operacional do ambiente |
| Configurações | Conta, Preferências, Tema, Idioma, Preferências pessoais | Preferências pessoais do usuário autenticado, não configuração da Unidade de Negócio |

Esta distribuição é decisão de produto aceita (ADR-0020), não mais proposta — resolve a dúvida de produto nº 2 registrada pela O1.1. `Permissões` deixa de ser sub-tela própria de `Administração`: o catálogo de permissões passa a ser gerenciado dentro de `Gestão de Perfis` (ver seção "Perfis" abaixo). `Workflow`, `Alçadas` (antiga "Aprovação" de configuração) e `Controle Orçamentário` deixam de pertencer a `Configurações` e passam a `Administração`, por serem motores de regra de negócio da Unidade, não preferência pessoal. `Configuração ERP` permanece em `Administração`, não em `Administração do Sistema`, por ser configuração de negócio por Unidade (qual ERP a Unidade usa), distinta da observabilidade técnica do ambiente.

## Índice

# Visão Geral

## Objetivo

Consolidar a documentação funcional necessária para que a Onda 1 do MVP 1.0 (Frontend First, `.ai/ROADMAP.md`) possa ser implementada sem dúvidas: frontend navegável completo, Administração operável e base para o blueprint completo do banco.

## Personas

- **Administrador da Unidade de Negócio** — configura Unidade de Negócio, usuários, perfis, permissões, Identity Provider, ERP, Workflow, alçadas de aprovação e orçamento da sua Unidade.
- **Usuário Comum** — colaborador autenticado que ainda não possui, na Onda 1, nenhum módulo operacional de compras disponível (Fornecedores/Solicitações/Cotações chegam nas Ondas 2 e 3); nesta Onda, sua jornada se limita a login, seleção de Unidade de Negócio e visão do Dashboard (estrutura visual).
- **Comprador** — perfil já previsto no modelo de permissões (ver `Administração > Perfis`), sem tela operacional própria nesta Onda.

## Fluxo

```
Login (e-mail corporativo + código)
  ↓
Seleção da Unidade de Negócio (se o usuário tiver acesso a mais de uma)
  ↓
Dashboard (estrutura visual — ver seção Dashboard)
  ↓
Administração (se o usuário tiver perfil administrador)
```

## Regras de Negócio

- Toda navegação e toda regra de negócio operam no contexto de exatamente uma `UnidadeNegocioId` por sessão (`ARCHITECTURE.md` §16).
- A primeira implantação (Onda 1) opera com uma única Unidade de Negócio, `UnidadeNegocioId = SOMA`; a arquitetura já suporta múltiplas Unidades sem reescrita (`ARCHITECTURE.md` §16, `ROADMAP.md` — "Administração (Onda 1)").
- Multiempresa por `UnidadeNegocioId` se estende a: Multi ERP, Multi Login, Workflow, Aprovação, Controle Orçamentário, ERP e Identity Provider — todos configuráveis por Unidade de Negócio (`ARCHITECTURE.md` §16).
- Nenhum módulo operacional de compras (Fornecedores, Materiais, Serviços, Solicitações, Cotações, Negociação, Pedidos, Recebimento Fiscal, Pagamentos) é entregue nesta Onda; onde já exista implementação técnica antecipada (Fornecedores), ela é tratada como módulo de Onda 2 já iniciado, não como entrega desta Onda.

## Campos

Não aplicável neste nível (seção de visão geral, sem tela própria).

## Ações

Não aplicável neste nível.

## Permissões

Ver `Administração > Perfis` e `Administração > Permissões`.

## Workflow

Ver `Administração > Workflow`.

## Controle Orçamentário

Ver `Administração > Controle Orçamentário`.

## Integrações

Nenhuma integração externa é exercida nesta Onda pelas telas de fundação (Login, Seleção de UN, Dashboard, Administração). Configuração ERP é cadastrada (Onda 1), mas a integração de dados é executada apenas a partir da Onda 4 (`ROADMAP.md`, "Estratégia de integração com o ERP").

## Banco +Compras

Blueprint completo do banco é entregável da Onda 1 (`ROADMAP.md`). Ver `docs/product/ComprasDataModel.md` para as entidades já mapeadas nesta atualização.

## Estruturas ERP

Nenhuma estrutura ERP é lida ou gravada pelas telas de fundação; `Configuração ERP` (Administração do Sistema) apenas registra parâmetros de conexão/mapeamento, sem executar sincronização nesta Onda.

## APIs

Nenhuma API de negócio é exposta como parte desta Onda além do necessário para autenticação, seleção de Unidade de Negócio e CRUDs de Administração (ver cada sub-tela).

## IA Envolvida

Nenhuma. A estratégia de IA (ADR-0013, ADR-0014) prevê agentes assistivos apenas a partir dos módulos operacionais das Ondas 2 e 3; nenhum agente atua sobre Login, Seleção de UN, Dashboard ou Administração.

## Auditoria

Toda operação de Administração (criação/edição/inativação de Unidade de Negócio, usuário, perfil, permissão, Identity Provider, configuração ERP, regra de workflow, alçada de aprovação, parâmetro orçamentário, feature flag, parâmetro do sistema) deve gerar registro de auditoria append-only: quem, quando, o quê, valor anterior, valor novo, `UnidadeNegocioId` e `CorrelationId` — mesmo padrão já adotado em Fornecedores (ver `docs/backend/integration/FornecedorSynchronization.md`).

## Critérios de Aceite

- [ ] Login, Seleção de Unidade de Negócio, Dashboard (estrutura) e todas as sub-telas de Administração especificadas neste documento.
- [ ] `+Compras UX` correspondente publicado e consistente com este documento.
- [ ] Blueprint de banco cobrindo as entidades desta Onda em `ComprasDataModel.md`.
- [ ] Nenhuma funcionalidade fora do escopo de Onda 1 implementada.

## Observações

Este documento não repete os fluxos operacionais de Fornecedores já implementados (B1/B2/B2.1/B2.2) — eles permanecem descritos em `docs/backend/procurement/Procurement.md` e `docs/frontend/Frontend.md` até que a Onda 2 os traga formalmente para `ComprasFuncional.md`.

---

# Login

## Objetivo

Autenticar o usuário corporativo para acesso ao +Compras por meio de **Login Passwordless via OTP por e-mail**, mecanismo oficial decidido pela revisão arquitetural R1.2 (ADR-0020), desacoplado do domínio de negócio e projetado para coexistir com o Microsoft Entra ID quando este for aprovado (`ARCHITECTURE.md` §16; ADR-0011; ADR-0020).

## Personas

Todo usuário do +Compras, independentemente de perfil.

## Fluxo

```
1. Usuário informa e-mail corporativo
2. Sistema valida domínio autorizado pela Unidade de Negócio/Identity Provider e envia código de verificação (OTP)
3. Usuário informa o código (6 dígitos)
4. Sistema valida o código, confirma que o usuário está Ativo e resolve o vínculo com a Unidade de Negócio
5. Sistema autentica a sessão e direciona para Seleção de Unidade de Negócio (se aplicável) ou Dashboard
```

Caso não exista nenhum Administrador Sênior cadastrado no +Compras, o Login é substituído pelo fluxo de `Bootstrap` (ver sub-tela `Bootstrap` abaixo) até a criação do primeiro Administrador Sênior.

Referência de comportamento (auth por e-mail + código, sem senha): `resources/design-system/ui_kits/portal-gdt/shell.jsx` (`AuthEmailStep`/`AuthOtpStep`) — kit de referência visual do AZZAS 2154/GDT Design System, não específico do +Compras.

## Regras de Negócio

- **Mecanismo oficial da Onda 1 (ADR-0020, R1.2):** Login Passwordless via OTP (código de verificação) enviado ao e-mail corporativo, sem senha. Esta decisão resolve a pendência de mecanismo de Login registrada pela O1.1/R1.1.
- O domínio do e-mail informado deve pertencer a um domínio autorizado pela Unidade de Negócio ou pelo Identity Provider configurado (`Administração do Sistema > Identity Providers`).
- Somente usuário com status **Ativo** pode concluir a autenticação (`Administração > Gestão de Usuários`).
- A autenticação sempre resolve/confirma o vínculo do usuário com uma Unidade de Negócio antes de liberar a sessão.
- Uma Unidade de Negócio pode ter **múltiplos Identity Providers configurados simultaneamente**: o OTP por e-mail é um Identity Provider entre outros possíveis.
- O Microsoft Entra ID é o provedor corporativo definitivo de produção e é projetado para **coexistir** com o OTP por e-mail como Identity Providers alternativos da mesma Unidade de Negócio — não para substituí-lo compulsoriamente. Entra ID **não está implementado** nesta Onda (`PROJECT_STATE.md`).
- O identificador temporário de desenvolvimento (`ICurrentIdentity`/`DevelopmentRequestIdentity`, ADR-0011) é restrito ao ambiente `Development` e não pode ser usado como login de produção.
- **Requisito obrigatório de segurança (ADR-0020, R1.2):** esta funcionalidade exige revisão arquitetural do Agente Engenheiro de Segurança Sênior antes da implementação e validação de segurança dedicada depois; não é considerada "Pronta" (`ROADMAP.md`) sem essas duas revisões documentadas.

## Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| E-mail corporativo | Sim | Deve pertencer a um domínio autorizado pela Unidade de Negócio ou pelo Identity Provider configurado. |
| Código de verificação (OTP) | Sim | 6 dígitos, validade limitada (referência visual: 15 minutos). |

Quando o Identity Provider da Unidade de Negócio for Entra ID, os campos de tela são substituídos pelo fluxo de autenticação federada do provedor (fora do controle do +Compras).

## Ações

- Continuar (envia o e-mail e solicita código OTP, ou redireciona para o Identity Provider federado, quando configurado).
- Verificar e entrar (valida o código).
- Reenviar código.
- Usar outro e-mail (retorna ao passo 1).
- Sair (logout, disponível após autenticado, ver Dashboard/AppShell).

## Permissões

Tela pública (pré-autenticação); não exige perfil.

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

- Identity Provider da Unidade de Negócio: OTP por e-mail (oficial da Onda 1, ADR-0020) e, futuramente, Microsoft Entra ID coexistindo como IdP alternativo — **PENDÊNCIA de implementação de ambos**.
- Serviço de envio de e-mail transacional para o código OTP — **PENDÊNCIA:** provedor ainda não escolhido/contratado; dependência explícita da Work Order de Estrutura da O1.2 (ADR-0020, risco registrado).

## Banco +Compras

Ver entidades `Usuario`, `IdentityProvider` e `SessaoAutenticacao` em `ComprasDataModel.md`.

## Estruturas ERP

Não aplicável.

## APIs

**PENDÊNCIA:** contrato de autenticação (endpoint, payload, formato de token/sessão) ainda não definido nesta Onda.

## IA Envolvida

Nenhuma.

## Auditoria

Toda tentativa de login (sucesso e falha) deve ser registrada: e-mail informado, Unidade de Negócio, resultado, IP/origem quando disponível, data/hora.

## Critérios de Aceite

- [ ] Usuário autentica com sucesso via OTP por e-mail.
- [ ] Domínio de e-mail não autorizado é rejeitado com mensagem clara.
- [ ] Usuário inativo não conclui a autenticação.
- [ ] Sessão expirada ou inválida redireciona para o Login.
- [ ] Auditoria de login registrada.
- [ ] Revisão do Agente Engenheiro de Segurança Sênior concluída antes da implementação; validação de segurança concluída depois (ADR-0020).

## Observações

Nenhuma tela do +Compras deve exibir controle de acesso definitivo antes do Entra ID estar aprovado e implementado, quando aplicável (`docs/frontend/Frontend.md`). Ver sub-tela `Bootstrap` para o caso de inicialização sem nenhum Administrador Sênior cadastrado.

---

# Bootstrap

## Objetivo

Permitir a inicialização de um novo ambiente +Compras quando não existir nenhum Administrador Sênior cadastrado — sem essa via, não haveria como criar o primeiro usuário administrador sem intervenção manual em banco de dados (ADR-0020, R1.2).

## Personas

Responsável técnico pela implantação inicial do ambiente (persona pré-autenticação, sem perfil administrativo prévio no +Compras).

## Fluxo

```
1. Sistema verifica se existe algum Administrador Sênior cadastrado
2. Se NÃO existir: sistema oferece o fluxo de Bootstrap em vez do Login
3. Usuário cadastra a primeira Unidade de Negócio
4. Usuário cadastra o primeiro Administrador Sênior (vinculado a essa Unidade)
5. Usuário completa a configuração inicial mínima necessária para o sistema operar
6. Sistema encerra o Bootstrap Mode permanentemente
7. Sistema direciona para o Login normal (OTP)
```

## Regras de Negócio

- O **Bootstrap Mode** só está disponível **enquanto não existir nenhum Administrador Sênior** cadastrado no +Compras (ADR-0020).
- Durante o Bootstrap Mode é possível criar: a primeira Unidade de Negócio, o primeiro usuário com perfil de Administrador Sênior, e a configuração inicial mínima.
- Assim que o primeiro Administrador Sênior é criado com sucesso, o Bootstrap Mode é **encerrado permanentemente** — não há reabertura, nem por perda de acesso, nem por remoção posterior de todos os Administradores Sênior.
- Recuperação de acesso após o encerramento do Bootstrap é procedimento operacional de suporte, fora do escopo desta tela.
- **PENDÊNCIA:** o perfil "Administrador Sênior" ainda não está no catálogo de Perfis aprovado (ver dúvida de produto sobre catálogo de Perfis/Permissões); a O1.2 precisa aprovar esse perfil como parte do catálogo antes de implementar o Bootstrap.
- **Requisito obrigatório de segurança (ADR-0020, R1.2):** esta funcionalidade exige revisão arquitetural do Agente Engenheiro de Segurança Sênior antes da implementação e validação de segurança dedicada depois.

## Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Identificador da Unidade de Negócio | Sim | Ver `Administração > Gestão de Unidades de Negócio`. |
| Nome da Unidade de Negócio | Sim | — |
| Nome do Administrador Sênior | Sim | — |
| E-mail corporativo do Administrador Sênior | Sim | Torna-se o identificador de login (OTP). |

## Ações

- Criar primeira Unidade de Negócio.
- Criar primeiro Administrador Sênior.
- Concluir configuração inicial e encerrar o Bootstrap Mode.

## Permissões

Nenhuma — disponível apenas na ausência de qualquer Administrador Sênior; após o encerramento, esta tela deixa de ser acessível a qualquer perfil.

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

Nenhuma.

## Banco +Compras

Ver entidades `UnidadeNegocio`, `Usuario`, `Perfil` em `ComprasDataModel.md`; estado do Bootstrap (`BootstrapConcluido`) — ver `ComprasDataModel.md`.

## Estruturas ERP

Não aplicável.

## APIs

**PENDÊNCIA:** contrato ainda não definido.

## IA Envolvida

Nenhuma.

## Auditoria

A conclusão do Bootstrap (criação da primeira Unidade de Negócio e do primeiro Administrador Sênior) é auditada de forma permanente e imutável — é o primeiro registro de auditoria do ambiente.

## Critérios de Aceite

- [ ] Bootstrap Mode disponível somente na ausência de qualquer Administrador Sênior.
- [ ] Encerramento permanente após a criação do primeiro Administrador Sênior, sem mecanismo de reabertura.
- [ ] Revisão do Agente Engenheiro de Segurança Sênior concluída antes da implementação; validação de segurança concluída depois (ADR-0020).

## Observações

Catálogo do perfil "Administrador Sênior" é pré-requisito de produto para esta tela — ver pendência acima.

---

# Seleção da Unidade de Negócio

## Objetivo

Estabelecer o contexto `UnidadeNegocioId` da sessão do usuário, do qual dependem ERP, Workflow, Aprovação, Controle Orçamentário e Identity Provider ativos (`ARCHITECTURE.md` §16).

## Personas

Todo usuário autenticado com acesso a mais de uma Unidade de Negócio.

## Fluxo

```
1. Sistema identifica as Unidades de Negócio às quais o usuário autenticado tem acesso
2. Se houver apenas uma Unidade: o sistema define o contexto automaticamente e segue para o Dashboard
3. Se houver mais de uma Unidade: o sistema exibe a lista para escolha explícita
4. Usuário seleciona a Unidade de Negócio
5. Sistema define o contexto UnidadeNegocioId da sessão e segue para o Dashboard
```

## Regras de Negócio

- O roteamento do produto ocorre sempre por `UnidadeNegocioId`, refletido na URL: `https://maiscompras.somagrupo.com.br/{unidade}` (ex.: `/soma`, `/reserva`, `/hering`, `/arezzo`) — `ARCHITECTURE.md` §16.
- Na Onda 1, a única Unidade de Negócio ativa é `SOMA`; a tela de seleção deve existir e estar preparada para múltiplas Unidades, mesmo operando hoje com uma única opção (`ROADMAP.md` — "Administração (Onda 1)").
- Trocar de Unidade de Negócio durante a sessão (quando o usuário tiver acesso a mais de uma) deve recarregar todo o contexto dependente: ERP, Workflow, Aprovação, Controle Orçamentário e Identity Provider ativos.
- **PENDÊNCIA:** não há definição de produto sobre se a troca de Unidade de Negócio, quando existir mais de uma, ocorre por uma tela dedicada (a desta seção) ou por um seletor persistente no cabeçalho (padrão comum em portais multiempresa). Ambas as opções são compatíveis com a arquitetura; a decisão de UX cabe ao Product Owner.

## Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Unidade de Negócio | Sim | Lista restrita às Unidades às quais o usuário tem acesso. |

## Ações

- Selecionar Unidade de Negócio.
- Continuar.

## Permissões

Disponível para qualquer usuário autenticado; a lista de opções é filtrada pelo vínculo usuário × Unidade de Negócio (ver `Administração > Usuários`).

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

Nenhuma nesta Onda.

## Banco +Compras

Ver entidades `UnidadeNegocio` e `UsuarioUnidadeNegocio` em `ComprasDataModel.md`.

## Estruturas ERP

Não aplicável — a Unidade de Negócio pode estar associada a um ERP específico (ver `Administração > Configuração ERP`), mas a seleção em si não lê o ERP.

## APIs

**PENDÊNCIA:** contrato ainda não definido (listar Unidades do usuário; definir contexto ativo da sessão).

## IA Envolvida

Nenhuma.

## Auditoria

Registrar toda troca de contexto de Unidade de Negócio durante a sessão: usuário, Unidade anterior, Unidade nova, data/hora.

## Critérios de Aceite

- [ ] Usuário com uma única Unidade de Negócio não vê a tela de seleção (contexto definido automaticamente).
- [ ] Usuário com múltiplas Unidades escolhe explicitamente antes de acessar o Dashboard.
- [ ] Todo o contexto dependente (ERP, Workflow, Aprovação, Orçamento, Identity Provider) reflete a Unidade selecionada.

## Observações

Nenhuma.

---

# Dashboard

## Objetivo

Ser a página inicial do Portal +Compras após login/seleção de Unidade de Negócio: visão executiva, indicadores, integrações, alertas e atividades recentes, sem substituir os módulos operacionais (ADR-0017).

## Personas

Todo usuário autenticado.

## Fluxo

```
Login/Seleção de UN → Dashboard → (usuário navega para Administração ou aguarda módulos futuros)
```

## Regras de Negócio

- O Dashboard é, nesta Onda, **estrutura visual** (🟡), não funcional — não existem ainda dados reais de indicadores de compras para exibir, porque os módulos operacionais (Fornecedores completo, Solicitações, Cotações, Pedidos) pertencem às Ondas 2 e 3 (`docs/frontend/Frontend.md`, tabela de estado dos módulos).
- O Dashboard não pode simular dados de negócio inexistentes (nenhuma funcionalidade falsa) — mesma regra aplicada aos módulos demonstrativos do Portal (`.ai/work-orders/active/PortalMaisComprasFrontend.md`).
- Conteúdo permitido nesta Onda: saudação ao usuário, atalhos para os módulos disponíveis (nesta Onda, apenas Administração), indicação honesta de que os demais módulos estão em roadmap.

## Campos

Não aplicável (tela de leitura/navegação).

## Ações

- Navegar para Administração (se o usuário tiver perfil administrador).
- Navegar para os módulos futuros exibidos como estrutura visual/planejado (sem ação funcional).

## Permissões

Disponível para qualquer usuário autenticado; os atalhos exibidos são filtrados pelo perfil do usuário (ver `Administração > Perfis`).

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável nesta Onda (nenhum dado orçamentário real ainda existe para exibir).

## Integrações

Nenhuma nesta Onda.

## Banco +Compras

Não aplicável nesta Onda — nenhuma entidade própria de Dashboard é criada; quando os indicadores reais existirem (Onda 2+), este documento será atualizado.

## Estruturas ERP

Não aplicável.

## APIs

Não aplicável nesta Onda (conteúdo estático/estrutural).

## IA Envolvida

Nenhuma.

## Auditoria

Não aplicável (tela sem escrita de dados).

## Critérios de Aceite

- [ ] Dashboard exibe estrutura visual honesta, sem dados fictícios de negócio.
- [ ] Navegação para Administração funcional para perfis autorizados.
- [ ] Demais módulos aparecem identificados como roadmap (não funcionais).

## Observações

Ver `docs/frontend/Frontend.md` para o estado alvo (🟢/🟡/⚪) de cada módulo do Portal — o Dashboard deve refletir esse estado, nunca antecipá-lo visualmente como concluído.

---

# Administração

> Governança organizacional, de acesso e motores de regra de negócio da Unidade de Negócio (ADR-0020). Sub-telas: Gestão de Unidades de Negócio, Gestão de Unidades de Alocação, Gestão de Filiais, Gestão de Centros de Custo, Gestão de Usuários, Gestão de Perfis, Workflow, Alçadas, Controle Orçamentário, Configuração ERP, Notificações.

## Objetivo

Permitir que o Administrador da Unidade de Negócio configure a estrutura organizacional, os cadastros integrados do ERP e os motores de regra de negócio do +Compras antes de qualquer módulo operacional entrar em uso.

## Personas

Administrador da Unidade de Negócio.

## Fluxo

```
Administração
├── Gestão de Unidades de Negócio
├── Gestão de Unidades de Alocação
├── Gestão de Filiais
├── Gestão de Centros de Custo
├── Gestão de Usuários
├── Gestão de Perfis
├── Workflow
├── Alçadas
├── Controle Orçamentário
├── Configuração ERP
└── Notificações
```

## Regras de Negócio

- Administração é implementada já na Onda 1, não como capacidade tardia (`ROADMAP.md`).
- Toda configuração é preparada para múltiplas Unidades de Negócio desde a Onda 1 (`ROADMAP.md`, `ARCHITECTURE.md` §16).
- Dados sincronizados do ERP (Filiais, Centros de Custo) são imutáveis no +Compras; toda tela de gestão de dado integrado do ERP distingue explicitamente código ERP, descrição ERP e descrição +Compras, sem jamais substituir ou ocultar a descrição oficial do ERP (ADR-0020).
- `Workflow`, `Alçadas` e `Controle Orçamentário` passam a pertencer a esta seção (não mais a `Configurações`), por serem motores de regra de negócio da Unidade — decisão da revisão R1.1 (ADR-0020), que substitui a distribuição provisória da O1.1.
- `Notificações` é sub-tela nova desta revisão (R1.1); seu conteúdo funcional específico (eventos, canais, destinatários) ainda não foi especificado — **PENDÊNCIA**.

## Campos

Ver cada sub-tela.

## Ações

Ver cada sub-tela.

## Permissões

Acesso restrito ao perfil Administrador (ver `Gestão de Perfis`); demais perfis não visualizam o módulo Administração no menu.

## Workflow

Ver sub-tela `Workflow` (nesta seção, a partir da revisão R1.1/ADR-0020).

## Controle Orçamentário

Ver sub-tela `Controle Orçamentário` (nesta seção, a partir da revisão R1.1/ADR-0020).

## Integrações

Nenhuma integração externa própria nesta seção, exceto o vínculo de configuração registrado em `Configuração ERP` (a integração efetiva ocorre apenas na Onda 4). Identity Providers, Feature Flags e demais itens técnicos pertencem a `Administração do Sistema`.

## Banco +Compras

Ver `ComprasDataModel.md`: `UnidadeNegocio`, `UnidadeAlocacao`, `Filial`, `CentroCusto`, `CentroCustoUnidadeAlocacao`, `UsuarioCentroCusto`, `Usuario`, `UsuarioUnidadeNegocio`, `Perfil`, `Permissao`, `PerfilPermissao`.

## Estruturas ERP

Não aplicável.

## APIs

Ver cada sub-tela.

## IA Envolvida

Nenhuma.

## Auditoria

Toda alteração em Unidade de Negócio, Usuário, Perfil ou Permissão gera registro de auditoria (ver seção "Visão Geral").

## Critérios de Aceite

Ver cada sub-tela.

## Observações

Nenhuma.

## Gestão de Unidades de Negócio

### Objetivo

Cadastrar e manter as Unidades de Negócio operadas pelo +Compras.

### Personas

Administrador (nível corporativo — cadastro de novas Unidades é uma operação sensível, tipicamente restrita a um perfil administrador de escopo mais amplo que o administrador de uma única Unidade; ver `Gestão de Perfis`).

### Fluxo

```
Listar Unidades de Negócio → Criar/Editar Unidade de Negócio → Salvar → Unidade disponível para seleção e configuração
```

### Regras de Negócio

- Cada Unidade de Negócio possui um identificador único (`UnidadeNegocioId`) refletido na URL (`ARCHITECTURE.md` §16, ex.: `soma`, `reserva`, `hering`, `arezzo`).
- Cada Unidade de Negócio pode ter um ERP distinto associado (ADR-0013: "Cada BU pode possuir um ERP distinto").
- Cada Unidade de Negócio pode ter um ou mais Identity Providers (`ARCHITECTURE.md` §16).
- Inativar uma Unidade de Negócio não pode remover seu histórico/auditoria.
- Nesta Onda, apenas `SOMA` está ativa; as demais podem ser pré-cadastradas como preparação, mas sem uso operacional (`ROADMAP.md`).

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Identificador (`UnidadeNegocioId`) | Sim | Único, usado na URL; imutável após criação (**PENDÊNCIA**: confirmar se é editável). |
| Nome | Sim | Nome de exibição da Unidade. |
| Status | Sim | Ativa / Inativa. |
| ERP associado | Não (nesta Onda) | Vínculo com `Administração > Configuração ERP`; pode ser definido depois. |

### Ações

- Criar Unidade de Negócio.
- Editar Unidade de Negócio.
- Ativar/Inativar Unidade de Negócio.
- Visualizar detalhes/auditoria.

### Permissões

Restrito ao perfil com permissão `UnidadeNegocio.Gerenciar` (ver `Gestão de Perfis`).

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Vínculo com `Configuração ERP` (Administração do Sistema) e `Identity Providers` (Administração do Sistema); sem integração própria.

### Banco +Compras

Entidade `UnidadeNegocio` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável (o vínculo com o ERP é configurado, não sincronizado, nesta Onda).

### APIs

**PENDÊNCIA:** CRUD de Unidade de Negócio ainda não possui contrato definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda criação/edição/inativação de Unidade de Negócio é auditada.

### Critérios de Aceite

- [ ] CRUD completo de Unidade de Negócio operável.
- [ ] Unidade `SOMA` disponível e ativa por padrão.
- [ ] Inativação preserva histórico.

### Observações

Nenhuma.

## Gestão de Unidades de Alocação

### Objetivo

Cadastrar e manter as Unidades de Alocação: a classificação gerencial da despesa usada para operação, orçamento e relatórios, substituindo formalmente o conceito informal e anterior de "Gestão de Empresas" (ADR-0020).

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar Unidades de Alocação → Criar/Editar Unidade de Alocação → Definir Tipo/Origem/Unidade de Negócio → Salvar
```

### Regras de Negócio

- Tipos iniciais: Marca, Corporativo, Localidade, Outro (ADR-0020).
- Origem: ERP (ex.: tabela Rede de Lojas) ou cadastro local no +Compras. Quando a origem é ERP, `CodigoErp` e `DescricaoErp` são imutáveis no +Compras, e apenas `DescricaoMaisCompras`/`AtivaNoMaisCompras` podem ser editados localmente.
- Exemplos: Animale, Farm, Fábula, SOMA Corporativo, Corporativo Jardim Botânico.
- Toda Unidade de Alocação pertence a uma Unidade de Negócio.
- Uma Unidade de Alocação inativa não pode ser selecionada em novas requisições, mas seu histórico permanece íntegro.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Identificador | Sim | Único. |
| Código ERP | Não | Presente somente quando a origem for ERP. |
| Descrição ERP | Não | Somente leitura; presente somente quando a origem for ERP. |
| Descrição +Compras | Não | Editável independentemente da origem. |
| Tipo | Sim | Marca / Corporativo / Localidade / Outro. |
| Origem | Sim | ERP / +Compras. |
| Ativa no +Compras | Sim | Controla disponibilidade para seleção; não altera o ERP. |
| Unidade de Negócio | Sim | — |

### Ações

- Criar Unidade de Alocação (somente quando origem = +Compras).
- Editar `DescricaoMaisCompras`.
- Ativar/Inativar no +Compras.
- Sincronizar a partir do ERP (quando origem = ERP) — **PENDÊNCIA:** mecanismo e frequência de sincronização não definidos nesta Onda.

### Permissões

Restrito ao perfil com permissão `UnidadeAlocacao.Gerenciar`.

### Workflow

Não aplicável.

### Controle Orçamentário

Unidade de Alocação é dimensão de agrupamento usado por `Controle Orçamentário` e por relatórios futuros; não define orçamento por si só.

### Integrações

ERP corporativo da Unidade de Negócio, quando a origem for ERP (ex.: tabela Rede de Lojas) — leitura, sem escrita.

### Banco +Compras

Entidade `UnidadeAlocacao` (ver `ComprasDataModel.md`).

### Estruturas ERP

Leitura somente, quando a origem for ERP; nenhuma alteração estrutural (`ROADMAP.md`, "Estratégia de integração com o ERP").

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda criação/edição/inativação de Unidade de Alocação é auditada.

### Critérios de Aceite

- [ ] CRUD de Unidades de Alocação de origem +Compras operável.
- [ ] Unidades de Alocação de origem ERP exibidas com código/descrição ERP somente leitura e descrição +Compras editável.
- [ ] Inativação preserva histórico.

### Observações

Substitui o conceito de "Gestão de Empresas"; nenhuma tela ou documento oficial deve manter esse nome (ADR-0020).

## Gestão de Filiais

### Objetivo

Gerenciar a disponibilidade, no +Compras, das Filiais integradas do ERP.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar Filiais (sincronizadas do ERP) → Ativar/Inativar no +Compras → Editar Descrição +Compras (opcional)
```

### Regras de Negócio

- Filiais são integradas do ERP; não podem ser criadas ou alteradas no +Compras (ADR-0020).
- Podem ser ativadas ou inativadas apenas para uso no +Compras; a inativação local não altera o ERP.
- `Código CliFor` e `Nome CliFor` são persistidos no banco +Compras porque compõem chaves usadas pelo ERP.
- `DescricaoMaisCompras` é opcional.
- O nome funcional oficial desta tela é **"Gestão de Filiais"**; "Cadastro de Filiais" não deve ser usado em nenhum material do produto (ADR-0020).

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Código CliFor | Sim | Somente leitura; origem ERP. |
| Nome CliFor | Sim | Somente leitura; origem ERP. |
| Descrição +Compras | Não | Editável. |
| Ativa no +Compras | Sim | Controla disponibilidade para uso no +Compras; não altera o ERP. |

### Ações

- Ativar/Inativar Filial no +Compras.
- Editar Descrição +Compras.

### Permissões

Restrito ao perfil com permissão `Filial.Gerenciar`.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável diretamente; Filial pode ser dimensão de relatório futuro.

### Integrações

ERP corporativo (leitura, via o mesmo padrão de sincronização já usado por Fornecedores/Linx — `docs/backend/integration/FornecedorSynchronization.md`); nenhuma escrita no ERP.

### Banco +Compras

Entidade `Filial` (ver `ComprasDataModel.md`).

### Estruturas ERP

Leitura somente; nenhuma alteração estrutural (`ROADMAP.md`, "Estratégia de integração com o ERP").

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda ativação/inativação e edição de Descrição +Compras é auditada.

### Critérios de Aceite

- [ ] Filiais sincronizadas do ERP exibidas com Código/Nome CliFor somente leitura.
- [ ] Ativação/inativação no +Compras operável, sem alterar o ERP.
- [ ] Tela nomeada "Gestão de Filiais" em toda a interface e documentação.

### Observações

Nenhuma.

## Gestão de Centros de Custo

### Objetivo

Gerenciar a disponibilidade, no +Compras, dos Centros de Custo integrados do ERP, e seu vínculo com Unidades de Alocação.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar Centros de Custo (sincronizados do ERP) → Ativar/Inativar no +Compras → Vincular Unidades de Alocação permitidas → (opcional) Definir Unidade de Alocação padrão
```

### Regras de Negócio

- Centros de Custo são integrados do ERP; seus dados mestres não podem ser alterados no +Compras (ADR-0020).
- Podem ser ativados ou inativados localmente, sem alterar o ERP.
- `DescricaoMaisCompras` é opcional.
- O nome funcional oficial desta tela é **"Gestão de Centros de Custo"**; "Cadastro de Centros de Custo" não deve ser usado em nenhum material do produto (ADR-0020).
- Cada Centro de Custo ativo pode possuir uma ou mais Unidades de Alocação permitidas (relação muitos-para-muitos), com uma podendo ser marcada como padrão.
- Ao escolher um Centro de Custo em uma requisição (Onda 3), o sistema filtra as Unidades de Alocação disponíveis a partir desse vínculo; se houver apenas uma permitida, ela pode ser preenchida automaticamente; não é permitido selecionar Unidade de Alocação fora do vínculo configurado.
- A Gestão de Centros de Custo (cadastro mestre) é separada da autorização de acesso do usuário a Centros de Custo (ver `Gestão de Usuários`); um usuário pode ter acesso a um, vários, ou a todos os Centros de Custo ativos, sem que isso altere o cadastro mestre.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Código ERP | Sim | Somente leitura; origem ERP. |
| Descrição ERP | Sim | Somente leitura; origem ERP. |
| Descrição +Compras | Não | Editável. |
| Ativo no +Compras | Sim | Controla disponibilidade para uso; não altera o ERP. |
| Unidades de Alocação permitidas | Não | Uma ou mais; ao menos uma recomendada antes do uso operacional (Onda 3). |
| Unidade de Alocação padrão | Não | Deve pertencer ao conjunto de Unidades de Alocação permitidas. |

### Ações

- Ativar/Inativar Centro de Custo no +Compras.
- Editar Descrição +Compras.
- Vincular/desvincular Unidades de Alocação permitidas.
- Definir Unidade de Alocação padrão.

### Permissões

Restrito ao perfil com permissão `CentroCusto.Gerenciar`. Autorização de acesso de usuário a Centros de Custo é permissão distinta, gerenciada em `Gestão de Usuários` — **PENDÊNCIA:** confirmar o nome exato dessa permissão (ex.: `CentroCusto.Acessar`).

### Workflow

Não aplicável nesta Onda; o vínculo Centro de Custo × Unidade de Alocação é consumido pelo fluxo de requisição a partir da Onda 3.

### Controle Orçamentário

Centro de Custo é a dimensão primária de `Controle Orçamentário` (ver `RegraOrcamentaria` em `ComprasDataModel.md`).

### Integrações

ERP corporativo (leitura, mesmo padrão de Filiais/Fornecedores); nenhuma escrita no ERP.

### Banco +Compras

Entidades `CentroCusto`, `CentroCustoUnidadeAlocacao` (associação) e `UsuarioCentroCusto` (autorização de acesso) — ver `ComprasDataModel.md`.

### Estruturas ERP

Leitura somente; nenhuma alteração estrutural (`ROADMAP.md`, "Estratégia de integração com o ERP").

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda ativação/inativação, edição de Descrição +Compras e alteração de vínculo com Unidades de Alocação é auditada.

### Critérios de Aceite

- [ ] Centros de Custo sincronizados do ERP exibidos com Código/Descrição ERP somente leitura.
- [ ] Vínculo N:N com Unidades de Alocação operável, com suporte a Unidade de Alocação padrão.
- [ ] Autorização de acesso de usuário a Centros de Custo não altera o cadastro mestre.
- [ ] Tela nomeada "Gestão de Centros de Custo" em toda a interface e documentação.

### Observações

Nenhuma.

## Gestão de Usuários

### Objetivo

Cadastrar usuários do +Compras e vinculá-los a uma ou mais Unidades de Negócio e a um ou mais Perfis.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar usuários → Criar/Editar usuário → Vincular Unidade(s) de Negócio e Perfil(is) → Salvar
```

### Regras de Negócio

- Um usuário pode ter acesso a mais de uma Unidade de Negócio (ver `Seleção da Unidade de Negócio`).
- **Consolidação do vínculo de acesso (ADR-0020, R1.2):** todo usuário carrega dois vínculos de acesso independentes entre si — um ou mais **Perfis** (governam permissões) e um ou mais **Centros de Custo** (governam o escopo de dados operacionais). Nenhum dos dois substitui o outro: um usuário pode ter um Perfil amplo e acesso restrito a poucos Centros de Custo, ou vice-versa.
- O modelo RBAC (ADR-0020) permite que um usuário tenha um ou vários Perfis (ex.: Administrador e Comprador simultaneamente); suas permissões efetivas são a união das permissões de todos os perfis vinculados. Usuários nunca recebem permissões individuais ou exceções diretas — ver `Gestão de Perfis`.
- Após a integração/configuração dos Centros de Custo (`Gestão de Centros de Custo`), um usuário pode ter acesso a um, vários, ou a **todos** os Centros de Custo ativos (ADR-0020). "Todos" é uma opção explícita de configuração — não uma listagem manual de todos os códigos existentes — para que a entrada de novos Centros de Custo não exija reconfiguração retroativa de usuários já marcados como "acesso total". Essa autorização é independente do cadastro mestre de Centro de Custo e não o altera.
- Login definitivo é OTP por e-mail (ADR-0020, R1.2), com Microsoft Entra ID coexistindo futuramente como Identity Provider alternativo; o cadastro de usuário nesta tela é o registro funcional do +Compras, não a fonte de autenticação.
- Usuário inativo não pode autenticar nem aparecer como aprovador/comprador disponível em outras telas.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome | Sim | — |
| E-mail corporativo | Sim | Único; usado como identificador de login (OTP). |
| Unidade(s) de Negócio | Sim (ao menos uma) | Define onde o usuário pode operar. |
| Perfil(is) | Sim (ao menos um) | Define permissões efetivas por união (ver `Gestão de Perfis`). |
| Acesso a Centro(s) de Custo | Não | Um, vários, ou "todos os ativos" (opção explícita); não altera o cadastro mestre. |
| Status | Sim | Ativo / Inativo. |

### Ações

- Criar usuário.
- Editar usuário.
- Ativar/Inativar usuário.
- Vincular/desvincular Unidade de Negócio.
- Vincular/desvincular Perfil.
- Autorizar/revogar acesso a Centro(s) de Custo.

### Permissões

Restrito ao perfil com permissão `Usuario.Gerenciar`.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Identity Provider da Unidade de Negócio, para a autenticação efetiva (fora do escopo desta tela, que apenas cadastra o usuário funcional).

### Banco +Compras

Entidades `Usuario`, `UsuarioUnidadeNegocio`, `UsuarioPerfil`, `UsuarioCentroCusto` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável — **PENDÊNCIA:** confirmar se o cadastro de compradores/usuários deve, no futuro, sincronizar com um cadastro de "comprador" do ERP (fora de escopo da Onda 1).

### APIs

**PENDÊNCIA:** CRUD de usuários ainda não possui contrato definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda criação/edição/inativação/vínculo de usuário é auditada.

### Critérios de Aceite

- [ ] CRUD completo de usuários operável.
- [ ] Vínculo com Unidade(s) de Negócio e Perfil(is) funcional.
- [ ] Usuário inativo não acessa o sistema.

### Observações

Nenhuma.

## Gestão de Perfis

### Objetivo

Definir os perfis de acesso do +Compras (ex.: Administrador, Comprador, Aprovador, Solicitante) segundo um modelo RBAC exclusivo por perfil, e o catálogo de permissões atômicas associado a cada um (ADR-0020).

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar perfis → Criar/Editar perfil → Associar permissões do catálogo → Salvar
```

### Regras de Negócio

- O modelo de segurança é **RBAC baseado exclusivamente em perfis** (ADR-0020). Cada Perfil contém nome, descrição, status, Unidade de Negócio (quando aplicável) e lista de permissões.
- Um usuário pode possuir um ou vários perfis (ver `Gestão de Usuários`); suas permissões efetivas são a **união** das permissões de todos os perfis vinculados.
- **Regra obrigatória:** usuários nunca recebem permissões individuais ou exceções diretas. Quando surgir uma necessidade de acesso diferente, deve ser criado um novo perfil — nunca uma exceção pontual no usuário (ADR-0020). Exemplo: "Analista" (criar, aprovar e cancelar pedido) e "Analista Jr" (somente criar pedido) são dois perfis distintos, não um perfil com exceção.
- O catálogo de permissões atômicas (ex.: `UnidadeNegocio.Gerenciar`, `Usuario.Gerenciar`, `Perfil.Gerenciar`, `Filial.Gerenciar`, `CentroCusto.Gerenciar`, `UnidadeAlocacao.Gerenciar`, `Fornecedor.Aprovar`) é gerenciado dentro desta mesma tela, não em sub-tela própria — a partir da ADR-0020, `Permissões` deixa de ser sub-tela independente de `Administração`.
- **PENDÊNCIA:** o catálogo definitivo de perfis do +Compras (nomes, papéis, se são globais ou por Unidade de Negócio) não está definido em nenhuma fonte oficial lida para esta atualização.
- **PENDÊNCIA:** não há definição oficial sobre se o catálogo de permissões é fixo (definido pelo código/sistema) ou permite criação de permissões customizadas via tela. A hipótese mais conservadora — permissões fixas, apenas visualizáveis e associáveis — é assumida neste documento até confirmação do Product Owner.
- Perfis podem ser específicos de uma Unidade de Negócio ou globais — **PENDÊNCIA de decisão**.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome do perfil | Sim | Único por Unidade de Negócio (ou global — pendência). |
| Descrição | Não | — |
| Permissões associadas | Sim (ao menos uma) | Selecionadas do catálogo de permissões atômicas do sistema. |
| Status | Sim | Ativo / Inativo. |
| Unidade de Negócio | Não | Presente quando o perfil não for global — pendência de decisão. |

### Ações

- Criar perfil.
- Editar perfil.
- Ativar/Inativar perfil.
- Associar/desassociar permissões do catálogo.
- Visualizar catálogo de permissões atômicas do sistema.

### Permissões

Restrito ao perfil com permissão `Perfil.Gerenciar`.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Nenhuma.

### Banco +Compras

Entidades `Perfil`, `Permissao`, `PerfilPermissao` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** CRUD de perfis e consulta do catálogo de permissões ainda não possuem contrato definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda criação/edição/inativação de perfil e alteração de permissões associadas é auditada. Não existe operação de "conceder permissão individual ao usuário" a ser auditada, pois essa operação não é permitida pelo modelo (ADR-0020).

### Critérios de Aceite

- [ ] CRUD completo de perfis operável.
- [ ] Associação de permissões do catálogo funcional.
- [ ] Nenhum mecanismo de permissão individual por usuário exposto na interface.
- [ ] Catálogo inicial de perfis e permissões aprovado pelo Product Owner (pendência bloqueante para homologação, não para navegação do mock).

### Observações

O modelo RBAC exclusivo por perfil é decisão aceita (ADR-0020); resta pendente apenas o conteúdo do catálogo (quais perfis e permissões existem), não a mecânica de autorização.

## Configuração ERP

### Objetivo

Registrar, por Unidade de Negócio, qual ERP está associado e os parâmetros de mapeamento necessários para a futura integração (Onda 4).

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Selecionar Unidade de Negócio → Configurar ERP associado e parâmetros de conexão/mapeamento → Salvar
```

### Regras de Negócio

- A partir da revisão R1.1 (ADR-0020), esta sub-tela passa a pertencer a `Administração` (não mais a `Administração do Sistema`), por ser configuração de negócio da Unidade — qual ERP a Unidade usa — e não observabilidade técnica do ambiente.
- Cada Unidade de Negócio pode possuir um ERP distinto (ADR-0013).
- O ERP nunca sofre alterações estruturais a partir do +Compras: proibidos `CREATE`, `ALTER`, `DROP`, triggers, CDC, Change Tracking ou qualquer alteração física (`ROADMAP.md`, "Estratégia de integração com o ERP"; `ARCHITECTURE.md` §17).
- Antes de qualquer integração efetiva (Onda 4), deve existir auditoria técnica da tabela ERP envolvida — esta tela apenas registra a configuração, não a executa.
- Referência real já implementada para o ERP Linx/fornecedores: `docs/backend/integration/FornecedorSynchronization.md` e `docs/backend/integration/Integration.md` — o padrão de connection string segregada (`MaisComprasConnection` / `ErpConnection`) deve se repetir para outros domínios quando aplicável.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Unidade de Negócio | Sim | — |
| Sistema ERP | Sim | Ex.: Linx (já usado por Fornecedores); outros — **PENDÊNCIA** de catálogo oficial de ERPs suportados. |
| Parâmetros de conexão | Sim, quando aplicável | Configuráveis via user-secrets/variáveis de ambiente, nunca hardcoded (`ARCHITECTURE.md`/ADR-0018). |
| Mapeamentos de domínio (ex.: `TipoFornecedor`, `CondicaoPagamento`) | Não, nesta Onda | Estrutura prevista por ADR-0016; conteúdo populado a partir da Onda 2. |

### Ações

- Configurar ERP da Unidade de Negócio.
- Editar configuração.
- Testar conexão — **PENDÊNCIA** de confirmação de escopo.

### Permissões

Restrito ao perfil com permissão `ConfiguracaoErp.Gerenciar`.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

ERP corporativo da Unidade de Negócio (ex.: `SOMA_DESENV`/`MAISCOMPRAS`, acessado via VPN — `docs/database/Database.md`); nenhuma leitura/escrita real ocorre nesta Onda.

### Banco +Compras

Entidade `ConfiguracaoErp` (ver `ComprasDataModel.md`).

### Estruturas ERP

Nenhuma leitura/escrita nesta Onda; apenas registro de configuração.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda alteração de configuração ERP é auditada; credenciais não aparecem em texto claro no log.

### Critérios de Aceite

- [ ] Configuração ERP cadastrável por Unidade de Negócio.
- [ ] Nenhuma operação estrutural ou de escrita real no ERP ocorre a partir desta tela.

### Observações

Nenhuma.

## Workflow

### Objetivo

Configurar o motor de workflow que orquestrará os passos de um processo de compra (Solicitação → Cotação → Negociação → Aprovação → Pedido) a partir da Onda 3.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar regras de workflow → Criar/Editar regra (etapas, condições, responsáveis) → Salvar
```

### Regras de Negócio

- A partir da revisão R1.1 (ADR-0020), esta sub-tela passa a pertencer a `Administração` (não mais a `Configurações`), por ser motor de regra de negócio da Unidade.
- O motor de `Workflow`/`WorkflowRunner` sequencial já existe tecnicamente no BlueprintOS, em estado básico (`PROJECT_STATE.md`); esta tela é a configuração de negócio sobre esse motor, não sua reimplementação.
- Workflow é configurável por Unidade de Negócio (`ARCHITECTURE.md` §16).
- **PENDÊNCIA:** o desenho exato das etapas configuráveis (nomes, condições, transições) para o processo de compras não está definido em nenhum documento funcional lido nesta atualização — apenas o fluxo de referência genérico descrito em `Proposta - Compras Indiretas.html`/`fluxo_compras_indiretas_html.html` (Solicitação → IA interpreta → Categorização → Budget → Aprovadores → Cotação → Equalização → Negociação → Pedido ERP → Fornecedor → Recebimento → Nota Fiscal → Entrada Fiscal → Pagamento → Encerramento), que é material de referência de mercado, não especificação aprovada do +Compras.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome da regra de workflow | Sim | — |
| Unidade de Negócio | Sim | — |
| Etapas | Sim | **PENDÊNCIA** de catálogo definitivo. |
| Condições de aplicação (categoria, valor, tipo de despesa) | Não | Depende do desenho aprovado. |
| Status | Sim | Ativa / Inativa. |

### Ações

- Criar regra de workflow.
- Editar regra de workflow.
- Ativar/Inativar regra de workflow.

### Permissões

Restrito ao perfil Administrador da Unidade de Negócio.

### Workflow

Esta é a própria tela de configuração do motor.

### Controle Orçamentário

Não aplicável diretamente (ver `Controle Orçamentário`, motor separado).

### Integrações

Nenhuma nesta Onda.

### Banco +Compras

Entidade `RegraWorkflow` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma na configuração; a orquestração inteligente sobre o workflow (ex.: IA sugerindo aprovadores) é capacidade futura, fora desta Onda.

### Auditoria

Toda criação/edição de regra de workflow é auditada.

### Critérios de Aceite

- [ ] Estrutura de configuração de workflow operável (mesmo com catálogo de etapas ainda simples/genérico nesta Onda).
- [ ] Desenho definitivo das etapas do processo de compras aprovado pelo Product Owner antes da Onda 3.

### Observações

Nenhuma.

## Alçadas

### Objetivo

Configurar alçadas de aprovação (por valor, categoria, Unidade de Negócio, centro de custo) que serão aplicadas ao processo de compras a partir da Onda 3.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar alçadas → Criar/Editar alçada (critério + aprovador(es) + nível) → Salvar
```

### Regras de Negócio

- A partir da revisão R1.1 (ADR-0020), esta sub-tela passa a pertencer a `Administração` (não mais a `Configurações`) e é renomeada de "Aprovação" para **"Alçadas"**, para não ser confundida com o fluxo transacional de Aprovação (Onda 3).
- Referência de mercado (não aprovada como especificação do +Compras, apenas como material de contexto): aprovação por valor, categoria, unidade e centro de custo; aprovação técnica, financeira, jurídica ou compliance; alçadas multinível; delegação e escalonamento por atraso (`fluxo_compras_indiretas_html.html`, etapa 5).
- Alçadas são configuráveis por Unidade de Negócio (`ARCHITECTURE.md` §16).
- **PENDÊNCIA:** critérios definitivos de alçada (quais dimensões — valor, categoria, centro de custo — são obrigatórias na Onda 1) não estão aprovados.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome da alçada | Sim | — |
| Critério (valor mínimo/máximo, categoria, centro de custo) | Sim (ao menos um) | **PENDÊNCIA** de catálogo definitivo. |
| Nível/ordem de aprovação | Sim | Sequência entre aprovadores. |
| Aprovador(es) ou perfil aprovador | Sim | Vínculo com `Administração > Gestão de Usuários`/`Gestão de Perfis`. |
| Unidade de Negócio | Sim | — |
| Status | Sim | Ativa / Inativa. |

### Ações

- Criar alçada.
- Editar alçada.
- Ativar/Inativar alçada.

### Permissões

Restrito ao perfil Administrador da Unidade de Negócio.

### Workflow

Integra-se ao motor de `Workflow` (etapa de aprovação dentro do fluxo configurado).

### Controle Orçamentário

Pode combinar-se com regras de `Controle Orçamentário` (ex.: exigir aprovação adicional em caso de estouro de verba) — **PENDÊNCIA** de definição de como essa combinação é configurada.

### Integrações

Nenhuma nesta Onda.

### Banco +Compras

Entidade `AlcadaAprovacao` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma nesta Onda.

### Auditoria

Toda criação/edição de alçada é auditada.

### Critérios de Aceite

- [ ] Estrutura de configuração de alçadas operável.
- [ ] Critérios definitivos de alçada aprovados pelo Product Owner antes da Onda 3.

### Observações

Nenhuma.

## Controle Orçamentário

### Objetivo

Configurar as regras de orçamento (por centro de custo, categoria, Unidade de Negócio) que serão consultadas antes de qualquer solicitação de compra avançar, a partir da Onda 3.

### Personas

Administrador da Unidade de Negócio.

### Fluxo

```
Listar regras orçamentárias → Criar/Editar regra (centro de custo, categoria, período, limite) → Salvar
```

### Regras de Negócio

- A partir da revisão R1.1 (ADR-0020), esta sub-tela passa a pertencer a `Administração` (não mais a `Configurações`), por ser motor de regra de negócio da Unidade.
- Referência de mercado (não aprovada como especificação do +Compras): consulta de orçamento disponível, verificação de saldo por centro de custo e categoria, identificação de estouro de verba, bloqueio/alerta/aprovação adicional (`fluxo_compras_indiretas_html.html`, etapa 4).
- Controle orçamentário é configurável por Unidade de Negócio (`ARCHITECTURE.md` §16).
- Centro de custo, nesta Onda, já é gerenciado em `Administração > Gestão de Centros de Custo`; o vínculo com Unidade de Alocação (ver essa sub-tela) é dimensão adicional de relatório, não de limite orçamentário nesta etapa.
- **PENDÊNCIA:** fonte de verdade do saldo orçamentário (ERP financeiro, planilha corporativa, ou saldo próprio do +Compras) não está definida — impacta diretamente o desenho de integração da Onda 4.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Centro de custo | Sim | Vínculo com `Administração > Gestão de Centros de Custo`. |
| Categoria | Não | Refinamento do limite. |
| Período (mês/ano, exercício) | Sim | — |
| Limite orçamentário | Sim | Valor monetário. |
| Comportamento em caso de estouro | Sim | Bloquear / Alertar / Exigir aprovação adicional. |
| Unidade de Negócio | Sim | — |

### Ações

- Criar regra orçamentária.
- Editar regra orçamentária.
- Ativar/Inativar regra orçamentária.

### Permissões

Restrito ao perfil Administrador da Unidade de Negócio.

### Workflow

Pode acionar aprovação adicional via `Alçadas` em caso de estouro configurado.

### Controle Orçamentário

Esta é a própria tela de configuração do motor.

### Integrações

**PENDÊNCIA:** possível integração futura com sistema financeiro/ERP para saldo real (Onda 4).

### Banco +Compras

Entidade `RegraOrcamentaria` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável nesta Onda.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma nesta Onda.

### Auditoria

Toda criação/edição de regra orçamentária é auditada.

### Critérios de Aceite

- [ ] Estrutura de configuração orçamentária operável.
- [ ] Fonte de verdade do saldo orçamentário definida antes da Onda 3/4.

### Observações

Nenhuma.

---

# Fornecedores

Fora do escopo desta atualização (Onda 2 — Cadastros, `.ai/BACKLOG.md`). Fornecedores já possui implementação técnica real e anterior à sequência formal de Ondas (B1, B2, B2.1, B2.1.1, B2.1.2, B2.1.3, B2.2 — concluídas; Portal Frontend com módulo Fornecedores conectado à API real). Essa divergência está registrada como dúvida de produto ao final deste documento. A especificação funcional formal de Fornecedores em `ComprasFuncional.md`, retroativa ao que já está implementado, é recomendada como item da O1.2 ou como abertura formal da Onda 2 — ver "Sugestões para a O1.2".

# Materiais

Conteúdo pertence à Onda 2 — Cadastros (`.ai/ROADMAP.md`, `.ai/BACKLOG.md`). Não desenvolvido nesta atualização.

# Serviços

Conteúdo pertence à Onda 2 — Cadastros. Não desenvolvido nesta atualização.

# Solicitações

Conteúdo pertence à Onda 3 — Processo de Compras. Não desenvolvido nesta atualização.

# Cotações

Conteúdo pertence à Onda 3 — Processo de Compras. Não desenvolvido nesta atualização.

# Negociação

Conteúdo pertence à Onda 3 — Processo de Compras. O fluxo consultivo de negociação por API (`POST /api/v1/negociacoes/recomendacoes`, Work Order A13) já existe tecnicamente, mas sem tela própria nesta Onda. Não desenvolvido nesta atualização.

# Aprovação

Fluxo transacional de aprovação de compra (Solicitação/Cotação/Pedido) pertence à Onda 3 — Processo de Compras. Não desenvolvido nesta atualização. A **configuração** de alçadas de aprovação está em `Administração > Alçadas` (a partir da revisão R1.1/ADR-0020), nesta mesma Onda 1.

# Pedidos

Conteúdo pertence à Onda 3 — Processo de Compras. Não desenvolvido nesta atualização.

# Recebimento Fiscal

Conteúdo pertence à Onda 4 — Integrações Operacionais. Não desenvolvido nesta atualização.

# Pagamentos

Conteúdo pertence à Onda 4 — Integrações Operacionais. Não desenvolvido nesta atualização.

# Relatórios

Conteúdo pertence a uma Onda ainda não classificada explicitamente (não aparece na tabela de reclassificação de `.ai/BACKLOG.md` lida nesta atualização). **PENDÊNCIA:** confirmar em qual Onda "Relatórios" será entregue.

---

# Administração do Sistema

> Configuração técnica/infraestrutural e observabilidade operacional do ambiente (ADR-0020). Sub-telas: Identity Providers, Feature Flags, Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs, Saúde.

## Objetivo

Concentrar as configurações técnicas e a observabilidade operacional que sustentam o funcionamento do +Compras: autenticação federada, controle de features, integrações técnicas, monitoramento de filas/reprocessamentos e trilhas de auditoria/log/saúde do sistema.

## Personas

Administrador de Sistema (perfil de escopo técnico, tipicamente mais restrito que o Administrador funcional de `Administração`) — **PENDÊNCIA:** confirmar se é o mesmo perfil Administrador ou um perfil técnico separado.

## Fluxo

```
Administração do Sistema
├── Identity Providers
├── Feature Flags
├── Integrações
├── Monitor
├── Filas
├── Reprocessamentos
├── Auditoria
├── Logs
└── Saúde
```

## Regras de Negócio

- `Configuração ERP` deixa de pertencer a esta seção e passa a `Administração` a partir da revisão R1.1 (ADR-0020), por ser configuração de negócio por Unidade, não observabilidade técnica do ambiente.
- `Integrações`, `Monitor`, `Filas`, `Reprocessamentos`, `Auditoria`, `Logs` e `Saúde` são sub-telas novas desta revisão (R1.1/ADR-0020); seu conteúdo funcional específico (campos, ações, contratos) ainda não foi especificado — **PENDÊNCIA**, a detalhar em revisão futura antes da Onda 1.2.
- **PENDÊNCIA:** `Parâmetros` (sub-tela já especificada por esta atualização, ver abaixo) não foi classificada explicitamente pela ADR-0020; permanece nesta seção até decisão em contrário do Product Owner.

## Campos

Ver cada sub-tela.

## Ações

Ver cada sub-tela.

## Permissões

Acesso restrito a perfil com escopo técnico (`Sistema.Gerenciar` ou equivalente).

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

Identity Providers e demais integrações técnicas são, por definição, pontos de integração externa (ver sub-telas).

## Banco +Compras

Ver `ComprasDataModel.md`: `IdentityProvider`, `FeatureFlag`, `Parametro`. Entidades de `Integrações`, `Monitor`, `Filas`, `Reprocessamentos`, `Auditoria`, `Logs` e `Saúde` ainda não foram modeladas — **PENDÊNCIA**.

## Estruturas ERP

Não aplicável nesta seção a partir da revisão R1.1 (`Configuração ERP` passou a `Administração`).

## APIs

Ver cada sub-tela.

## IA Envolvida

Nenhuma.

## Auditoria

Toda alteração é auditada (ver "Visão Geral").

## Critérios de Aceite

Ver cada sub-tela.

## Observações

As sub-telas novas desta revisão (`Integrações`, `Monitor`, `Filas`, `Reprocessamentos`, `Auditoria`, `Logs`, `Saúde`) existem apenas como item de índice aprovado por esta ADR; sua especificação funcional completa (Objetivo, Fluxo, Campos, Ações, Critérios de Aceite) é trabalho pendente de uma próxima revisão, não desta.

## Identity Providers

### Objetivo

Cadastrar e manter o(s) Identity Provider(s) de cada Unidade de Negócio, preparando a integração com o Microsoft Entra ID.

### Personas

Administrador de Sistema.

### Fluxo

```
Listar Identity Providers da Unidade de Negócio → Criar/Editar → Salvar → Provider disponível para a tela de Login
```

### Regras de Negócio

- Cada Unidade de Negócio pode possuir um ou mais Identity Providers (`ARCHITECTURE.md` §16).
- A autenticação permanece desacoplada do domínio de negócio; novos métodos de login podem ser adicionados sem alteração de arquitetura (`ARCHITECTURE.md` §16).
- Microsoft Entra ID é o provedor definitivo de produção; ele ainda não está implementado (`PROJECT_STATE.md`). Esta tela cadastra a configuração; a integração efetiva depende de Work Order própria de Identity, fora do escopo funcional descrito aqui.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Unidade de Negócio | Sim | — |
| Tipo de provider | Sim | Ex.: Microsoft Entra ID; **PENDÊNCIA:** confirmar se a Onda 1 precisa suportar um segundo tipo de provider (ex.: e-mail/código) enquanto o Entra ID não está disponível. |
| Domínio(s) de e-mail autorizado(s) | Sim | Usado na validação do Login. |
| Parâmetros de configuração do provider (client id, tenant id, etc.) | Sim, quando o tipo exigir | Sensíveis — nunca exibidos em texto claro após salvos. |
| Status | Sim | Ativo / Inativo. |

### Ações

- Criar Identity Provider.
- Editar Identity Provider.
- Ativar/Inativar Identity Provider.
- Testar conexão — **PENDÊNCIA:** confirmar se esta ação existe nesta Onda.

### Permissões

Restrito a `Sistema.Gerenciar` ou equivalente.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Microsoft Entra ID (futuro); qualquer outro provedor aprovado pelo Product Owner.

### Banco +Compras

Entidade `IdentityProvider` (ver `ComprasDataModel.md`). Parâmetros sensíveis nunca são persistidos em texto claro.

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda criação/edição de Identity Provider é auditada; parâmetros sensíveis não aparecem no log de auditoria em texto claro.

### Critérios de Aceite

- [ ] CRUD de Identity Provider operável por Unidade de Negócio.
- [ ] Nenhuma credencial sensível exibida após o salvamento.
- [ ] Decisão sobre suportar um segundo tipo de provider (fora do Entra ID) registrada.

### Observações

Nenhuma.

## Feature Flags

### Objetivo

Permitir habilitar/desabilitar funcionalidades do +Compras por Unidade de Negócio, sem deploy, para controlar a exposição incremental dos módulos (coerente com a evolução por estado 🟢/🟡/⚪ descrita em `docs/frontend/Frontend.md`).

### Personas

Administrador de Sistema.

### Fluxo

```
Listar feature flags → Selecionar Unidade de Negócio → Ativar/Desativar flag → Salvar
```

### Regras de Negócio

- Feature flag nunca substitui a regra de negócio; ela apenas controla visibilidade/disponibilidade de uma funcionalidade já implementada.
- **PENDÊNCIA:** não há catálogo oficial de flags previstas para a Onda 1; a tela deve nascer vazia/estrutural e ser populada conforme cada módulo futuro definir sua própria flag.

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Nome da flag | Sim | Identificador técnico. |
| Descrição | Sim | O que a flag controla. |
| Unidade(s) de Negócio | Sim | Escopo de ativação. |
| Status | Sim | Ativa / Inativa. |

### Ações

- Criar feature flag (técnica, pelo sistema/deploy) — **PENDÊNCIA:** confirmar se a criação é via tela ou apenas via código/deploy, com a tela restrita a ativar/desativar.
- Ativar/Desativar por Unidade de Negócio.

### Permissões

Restrito a `Sistema.Gerenciar` ou equivalente.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Nenhuma.

### Banco +Compras

Entidade `FeatureFlag` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda alteração de estado de flag é auditada.

### Critérios de Aceite

- [ ] Ativação/desativação de flag por Unidade de Negócio operável.
- [ ] Decisão sobre origem da criação de flags (tela vs. deploy) registrada.

### Observações

Nenhuma.

## Parâmetros

### Objetivo

Centralizar parâmetros de sistema não cobertos pelas demais telas de Administração/Configurações (ex.: limites técnicos, textos institucionais, valores default), por Unidade de Negócio quando aplicável.

### Personas

Administrador de Sistema.

### Fluxo

```
Listar parâmetros → Editar valor → Salvar
```

### Regras de Negócio

- **PENDÊNCIA:** não há catálogo oficial de parâmetros previstos para a Onda 1; esta tela é estrutural até que módulos futuros declarem parâmetros próprios.
- Parâmetros nunca substituem regra de negócio codificada em Workflow/Aprovação/Controle Orçamentário — esses têm telas de configuração próprias (ver `Configurações`).

### Campos

| Campo | Obrigatório | Regra |
|---|---|---|
| Chave do parâmetro | Sim | Identificador técnico único. |
| Valor | Sim | Tipo depende do parâmetro (texto, número, booleano). |
| Unidade de Negócio | Não | Global por padrão, salvo quando o parâmetro for específico de Unidade. |
| Descrição | Sim | Texto de negócio explicando o efeito do parâmetro. |

### Ações

- Editar valor de parâmetro.
- Restaurar valor default — **PENDÊNCIA** de confirmação de escopo.

### Permissões

Restrito a `Sistema.Gerenciar` ou equivalente.

### Workflow

Não aplicável.

### Controle Orçamentário

Não aplicável.

### Integrações

Nenhuma.

### Banco +Compras

Entidade `Parametro` (ver `ComprasDataModel.md`).

### Estruturas ERP

Não aplicável.

### APIs

**PENDÊNCIA:** contrato ainda não definido.

### IA Envolvida

Nenhuma.

### Auditoria

Toda alteração de parâmetro é auditada (valor anterior/novo).

### Critérios de Aceite

- [ ] Edição de parâmetros técnicos operável.
- [ ] Catálogo inicial de parâmetros definido antes da homologação.

### Observações

Nenhuma.

---

# Configurações

> Preferências pessoais do usuário autenticado (ADR-0020) — não confundir com motores de regra de negócio da Unidade (Workflow, Alçadas, Controle Orçamentário), que passam a `Administração` a partir da revisão R1.1. Sub-telas: Conta, Preferências, Tema, Idioma.

## Objetivo

Permitir que o usuário autenticado gerencie sua própria conta e preferências pessoais de uso do Portal +Compras.

## Personas

Todo usuário autenticado.

## Fluxo

```
Configurações
├── Conta
├── Preferências
├── Tema
└── Idioma
```

## Regras de Negócio

- A partir da revisão R1.1 (ADR-0020), `Workflow`, `Alçadas` (antiga "Aprovação" de configuração) e `Controle Orçamentário` deixam de pertencer a esta seção e passam a `Administração`, por serem motores de regra de negócio da Unidade, não preferência pessoal do usuário.
- `Conta`, `Preferências`, `Tema` e `Idioma` são sub-telas novas desta revisão; seu conteúdo funcional específico (campos, ações, contratos) ainda não foi especificado — **PENDÊNCIA**, a detalhar em revisão futura antes da Onda 1.2.
- Nenhuma das sub-telas desta seção altera dados de negócio da Unidade de Negócio; escopo restrito à experiência pessoal do usuário autenticado.

## Campos

Ver cada sub-tela (não especificadas nesta revisão — ver "Regras de Negócio").

## Ações

Ver cada sub-tela.

## Permissões

Disponível para qualquer usuário autenticado, restrito aos próprios dados/preferências.

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

Nenhuma nesta Onda.

## Banco +Compras

Entidades de `Conta`/`Preferências`/`Tema`/`Idioma` ainda não foram modeladas — **PENDÊNCIA**.

## Estruturas ERP

Não aplicável.

## APIs

Ver cada sub-tela.

## IA Envolvida

Nenhuma.

## Auditoria

Alteração de conta/preferências pessoais é auditada com o mesmo padrão das demais telas (ver "Visão Geral").

## Critérios de Aceite

- [ ] Especificação funcional completa de Conta, Preferências, Tema e Idioma aprovada antes da O1.2.

## Observações

Esta seção existe, nesta revisão, apenas como item de índice aprovado pela ADR-0020; sua especificação funcional completa é trabalho pendente de uma próxima revisão, não desta.

---


# IA

## Objetivo

Registrar, nesta Onda, apenas o estado honesto da IA no Portal: nenhum agente de IA está conectado ao frontend nesta Onda.

## Personas

Não aplicável (sem tela funcional).

## Fluxo

Não aplicável.

## Regras de Negócio

- "Agentes IA" aparece no mapa do Portal como módulo **⚪ Planejado** — sem estrutura visual ou funcional até Work Order aprovada (`docs/frontend/Frontend.md`, ADR-0017).
- É proibido implementar agentes de IA reais no frontend nesta fase (`.ai/work-orders/active/PortalMaisComprasFrontend.md`, "Fora de escopo").
- Toda operação crítica possui alternativa manual; IA acelera e orienta, mas não é pré-requisito (ADR-0013/`PROJECT_STATE.md`).
- A estratégia de LLM (ADR-0014) define Ollama local para desenvolvimento e plataforma corporativa para produção; nenhuma dessas capacidades é exposta em tela nesta Onda.

## Campos

Não aplicável.

## Ações

Não aplicável.

## Permissões

Não aplicável.

## Workflow

Não aplicável.

## Controle Orçamentário

Não aplicável.

## Integrações

Nenhuma.

## Banco +Compras

Não aplicável nesta Onda.

## Estruturas ERP

Não aplicável.

## APIs

O único endpoint de IA já existente é consultivo e não está vinculado a nenhuma tela desta Onda: `POST /api/v1/negociacoes/recomendacoes` (Work Order A13).

## IA Envolvida

Nenhuma nesta Onda (ver regras de negócio).

## Auditoria

Não aplicável.

## Critérios de Aceite

- [ ] Nenhuma tela desta Onda apresenta funcionalidade de IA como concluída ou operável.

## Observações

Esta seção será desenvolvida com conteúdo real a partir da Work Order que aprovar Agentes IA no Portal.

---

# Glossário

| Termo | Definição |
|---|---|
| **+Compras** | Nome do produto/sistema de Procurement do SOMA BlueprintOS. |
| **BlueprintOS** | Plataforma corporativa de IA e automação sobre a qual o +Compras é construído. |
| **Onda** | Bloco de entrega do MVP 1.0 dentro da estratégia Frontend First (Onda 1 a 5, ver `.ai/ROADMAP.md`). |
| **Frontend First** | Estratégia oficial: nenhuma funcionalidade é implementada antes de `+Compras Funcional` → `+Compras UX` → Mock navegável aprovados. |
| **UnidadeNegocioId** | Identificador da Unidade de Negócio que define o contexto de toda a sessão (ERP, Workflow, Aprovação, Controle Orçamentário, Identity Provider). |
| **Identity Provider (IdP)** | Provedor de autenticação de uma Unidade de Negócio; o definitivo de produção é o Microsoft Entra ID. |
| **AZZAS 2154** | Holding formada pela fusão entre Grupo Soma e Arezzo&Co; proprietária da marca visual usada pelo Design System GDT. |
| **GDT Design System** | AZZAS 2154 — GDT (Gestão de Demandas de Tecnologia) Design System; sistema de design oficial para portais internos de tecnologia. |
| **AppShell** | Componente de shell de navegação do Portal (cabeçalho, navegação, área de conteúdo). |
| **ADR** | Architecture Decision Record; decisões arquiteturais registradas em `.ai/DECISIONS.md`. |
| **Vertical slice** | Fluxo funcional de ponta a ponta implementado para um único domínio (ex.: Fornecedores), usado para validar a arquitetura antes de escalar para outros domínios. |
| **Feature Flag** | Mecanismo de ativação/desativação de funcionalidade sem novo deploy. |
| **Maverick Buying** | Termo de mercado (não específico do +Compras) para compras realizadas fora da política — citado apenas como referência em `fluxo_compras_indiretas_html.html`. |

---

# Dúvidas de produto (para validação do Product Owner)

**Resolvidas pela revisão R1.1 (ADR-0020, 06/08/2026):**

1. ~~Corte entre Administração / Administração do Sistema / Configurações~~ — **RESOLVIDA.** Distribuição aprovada formalmente (ver "Nota de escopo desta atualização (R1.1)"): Administração ganha Unidades de Alocação, Filiais, Centros de Custo, Workflow, Alçadas, Controle Orçamentário e Configuração ERP; Administração do Sistema ganha Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs e Saúde; Configurações passa a ser exclusivamente preferência pessoal (Conta, Preferências, Tema, Idioma).
2. ~~Múltiplos perfis por usuário~~ — **RESOLVIDA.** O modelo RBAC (ADR-0020) permite múltiplos perfis por usuário; as permissões efetivas são a união das permissões de todos os perfis vinculados. Usuários nunca recebem permissão individual ou exceção direta.
3. ~~Customização de Permissões~~ — **PARCIALMENTE RESOLVIDA.** A mecânica é RBAC exclusiva por perfil, sem exceção individual (decidido). Se o catálogo de permissões admite customização via tela permanece pendente (ver item 8 abaixo).

**Resolvidas pela revisão R1.2 (ADR-0020, atualização 06/08/2026):**

4. ~~Mecanismo de Login da Onda 1~~ — **RESOLVIDA.** Login Passwordless via OTP por e-mail é o mecanismo oficial, com Microsoft Entra ID projetado para coexistir futuramente como Identity Provider alternativo (não substituto compulsório). Ver sub-tela `Login`.
5. ~~Como inicializar o sistema sem nenhum Administrador cadastrado~~ — **RESOLVIDA.** Bootstrap Mode: disponível somente enquanto não existir Administrador Sênior; cria a primeira Unidade de Negócio, o primeiro Administrador Sênior e a configuração inicial; encerra-se permanentemente após uso. Ver sub-tela `Bootstrap`.
6. ~~Relação entre vínculo de Perfil e vínculo de Centro de Custo do usuário~~ — **RESOLVIDA.** São dois vínculos de acesso independentes (Perfis governam permissões; Centros de Custo governam escopo de dados); nenhum substitui o outro. Ver `Gestão de Usuários`.

**Ainda pendentes:**

7. **Fornecedores fora de sequência de Onda:** o módulo Fornecedores já está tecnicamente implementado e conectado ao backend (B1/B2/B2.1/B2.2, Portal Frontend), mas está formalmente classificado na Onda 2 (`.ai/BACKLOG.md`). A O1.1 deve documentar retroativamente Fornecedores em `ComprasFuncional.md`/`ComprasUX.md`, ou isso permanece reservado para a abertura formal da Onda 2?
8. **Catálogo de Perfis e Permissões, incluindo "Administrador Sênior":** quais Perfis existem de fato (Administrador Sênior, Administrador, Comprador, Aprovador, Solicitante, outros?) e quais Permissões atômicas compõem cada um? O modelo (RBAC por perfil, ADR-0020) está decidido; o conteúdo do catálogo não está — e é agora bloqueante também para o Bootstrap, que depende do perfil "Administrador Sênior" existir.
9. **Provedor de e-mail transacional para o OTP:** nenhum provedor de envio de e-mail foi escolhido ou contratado; é dependência explícita da implementação do Login (ADR-0020, risco registrado).
10. **Etapas de Workflow, critérios de Alçada e fonte de verdade do saldo orçamentário:** nenhum desenho definitivo existe hoje; o material de `fluxo_compras_indiretas_html.html`/`Proposta - Compras Indiretas.html` é referência de mercado, não especificação aprovada.
11. **Onda de "Relatórios":** não aparece classificado em `.ai/BACKLOG.md` nesta leitura — confirmar a qual Onda pertence.
12. **Especificação funcional das sub-telas novas da ADR-0020:** Unidades de Alocação, Filiais e Centros de Custo já foram especificadas nesta revisão; `Notificações`, `Integrações`, `Monitor`, `Filas`, `Reprocessamentos`, `Auditoria`, `Logs`, `Saúde`, `Conta`, `Preferências`, `Tema` e `Idioma` existem apenas como item de índice aprovado — sua especificação funcional completa é pendência para a próxima revisão.
13. **Processo de acionamento do Agente Engenheiro de Segurança Sênior:** a ADR-0020 exige revisão de segurança obrigatória para toda funcionalidade de autenticação, mas não define como esse agente é acionado (fluxo humano, agente de IA automatizado, ou ambos) nem os critérios objetivos de aprovação — a definir na Work Order de Estrutura que implementar Login/Bootstrap.

# Sugestões para facilitar a próxima revisão (O1.2)

1. ~~Aprovar formalmente a distribuição Administração / Administração do Sistema / Configurações~~ — concluído pela ADR-0020 (revisão R1.1).
2. ~~Decidir o mecanismo de Login da Onda 1~~ — concluído pela ADR-0020 (revisão R1.2): OTP por e-mail, Entra ID coexistindo no futuro.
3. Definir o conteúdo do catálogo de Perfis e Permissões (dúvida 8) como primeira decisão de produto da O1.2, incluindo formalmente o perfil "Administrador Sênior" exigido pelo Bootstrap — é pré-requisito de quase todas as demais telas (visibilidade de menu, ações permitidas) e do próprio Bootstrap.
4. Escolher e contratar o provedor de e-mail transacional para o OTP (dúvida 9) antes de iniciar a implementação de Login.
5. Definir, junto ao responsável pela função de segurança, o processo de acionamento do Agente Engenheiro de Segurança Sênior (dúvida 13) antes de iniciar a implementação de Login/Bootstrap — a exigência já está aprovada (ADR-0020); falta o processo operacional.
6. Tratar a documentação retroativa de Fornecedores (dúvida 7) como item curto e isolado, para não misturar débito documental de Onda 2 com a entrega de Onda 1.
7. Especificar funcionalmente as sub-telas novas da ADR-0020 ainda não detalhadas (dúvida 12): Notificações, Integrações, Monitor, Filas, Reprocessamentos, Auditoria, Logs, Saúde, Conta, Preferências, Tema, Idioma.
8. Usar o kit de referência `resources/design-system/ui_kits/portal-gdt/` (Auth OTP, AppShell, Sidebar "afirmativa" de admin) como ponto de partida visual do Mock de Login/Bootstrap/Administração — ele já é a linguagem visual oficial AZZAS 2154/GDT, mesmo não sendo específico do +Compras.
