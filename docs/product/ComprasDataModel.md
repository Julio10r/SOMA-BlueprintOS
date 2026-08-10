# +Compras Data Model

## Objetivo

Acompanhar a evolução funcional do modelo de dados do +Compras, módulo a módulo, à medida que cada funcionalidade é especificada e implementada (ver `.ai/ROADMAP.md`, estratégia Frontend First). Cada módulo segue o [template de modelo de dados oficial](./templates/DataModelTemplate.md), estendido com o campo **Telas que utilizam** (orientação explícita da consolidação O1.1).

## Responsabilidade

Registrar, por módulo, à medida que existir: entidade, tabela +Compras, tabela ERP, relacionamentos, integrações e observações. Nenhuma tabela é inventada nesta etapa — apenas a estrutura é criada.

## Público

Desenvolvimento, Arquitetura e QA.

## Nota de escopo desta atualização (O1.1)

Este documento cobre exclusivamente as entidades funcionais necessárias para a Onda 1 — Fundação Funcional (Login, Seleção de Unidade de Negócio, Dashboard, Administração, Administração do Sistema, Configurações), conforme `ComprasFuncional.md`. **Não é o modelo físico** (sem tipos de coluna, tamanhos, precisão, índices físicos, migrations ou nomes de tabela definitivos) — é o blueprint funcional exigido pela Onda 1 (`.ai/ROADMAP.md`: "blueprint completo do banco"). O modelo físico definitivo, alinhado byte a byte ao ERP quando aplicável, só é obrigatório antes do Go Live (Onda 5, `.ai/ROADMAP.md` — "Estratégia de banco de dados").

O modelo já real de Fornecedores (`Fornecedor`, `Cnpj`, `ScoreFornecedor`, `FornecedorCanonico` — ver `docs/backend/procurement/Procurement.md`, ADR-0012, ADR-0015, ADR-0016) não é repetido aqui porque pertence à Onda 2; ele permanece documentado em `docs/backend/procurement/Procurement.md` até que `ComprasDataModel.md` o incorpore formalmente.

## Nota de escopo desta atualização (R1.1 — Revisão Arquitetural da Onda 1)

A ADR-0020 (`.ai/DECISIONS.md`) acrescenta ao blueprint funcional da Onda 1 as entidades de classificação gerencial e cadastros integrados do ERP (`UnidadeAlocacao`, `Filial`, `CentroCusto`, `CentroCustoUnidadeAlocacao`) e a entidade de autorização de acesso por Centro de Custo (`UsuarioCentroCusto`), além de reafirmar o modelo RBAC exclusivo por perfil já previsto por `Perfil`/`Permissao`/`PerfilPermissao`. Nenhuma entidade documentada pela O1.1 foi removida; `UnidadeAlocacao` substitui apenas o nome informal "Gestão de Empresas", nunca usado como entidade formal neste documento.

## Nota de escopo desta atualização (R1.2 — Revisão Arquitetural da Onda 1, continuação)

A atualização da ADR-0020 pela revisão R1.2 acrescenta a entidade `CodigoVerificacaoOtp` (código OTP do Login Passwordless) e o campo/entidade de estado do Bootstrap (`BootstrapEstado`), além de detalhar `SessaoAutenticacao` com o resultado do Login OTP. Nenhuma entidade da R1.1 foi removida ou renomeada por esta atualização.

## Índice

# Visão Geral

O blueprint funcional da Onda 1 organiza-se em cinco grupos de entidades:

```
Identidade e Acesso
  UnidadeNegocio, Usuario, UsuarioUnidadeNegocio, UsuarioPerfil, UsuarioCentroCusto, Perfil, Permissao, PerfilPermissao, SessaoAutenticacao, CodigoVerificacaoOtp, BootstrapEstado

Cadastros Integrados do ERP e Classificação Gerencial (ADR-0020)
  UnidadeAlocacao, Filial, CentroCusto, CentroCustoUnidadeAlocacao

Configuração Técnica
  IdentityProvider, ConfiguracaoErp, FeatureFlag, Parametro

Motores de Regra de Negócio (configuração — operação real a partir da Onda 3)
  RegraWorkflow, AlcadaAprovacao, RegraOrcamentaria

Auditoria (transversal a todos os grupos)
  RegistroAuditoria
```

Todas as entidades abaixo carregam `UnidadeNegocioId` como chave de particionamento lógico (`ARCHITECTURE.md` §16), exceto `Permissao` (catálogo global) e `BootstrapEstado` (global por definição — existe antes de qualquer Unidade de Negócio), ver observações de cada entidade.

# Login

# Bootstrap

# Seleção da Unidade de Negócio

# Dashboard

Nenhuma entidade própria nesta Onda para Seleção de Unidade de Negócio ou Dashboard, além das listadas em `Administração`/`Administração do Sistema` abaixo, que essas telas consultam. Login e Bootstrap têm entidades próprias detalhadas logo abaixo (`CodigoVerificacaoOtp`, `BootstrapEstado`), além de `SessaoAutenticacao`, `Usuario`, `UnidadeNegocio` e `Perfil` já descritas em `Administração`.

## Entidade: CodigoVerificacaoOtp

### Objetivo

Representar o código de verificação (OTP) enviado ao e-mail corporativo durante o Login Passwordless (ADR-0020, R1.2).

### Origem dos Dados

Gerado pelo backend ao iniciar o passo 2 do fluxo de `Login`.

### Destino dos Dados

Validado no passo 2 de `Login` para autenticar a sessão.

### Banco +Compras

Entidade lógica `CodigoVerificacaoOtp`: `UsuarioId` ou e-mail informado (antes de resolver o usuário), código (hash, nunca texto claro), data de geração, data de expiração (referência visual: 15 minutos), status (Pendente/Consumido/Expirado), tentativas de validação.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `Usuario` (quando o e-mail corresponder a um usuário existente).

### Índices

Índice em (e-mail, status); expiração tratada por job/consulta por data, não por índice físico definido nesta etapa.

### Integrações

Serviço de envio de e-mail transacional — **PENDÊNCIA:** provedor não escolhido/contratado (ADR-0020, risco registrado).

### Observações

O código nunca é persistido em texto claro (mesmo padrão de segredo aplicado a `IdentityProvider`/`ConfiguracaoErp`). Esta entidade e todo o fluxo de Login exigem revisão do Agente Engenheiro de Segurança Sênior antes da implementação (ADR-0020).

### Telas que utilizam

`Login`.

## Entidade: BootstrapEstado

### Objetivo

Registrar se o Bootstrap Mode do +Compras já foi concluído, controlando se a tela `Bootstrap` ou a tela `Login` é exibida (ADR-0020, R1.2).

### Origem dos Dados

Criada/atualizada exclusivamente pelo fluxo `Bootstrap`, ao concluir a criação da primeira Unidade de Negócio e do primeiro Administrador Sênior.

### Destino dos Dados

Consultada antes de renderizar `Login` ou `Bootstrap`, e por qualquer verificação de que o ambiente já foi inicializado.

### Banco +Compras

Entidade lógica `BootstrapEstado`: `Concluido` (booleano), `ConcluidoEm` (data/hora), `UsuarioAdministradorSeniorId` (o primeiro criado). Global — não particionada por `UnidadeNegocioId`, pois existe antes de qualquer Unidade de Negócio.

### Banco ERP

Não aplicável.

### Relacionamentos

1:1 com o primeiro `Usuario` criado como Administrador Sênior.

### Índices

Não aplicável — existe no máximo um registro nesta entidade.

### Integrações

Nenhuma.

### Observações

Uma vez `Concluido = true`, este valor nunca é revertido pelo sistema (ADR-0020: "encerrado permanentemente"); qualquer alteração posterior é procedimento operacional de suporte, fora do escopo funcional desta entidade.

### Telas que utilizam

`Bootstrap`, `Login` (consulta, para decidir qual tela exibir).

# Administração

## Entidade: UnidadeNegocio

### Objetivo

Representar cada Unidade de Negócio operada pelo +Compras, unidade de particionamento de todo o contexto (ERP, Workflow, Aprovação, Controle Orçamentário, Identity Provider).

### Origem dos Dados

Cadastro manual pelo Administrador (tela `Administração > Gestão de Unidades de Negócio`).

### Destino dos Dados

Consumida por toda regra de negócio que dependa de `UnidadeNegocioId` (praticamente todo o restante do modelo).

### Banco +Compras

Entidade lógica `UnidadeNegocio`: identificador (`UnidadeNegocioId`), nome, status (Ativa/Inativa). Estrutura física (tipos, tamanhos) não definida nesta etapa.

### Banco ERP

Não aplicável — Unidade de Negócio é conceito do +Compras; o vínculo com o ERP de cada Unidade é registrado em `ConfiguracaoErp`, não nesta entidade.

### Relacionamentos

- 1:N com `UsuarioUnidadeNegocio`.
- 1:N com `IdentityProvider`.
- 1:1 (ou 1:N, a confirmar) com `ConfiguracaoErp`.
- 1:N com `RegraWorkflow`, `AlcadaAprovacao`, `RegraOrcamentaria`, `FeatureFlag` (quando escopada por Unidade).

### Índices

Índice único em `UnidadeNegocioId`. Demais índices dependem do modelo físico (fora de escopo desta etapa).

### Integrações

Nenhuma integração direta; é a chave de particionamento usada por integrações de outras entidades.

### Observações

Nesta Onda, apenas `SOMA` está ativa; demais Unidades podem existir como pré-cadastro sem uso operacional.

### Telas que utilizam

`Administração > Gestão de Unidades de Negócio`, `Seleção da Unidade de Negócio`, e indiretamente toda tela que exiba/filtre por `UnidadeNegocioId`.

## Entidade: Usuario

### Objetivo

Representar o usuário funcional do +Compras, distinto da identidade de autenticação federada (que virá do Identity Provider).

### Origem dos Dados

Cadastro manual pelo Administrador (tela `Administração > Gestão de Usuários`); futuramente, pode ser sincronizado a partir do Identity Provider/Entra ID — **PENDÊNCIA** de decisão (ver `ComprasFuncional.md`).

### Destino dos Dados

Consumida por autenticação (vínculo com sessão), autorização (vínculo com Perfil) e por toda tela que exiba "responsável"/"aprovador"/"criado por".

### Banco +Compras

Entidade lógica `Usuario`: nome, e-mail corporativo (identificador de login), status (Ativo/Inativo).

### Banco ERP

Não aplicável nesta Onda. **PENDÊNCIA:** confirmar se, no futuro, o cadastro de compradores deve refletir um cadastro de "comprador" do ERP.

### Relacionamentos

- N:N com `UnidadeNegocio` via `UsuarioUnidadeNegocio`.
- N:N com `Perfil` via `UsuarioPerfil`.
- 1:N com `SessaoAutenticacao`.

### Índices

Índice único em e-mail corporativo.

### Integrações

Identity Provider da Unidade de Negócio, para a autenticação efetiva (fora do escopo de persistência desta entidade).

### Observações

Substitui, no domínio funcional, o identificador temporário de desenvolvimento (`DevelopmentRequestIdentity`/`ICurrentIdentity`, ADR-0011) usado hoje apenas em Fornecedores; a estratégia de migração desse identificador temporário para o modelo definitivo de `Usuario`/Entra ID é uma dependência declarada da ADR-0011, não resolvida nesta etapa.

### Telas que utilizam

`Administração > Gestão de Usuários`, `Login`, `Seleção da Unidade de Negócio`, e qualquer tela futura que exiba autoria/responsável.

## Entidade: UsuarioUnidadeNegocio

### Objetivo

Vincular um Usuário a uma ou mais Unidades de Negócio às quais ele tem acesso.

### Origem dos Dados

Criado/editado na tela `Administração > Gestão de Usuários`.

### Destino dos Dados

Consultado por `Login`/`Seleção da Unidade de Negócio` para determinar as Unidades disponíveis ao usuário.

### Banco +Compras

Entidade de associação: `UsuarioId`, `UnidadeNegocioId`.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N entre `Usuario` e `UnidadeNegocio`.

### Índices

Índice composto único em (`UsuarioId`, `UnidadeNegocioId`).

### Integrações

Nenhuma.

### Observações

Nenhuma.

### Telas que utilizam

`Administração > Gestão de Usuários`, `Seleção da Unidade de Negócio`.

## Entidade: Perfil

### Objetivo

Agrupar Permissões em um papel de acesso (ex.: Administrador, Comprador, Aprovador, Solicitante — catálogo definitivo pendente, ver `ComprasFuncional.md`).

### Origem dos Dados

Cadastro manual pelo Administrador (tela `Administração > Gestão de Perfis`).

### Destino dos Dados

Consumido por controle de visibilidade de menu e de ações em toda tela do sistema.

### Banco +Compras

Entidade lógica `Perfil`: nome, descrição, status. **PENDÊNCIA:** se é escopado por Unidade de Negócio ou global (ver `ComprasFuncional.md`).

### Banco ERP

Não aplicável.

### Relacionamentos

- N:N com `Usuario` via `UsuarioPerfil`.
- N:N com `Permissao` via `PerfilPermissao`.

### Índices

Índice único em nome do perfil (por Unidade de Negócio, se escopado; global, se não).

### Integrações

Nenhuma.

### Observações

Catálogo inicial de Perfis é dúvida de produto registrada em `ComprasFuncional.md`.

### Telas que utilizam

`Administração > Gestão de Perfis`, `Administração > Gestão de Usuários` (associação), e todo controle de visibilidade de menu/ação do Portal.

## Entidade: Permissao

### Objetivo

Representar uma ação de negócio atômica e verificável (ex.: `UnidadeNegocio.Gerenciar`, `Usuario.Gerenciar`, `Perfil.Gerenciar`, `Fornecedor.Aprovar`).

### Origem dos Dados

**PENDÊNCIA:** catálogo fixo definido pelo sistema/código (hipótese assumida em `ComprasFuncional.md`) ou cadastrável pela tela `Administração > Gestão de Perfis`.

### Destino dos Dados

Consumido por `Perfil` (associação) e por toda verificação de autorização no backend/frontend.

### Banco +Compras

Entidade lógica `Permissao`: código, descrição, módulo/domínio.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N com `Perfil` via `PerfilPermissao`.

### Índices

Índice único em código da permissão.

### Integrações

Nenhuma.

### Observações

Catálogo é global (não particionado por `UnidadeNegocioId`), pois representa capacidades do sistema, não dados de negócio de uma Unidade específica — hipótese a confirmar.

### Telas que utilizam

`Administração > Gestão de Perfis` (catálogo e associação; a partir da ADR-0020, `Permissões` deixa de ser tela própria).

## Entidade: PerfilPermissao

### Objetivo

Associar Permissões a um Perfil.

### Origem dos Dados

Tela `Administração > Gestão de Perfis`.

### Destino dos Dados

Consultado em toda verificação de autorização.

### Banco +Compras

Entidade de associação: `PerfilId`, `PermissaoId`.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N entre `Perfil` e `Permissao`.

### Índices

Índice composto único em (`PerfilId`, `PermissaoId`).

### Integrações

Nenhuma.

### Observações

Nenhuma.

### Telas que utilizam

`Administração > Gestão de Perfis`.

## Entidade: UsuarioPerfil

### Objetivo

Associar um ou mais Perfis a um Usuário.

### Origem dos Dados

Tela `Administração > Gestão de Usuários`.

### Destino dos Dados

Consultado em toda verificação de autorização e filtragem de menu.

### Banco +Compras

Entidade de associação: `UsuarioId`, `PerfilId`.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N entre `Usuario` e `Perfil`.

### Índices

Índice composto único em (`UsuarioId`, `PerfilId`).

### Integrações

Nenhuma.

### Observações

Se o Product Owner decidir que cada vínculo usuário×Unidade tem um único Perfil (ver dúvida de produto em `ComprasFuncional.md`), esta entidade é substituída por um campo `PerfilId` direto em `UsuarioUnidadeNegocio`.

### Telas que utilizam

`Administração > Gestão de Usuários`.

## Entidade: UsuarioCentroCusto

### Objetivo

Autorizar o acesso de um Usuário a um ou mais Centros de Custo ativos, independentemente do cadastro mestre do Centro de Custo (ADR-0020).

### Origem dos Dados

Tela `Administração > Gestão de Usuários`.

### Destino dos Dados

Consultado, a partir da Onda 3, para filtrar os Centros de Custo disponíveis ao usuário em uma requisição.

### Banco +Compras

Entidade de associação: `UsuarioId`, `CentroCustoId`. Ausência de qualquer registro para um usuário — **PENDÊNCIA:** confirmar se implica nenhum acesso ou acesso a todos os Centros de Custo ativos (ADR-0020 permite ambos os modelos: "um, vários, ou todos"); o mecanismo de "todos" pode ser um flag em `Usuario` em vez de N registros — a decidir na especificação técnica.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N entre `Usuario` e `CentroCusto`.

### Índices

Índice composto único em (`UsuarioId`, `CentroCustoId`).

### Integrações

Nenhuma.

### Observações

Esta entidade nunca altera o cadastro mestre de `CentroCusto` (ADR-0020); é exclusivamente autorização de acesso.

### Telas que utilizam

`Administração > Gestão de Usuários`, `Administração > Gestão de Centros de Custo` (consulta).

## Entidade: UnidadeAlocacao

### Objetivo

Representar a classificação gerencial da despesa usada para operação, orçamento e relatórios, substituindo formalmente o conceito informal e anterior de "Gestão de Empresas" (ADR-0020).

### Origem dos Dados

Sincronização do ERP (ex.: tabela Rede de Lojas) ou cadastro manual pelo Administrador na tela `Administração > Gestão de Unidades de Alocação`.

### Destino dos Dados

Consumida por `Administração > Gestão de Centros de Custo` (vínculo N:N) e, a partir da Onda 3, por requisições, orçamento e relatórios.

### Banco +Compras

Entidade lógica `UnidadeAlocacao`: identificador, `CodigoErp` (opcional), `DescricaoErp` (opcional, somente leitura quando origem ERP), `DescricaoMaisCompras` (opcional), `Tipo` (Marca/Corporativo/Localidade/Outro), `Origem` (ERP/+Compras), `AtivaNoMaisCompras`, `UnidadeNegocioId`.

### Banco ERP

Quando `Origem = ERP`: leitura de uma tabela corporativa de classificação (ex.: Rede de Lojas); nenhuma escrita.

### Relacionamentos

- N:1 com `UnidadeNegocio`.
- N:N com `CentroCusto` via `CentroCustoUnidadeAlocacao`.

### Índices

Índice único em identificador; índice em (`UnidadeNegocioId`, `Tipo`).

### Integrações

ERP corporativo, quando `Origem = ERP` (leitura).

### Observações

Exemplos: Animale, Farm, Fábula, SOMA Corporativo, Corporativo Jardim Botânico. Quando `Origem = ERP`, `CodigoErp`/`DescricaoErp` são imutáveis no +Compras.

### Telas que utilizam

`Administração > Gestão de Unidades de Alocação`, `Administração > Gestão de Centros de Custo` (vínculo).

## Entidade: Filial

### Objetivo

Representar, no +Compras, a disponibilidade local de uma Filial integrada do ERP (ADR-0020).

### Origem dos Dados

Sincronização do ERP; ativação/inativação e `DescricaoMaisCompras` editadas na tela `Administração > Gestão de Filiais`.

### Destino dos Dados

Consumida por telas operacionais futuras (Onda 2+) que precisem filtrar por Filial.

### Banco +Compras

Entidade lógica `Filial`: `CodigoCliFor` (somente leitura, origem ERP), `NomeCliFor` (somente leitura, origem ERP), `DescricaoMaisCompras` (opcional), `AtivaNoMaisCompras`.

### Banco ERP

`CADASTRO_CLI_FOR` (mesmo cadastro mestre já usado por Fornecedores, ver ADR-0016) — leitura somente; `CodigoCliFor`/`NomeCliFor` compõem chaves usadas pelo ERP e por isso são persistidos no +Compras.

### Relacionamentos

Nenhum relacionamento formal adicional nesta Onda — **PENDÊNCIA:** confirmar se Filial se relaciona com `UnidadeNegocio` diretamente ou apenas indiretamente via Fornecedor/Centro de Custo.

### Índices

Índice único em `CodigoCliFor`.

### Integrações

ERP corporativo (leitura, mesmo padrão de sincronização de Fornecedores/Linx — `docs/backend/integration/FornecedorSynchronization.md`).

### Observações

Nome funcional oficial é "Gestão de Filiais"; nenhuma migration ou nome de tabela deve usar "Cadastro de Filiais" (ADR-0020).

### Telas que utilizam

`Administração > Gestão de Filiais`.

## Entidade: CentroCusto

### Objetivo

Representar, no +Compras, a disponibilidade local de um Centro de Custo integrado do ERP e seu vínculo com Unidades de Alocação (ADR-0020).

### Origem dos Dados

Sincronização do ERP; ativação/inativação, `DescricaoMaisCompras` e vínculo com Unidades de Alocação editados na tela `Administração > Gestão de Centros de Custo`.

### Destino dos Dados

Consumida por `Configurações Orçamentárias` (`RegraOrcamentaria`), por `UsuarioCentroCusto` (autorização de acesso) e, a partir da Onda 3, pelo fluxo de requisição.

### Banco +Compras

Entidade lógica `CentroCusto`: `CodigoErp` (somente leitura, origem ERP), `DescricaoErp` (somente leitura, origem ERP), `DescricaoMaisCompras` (opcional), `AtivoNoMaisCompras`.

### Banco ERP

Cadastro corporativo de centro de custo (referência a identificar por sistema ERP da Unidade) — leitura somente; nenhuma alteração estrutural.

### Relacionamentos

- N:N com `UnidadeAlocacao` via `CentroCustoUnidadeAlocacao`.
- N:N com `Usuario` via `UsuarioCentroCusto`.
- N:1 com `RegraOrcamentaria` (referenciado como dimensão).

### Índices

Índice único em `CodigoErp`.

### Integrações

ERP corporativo (leitura).

### Observações

Nome funcional oficial é "Gestão de Centros de Custo"; nenhuma migration ou nome de tabela deve usar "Cadastro de Centros de Custo" (ADR-0020). O cadastro mestre é separado da autorização de acesso do usuário (`UsuarioCentroCusto`).

### Telas que utilizam

`Administração > Gestão de Centros de Custo`, `Administração > Gestão de Usuários` (autorização), `Administração > Controle Orçamentário`.

## Entidade: CentroCustoUnidadeAlocacao

### Objetivo

Vincular um Centro de Custo a uma ou mais Unidades de Alocação permitidas, com suporte a uma Unidade de Alocação padrão (ADR-0020).

### Origem dos Dados

Tela `Administração > Gestão de Centros de Custo`.

### Destino dos Dados

Consultado, a partir da Onda 3, para filtrar as Unidades de Alocação disponíveis ao selecionar um Centro de Custo em uma requisição; preenchimento automático quando houver apenas uma Unidade permitida.

### Banco +Compras

Entidade de associação: `CentroCustoId`, `UnidadeAlocacaoId`, `Padrao` (booleano).

### Banco ERP

Não aplicável.

### Relacionamentos

N:N entre `CentroCusto` e `UnidadeAlocacao`.

### Índices

Índice composto único em (`CentroCustoId`, `UnidadeAlocacaoId`); restrição garantindo no máximo um `Padrao = true` por `CentroCustoId`.

### Integrações

Nenhuma.

### Observações

Não é permitido selecionar, em uma requisição, Unidade de Alocação fora deste vínculo (ADR-0020).

### Telas que utilizam

`Administração > Gestão de Centros de Custo`.

## Entidade: SessaoAutenticacao

### Objetivo

Representar a sessão autenticada do usuário, incluindo a Unidade de Negócio ativa no momento.

### Origem dos Dados

Criada no fluxo de `Login`, após validação do `CodigoVerificacaoOtp` (mecanismo oficial, ADR-0020) ou redirecionamento do Identity Provider (Entra ID, quando configurado), e atualizada em `Seleção da Unidade de Negócio` (troca de contexto).

### Destino dos Dados

Consultada por toda tela/API que dependa do usuário autenticado e da `UnidadeNegocioId` ativa.

### Banco +Compras

Entidade lógica `SessaoAutenticacao`: `UsuarioId`, `UnidadeNegocioId` ativa, `IdentityProviderId` usado nesta sessão (OTP por e-mail ou Entra ID, ADR-0020), criada em, expira em. **PENDÊNCIA:** mecanismo físico de sessão (token/JWT stateless vs. sessão persistida em banco) não definido — decisão técnica da Work Order de Estrutura, não deste blueprint funcional.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `Usuario`; referencia `UnidadeNegocioId` ativa; referencia `IdentityProviderId` usado na autenticação.

### Índices

Não aplicável nesta etapa (depende do mecanismo escolhido).

### Integrações

Identity Provider ativo da Unidade de Negócio: OTP por e-mail (oficial da Onda 1) ou Microsoft Entra ID (futuro, coexistindo), conforme ADR-0020.

### Observações

O mecanismo de Login em si (OTP por e-mail, com Entra ID coexistindo no futuro) está decidido pela ADR-0020; o que permanece pendente é apenas o formato físico de persistência da sessão, decisão técnica de implementação. Esta entidade e o fluxo de Login que a cria exigem revisão do Agente Engenheiro de Segurança Sênior antes da implementação (ADR-0020).

### Telas que utilizam

`Login`, `Seleção da Unidade de Negócio`, e implicitamente toda tela autenticada.

# Fornecedores

# Materiais

# Serviços

# Solicitações

# Cotações

# Negociação

# Aprovação

Entidades de Fornecedores, Materiais, Serviços, Solicitações, Cotações, Negociação e do fluxo transacional de Aprovação pertencem às Ondas 2 e 3 e não são desenvolvidas nesta atualização (ver `ComprasFuncional.md`). O modelo já real de Fornecedores permanece em `docs/backend/procurement/Procurement.md` até ser incorporado formalmente a este documento.

# Pedidos

# Recebimento Fiscal

# Pagamentos

# Relatórios

Pertencem às Ondas 3/4 (Pedidos, Recebimento Fiscal, Pagamentos) ou a uma Onda não classificada nesta leitura (Relatórios — ver dúvida de produto em `ComprasFuncional.md`). Não desenvolvidas nesta atualização.

# Administração do Sistema

## Entidade: IdentityProvider

### Objetivo

Registrar o(s) provedor(es) de identidade de cada Unidade de Negócio.

### Origem dos Dados

Tela `Administração do Sistema > Identity Providers`.

### Destino dos Dados

Consultado por `Login` para determinar o mecanismo de autenticação da Unidade de Negócio informada/inferida.

### Banco +Compras

Entidade lógica `IdentityProvider`: `UnidadeNegocioId`, tipo de provider, domínio(s) de e-mail autorizado(s), parâmetros de configuração (armazenados de forma segura, nunca em texto claro), status.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `UnidadeNegocio` (uma Unidade pode ter mais de um Identity Provider).

### Índices

Índice em `UnidadeNegocioId`; índice único em domínio de e-mail por Unidade (para resolução do provider correto a partir do e-mail informado no Login).

### Integrações

Microsoft Entra ID (futuro); demais provedores aprovados pelo Product Owner.

### Observações

Parâmetros sensíveis (client id/secret, tenant id) exigem armazenamento cifrado — decisão de infraestrutura, não tratada neste blueprint funcional.

### Telas que utilizam

`Administração do Sistema > Identity Providers`, `Login` (consulta), `Administração > Gestão de Unidades de Negócio` (vínculo).

## Entidade: ConfiguracaoErp

### Objetivo

Registrar o ERP associado a cada Unidade de Negócio e seus parâmetros de conexão/mapeamento.

### Origem dos Dados

Tela `Administração > Configuração ERP`.

### Destino dos Dados

Consumida, a partir da Onda 4, pelas integrações reais de sincronização com o ERP (`ARCHITECTURE.md` §17).

### Banco +Compras

Entidade lógica `ConfiguracaoErp`: `UnidadeNegocioId`, sistema ERP, parâmetros de conexão (armazenados de forma segura), status.

### Banco ERP

Não aplicável nesta Onda — nenhuma leitura/escrita real ocorre; referência de padrão já real para Fornecedores/Linx em `docs/backend/integration/FornecedorSynchronization.md` (connection strings segregadas `MaisComprasConnection`/`ErpConnection`).

### Relacionamentos

N:1 (ou 1:1, a confirmar) com `UnidadeNegocio`.

### Índices

Índice único em `UnidadeNegocioId` (se 1:1) ou índice em `UnidadeNegocioId` (se 1:N, permitindo múltiplos ERPs por Unidade — **PENDÊNCIA** não coberta pela ADR-0013, que só afirma "cada BU pode possuir um ERP distinto", sem tratar múltiplos ERPs por BU).

### Integrações

ERP corporativo da Unidade de Negócio (ex.: Linx, `SOMA_DESENV`/`MAISCOMPRAS` via VPN).

### Observações

Mapeamentos de domínio (`TipoFornecedor`, `CondicaoPagamento`, etc., ADR-0016) são estrutura prevista, mas populada a partir da Onda 2 — não fazem parte do blueprint da Onda 1 além do registro de que a estrutura de domínio sincronizado existirá.

### Telas que utilizam

`Administração > Configuração ERP`, `Administração > Gestão de Unidades de Negócio` (vínculo).

## Entidade: FeatureFlag

### Objetivo

Controlar a disponibilidade de funcionalidades já implementadas, por Unidade de Negócio, sem novo deploy.

### Origem dos Dados

Tela `Administração do Sistema > Feature Flags` (ativação/desativação); criação da flag em si pode ser técnica (deploy) — **PENDÊNCIA** (ver `ComprasFuncional.md`).

### Destino dos Dados

Consultada por qualquer módulo que precise verificar se está habilitado para a Unidade de Negócio ativa.

### Banco +Compras

Entidade lógica `FeatureFlag`: nome, descrição, Unidade(s) de Negócio associadas, status.

### Banco ERP

Não aplicável.

### Relacionamentos

N:N com `UnidadeNegocio`.

### Índices

Índice único em nome da flag.

### Integrações

Nenhuma.

### Observações

Catálogo inicial de flags para a Onda 1 não está definido; entidade nasce vazia.

### Telas que utilizam

`Administração do Sistema > Feature Flags`.

## Entidade: Parametro

### Objetivo

Centralizar parâmetros de sistema não cobertos pelas demais entidades administrativas.

### Origem dos Dados

Tela `Administração do Sistema > Parâmetros`.

### Destino dos Dados

Consultado por qualquer módulo que precise de um valor configurável não coberto por Workflow/Aprovação/Controle Orçamentário.

### Banco +Compras

Entidade lógica `Parametro`: chave, valor, descrição, `UnidadeNegocioId` (opcional — global quando ausente).

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 opcional com `UnidadeNegocio` (parâmetro pode ser global ou por Unidade).

### Índices

Índice único em (chave, `UnidadeNegocioId`) — permitindo o mesmo parâmetro global e sobrescrito por Unidade, se essa semântica for aprovada (**PENDÊNCIA**).

### Integrações

Nenhuma.

### Observações

Catálogo inicial de parâmetros para a Onda 1 não está definido; entidade nasce vazia.

### Telas que utilizam

`Administração do Sistema > Parâmetros`.

# Administração (Workflow, Alçadas, Controle Orçamentário)

> A partir da revisão R1.1 (ADR-0020), `RegraWorkflow`, `AlcadaAprovacao` e `RegraOrcamentaria` pertencem a `Administração`, não mais a `Configurações`. `Configurações` passa a conter apenas preferências pessoais do usuário (Conta, Preferências, Tema, Idioma), sem entidade própria especificada nesta revisão.

## Entidade: RegraWorkflow

### Objetivo

Registrar a configuração do motor de workflow (etapas, condições, responsáveis) por Unidade de Negócio.

### Origem dos Dados

Tela `Administração > Workflow` (a partir da revisão R1.1/ADR-0020; antes em `Configurações`).

### Destino dos Dados

Consumida, a partir da Onda 3, pelo motor de workflow (`Workflow`/`WorkflowRunner`, já existente em estado básico — `PROJECT_STATE.md`) na orquestração do processo de compras.

### Banco +Compras

Entidade lógica `RegraWorkflow`: nome, `UnidadeNegocioId`, etapas (estrutura ainda não definida — **PENDÊNCIA**, ver `ComprasFuncional.md`), condições de aplicação, status.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `UnidadeNegocio`; relação futura com entidades transacionais do processo de compras (Solicitação/Cotação/Pedido — Onda 3, fora de escopo aqui).

### Índices

Índice em `UnidadeNegocioId`.

### Integrações

Nenhuma nesta Onda.

### Observações

Desenho definitivo das etapas é dúvida de produto registrada em `ComprasFuncional.md`; o material de `fluxo_compras_indiretas_html.html` é apenas referência de mercado.

### Telas que utilizam

`Administração > Workflow`.

## Entidade: AlcadaAprovacao

### Objetivo

Registrar a configuração de alçadas de aprovação (critério, nível, aprovador) por Unidade de Negócio.

### Origem dos Dados

Tela `Administração > Alçadas` (a partir da revisão R1.1/ADR-0020; antes em `Configurações > Aprovação`).

### Destino dos Dados

Consumida, a partir da Onda 3, pelo fluxo transacional de aprovação de compra.

### Banco +Compras

Entidade lógica `AlcadaAprovacao`: nome, `UnidadeNegocioId`, critério (valor/categoria/centro de custo — **PENDÊNCIA** de catálogo definitivo), nível/ordem, aprovador ou perfil aprovador, status.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `UnidadeNegocio`; N:1 com `Usuario`/`Perfil` (aprovador); relação futura com `RegraWorkflow` (etapa de aprovação dentro do workflow).

### Índices

Índice em `UnidadeNegocioId`.

### Integrações

Nenhuma nesta Onda.

### Observações

Critérios definitivos de alçada são dúvida de produto registrada em `ComprasFuncional.md`.

### Telas que utilizam

`Administração > Alçadas`.

## Entidade: RegraOrcamentaria

### Objetivo

Registrar a configuração de controle orçamentário (centro de custo, categoria, período, limite, comportamento em estouro) por Unidade de Negócio.

### Origem dos Dados

Tela `Administração > Controle Orçamentário` (a partir da revisão R1.1/ADR-0020; antes em `Configurações`).

### Destino dos Dados

Consumida, a partir da Onda 3/4, pela validação de orçamento antes de uma solicitação de compra avançar.

### Banco +Compras

Entidade lógica `RegraOrcamentaria`: `UnidadeNegocioId`, `CentroCustoId` (vínculo com `Administração > Gestão de Centros de Custo`, ADR-0020), categoria (opcional), período, limite, comportamento em estouro (Bloquear/Alertar/Exigir aprovação adicional).

### Banco ERP

**PENDÊNCIA:** fonte de verdade do saldo orçamentário (ERP financeiro, planilha corporativa, ou saldo próprio do +Compras) não definida — ver `ComprasFuncional.md`.

### Relacionamentos

N:1 com `UnidadeNegocio`; N:1 com `CentroCusto` (ADR-0020); relação futura com `AlcadaAprovacao` (quando o comportamento em estouro exigir aprovação adicional).

### Índices

Índice em (`UnidadeNegocioId`, centro de custo, período).

### Integrações

Possível integração futura com sistema financeiro/ERP (Onda 4) — pendente.

### Observações

Nenhuma.

### Telas que utilizam

`Administração > Controle Orçamentário`.

# IA

Nenhuma entidade própria nesta Onda (ver `ComprasFuncional.md`).

# Glossário

Ver `ComprasFuncional.md` (glossário único, não duplicado aqui).

---

# Entidade transversal: RegistroAuditoria

### Objetivo

Registrar, de forma append-only, toda operação de escrita sobre as entidades administrativas desta Onda (mesmo padrão já adotado em Fornecedores — ver `docs/backend/integration/FornecedorSynchronization.md`).

### Origem dos Dados

Gerado automaticamente por toda operação de criação/edição/inativação nas telas de Administração, Administração do Sistema e Configurações.

### Destino dos Dados

Consultado por telas de auditoria (quando existirem) e por investigação/compliance.

### Banco +Compras

Entidade lógica `RegistroAuditoria`: `UnidadeNegocioId`, entidade afetada, identificador do registro, usuário responsável, data/hora, valor anterior, valor novo, `CorrelationId`.

### Banco ERP

Não aplicável.

### Relacionamentos

N:1 com `Usuario`; referencia genericamente qualquer entidade administrativa desta Onda.

### Índices

Índice em (entidade afetada, identificador do registro); índice em (`UnidadeNegocioId`, data/hora).

### Integrações

Nenhuma.

### Observações

Estrutura deliberadamente genérica nesta etapa; o modelo físico definitivo (uma tabela genérica vs. uma tabela de auditoria por entidade) é decisão de arquitetura, fora de escopo funcional.

### Telas que utilizam

Transversal — toda tela de escrita em Administração, Administração do Sistema e Configurações gera registros aqui; nenhuma tela de consulta de auditoria própria foi especificada nesta Onda (**PENDÊNCIA**: confirmar se é necessária uma tela dedicada de auditoria já na Onda 1).
