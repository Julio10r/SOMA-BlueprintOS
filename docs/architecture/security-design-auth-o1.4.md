# Security Design Review — Autenticação O1.4 (O1.4.1)

> Revisão arquitetural de segurança exigida por ADR-0020 (item 13) e por [domain-principles.md](./domain-principles.md) §Segurança, antes de qualquer implementação de autenticação da Onda 1. Este documento é exclusivamente de arquitetura, threat modeling e definição de controles — **nenhum código de autenticação, endpoint, migration, OTP ou Bootstrap foi implementado como parte desta revisão.**

**Data:** 06/08/2026
**Executor:** Claude (Agente Engenheiro de Segurança Sênior, função ad hoc para esta revisão)
**Status:** Gate de Segurança — ver seção 14.

---

## 1. Arquitetura recomendada

### 1.1 Componentes de autenticação

```
[Browser] --HTTPS--> [API BlueprintOS]
   |                       |
   | Cookie de sessão      | valida sessão / consulta SessaoAutenticacao
   | (HttpOnly, Secure,    | resolve Usuario + UnidadeNegocioId ativa
   |  SameSite=Strict)     | resolve Perfis -> Permissoes (RBAC)
```

- **Autenticação desacoplada de autorização** (já um princípio aprovado): o resultado da autenticação é apenas "este é o `UsuarioId` X, nesta `UnidadeNegocioId` Y, criado por este IdP". RBAC (Perfis → Permissões) é resolvido em toda requisição a partir do `UsuarioId`, nunca embutido de forma imutável no token/sessão além de um cache de curta duração (ver 5.5).
- **Múltiplos Identity Providers coexistentes por Unidade de Negócio**: cada IdP (Email OTP, Entra ID, futuros) implementa um contrato comum de resultado — `(UsuarioId resolvido | rejeitado, motivo interno não exposto ao cliente)`. A camada de sessão é agnóstica ao IdP usado; `SessaoAutenticacao.IdentityProviderId` apenas registra qual foi usado.
- **Resolução de domínio → IdP**: ao receber um e-mail, o backend consulta `IdentityProvider` pelo domínio autorizado para decidir se o fluxo é OTP local ou redirecionamento Entra ID. Essa resolução **nunca deve revelar** ao cliente se o e-mail existe como usuário — apenas qual mecanismo de login usar para aquele domínio (o domínio autorizado é informação pública de configuração da BU, não do usuário).
- **`ICurrentIdentity`/`DevelopmentRequestIdentity` (ADR-0011)**: continua sendo o adaptador válido apenas em `Development`. A implementação real de produção deve ser um novo adaptador (`SessionCurrentIdentity` ou equivalente) que lê a sessão validada, nunca headers de cliente.

### 1.2 Sessão: modelo físico recomendado

**Recomendação: sessão persistida no banco (server-side), referenciada por um identificador opaco em cookie — não JWT stateless.**

Motivos:
- `SessaoAutenticacao` já é uma entidade de domínio persistida (ComprasDataModel.md) — o modelo já pressupõe estado no servidor, não um token autocontido.
- Revogação imediata (logout, admin desativa usuário, mudança de perfil) é trivial com sessão server-side; com JWT stateless, revogação exige blocklist adicional — complexidade equivalente sem benefício, já que o sistema já persiste a sessão.
- Rotação de contexto (troca de `UnidadeNegocioId` em "Seleção da Unidade de Negócio") é uma **atualização de estado**, não reemissão de token — mais simples com sessão persistida.
- `UnidadeNegocioId` pode mudar de decisão de particionamento de dados no futuro; acoplar isso a claims de um JWT de longa duração é mais rígido do que uma linha de tabela.

O identificador de sessão no cookie deve ser um valor aleatório opaco de alta entropia (≥128 bits, gerado por CSPRNG), nunca o `SessaoAutenticacao.Id` sequencial nem qualquer dado derivável do usuário. Armazenar no banco apenas o **hash** do identificador (mesmo princípio do OTP, seção 5), para que um dump de banco não permita sequestro direto de sessões ativas.

**Isso é uma recomendação de arquitetura, não uma decisão fechada** — ComprasDataModel.md marca esse ponto como PENDÊNCIA explícita, delegada à "Work Order de Estrutura". Este documento resolve a pendência do ponto de vista de segurança; a decisão final ainda deve ser ratificada pelo Product Owner/CTO na Work Order técnica de O1.4.2, já que envolve escolha de stack (ex.: `Microsoft.AspNetCore.Authentication.Cookies` vs. implementação própria).

---

## 2. Threat model

Formato: cenário → ameaças relevantes → mitigação recomendada (detalhe de controle na seção 3).

### 2.1 Solicitação de OTP
- **User enumeration**: resposta ao solicitar OTP deve ser idêntica para e-mail existente/inexistente/inativo/domínio não autorizado *dentro da mesma categoria de decisão pública*. Especificamente: se o domínio não é autorizado por nenhuma BU, é aceitável informar isso (é config pública, não vaza existência de usuário) — mas se o domínio é autorizado, a resposta para "e-mail existe e está ativo" e "e-mail não existe/inativo" deve ser **idêntica** ("se o e-mail for válido, um código foi enviado"), com o mesmo tempo de resposta (evitar timing side-channel entre "usuário existe, hash+envio" e "usuário não existe, no-op").
- **Rate limit abuse / brute force de solicitação**: sem limite, um atacante pode inundar caixas de entrada de terceiros (spam/harassment) ou esgotar cota do provedor de e-mail transacional. Mitigar com rate limiting por e-mail e por IP (seção 3.3).
- **Race condition**: duas solicitações simultâneas para o mesmo e-mail não devem gerar dois OTPs válidos simultâneos — a criação de um novo `CodigoVerificacaoOtp` deve invalidar (status `Expirado`/substituído) qualquer código `Pendente` anterior do mesmo usuário, dentro de uma transação.

### 2.2 Entrega do OTP
- **Exposição em logs**: o código nunca deve aparecer em log de aplicação, log de provedor de e-mail (assunto/corpo logado por engano), ferramenta de observability, ou resposta de API — nem em ambiente de erro/debug.
- **Interceptação de e-mail (conta de e-mail comprometida)**: risco residual aceito — é a superfície de ataque inerente ao Email OTP; mitigado por curta validade (5–10 min) e uso único.
- **Provedor transacional não confiável/mal configurado**: dependência ainda não contratada (PROJECT_STATE.md, pendência). Bloqueador para implementação — ver seção 12.

### 2.3 Validação do OTP
- **OTP guessing / brute force**: código de 6 dígitos = 1.000.000 combinações; sem limite de tentativas, é trivialmente quebrável. Mitigar com limite estrito de tentativas por código (ex.: 5) + invalidação do código após o limite + rate limiting por IP/e-mail.
- **OTP replay**: uso único — status muda para `Consumido` atomicamente na primeira validação bem-sucedida; tentativa de reuso é rejeitada mesmo dentro da validade.
- **Timing attack na comparação de hash**: comparação deve ser de tempo constante (usar comparação de hash própria da lib de hashing, não `==` de string).
- **Confusão de contexto**: validar novamente, no momento da criação da sessão (não só no momento de gerar o OTP), que o usuário está Ativo e possui vínculo com a Unidade de Negócio — estado pode ter mudado entre a solicitação e a validação do código.

### 2.4 Criação de sessão
- **Session fixation**: nunca aceitar um identificador de sessão fornecido pelo cliente antes da autenticação; o identificador é sempre gerado pelo servidor no momento da criação, após validação bem-sucedida do OTP (ou do Entra ID).
- **Privilege confusion entre BUs**: a sessão é criada com uma `UnidadeNegocioId` ativa resolvida no momento do login (ou selecionada, se houver múltiplas). Toda operação subsequente deve validar que o recurso acessado pertence à `UnidadeNegocioId` ativa da sessão — nunca confiar em um `UnidadeNegocioId` vindo do payload/query string do cliente sem cruzar com a sessão.

### 2.5 Renovação/expiração de sessão
- **Sessão eterna**: definir expiração absoluta (ex.: 12–24h) **e** expiração por inatividade (ex.: 30–60 min sem requisição) — ambas necessárias; só uma das duas deixa brecha (sessão nunca usada mas nunca expira vs. sessão usada indefinidamente).
- **Sessão sobrevive a mudanças de estado**: se o usuário for inativado, o Perfil for removido/alterado, ou o vínculo com a BU for removido **durante** uma sessão ativa, a próxima requisição autenticada deve falhar (revalidação do estado do usuário a cada requisição autorizada, não apenas checagem de expiração de tempo) — mitiga privilege escalation residual pós-mudança administrativa.

### 2.6 Logout
- **Logout que não revoga**: logout deve marcar a sessão como revogada no servidor (não só apagar o cookie no cliente) — essencial no modelo de sessão persistida; caso contrário, o identificador de sessão roubado antes do logout continuaria válido.
- **CSRF em logout**: logout deve exigir método não idempotente (POST) com verificação de CSRF, para evitar logout forçado via link malicioso (baixo impacto, mas inconsistente se não tratado uniformemente).

### 2.7 Bootstrap inicial
- **Reabertura de Bootstrap / condição de corrida**: a checagem "não existe nenhum Administrador Sênior" deve ser atômica com a criação do primeiro Administrador Sênior — usar uma transação com isolamento adequado (ou constraint única em `BootstrapEstado.Concluido`) para impedir duas requisições concorrentes de Bootstrap criando dois "primeiros" administradores.
- **Bootstrap exposto publicamente sem controle**: qualquer visitante não autenticado que descubra a rota de Bootstrap, antes do primeiro Admin existir, poderia se autonomear Administrador Sênior. Mitigar com o "segredo de implantação" (ver 3.7) como controle adicional, não apenas a ausência de Admin.
- **Bootstrap reaberto por engenharia social alegando "perda de acesso"**: por decisão de produto (ADR-0020), isso é explicitamente proibido — Bootstrap nunca reabre; deve ser tecnicamente impossível (constraint, não apenas regra de negócio condicional), e recuperação de acesso é procedimento operacional fora do Bootstrap.
- **Bootstrap incompleto (falha no meio do fluxo)**: se a criação da BU tiver sucesso mas a criação do Admin Sênior falhar, `BootstrapEstado.Concluido` não deve ser marcado — todo o fluxo (criar BU + criar Admin) deve ser atômico (uma transação), ou o Bootstrap deve ser reentrante de forma segura até `Concluido=true`.

### 2.8 Autorização por Perfis
- **Cache de permissões desatualizado**: se permissões efetivas forem cacheadas por sessão sem invalidação, uma mudança de Perfil/Permissão não reflete imediatamente — ver mitigação em 5.5.
- **IDOR**: qualquer endpoint que recebe um ID de recurso (ex.: `UsuarioId`, `PerfilId`, `CentroCustoId`) deve validar que o recurso pertence à `UnidadeNegocioId` ativa da sessão E que o usuário tem a Permissão específica sobre aquele tipo de recurso — nunca assumir que "ID válido" implica "acesso permitido".
- **Escalonamento via criação de Perfil**: como Perfis definem permissões, a Permissão `Perfil.Gerenciar` é, por transitividade, a permissão mais sensível do sistema (permite criar um Perfil com qualquer combinação de permissões e se auto-atribuir). Deve ser tratada como equivalente a "admin total" e auditada com o mesmo rigor de mudanças de Permissão em `Usuario`.

### 2.9 Seleção/contexto de Unidade de Negócio
- **Troca de BU sem revalidação**: ao trocar `UnidadeNegocioId` ativa, revalidar que o usuário possui vínculo (`UsuarioUnidadeNegocio`) com a nova BU antes de atualizar a sessão — nunca aceitar a troca apenas porque o cliente enviou um `UnidadeNegocioId` diferente.
- **Vazamento cross-tenant**: qualquer consulta a dados de negócio deve filtrar por `UnidadeNegocioId` da sessão no nível da camada de acesso a dados (não apenas no controller) — um filtro esquecido em uma única query é suficiente para vazamento entre BUs (consistente com `context/security.md`, seção Autorização, sobre multi-tenant).

### 2.10 APIs protegidas
- **Endpoints sem `[Authorize]` por omissão**: adotar postura "seguro por padrão" — todo endpoint exige autenticação a menos que explicitamente marcado como público (login, solicitação de OTP, health check), nunca o inverso. Hoje **nenhum** endpoint tem `[Authorize]` (achado da auditoria de código, seção 8) — isso é aceitável apenas porque não há autenticação real ainda; passa a ser bloqueador no momento em que autenticação real existir e qualquer endpoint de negócio permanecer sem proteção.
- **Mensagens de erro internas expostas**: já é regra existente (`context/security.md`) — reforçar especificamente para erros de autenticação (nunca diferenciar "usuário não existe" de "senha/código errado" na mensagem ao cliente).
- **CORS/CSRF combinados incorretamente**: ver seção 3.5/3.6 — o CORS atual (`AllowAnyHeader/AllowAnyMethod`, sem `AllowCredentials`) é incompatível com cookies de sessão cross-origin; precisa de revisão dedicada antes de qualquer cookie de sessão existir (ver achado crítico na seção 8).

---

## 3. Controles obrigatórios

### 3.1 OTP
| Aspecto | Controle recomendado |
|---|---|
| Formato | 6 dígitos numéricos, gerados por CSPRNG (não `Random` padrão) |
| Validade | 10 minutos (equilíbrio entre UX e janela de ataque; ComprasDataModel.md cita 15 min como referência visual — 10 min é o recomendado desta revisão, decisão final cabe ao PO) |
| Uso único | Status muda para `Consumido` atomicamente na 1ª validação correta; qualquer tentativa seguinte é rejeitada mesmo com o mesmo código dentro da validade |
| Armazenamento | Apenas hash (ex.: SHA-256 com salt, ou algoritmo de senha como Argon2/BCrypt se custo computacional for aceitável para o volume esperado) — nunca texto claro, nunca reversível |
| Limite de tentativas | Máx. 5 tentativas de validação por código; ao exceder, código é invalidado (`Expirado`) e novo código deve ser solicitado |
| Rate limiting de solicitação | Máx. ~3 solicitações por e-mail em 15 min; cooldown mínimo de 60s entre reenvios; limite adicional por IP para conter ataques distribuídos por e-mails diferentes |
| Invalidação de códigos anteriores | Gerar novo código invalida (marca `Expirado`) qualquer código `Pendente` anterior do mesmo usuário, na mesma transação |
| Anti-enumeração | Resposta e tempo de resposta idênticos para e-mail de domínio autorizado, independente de o usuário existir/estar ativo (seção 2.1) |
| Auditoria sem código | Registrar evento (e-mail, timestamp, resultado, IP) sem nunca persistir o código em claro em log ou auditoria |

### 3.2 Sessão
- Cookie de sessão: `HttpOnly`, `Secure`, `SameSite=Strict` (ou `Lax` apenas se algum fluxo legítimo de navegação cross-site exigir — não identificado nenhum caso no design atual, então `Strict` é a recomendação).
- Identificador opaco de alta entropia, armazenado como hash no servidor (seção 1.2).
- Expiração absoluta: 12h (ajustável). Expiração por inatividade: 30 min sem requisição autenticada.
- Rotação do identificador de sessão em qualquer elevação de contexto (troca de BU, ou se no futuro houver step-up de autenticação) — mitiga reaproveitamento do identificador anterior.
- Revogação: logout marca sessão como revogada; painel futuro de "sessões ativas" (fora do escopo O1.4, mas o modelo de dados já suporta, já que `SessaoAutenticacao` é uma entidade persistida por sessão) permitiria revogação remota.
- Múltiplas sessões simultâneas: permitir por padrão (um usuário pode estar logado em mais de um dispositivo) — nenhuma decisão de produto pede o contrário; revisar apenas se o PO quiser single-session-per-user no futuro.
- Sessão após mudança de Perfil/Status: revalidar status do usuário e vínculo com a BU a cada requisição (não apenas na criação da sessão) — ver 2.5 e 5.5.
- Nenhum token sensível em localStorage/sessionStorage/HTML/query string/JS acessível — exclusivamente cookie `HttpOnly` (o backend audit já confirma que hoje não há nenhum uso de localStorage/sessionStorage no frontend, o que facilita manter essa garantia desde o início).

### 3.3 Rate limiting
- Por e-mail: solicitação de OTP e validação de OTP (limites da seção 3.1).
- Por IP: camada adicional para conter abuso distribuído entre e-mails diferentes vindos do mesmo IP/rede.
- Aplicar em nível de middleware/gateway (ex.: `Microsoft.AspNetCore.RateLimiting`), não apenas lógica de aplicação, para resistir a bypass por chamada direta ao endpoint.
- Bootstrap: rate limiting adicional e mais estrito na tentativa de Bootstrap (é um endpoint pré-autenticação de altíssimo privilégio).

### 3.4 Browser (headers HTTP)
Nenhum destes está implementado hoje (confirmado por auditoria de código, seção 8) — todos são bloqueadores antes de expor autenticação real:

| Header | Valor recomendado |
|---|---|
| `Content-Security-Policy` | Restritiva por padrão (`default-src 'self'`), ajustada para os domínios reais de assets/API; sem `unsafe-inline`/`unsafe-eval` se possível |
| `Strict-Transport-Security` | `max-age=31536000; includeSubDomains` (ativar `UseHsts()` já disponível no ASP.NET Core) |
| `X-Content-Type-Options` | `nosniff` |
| `Referrer-Policy` | `strict-origin-when-cross-origin` ou `no-referrer` |
| `X-Frame-Options` / `frame-ancestors` | `DENY` (ou `frame-ancestors 'none'` via CSP) — não há necessidade de embedding em iframe |
| `Cache-Control` para conteúdo autenticado | `no-store` em toda resposta de API autenticada, para impedir cache de dados sensíveis em proxies/browser |

### 3.5 CSRF
Cookies de sessão sem token anti-CSRF são vulneráveis a CSRF em navegadores que não isolam bem `SameSite`. Estratégia recomendada, compatível com o modelo de cookie de sessão:
- `SameSite=Strict` já mitiga a maioria dos cenários de CSRF cross-site.
- Para operações de mutação (POST/PUT/DELETE), adotar padrão **double-submit token** ou cabeçalho customizado (`X-Requested-With` ou token CSRF em header, verificado contra o valor da sessão) como defesa em profundidade — não depender exclusivamente de `SameSite`, já que navegadores/extensões legados podem não honrá-lo.

### 3.6 CORS
Achado crítico na auditoria (seção 8): a configuração atual (`AllowAnyHeader().AllowAnyMethod()`, sem `AllowCredentials()`) funciona hoje porque não há cookies — mas está fundamentalmente incompatível com o modelo de sessão via cookie recomendado aqui, e o fallback de origem (`localhost:5173`) não é isolado por ambiente, o que é um risco de configuração incorreta em produção.

Recomendação para quando cookies de sessão existirem:
- `AllowCredentials()` deve ser habilitado **apenas** junto com uma lista explícita e fechada de origens (`WithOrigins`) — nunca com `AllowAnyOrigin()`, que o ASP.NET Core já proíbe combinar com credentials, mas o fallback atual precisa ser auditado para garantir que nunca resolve para `*` em produção.
- A origem permitida em produção deve vir exclusivamente de configuração explícita por ambiente (ex.: Secret Manager/config de produção), nunca do fallback hardcoded de desenvolvimento — o fallback atual (`localhost:5173`) deve ser condicionado a `IsDevelopment()`, não aplicado incondicionalmente quando a chave de config está ausente.
- Restringir `AllowAnyHeader`/`AllowAnyMethod` aos métodos/headers realmente usados pela API, reduzindo superfície.

### 3.7 Bootstrap — controles específicos
- **Segredo de implantação**: um valor secreto (gerado na implantação, armazenado em Secret Manager conforme `context/security.md`, nunca em `appsettings.json`/repositório) deve ser exigido como cabeçalho/parâmetro adicional para acessar o fluxo de Bootstrap — camada extra além da condição "nenhum Admin existe".
- **Identidade inicial pré-autorizada**: o e-mail que pode se tornar o primeiro Administrador Sênior deve estar pré-configurado (ex.: variável de ambiente/Secret Manager, definida na implantação) — Bootstrap não deve aceitar qualquer e-mail arbitrário como candidato a primeiro admin.
- **OTP também no Bootstrap**: mesmo sendo pré-autorizado, o e-mail candidato ainda deve provar posse da caixa de entrada via OTP — nunca criar o Admin Sênior apenas por conhecer o segredo de implantação.
- **Atomicidade** (seção 2.7): criação de BU + Admin Sênior + fechamento do Bootstrap em uma única transação.
- **Encerramento permanente**: `BootstrapEstado.Concluido=true` é a fonte de verdade única e definitiva; a rota/endpoint de Bootstrap deve checar essa flag a cada requisição e retornar 404/rejeição indistinguível de "rota inexistente" quando já concluído — nunca um 403 que confirme a existência do endpoint pós-conclusão (redução de superfície de reconhecimento).

### 3.8 Auditoria
Eventos mínimos (nunca registrar OTP, cookie, segredo, token, credencial em claro):

| Evento | Dados registrados |
|---|---|
| OTP solicitado | e-mail (ou hash do e-mail), IP, timestamp, resultado (aceito/rate-limited) |
| OTP enviado | e-mail, timestamp, provedor usado, sucesso/falha de envio |
| Tentativa de validação inválida | e-mail, IP, timestamp, motivo genérico (código incorreto/expirado/excedeu tentativas) |
| Login realizado | `UsuarioId`, `UnidadeNegocioId`, `IdentityProviderId`, IP, timestamp |
| Logout | `UsuarioId`, `SessaoAutenticacao.Id` (ou seu hash), timestamp |
| Sessão expirada/revogada | `UsuarioId`, motivo (inatividade/absoluta/logout/admin), timestamp |
| Bootstrap iniciado | IP, timestamp, e-mail candidato (se aplicável) |
| Bootstrap concluído | `UsuarioId` do primeiro Admin Sênior, `UnidadeNegocioId` criada, timestamp — primeiro registro de auditoria do ambiente |
| Falhas relevantes | domínio não autorizado, usuário inativo tentando autenticar, tentativa de Bootstrap pós-conclusão |

---

## 4. Modelo de sessão (resumo executivo)
Ver seções 1.2, 2.4–2.6, 3.2. Sessão persistida server-side, cookie opaco `HttpOnly/Secure/SameSite=Strict`, expiração absoluta + por inatividade, revalidação de estado do usuário a cada requisição, revogação real no logout.

## 5. Modelo de OTP (resumo executivo)
Ver seção 3.1. 6 dígitos, CSPRNG, 10 min de validade, uso único, hash no armazenamento, 5 tentativas, rate limiting por e-mail e IP, invalidação em cascata, resposta anti-enumeração.

### 5.5 Cache de permissões (detalhe transversal)
Se, por performance, as permissões efetivas forem cacheadas (ex.: em memória por request ou cache de curta duração), o cache deve ter TTL curto (segundos, não minutos) ou ser invalidado explicitamente em qualquer mutação de `Usuario.Status`, `UsuarioPerfil`, `PerfilPermissao`. Recomendação mais simples para O1.4: **resolver permissões a cada requisição diretamente do banco**, sem cache, até que haja evidência de necessidade de performance — evita toda a classe de bug de cache desatualizado descrita em 2.8.

## 6. Modelo de Bootstrap (resumo executivo)
Ver seções 2.7 e 3.7. Condição atômica, segredo de implantação, identidade pré-autorizada, OTP obrigatório mesmo no Bootstrap, transação única, encerramento permanente e indistinguível de "não existe" após conclusão.

## 7. Requisitos de cookies
Ver seção 3.2. `HttpOnly`, `Secure`, `SameSite=Strict`, identificador opaco de alta entropia, hash no armazenamento server-side.

## 8. Requisitos de headers
Ver seção 3.4. **Achado de auditoria: nenhum destes headers está implementado hoje** (`Program.cs` não configura CSP/HSTS/X-Frame-Options/X-Content-Type-Options/Referrer-Policy/Cache-Control) — bloqueador antes de expor autenticação real fora de Development.

## 9. Rate limiting
Ver seção 3.3. Não implementado hoje — bloqueador.

## 10. Auditoria
Ver seção 3.8. Nenhuma trilha de auditoria de autenticação existe hoje (consistente com "nenhuma autenticação implementada ainda") — a entidade `RegistroAuditoria` já existe no blueprint para mudanças administrativas; deve ser estendida para cobrir os eventos de autenticação listados.

## 11. Autorização
Modelo confirmado e reafirmado sem exceção (ADR-0020, `domain-principles.md`): `Usuario → Perfis (1..N) → Permissões`, permissões efetivas = união de todos os Perfis vinculados, nunca permissão individual.

Análise dos cenários solicitados:
- **Usuário inativado durante sessão**: deve invalidar a sessão na próxima requisição (revalidação por requisição, seção 2.5/3.2) — não esperar expiração natural.
- **Perfil inativado**: se um Perfil vinculado a um usuário for inativado, as permissões daquele Perfil devem deixar de contar na união efetiva imediatamente (sem cache de longa duração, seção 5.5) — o usuário não perde a sessão, apenas perde as permissões daquele Perfil (a menos que fique sem nenhum Perfil ativo, cenário equivalente a usuário sem acesso).
- **Alteração de permissões durante sessão**: mesmo princípio — refletir na próxima requisição, sem exigir novo login.
- **Acesso por Unidade de Negócio**: toda autorização é sempre relativa à `UnidadeNegocioId` ativa da sessão; Perfis podem ser globais ou por BU (pendência de modelo de dados ainda aberta, ComprasDataModel.md) — do ponto de vista de segurança, isso é neutro **desde que** a verificação de permissão sempre cruze Perfil × BU ativa quando o Perfil for escopado por BU.

## 12. Bloqueadores (impedem início de O1.4.2)

1. **Catálogo de Perfis/Permissões, incluindo "Administrador Sênior"** — sem esse catálogo aprovado, não há como implementar Bootstrap (que depende de um Perfil "Administrador Sênior" existente) nem RBAC real. **Decisão do Product Owner**, não desta revisão.
2. **Provedor transacional de e-mail para OTP** — não contratado/selecionado. Sem ele, OTP não pode ser implementado nem testado.
3. **Escopo de `Perfil` (global vs. por BU)** e **natureza do catálogo de `Permissao` (global vs. por BU)** — pendências de modelo de dados que afetam diretamente como a verificação de autorização é escrita (seção 11). Precisam de decisão antes da Work Order técnica de estrutura.
4. **Semântica de "nenhum `UsuarioCentroCusto`"** (nenhum acesso vs. acesso a todos) — não bloqueia autenticação em si, mas bloqueia qualquer endpoint que dependa de escopo de Centro de Custo pós-login.
5. **Mecanismo físico de sessão** — esta revisão recomenda sessão persistida (seção 1.2), mas a decisão final de stack/implementação cabe à Work Order técnica de O1.4.2, com este documento como insumo de segurança, não substituto da decisão de arquitetura técnica.

Nenhum destes bloqueadores exige alteração da arquitetura já aprovada em ADR-0020 — são decisões de detalhamento já sinalizadas como pendentes nos próprios documentos de produto/dados.

## 13. Riscos residuais (aceitos, não bloqueadores)

- **Interceptação de e-mail corporativo comprometido** — risco inerente a qualquer Email OTP; mitigado, não eliminado, por validade curta e uso único. Aceitável para Onda 1; Entra ID (MFA corporativo) é o caminho de mitigação de longo prazo, já previsto para coexistir.
- **Dependência de disponibilidade do provedor de e-mail transacional** — login falha se o provedor estiver fora do ar; sem mitigação de fallback definida nesta revisão (fora de escopo de segurança, é decisão de disponibilidade/produto).
- **Ausência de MFA no fluxo OTP-por-e-mail em si** — o próprio OTP por e-mail já é considerado "algo que você tem" (acesso à caixa de entrada); não há segundo fator adicional no Onda 1, consistente com a decisão de produto já aprovada.

## 14. Achados de auditoria de código (estado atual, greenfield)

Confirmado por auditoria dedicada do código atual (backend .NET/ASP.NET Core, frontend React/TS):

- **Nenhuma autenticação real implementada.** O único mecanismo de identidade existente é `DevelopmentRequestIdentity` (ADR-0011), que lê headers de cliente não assinados (`X-Development-User-Id`/`X-Development-Role`), sem qualquer validação — corretamente restrito a `Development` por uma checagem em runtime, mas **registrado incondicionalmente no container de DI** (`Program.cs`), dependendo inteiramente dessa checagem interna como única barreira. Recomendação: quando a autenticação real for implementada, o registro do adaptador de identidade no DI deve também ser condicionado ao ambiente (`if (env.IsDevelopment()) { ... DevelopmentRequestIdentity ... } else { ... SessionCurrentIdentity ... }`), eliminando a dependência de uma única checagem em runtime como barreira de defesa em profundidade.
- **CORS atual incompatível com cookies de sessão** e com fallback de origem não isolado por ambiente (seção 3.6) — ajuste necessário antes de O1.4.2 introduzir cookies.
- **Nenhum header de segurança HTTP configurado** (seção 3.4/8) — bloqueador antes de exposição fora de Development.
- **Nenhum RBAC/`[Authorize]` implementado** — esperado no estado atual (não há autenticação ainda); torna-se bloqueador apenas quando autenticação real existir.
- **Nenhum uso de localStorage/sessionStorage/cookies no frontend hoje** — estado limpo, facilita manter a garantia de "nenhum token sensível fora de cookie HttpOnly" desde o primeiro commit de autenticação.
- **Nenhum segredo real commitado** — `appsettings.json` usa placeholders, consistente com a política de `context/security.md` (Secret Manager em runtime).

Nenhum destes achados exige ação imediata nesta revisão (nenhum código de autenticação existe ainda para corrigir) — são registrados como **requisitos de implementação obrigatórios para O1.4.2**, não como bugs a corrigir agora.

## 15. Requisitos de testes (para O1.4.2)

- Testes de rejeição: domínio não autorizado, usuário inexistente, usuário inativo, usuário sem vínculo com a BU — todos devem retornar resposta indistinguível ao cliente (anti-enumeração).
- Teste de uso único de OTP: segunda tentativa de validação do mesmo código, mesmo dentro da validade, deve falhar.
- Teste de expiração de OTP.
- Teste de limite de tentativas de validação (5ª tentativa correta após 5 erradas deve falhar).
- Teste de invalidação em cascata (novo OTP solicitado invalida o anterior).
- Teste de rate limiting (solicitação e validação).
- Teste de sessão: expiração absoluta, expiração por inatividade, revogação por logout, revogação por inativação do usuário durante sessão ativa.
- Teste de troca de `UnidadeNegocioId` sem vínculo válido (deve ser rejeitada).
- Teste de Bootstrap: dupla tentativa concorrente cria apenas um Admin Sênior; tentativa pós-conclusão retorna resposta indistinguível de rota inexistente; Bootstrap sem segredo de implantação correto é rejeitado.
- Teste de CORS: origem não listada é rejeitada; cookie de sessão não é enviado/aceito em requisição cross-origin não autorizada.
- Teste de headers de segurança presentes em toda resposta (verificação automatizada, não apenas manual).
- Teste de IDOR: acesso a recurso de outra `UnidadeNegocioId` com sessão válida de BU diferente deve ser rejeitado.

## 16. Recomendação para O1.4.2

O1.4.2 (implementação) pode iniciar o **detalhamento técnico e a Work Order** com base nesta revisão, mas a **implementação de código só pode começar** após:
1. Product Owner resolver os bloqueadores 1–3 da seção 12 (catálogo de Perfis/Permissões incluindo Administrador Sênior; provedor de e-mail; escopo de Perfil/Permissão);
2. Product Owner/CTO ratificar (ou substituir) a recomendação de modelo de sessão da seção 1.2 na Work Order técnica.

Nenhuma decisão deste documento deve ser tratada como arquitetura definitiva além do que ADR-0020 já aprovou — onde este documento recomenda algo não decidido nos documentos de produto/dados (ex.: modelo físico de sessão, formato exato do OTP), a decisão final permanece com o Product Owner/CTO, não com esta revisão.

---

## 17. Estratégia de Autenticação em Development (O1.4.1.1 — complemento, 07/08/2026)

> Complemento formal da O1.4.1, produzido por uma revisão adversarial dedicada à estratégia de desenvolvimento do fluxo OTP **sem dependência imediata da Infra**. Resultado da revisão: **Development Auth Strategy — APROVADA COM AJUSTES**, com aprovação explícita do Product Owner. Esta seção formaliza as decisões antes do início da implementação de O1.4.2 — nenhum código, provider, configuração de SMTP/Microsoft Graph, App Registration ou credencial real foi criado por esta seção.

### 17.1 Decisão de fase

O +Compras permanece atualmente em fase de desenvolvimento/modelagem **Frontend First**. A integração corporativa definitiva de autenticação/e-mail **não será solicitada à Infra neste momento**. Microsoft Entra ID, Microsoft Graph e o provider corporativo definitivo de e-mail serão preparados antes da entrada em Homologação, quando estiverem consolidados: ambientes, hostnames, URLs, callbacks, permissões, mailbox, secrets e demais requisitos operacionais/documentais exigidos pela Infra.

Esta é uma decisão deliberada de engenharia e produto — não uma pendência esquecida. Ela **não substitui** o bloqueador nº 2 da seção 12 (provedor transacional de e-mail); apenas define que esse bloqueador passa a ser exigido antes de Homologação (seção 17.7), não antes do início da implementação de O1.4.2. Os bloqueadores nº 1, 3 e 4 da seção 12 (catálogo de Perfis/Permissões incluindo "Administrador Sênior"; escopo de `Perfil`/`Permissao`; semântica de "nenhum `UsuarioCentroCusto`") permanecem inalterados e são decisões de Product Owner **fora do escopo desta seção**.

### 17.2 Contrato de e-mail OTP

Fica formalizada a preferência por um contrato específico, `IOtpEmailSender`, em vez de um `IEmailSender` genérico. Responsabilidade exclusiva: envio do OTP de autenticação. O domínio/aplicação não deve conhecer SMTP, Microsoft Graph, Office 365, Entra ID ou qualquer provider específico — apenas o contrato `IOtpEmailSender`, consistente com o princípio de desacoplamento entre autenticação e Identity Provider já adotado na seção 1.

### 17.3 Development Provider

Development poderá utilizar uma implementação própria (`DevelopmentOtpEmailSender`) para permitir o desenvolvimento/teste do fluxo OTP sem integração externa. Regras obrigatórias:

- `DevelopmentOtpEmailSender` é estritamente exclusivo de `Development`.
- Não pode existir fallback automático para ele em nenhum outro ambiente.
- Staging/Homologação/Production sem um provider corporativo válido configurado devem **falhar de forma fechada** (fail-closed) — nunca degradar silenciosamente para o provider de Development.

### 17.4 Defesa em profundidade (requisitos obrigatórios de O1.4.2)

- Seleção do provider **exclusivamente por `IHostEnvironment`** — nunca por `appsettings`/feature flag.
- Validação antecipada da configuração do provider corporativo fora de Development, com `ValidateOnStart()` para toda configuração obrigatória.
- Fail-closed antes de aceitar tráfego: a aplicação não deve subir em ambiente não-Development sem um provider corporativo válido configurado.
- Proteção interna adicional no próprio Development provider (além da seleção por ambiente).
- Quando tecnicamente viável, o Development provider deve estar fisicamente ausente do artefato de Release.
- Exatamente uma implementação válida de `IOtpEmailSender` registrada por ambiente — nenhum fallback silencioso.
- **Nota de threat model:** múltiplas checagens de `IsDevelopment()` espalhadas pelo código não constituem defesa em profundidade independente, pois todas dependem da mesma fonte de verdade (`IHostEnvironment`). A defesa em profundidade real está na combinação de seleção por ambiente + validação antecipada + fail-closed + (quando viável) ausência física do artefato de Release — não na repetição da mesma checagem em múltiplos pontos.

### 17.5 Regra absoluta para OTP

Esta seção formaliza uma decisão **mais restritiva** que a sugestão original da seção 3.1/3.8: o OTP **nunca** pode aparecer em log, resposta de API, HTML, frontend, query string, arquivo, telemetria, traces ou eventos de auditoria — **inclusive em Development**. Não deve ser usado nenhum prefixo de conveniência (ex.: `[DEV-OTP]`) em log, mesmo em Development.

Se testes locais/E2E precisarem recuperar o OTP para automação, deverá existir posteriormente um mecanismo específico de diagnóstico/teste que seja: exclusivo de Development, isolado do fluxo normal de autenticação, inacessível externamente, e preferencialmente ausente do build de Release. A implementação concreta desse mecanismo é responsabilidade de O1.4.2 — não desta seção, que apenas formaliza o requisito.

### 17.6 Secrets

- **Development:** .NET User Secrets preferencialmente; variáveis de ambiente quando apropriado.
- **CI:** secrets próprios da plataforma de CI.
- **Homologação/Produção:** secret manager corporativo (consistente com `context/security.md`).

Nunca versionar: senha de e-mail, client secret, certificado privado, Bootstrap secret (seção 3.7), OTP, identificador de sessão, tokens, ou connection string contendo credenciais.

### 17.7 Authentication Infra Readiness Gate (obrigatório antes de Homologação)

Fica formalizado um gate obrigatório, distinto do Security Design Gate (seção 12) e a ser satisfeito **antes da entrada em Homologação**, não antes do início de O1.4.2. Deve validar, no mínimo:

- Provider corporativo de e-mail definido e aprovado pela Infra.
- Integração real de envio de OTP validada.
- Mailbox/remetente definido.
- Secrets migrados para o mecanismo corporativo (fora de Development/CI).
- Development provider comprovadamente indisponível fora de Development.
- Entra ID/App Registration definidos, quando aplicável.
- URLs/callbacks definitivos.
- Headers de segurança (seção 3.4), CORS (seção 3.6) e CSRF (seção 3.5) revisados para o ambiente real.
- Rate limiting (seção 3.3) validado no ambiente real.
- Secret scanning ativo no pipeline.
- Testes de segurança executados (seção 15).
- Runbook operacional documentado.
- Processo de rotação de credenciais definido.

**Homologação não poderá iniciar enquanto este Gate estiver bloqueado.**

### 17.8 Microsoft Entra ID / Microsoft Graph

Entra ID e Microsoft Graph **não estão descartados** — estão deliberadamente postergados, consistente com a coexistência de múltiplos Identity Providers já prevista em ADR-0020 (item 11) e na seção 1.1 deste documento. Continuam sendo candidatos preferenciais para login corporativo via Entra ID e para envio corporativo de OTP via Microsoft Graph. A decisão definitiva sobre ambos ocorrerá no preparo para Homologação, em conjunto com a Infra, como parte do Authentication Infra Readiness Gate (seção 17.7).

### 17.9 Relação com a seção 12 (bloqueadores) e com a seção 16 (recomendação para O1.4.2)

Esta seção altera o efeito prático do bloqueador nº 2 da seção 12 (provedor transacional de e-mail): ele deixa de impedir o **início** da implementação de O1.4.2 e passa a ser exigido apenas como parte do Authentication Infra Readiness Gate (17.7), antes de Homologação. Os bloqueadores nº 1, 3 e 4 da seção 12, e a exigência de ratificação do modelo de sessão (seção 1.2) pelo Product Owner/CTO na Work Order técnica, **permanecem inalterados** — não são resolvidos por esta seção.

---

## 18. Security Hardening pós-implementação (O1.4.2.1, 07/08/2026)

> Complemento produzido em resposta à Security Validation adversarial da O1.4.2 (Security Implementation Gate: **APROVADO COM PENDÊNCIAS**). Fecha os quatro achados ALTO e fortalece a cobertura de testes do achado MÉDIO/N. Não altera nenhuma decisão arquitetural das seções 1–17 — é hardening da implementação existente, não uma nova arquitetura.

### 18.1 Rate limiting por e-mail (Achado A)

Adicionado throttle server-side por e-mail normalizado (`OtpRequestThrottle`), independente e complementar ao rate limiting por IP já existente (seção 3.3): ~3 solicitações por e-mail em 15 minutos, cooldown mínimo de 60s entre solicitações — exatamente os valores desta seção 3.1. Aplicado **antes** e **da mesma forma** para e-mail existente/inexistente/inativo, para que o throttle nunca seja um oráculo de enumeração. Concorrência tratada por otimistic concurrency (RowVersion) com retry limitado; sob corrida persistente, falha para o lado seguro (nega).

### 18.2 Consumo único atômico do OTP (Achado B)

`CodigoVerificacaoOtp` recebeu um token de concorrência otimista (RowVersion). Duas validações concorrentes do mesmo código produzem exatamente um sucesso e uma perda de corrida (`DbUpdateConcurrencyException`, traduzida para `ConcurrencyConflictException` na fronteira Application/Infrastructure) — nunca duas sessões a partir do mesmo OTP. Adicionalmente, um índice único filtrado (`Status = Pendente`) por `UsuarioId` garante, no próprio banco, que no máximo um código pendente exista por usuário em qualquer momento — a invalidação do código anterior ao reenviar passa a ser best-effort (a garantia final vem do índice único na inserção do novo código, não da invalidação do antigo). Comprovado por testes de concorrência real (`Task.WhenAll` com múltiplas instâncias de `DbContext`, não fakes sequenciais) — ver seção 18.5 para a limitação documentada do provider de teste.

### 18.3 Secure-by-default (Achado C)

`AuthorizationOptions.FallbackPolicy` exige usuário autenticado por padrão em todo endpoint; anônimo passa a ser exceção explícita (`.AllowAnonymous()`). Dois esquemas de autenticação foram introduzidos — um por ambiente, selecionados exclusivamente por `IHostEnvironment` na composição raiz, nunca por configuração:

- **Fora de Development:** `SessionCookieAuthenticationHandler` — resolve a sessão via `IObterIdentidadeAtualUseCase` (a mesma revalidação de usuário Ativo já exigida por §2.5) e publica o resultado em `HttpContext.User`. `SessionCurrentIdentity` passou a ler apenas essas claims, eliminando a dívida técnica de `GetAwaiter().GetResult()` registrada na O1.4.2 (nenhuma I/O acontece mais em `ICurrentIdentity.GetRequired()` fora de Development).
- **Em Development:** `DevelopmentHeaderAuthenticationHandler` — decide apenas se a *fallback policy* deixa a requisição passar, a partir do mesmo header `X-Development-User-Id` (ADR-0011). `DevelopmentRequestIdentity` continua existindo, inalterada, como segunda barreira independente — nenhuma fonte de identidade conflitante foi introduzida.

Endpoints anônimos finais e justificativa:

| Endpoint | Anônimo? | Justificativa |
|---|---|---|
| `GET /health` | Sim | Usado por orquestração/monitoramento, sem sessão. |
| `POST /auth/otp/request` | Sim | É o próprio mecanismo de login — não pode exigir a sessão que ainda não existe. |
| `POST /auth/otp/verify` | Sim | Idem. |
| `POST /auth/logout` | Sim | Idempotente e seguro mesmo sem sessão; o handler já verifica a presença/validade do cookie internamente. |
| `GET /auth/me` | **Não** | Depende de sessão por definição — a ausência já produz 401 automaticamente via a policy global. |
| `GET /dev/otp` (só Development) | Sim | Mecanismo de diagnóstico isolado do fluxo normal (seção 17.5/18.4). |
| Demais endpoints de negócio (Suppliers/Negotiations) | Não | Herdam a policy global sem alteração de código — `ICurrentIdentity.GetRequired()` continua como segunda barreira dentro dos casos de uso. |

Efeito colateral positivo: o Achado F (GET /auth/me alterando `LastActivityAt`) fica resolvido por consequência arquitetural — agora toda requisição autenticada estende a janela de inatividade, não apenas `/auth/me` isoladamente, que é o comportamento correto de uma janela deslizante (§2.5), aplicado uniformemente.

### 18.4 Hardening do /dev/otp (Achado D)

O handler agora verifica `IHostEnvironment.IsDevelopment()` internamente, como segunda barreira independente do `if` em `Program.cs` que condiciona o mapeamento da rota. Nenhum `UseForwardedHeaders()` foi adicionado — `X-Forwarded-For`/`Forwarded`/`Host` continuam não sendo lidos nem honrados; `RemoteIpAddress` é sempre o peer TCP real. Documentado explicitamente no código: este mecanismo não é suportado através de proxy reverso, túnel, ngrok, acesso remoto ou rede compartilhada, mesmo em Development — na dúvida sobre a origem, nega (fail-closed).

### 18.5 Fail-closed real (Achado MÉDIO/N)

Testes que iniciam um `IHost` real (`Host.CreateDefaultBuilder().Build().StartAsync()`) em Staging/Production sem `Identity:Otp:Corporate:Provider` configurado, comprovando que o startup falha (`OptionsValidationException`) — não apenas que o tipo certo é resolvido via `BuildServiceProvider()`, como nos testes anteriores. Testes de concorrência real (RowVersion via EF Core InMemory com múltiplas instâncias de `DbContext`, `Task.WhenAll`) substituem a lacuna anterior de "só fakes sequenciais".

**Limitação documentada:** o provider InMemory do EF Core não avalia a cláusula de filtro de índices únicos (`HasFilter` é API relacional) — o índice único de "um código Pendente por usuário" comporta-se, sob InMemory, como incondicional sobre `UsuarioId`. Os testes de concorrência ainda são válidos (provam "no máximo um código pendente sobrevive à corrida"), mas a filtragem exata por `Status = Pendente` só foi verificada por leitura de configuração/migration, não empiricamente contra SQL Server real — pendente de validação quando o ambiente de banco compartilhado estiver consistente (ver PROJECT_STATE.md).

### 18.6 Auditoria (Achado G)

`SolicitarOtpUseCase`/`ValidarOtpUseCase`/`LogoutUseCase` passaram a incluir um identificador de correlação não reversível (`EmailAuditHasher` — SHA-256 determinístico truncado, sem salt, propositalmente, para permitir correlacionar eventos do mesmo e-mail entre linhas de log) ou o `UsuarioId` (Guid interno, não um segredo) nos eventos já registrados. Nunca o e-mail em claro, nunca OTP/sessão/token/segredo.

### 18.7 Achado H — postergado

CSP hoje protege apenas as respostas JSON da API — a SPA é servida separadamente (Vite/dist) e não recebe este header. Corrigir isso depende de uma decisão arquitetural de hospedagem/serving ainda não tomada; registrado como requisito do Authentication Infra Readiness Gate (seção 17.7), não corrigido nesta iteração.

---

## 19. Hardening pontual do DevelopmentHeaderAuthenticationHandler (O1.4.2.2, 07/08/2026)

> Resposta ao Achado E encontrado pela segunda Security Validation independente (adversarial) sobre a O1.4.2.1: `DevelopmentHeaderAuthenticationHandler` (introduzido na Etapa 3 da O1.4.2.1) autenticava qualquer requisição que alcançasse o processo em Development com um `X-Development-User-Id` sintaticamente válido, **sem checagem de origem** — diferente de `GET /dev/otp`, que já exigia loopback desde a mesma iteração. Como esse handler alimenta a `AuthorizationOptions.FallbackPolicy` (secure-by-default, O1.4.2.1 §18.3), essa lacuna concedia identidade completa sobre **qualquer** endpoint protegido, não apenas a exposição pontual de um código OTP.

O handler passou a exigir, além de `IHostEnvironment.IsDevelopment()` (mantido como segunda barreira independente do registro condicional em `Program.cs`), que `HttpContext.Connection.RemoteIpAddress` seja estritamente loopback (IPv4 `127.0.0.1` ou IPv6 `::1`) — a mesma defesa já usada em `/dev/otp`. Nenhum forwarded header (`X-Forwarded-For`, `Forwarded`, `Host`, `Origin`, `Referer`) é lido ou honrado como prova de origem; `UseForwardedHeaders()` não está registrado em `Program.cs`, então `RemoteIpAddress` continua sendo sempre o peer TCP real. Documentado explicitamente no código: este mecanismo não é suportado através de proxy reverso, túnel (ngrok ou equivalente), rede compartilhada ou acesso remoto, mesmo em Development — uma necessidade futura de Development remoto exige uma decisão de segurança própria, não uma flexibilização desta checagem.

Comportamento de falha: origem não-loopback nunca autentica (nem parcialmente) — resulta em `AuthenticateResult.Fail`, que a `FallbackPolicy` resolve como 401 no endpoint protegido, sem fallback permissivo.

Testado por: 8 testes diretos do handler (loopback IPv4/IPv6 autentica; não-loopback nunca autentica mesmo com header válido; header ausente/malformado nunca autentica; Staging/Production nunca autenticam mesmo com loopback+header válido; `X-Forwarded-For` forjando loopback com IP externo real nunca autentica; IP externo forjando `X-Forwarded-For` não afeta o resultado quando o peer real é loopback — documentando que o header nunca é lido) + 3 testes de pipeline HTTP real (Kestrel real via `WebApplication`, sem `AddInfrastructure`) confirmando 401 sem header, 401 com header malformado, e sucesso apenas para requisição loopback real com header válido.

**Pendências não fechadas nesta iteração (mantidas explicitamente em aberto):** `EmailAuditHasher` sem salt/HMAC (Achado F da Security Validation II) — avaliação futura de HMAC com chave de aplicação, não bloqueante para Development. Validação de `RowVersion`+índice único filtrado em provider relacional real (SQL Server ou equivalente isolado) — obrigatória antes de Homologação, não tentada novamente nesta iteração por instrução explícita de não tocar no banco compartilhado.

---

## 20. Bootstrap Mode e Administrador Sênior — Security Design Review (O1.4.3, 07/08/2026)

> Revisão de segurança exigida por ADR-0020 (item 13) e `domain-principles.md` §Segurança, antes de qualquer implementação de código do Bootstrap Mode/Administrador Sênior. **Documento exclusivamente de arquitetura, threat modeling e definição de controles — nenhum código, endpoint, migration, EF Core, ou alteração de frontend/backend/banco compartilhado foi produzido por esta revisão.** Pré-condição satisfeita: O1.4.2 (Login Passwordless OTP e Sessão Segura) está formalmente concluída — Security Implementation Gate III: **APROVADO COM PENDÊNCIAS NÃO BLOQUEANTES PARA DEVELOPMENT** (ver `.ai/CURRENT_SPRINT.md` e `.ai/PROJECT_STATE.md`).

**Data:** 07/08/2026
**Executor:** Claude (Agente Arquiteto/Engenheiro de Segurança Sênior, função ad hoc para esta revisão)
**Status:** Gate de Segurança — ver seção 20.20.

### 20.1 Objetivo e não-objetivo

O Bootstrap Mode existe exclusivamente para resolver: *"como criar com segurança o primeiro Administrador Sênior quando ainda não existe nenhum administrador capaz de administrar o sistema?"* Não é usuário padrão, senha padrão, backdoor, conta técnica permanente, bypass de autenticação ou modo administrativo reutilizável. Após conclusão válida, é encerrado permanentemente — decisão já fixada em ADR-0020 (item 12) e `domain-principles.md` §Segurança, reafirmada, não reaberta, por esta revisão.

### 20.2 Estados do Bootstrap

**Decisão: dois estados persistidos, não quatro.** `BootstrapEstado` (já modelado em `ComprasDataModel.md`) permanece com exatamente o desenho já aprovado: `Concluido` (booleano), `ConcluidoEm`, `UsuarioAdministradorSeniorId` — entidade global, linha única, sem particionamento por `UnidadeNegocioId`.

- **Disponível** (`Concluido = false`): condição padrão antes da primeira conclusão bem-sucedida.
- **Concluído** (`Concluido = true`): estado terminal, permanente, único, nunca revertido pelo sistema.

Rejeita-se a modelagem de 4 estados sugerida como exemplo conceitual na tarefa (`NaoInicializado`/`BootstrapDisponivel`/`BootstrapEmExecucao`/`BootstrapConcluido`). Razão: `BootstrapEmExecucao` seria um estado transitório de um fluxo multi-etapa (secret → identidade → OTP → criação) vivido por um candidato específico — persistir isso como estado *global* criaria uma segunda fonte de verdade paralela ao `Concluido`, com sua própria superfície de corrida (dois candidatos "em execução" simultaneamente, o que é exatamente o cenário que a seção 20.9 precisa resolver de qualquer forma pela transação atômica, não por um estado intermediário). O estado "em execução" de um candidato individual é modelado como estado de sessão/fluxo (análogo ao `CodigoVerificacaoOtp`/`SessaoAutenticacao` já existentes — ver seção 20.5), nunca como uma terceira variante de `BootstrapEstado`. `NaoInicializado` também é rejeitado como estado distinto de `Disponível`: não há diferença de comportamento observável entre "banco nunca populado" e "banco populado mas sem Admin Sênior" — ambos permitem Bootstrap da mesma forma.

- **Condição exata que permite Bootstrap:** `BootstrapEstado.Concluido == false` (linha inexistente é equivalente a `false` — tratar ausência de linha como estado inicial implícito, sem exigir seed explícito).
- **Condição exata que bloqueia Bootstrap:** `BootstrapEstado.Concluido == true`, incondicionalmente — nenhuma outra condição (estado de usuários, perfis, BUs) sobrepõe esta.
- **Persistência do estado:** tabela própria, linha única, sem soft-delete, sem histórico de reversão — a mutação `false → true` é o único caminho de escrita depois da criação inicial (seed ou primeira leitura implícita).
- **Comportamento concorrente:** ver seção 20.9 — resolvido por transação + constraint, não por um estado "em execução" bloqueante.
- **Comportamento após falha parcial:** ver seção 20.8 — resolvido por atomicidade total (tudo ou nada), não por um estado recuperável intermediário.

### 20.3 Condição de abertura

A condição de abertura é **exclusivamente** `Concluido == false` — deliberadamente desacoplada de qualquer heurística sobre o estado de administradores, unidades de negócio ou dados. Isso elimina toda a classe de ambiguidade listada na tarefa (administrador inativo, excluído logicamente, perfil removido, dados inconsistentes, mais de uma Unidade de Negócio, banco parcialmente inicializado): nenhuma dessas condições é lida para decidir se o Bootstrap está disponível, porque a única pergunta que importa antes da primeira conclusão é "alguém já concluiu o Bootstrap alguma vez?" — não "o sistema está atualmente sem administrador funcional?".

Consequência direta e desejada: se, hipoteticamente, todo Administrador Sênior for inativado ou removido **depois** de `Concluido = true`, o Bootstrap **não** reabre — reafirmação de ADR-0020 (item 12) e da pergunta nº 1 desta revisão (resposta: NÃO). Esse cenário é tratado por Recovery (seção 20.11), nunca por reavaliar a condição de abertura.

Tratamento explícito dos cenários levantados, todos irrelevantes para a condição de abertura em si, mas relevantes para o *comportamento dentro* de um Bootstrap ainda disponível:
- **Mais de uma Unidade de Negócio já existente:** Bootstrap não força criação de uma nova BU se pelo menos uma já existir sem Administrador Sênior — o candidato seleciona a BU à qual o primeiro Administrador Sênior será vinculado (ou cria uma nova, se nenhuma existir), evitando duplicar BUs de teste/seed pré-existentes. Ver seção 20.7.
- **Banco parcialmente inicializado:** se `Concluido` nunca foi escrito (ausência de linha), trata-se como `false` — Bootstrap disponível. Nenhuma migration cria a linha com um valor diferente de `false`/ausente.

### 20.4 Bootstrap Secret

**Formato:** valor aleatório de alta entropia (≥256 bits / 32 bytes), gerado por CSPRNG, codificado em Base64URL ou hex — nunca uma frase memorável ou senha escolhida por humano (não é "senha do Bootstrap", é um segredo de implantação).

**Entropia:** ≥256 bits — deliberadamente mais que os ≥128 bits do identificador de sessão (seção 1.2), porque este segredo protege o endpoint pré-autenticação de maior privilégio do sistema e pode ter vida útil mais longa que uma sessão individual.

**Armazenamento:** nunca em texto claro no banco (o sistema não persiste o valor — ele vive exclusivamente em configuração de ambiente/secret manager, comparado em tempo de requisição). Se, por alguma necessidade futura, precisar ser persistido (ex.: para suportar rotação auditável), aplicar o mesmo padrão já usado para OTP: hash com salt, nunca reversível.

**Validade:** sem expiração de calendário fixa — sua "validade" é inteiramente amarrada ao estado do Bootstrap: um segredo só tem efeito enquanto `Concluido == false`. Uma vez concluído, o mesmo valor de secret configurado deixa de autenticar qualquer coisa (a checagem de `Concluido` acontece antes da comparação do secret, nunca depois) — não é necessário um mecanismo de expiração temporal independente, o encerramento do Bootstrap já é a expiração.

**Uso único ou rotativo:** não é single-use por design de token (o mesmo secret pode ser reapresentado em múltiplas tentativas de um fluxo multi-etapa legítimo, como reenvio de OTP), mas é efetivamente de uso único *no agregado*, porque só existe uma conclusão possível — depois dela, o secret nunca mais tem efeito. Rotação é responsabilidade operacional (Infra pode trocar o valor no secret manager a qualquer momento antes da conclusão, sem exigir suporte de código para "revogar" o valor antigo).

**Comparação segura:** comparação de tempo constante (`CryptographicOperations.FixedTimeEquals` ou equivalente), nunca `==`/`Equals` de string — mesmo princípio já aplicado ao hash de OTP (seção 2.3).

**Expiração:** não aplicável como campo próprio (ver "Validade" acima) — a expiração é o próprio `Concluido = true`.

**Rate limiting:** obrigatório e mais estrito que qualquer outro endpoint do sistema (ver seção 20.16) — o Bootstrap Secret, sendo um valor único e de longa vida útil (ao contrário do OTP, que roda a cada 10 minutos), é o alvo mais valioso de brute force de todo o sistema de autenticação.

**Auditoria:** toda tentativa com secret incorreto é auditada (e-mail candidato se informado, IP, timestamp) — nunca o valor do secret, correto ou incorreto, em log/auditoria/erro.

**Nunca armazenar em:** Git, `appsettings.json` versionado, frontend, HTML, URL (nunca em query string — sempre em header ou corpo de requisição POST), logs. Development pode usar .NET User Secrets (consistente com seção 17.6). Homologação/Produção usam o secret manager corporativo já referenciado em `context/security.md`.

### 20.5 Identidade inicial autorizada

**Decisão: Opção A — lista explícita de e-mails autorizados em configuração segura (secret manager/User Secrets), não domínio+lista, não token administrativo separado.**

Justificativa da rejeição das alternativas:
- **"Qualquer e-mail corporativo" (não listada, mas explicitamente proibida pela tarefa):** rejeitada — qualquer pessoa com caixa de e-mail corporativa poderia se tornar Administrador Sênior apenas conhecendo o secret, contrariando o princípio de identidade *pré-autorizada* de ADR-0020 (item 12) e da seção 3.7 deste documento.
- **Opção B (domínio + lista):** rejeitada como mecanismo primário — um domínio autorizado já é, por natureza, uma condição ampla (qualquer caixa válida daquele domínio); combinar com lista tornaria a lista redundante ou o domínio inócuo. Um domínio sozinho nunca deve autorizar Bootstrap.
- **Opção C (token administrativo separado):** rejeitada por criar um segundo segredo de mesma categoria que o Bootstrap Secret sem benefício de segurança adicional claro — duas variáveis de configuração para o mesmo problema aumentam a superfície de erro operacional (qual delas expirou, qual foi rotacionada) sem eliminar nenhuma classe de ataque que a combinação Secret + Identidade + OTP já não cubra.

**Mecanismo:** lista explícita e fechada de e-mails (ex.: `Bootstrap:AllowedCandidateEmails`, array de strings), armazenada no secret manager corporativo (Homologação/Produção) ou User Secrets (Development) — nunca em `appsettings.json` versionado. Comparação por e-mail normalizado (mesma normalização já usada em `OtpRequestThrottle`, seção 18.1), case-insensitive, sem wildcard, sem sufixo de domínio interpretado como padrão.

**Reforço explícito (já uma premissa aprovada, reafirmada):** Secret sozinho **não** autentica (resposta à pergunta nº 4: NÃO); OTP sozinho **não** autentica (pergunta nº 5: NÃO); apenas a combinação **Secret + identidade pré-autorizada + OTP válido** completa a autenticação de Bootstrap — nenhuma etapa isolada é suficiente, cada uma mitiga uma classe de ataque diferente (secret mitiga descoberta do endpoint; identidade pré-autorizada mitiga e-mail arbitrário mesmo com secret vazado; OTP mitiga posse falsa da identidade mesmo com as outras duas conhecidas).

### 20.6 Autenticação

**Fluxo (reaproveitando a base da O1.4.2, sem mecanismo paralelo):**

1. Cliente envia e-mail candidato + Bootstrap Secret (header, nunca query string) a um endpoint de "iniciar Bootstrap".
2. Backend valida, nesta ordem: `Concluido == false` → secret válido (tempo constante) → e-mail pertence à lista pré-autorizada. Resposta idêntica para "e-mail não autorizado" e "secret incorreto" do ponto de vista de tempo de resposta e mensagem — nunca revelar qual das duas condições falhou (mitiga enumeração da lista de e-mails autorizados). Se `Concluido == true`, resposta indistinguível de rota inexistente (404) — nunca chega a avaliar secret/e-mail (seção 20.10).
3. Se as três condições passarem, reutiliza `ISolicitarOtpUseCase` (ou uma variante que aceita explicitamente um "contexto Bootstrap") para emitir OTP ao e-mail candidato — mesmo hash/salt/validade/uso único/rate limiting já implementados em O1.4.2/O1.4.2.1.
4. Cliente valida o OTP via um endpoint próprio de Bootstrap (não `/auth/otp/verify` — ver seção 20.7 sobre por que a sessão resultante é distinta). Validação reaproveita `IValidarOtpUseCase`/`CodigoVerificacaoOtp` sem duplicar a lógica de hash/tentativas/expiração.
5. Sucesso → sessão Bootstrap limitada (seção 20.7), não uma `SessaoAutenticacao` normal.

**Não criar mecanismo de autenticação paralelo:** OTP, hashing, throttle e RowVersion são os mesmos componentes de `Application/Identity` já existentes — Bootstrap é um *consumidor* adicional desses componentes, não uma reimplementação.

### 20.7 Sessão Bootstrap

**Decisão: sessão Bootstrap deve ser distinta da sessão normal (pergunta nº 6: privilégios limitados — SIM).**

Reaproveitar `SessaoAutenticacao`/`AuthCookie` diretamente é rejeitado: uma sessão Bootstrap bem-sucedida não deve, nem por um instante, ser indistinguível de uma sessão de usuário autenticado comum — se fosse a mesma entidade/cookie, qualquer código que resolvesse identidade a partir do cookie precisaria de uma checagem adicional espalhada por toda a aplicação para negar acesso a rotas de negócio durante um Bootstrap em andamento, recriando exatamente a classe de bug que a seção 2.8 (cache de permissões) já identifica como perigosa.

**Modelo recomendado:** um esquema de autenticação (`AuthenticationHandler`) e um cookie próprios (`BootstrapSessionCookie`, nome diferente de `AuthCookie.Name`), com claims mínimas (identificador da tentativa de Bootstrap + e-mail candidato já validado por OTP — nunca `UsuarioId`, porque o usuário ainda não existe). Vida útil curta (ex.: 10–15 minutos, alinhada à validade do próprio OTP), uso único (uma sessão Bootstrap só permite completar exatamente um fluxo de conclusão — nunca reemitida após sucesso ou falha definitiva).

**Privilégios durante a sessão Bootstrap — allowlist explícita, não fallback policy padrão:** os endpoints de Bootstrap **não** herdam `AuthorizationOptions.FallbackPolicy` (que exige sessão normal) nem ficam `AllowAnonymous()` de forma ampla — usam sua própria política de autorização, restrita a um `AuthenticationScheme` "BootstrapSession" e nada além dele. Operações permitidas nesta sessão: consultar estado do Bootstrap; configurar a primeira Unidade de Negócio (se necessária); criar/vincular o primeiro Administrador Sênior; concluir o Bootstrap. Nenhuma outra rota de negócio aceita este esquema de autenticação — mesmo que a sessão Bootstrap esteja tecnicamente válida, ela é opaca para todo endpoint fora do grupo `/bootstrap/*`.

### 20.8 Administrador Sênior

**Conceito:** perfil especial de plataforma, com o maior privilégio administrativo — já modelado no código atual como `Perfil.AdministradorSenior` (constante de nome, `backend/src/BlueprintOS.Domain/Identity/Perfil.cs`). Não é uma permissão individual: um usuário recebe o Perfil "Administrador Sênior" através do mesmo modelo RBAC já vigente (`UsuarioPerfil`), nunca por um campo booleano direto em `Usuario`. É auditável pela mesma trilha já usada para qualquer vínculo `UsuarioPerfil` (seção 3.8), sem necessidade de evento de auditoria de categoria própria além do já previsto "Bootstrap concluído" (seção 20.15).

**Pode existir mais de um? SIM (pergunta nº 2).** Nada em ADR-0020 restringe a exatamente um; o próprio modelo RBAC (N:N `Usuario`×`Perfil`) já suporta múltiplos usuários com o mesmo Perfil. Permitir múltiplos Administradores Sênior é, além disso, a mitigação estrutural mais simples para o cenário de "administrador único indisponível" sem reabrir o Bootstrap — reduz a probabilidade de o sistema chegar ao estado de Recovery (seção 20.11) sem enfraquecer nenhum controle (auditabilidade e RBAC permanecem idênticos com N administradores).

**Quem pode conceder o perfil após o Bootstrap?** Um usuário já portador do Perfil "Administrador Sênior" (via a futura tela `Gestão de Perfis`/`Gestão de Usuários`, sujeita à permissão `PERFIL.GERENCIAR`/equivalente de gestão de usuários — catálogo global já ratificado nesta sprint, `.ai/CURRENT_SPRINT.md`). Nenhum mecanismo de auto-concessão fora do Bootstrap.

**Quem pode remover? O último Administrador Sênior pode ser inativado/removido? NÃO (pergunta nº 3).** Confirma-se a hipótese do Product Owner: o sistema deve impedir ficar sem nenhum Administrador Sênior ativo após o Bootstrap. Mecanismo recomendado: antes de persistir qualquer inativação de `Usuario` ou remoção de vínculo `UsuarioPerfil` que resultaria em zero usuários Ativos com o Perfil "Administrador Sênior" ativo, a operação é rejeitada (validação de domínio, não apenas de UI) — análogo em espírito à invariante já usada para consumo único de OTP (seção 2.3), mas aplicada como regra de negócio de RBAC, não de autenticação. Esta é uma extensão pontual do modelo RBAC para este Perfil específico — todos os demais Perfis continuam sem essa restrição.

### 20.9 Atomicidade e concorrência

**Conclusão do Bootstrap deve ser transacional? SIM (pergunta nº 7). Pode ser executado duas vezes? NÃO (pergunta nº 8).**

**Modelo recomendado — uma única transação de banco, sem saga multi-etapa com persistência intermediária:** a etapa de conclusão (após sessão Bootstrap válida) executa, na mesma transação: (a) criar ou selecionar a `UnidadeNegocio`; (b) criar o `Usuario`; (c) garantir a existência do `Perfil` "Administrador Sênior" (criar se não existir, reaproveitar se já existir — idempotente dentro da própria transação); (d) criar o vínculo `UsuarioPerfil`; (e) marcar `BootstrapEstado.Concluido = true` com um **UPDATE condicional** (`UPDATE BootstrapEstado SET Concluido = 1, ConcluidoEm = @agora, UsuarioAdministradorSeniorId = @usuarioId WHERE Concluido = 0`, verificando `rowsAffected == 1`). Se qualquer etapa falhar — incluindo o UPDATE condicional afetando zero linhas, o que indica que outra conclusão já venceu a corrida — a transação inteira é revertida: nenhum `Usuario`, `UnidadeNegocio` ou `UsuarioPerfil` órfão permanece.

Esse desenho elimina, por construção, a classe inteira de "falha parcial deixa estado inseguro" (seção 20.10 na tarefa): não existe estado intermediário persistido entre "nada aconteceu" e "tudo aconteceu com sucesso" — é tudo ou nada, na mesma transação, sem depender de reentrância para corrigir um meio-caminho.

**Concorrência (threat model obrigatório — dois atores tentando Bootstrap simultaneamente):** o `UPDATE ... WHERE Concluido = 0` funciona como uma operação compare-and-swap ao nível do banco — sob isolamento `READ COMMITTED` (padrão do SQL Server) ou superior, apenas uma transação concorrente consegue afetar a linha antes que a outra veja `Concluido = 1`; a segunda tentativa afeta zero linhas e reverte. Não depender exclusivamente de um `if (!existeAdmin) { criar(); }` de aplicação (a checagem "se não existe admin, então cria" mencionada na tarefa) — essa forma tem uma janela de corrida entre o `SELECT` e o `INSERT`/`UPDATE` que o `UPDATE` condicional elimina. RowVersion (padrão já usado em `CodigoVerificacaoOtp`/`OtpRequestThrottle`) é uma alternativa equivalente e igualmente aceitável — o requisito é apenas que a transição `false → true` seja atômica e verificável por linhas afetadas/conflito de concorrência, não que seja especificamente RowVersion.

**Idempotência:** um `Usuario` já criado por uma tentativa concorrente que perdeu a corrida (se a criação do usuário ocorresse fora da transação — o que este desenho evita) nunca deve persistir; com tudo na mesma transação, essa pergunta se torna moot — a tentativa perdedora nunca commita nada.

### 20.10 Encerramento permanente

Já coberto na arquitetura (seções 20.2, 20.3, 20.9): `BootstrapEstado.Concluido = true` é a fonte de verdade única. Reforços operacionais explícitos:

- **Endpoint de início nega:** a checagem `Concluido == true` acontece antes de qualquer outra validação (secret, e-mail, OTP) — resposta 404, indistinguível de rota inexistente, nunca 403 (que confirmaria a existência do endpoint).
- **Segredo não pode reabrir:** o mesmo Bootstrap Secret configurado continua existindo no secret manager após a conclusão (não há necessidade operacional de removê-lo às pressas), mas é inócuo — a checagem de `Concluido` bloqueia antes de qualquer comparação de secret ser executada.
- **Identidade inicial não pode reabrir:** mesmo raciocínio — a lista de e-mails pré-autorizados permanece configurada, mas nunca é consultada após `Concluido = true`.
- **Restart não pode reabrir:** `Concluido` é persistido em banco, não em memória/cache de processo — sobrevive a qualquer reinício da aplicação.
- **Mudança de configuração não pode reabrir:** nenhuma variável de ambiente/feature flag participa da condição de abertura — apenas o valor persistido de `Concluido`. Não introduzir nenhum "modo de emergência" configurável que ignore essa flag.
- **Inativação do administrador não pode reabrir:** conforme seção 20.3 — a condição de abertura nunca reconsulta o estado de administradores.

### 20.11 Recovery

**Recovery pós-Bootstrap deve ser separado do Bootstrap? SIM (pergunta nº 10).**

Cenário: todos os Administradores Sênior ficaram indisponíveis (inativados por erro operacional antes da regra da seção 20.8 existir; contas comprometidas e desativadas por segurança; ou saída de todos os responsáveis sem transição). Não utilizar reabertura automática do Bootstrap sob nenhuma circunstância — isso reintroduziria exatamente a janela de escalonamento de privilégio que o Bootstrap Mode foi criado para eliminar (ADR-0020, alternativa descartada nº 6).

**Procedimento recomendado (conceitual, fora do escopo de implementação da O1.4.3):** um script/comando administrativo *offline* (executado por acesso direto de Infra ao ambiente/banco, nunca por um endpoint HTTP exposto), auditado fora da aplicação (log de acesso de Infra, não `RegistroAuditoria` da aplicação, já que a aplicação pode estar exatamente no estado que impede login), que reativa ou vincula o Perfil "Administrador Sênior" a um `Usuario` específico já existente e Ativo — nunca cria um usuário novo por esse caminho, para não recriar uma via alternativa de "primeiro acesso" fora do Bootstrap. Esse procedimento é operacional/break-glass, exige aprovação explícita fora da aplicação (ex.: dois responsáveis de Infra, "four eyes"), e deve ser registrado no runbook operacional já referenciado como requisito do Authentication Infra Readiness Gate (seção 17.7) — este documento apenas fixa que ele deve existir e nunca pode ser um endpoint HTTP acionável remotamente. Implementação concreta do script fica fora do escopo da O1.4.3.

### 20.12 Threat model

| Ameaça | Mitigação |
|---|---|
| Descoberta do endpoint Bootstrap | Pós-conclusão: 404 indistinguível de rota inexistente (seção 20.10). Pré-conclusão: endpoint existe mas exige secret + identidade + OTP; nenhuma enumeração de sua existência vaza informação sensível por si só. |
| Brute force do secret | Rate limiting específico e mais restritivo que login normal (seção 20.16); ≥256 bits de entropia torna brute force online inviável dentro de qualquer limite de rate limiting razoável. |
| Brute force OTP | Reaproveita os mesmos controles de `CodigoVerificacaoOtp` (5 tentativas, hash, uso único) já validados em O1.4.2/O1.4.2.1. |
| Replay | OTP de uso único (herdado); sessão Bootstrap de uso único (seção 20.7) — nunca reemitida após conclusão ou falha definitiva. |
| Uso do secret vazado | Insuficiente isoladamente — exige também e-mail pré-autorizado e posse da caixa de entrada (OTP) — três fatores independentes, nenhum vazamento isolado é suficiente (seção 20.5). |
| E-mail não autorizado | Rejeitado antes do OTP ser emitido; resposta indistinguível de "secret incorreto" (seção 20.6) — mitiga enumeração da lista pré-autorizada. |
| User enumeration | Mesma resposta/tempo para secret incorreto e e-mail não autorizado (seção 20.6); nenhuma mensagem diferencia as duas causas. |
| Session fixation | Sessão Bootstrap gerada exclusivamente pelo servidor após OTP validado — mesmo princípio já aplicado à sessão normal (seção 2.4). |
| Privilege escalation | Sessão Bootstrap não concede nenhum privilégio de negócio — allowlist explícita de endpoints (seção 20.7); nunca herda `FallbackPolicy` de sessão normal. |
| Race condition / dupla conclusão | `UPDATE` condicional atômico dentro de transação única (seção 20.9) — apenas uma conclusão possível por construção, não por convenção. |
| CSRF | Mesma defesa em profundidade já usada em `/auth/otp/*` — `CsrfHeaderFilter` (header customizado) aplicado a todos os endpoints de mutação de Bootstrap. |
| XSS | Nenhuma superfície nova — frontend de Bootstrap segue a mesma política de não persistir nada sensível em `localStorage`/`sessionStorage`/DOM já vigente (seção 3.2); CSP já aplicada a respostas da API cobre também estas rotas. |
| IDOR | Não aplicável diretamente — Bootstrap não opera sobre IDs de recursos de terceiros; a única entidade mutável é a própria `BootstrapEstado` (linha única) e as entidades recém-criadas pela própria sessão. |
| Bypass via `DevelopmentHeaderAuthenticationHandler` | Bootstrap não usa `ICurrentIdentity`/claims de Development — usa exclusivamente o esquema `BootstrapSession` (seção 20.7), que não é satisfeito por nenhum header de Development. Nenhuma interseção entre os dois mecanismos. |
| Acesso remoto a ferramentas de Development | Mesma postura já aplicada a `/dev/otp` (loopback estrito, seção 18.4/19) deve se aplicar a qualquer ferramenta de diagnóstico de Bootstrap em Development, se uma vier a existir (seção 20.13). |
| Reabertura após conclusão | Estruturalmente impossível — seção 20.10. |
| Manipulação direta de estado (edição manual de `BootstrapEstado` no banco) | Fora do modelo de ameaça de uma aplicação web (equivalente a acesso de superusuário ao banco) — mitigado por controle de acesso ao banco de produção (fora do escopo desta revisão), não por lógica de aplicação. |
| Rollback de banco / restore de backup antigo | Um restore para um ponto anterior a `Concluido = true` reabre o Bootstrap tecnicamente (o dado é literalmente antigo) — risco residual aceito, mitigado operacionalmente (runbook de restore deve alertar explicitamente sobre esta implicação; fora do escopo de código). |
| Insider threat | Mitigado pela combinação de auditoria (seção 20.15, quem tem acesso ao secret manager já é rastreado por controles de Infra) e pela exigência de posse de e-mail via OTP mesmo para quem conhece o secret — nenhum insider único, sozinho, completa o Bootstrap sem também controlar a caixa de e-mail pré-autorizada. |

### 20.13 Development

**Development pode ter shortcut/bypass específico? NÃO (pergunta nº 9).** Nenhum secret hardcoded, admin fixo, shortcut de frontend, query param, botão oculto ou bypass via header — mesma régua já aplicada a `DevelopmentHeaderAuthenticationHandler` (nunca autentica fora de loopback, mesmo em Development).

Development testa o fluxo real: Bootstrap Secret via User Secrets (`dotnet user-secrets set "Bootstrap:Secret" "..."`), lista de e-mails pré-autorizados também via User Secrets, e OTP obtido pelo mesmo mecanismo de diagnóstico já existente (`GET /dev/otp`, restrito a loopback) — sem criar um segundo mecanismo de diagnóstico paralelo para Bootstrap; se o fluxo de OTP do Bootstrap reaproveita `CodigoVerificacaoOtp`, `/dev/otp` já é suficiente para recuperá-lo em testes locais, sem extensão de código. Nenhuma arquitetura paralela de Bootstrap nasce em Development — os mesmos handlers/endpoints rodam em todos os ambientes, apenas com valores de configuração diferentes (mesmo princípio de defesa em profundidade por `IHostEnvironment` já formalizado na seção 17.4).

### 20.14 Frontend (UX conceitual, sem implementação)

Estado normal: `/login`. Estado Bootstrap disponível: fluxo dedicado — rota candidata `/bootstrap` (não definitiva; qualquer alternativa que preserve a separação de fluxos é aceitável, decisão final cabe à Work Order técnica). A tela raiz deve consultar o estado do Bootstrap (endpoint público de leitura, sem expor detalhes sensíveis — apenas "disponível"/"não disponível", nunca contagem de tentativas ou e-mails autorizados) para decidir entre exibir `/login` ou o fluxo de Bootstrap.

UX deve deixar claro, em cada etapa: que se trata de configuração inicial do ambiente (não um login comum); qual identidade está executando a etapa atual (o e-mail candidato já validado, não um usuário genérico); quantos passos restam; e, na etapa final, um aviso explícito e inequívoco de que a conclusão é irreversível e encerra permanentemente este fluxo. Nenhum layout é implementado por esta revisão.

### 20.15 Modelo de dados conceitual

Reaproveita majoritariamente entidades já existentes (`CodigoVerificacaoOtp`, `Usuario`, `UnidadeNegocio`, `Perfil`, `UsuarioPerfil`, `BootstrapEstado` já modelada em `ComprasDataModel.md`). Nenhuma migration criada por esta revisão. Blueprint conceitual de ajustes/novas entidades para a Work Order técnica avaliar:

- `BootstrapEstado`: mantém o desenho já aprovado (`Concluido`, `ConcluidoEm`, `UsuarioAdministradorSeniorId`) — recomenda-se avaliar, na Work Order técnica, um mecanismo de banco que garanta linha única (ex.: chave primária fixa/constraint de linha única), reforçando estruturalmente a premissa "existe no máximo um registro" já registrada em `ComprasDataModel.md`.
- Sessão Bootstrap: entidade ou mecanismo análogo a `SessaoAutenticacao`, mas com campos próprios (sem `UsuarioId`, pois o usuário ainda não existe; com e-mail candidato já validado por OTP, expiração curta, flag de uso único) — nome sugerido `SessaoBootstrap`, decisão final de nome/campos cabe à Work Order técnica.
- Nenhuma entidade nova para o Bootstrap Secret ou lista de e-mails pré-autorizados — ambos vivem em configuração/secret manager, nunca em tabela de banco (seção 20.4/20.5).

### 20.16 Rate limiting

Mais restritivo que login normal, aplicado em nível de middleware (mesmo padrão de `RateLimitingPolicies`, seção 3.3/18.1):

- **Bootstrap Secret (início do fluxo):** limite agressivo por IP (ex.: 3 tentativas / 15 min, alinhado ao limite já usado para `otp-request`) e por e-mail candidato normalizado (mesmo padrão de `OtpRequestThrottle`) — nunca superior ao limite de OTP request, pois este endpoint é ainda mais sensível.
- **Identidade/e-mail:** mesmo throttle por e-mail já existente (`OtpRequestThrottle`) é reaproveitado, aplicado igualmente a e-mail autorizado/não autorizado (mesmo princípio anti-oráculo da seção 18.1).
- **IP:** camada adicional, mesmo padrão de `RateLimitingPolicies.OtpRequest`/`OtpVerify`.
- **OTP:** reaproveita os limites já vigentes de `otp-verify` (10 tentativas/15 min) — sem necessidade de um limite mais permissivo, já que Bootstrap é *mais* sensível, nunca menos.

### 20.17 Secure by default

Bootstrap respeita a `FallbackPolicy` existente (secure-by-default, seção 18.3) através de um mecanismo próprio, não de uma exceção ampla:

| Endpoint | Anônimo? | Justificativa |
|---|---|---|
| `GET /bootstrap/estado` | Sim | Leitura pública mínima (disponível/concluído) necessária para o frontend decidir `/login` vs. `/bootstrap`, sem expor dado sensível. |
| `POST /bootstrap/iniciar` | Sim | É o próprio mecanismo de entrada no fluxo — não pode exigir sessão que ainda não existe; protegido por secret + e-mail pré-autorizado + rate limiting, não por autenticação prévia. |
| `POST /bootstrap/otp/verificar` | Sim | Idem — mas apenas aceita como válido um fluxo já iniciado por `/bootstrap/iniciar` (ver contexto de tentativa, não um novo secret). |
| `POST /bootstrap/unidade-negocio` (se necessário) | **Não** | Exige esquema `BootstrapSession` (seção 20.7) — sessão Bootstrap válida, nunca `AllowAnonymous()`. |
| `POST /bootstrap/concluir` | **Não** | Idem — exige `BootstrapSession`; é a operação de maior privilégio de todo o fluxo. |

Nenhum endpoint de Bootstrap usa `AllowAnonymous()` de forma ampla sem justificativa individual listada acima — os dois primeiros são anônimos por necessidade estrutural (não existe sessão antes deles); os demais exigem o esquema de autorização próprio da seção 20.7, nunca a ausência total de autorização.

### 20.18 Plano de testes obrigatório (para a implementação futura)

- Bootstrap indisponível após conclusão: `POST /bootstrap/iniciar` retorna 404 quando `Concluido = true`, independentemente de secret/e-mail informados.
- Secret inválido: rejeitado com a mesma resposta/tempo que e-mail não autorizado.
- Identidade inválida (e-mail fora da lista): idem.
- OTP inválido/expirado/tentativas excedidas: reaproveita os testes já existentes de `CodigoVerificacaoOtp`, aplicados ao fluxo de Bootstrap.
- Brute force: rate limiting de secret, e-mail e IP todos testados isoladamente e em combinação.
- Concorrência: teste real de duas transações simultâneas tentando concluir o Bootstrap (`Task.WhenAll` com múltiplas instâncias de `DbContext`, mesmo padrão já usado para OTP em O1.4.2.1 §18.5) — exatamente uma conclui, a outra recebe erro/rejeição sem deixar dado órfão.
- Atomicidade: falha simulada em qualquer etapa intermediária (ex.: exceção ao criar `UsuarioPerfil`) não deixa `Usuario`/`UnidadeNegocio`/`BootstrapEstado` parcialmente alterado.
- Último Administrador Sênior: tentativa de inativar/remover o vínculo do único Administrador Sênior ativo restante é rejeitada.
- Reabertura: nenhuma combinação de configuração, restart, ou inativação de administrador reabre o Bootstrap após `Concluido = true`.
- Rollback (documental, não testável em CI): runbook de restore de backup deve alertar sobre a implicação da seção 20.12.
- Endpoints protegidos: os dois endpoints de conclusão exigem `BootstrapSession`; nenhum outro endpoint de negócio aceita esse esquema.
- CSRF: `CsrfHeaderFilter` aplicado e testado em todos os endpoints de mutação de Bootstrap.
- Rate limiting: testado por secret, e-mail e IP.
- Audit trail: todos os eventos da seção 20.15/20.19 são gerados nos testes de integração do fluxo completo.
- Development bypass: teste negativo confirmando que nenhum header/query param/flag de Development altera o resultado de qualquer validação de Bootstrap.

### 20.19 Auditoria

Eventos mínimos (nunca registrar secret, OTP, cookie/token de sessão Bootstrap, credenciais):

| Evento | Dados registrados |
|---|---|
| Bootstrap consultado | timestamp, resultado (disponível/concluído) — sem IP obrigatório, é leitura pública de baixo risco. |
| Tentativa de Bootstrap iniciada | e-mail candidato (ou hash), IP, timestamp, resultado (aceito/rejeitado e categoria genérica de rejeição). |
| Secret inválido | IP, timestamp — nunca o valor tentado. |
| Identidade não autorizada | e-mail (ou hash), IP, timestamp — mesma trilha do item anterior, resposta ao cliente idêntica (seção 20.6), mas auditoria interna pode diferenciar as duas causas para investigação, desde que nunca exposta ao cliente. |
| OTP solicitado (Bootstrap) | reaproveita o evento já existente de "OTP solicitado" (seção 3.8), com uma flag/contexto indicando origem Bootstrap. |
| OTP validado (Bootstrap) | idem, contexto Bootstrap. |
| Criação da Unidade de Negócio (via Bootstrap) | `UnidadeNegocioId`, timestamp. |
| Criação/vínculo do Administrador Sênior | `UsuarioId`, `UnidadeNegocioId`, timestamp — primeiro registro de auditoria funcional do ambiente. |
| Bootstrap concluído | `UsuarioId` do primeiro Administrador Sênior, `UnidadeNegocioId`, timestamp. |
| Tentativa de reabertura pós-conclusão | IP, timestamp — mesmo que a resposta ao cliente seja 404 indistinguível, o evento interno é registrado para monitoramento de abuso. |

### 20.20 Riscos residuais e pendências

**Riscos residuais (aceitos, não bloqueantes):**
- Rollback de banco/restore de backup antigo pode reabrir o Bootstrap tecnicamente (seção 20.12) — mitigação é operacional (runbook), não de código.
- Manipulação direta de `BootstrapEstado` por acesso de superusuário ao banco está fora do modelo de ameaça de uma aplicação web.

**Pendências (não bloqueantes para o Gate, a resolver na Work Order técnica de implementação):**
- Nome definitivo e schema exato da sessão Bootstrap (`SessaoBootstrap` é nome sugerido, não definitivo).
- Nome definitivo da rota de frontend (`/bootstrap` é candidata, não definitiva).
- Decisão de constraint física para garantir linha única em `BootstrapEstado` (chave fixa vs. índice/constraint único) — decisão de implementação, não de arquitetura.
- Runbook operacional de Recovery (seção 20.11) — conceitualmente definido aqui, mas o script/procedimento concreto é responsabilidade de Infra/Work Order futura, fora do escopo da O1.4.3.
- Procedimento de rotação do Bootstrap Secret antes da primeira conclusão, se a Infra desejar trocar o valor — operacional, não bloqueia o Gate.

### 20.21 Respostas SIM/NÃO

| # | Pergunta | Resposta |
|---|---|---|
| 1 | Bootstrap deve reabrir se não existir Administrador Sênior ativo? | **NÃO** |
| 2 | Pode existir mais de um Administrador Sênior? | **SIM** |
| 3 | O último Administrador Sênior pode ser inativado/removido? | **NÃO** |
| 4 | Bootstrap Secret sozinho autentica? | **NÃO** |
| 5 | OTP sozinho autentica Bootstrap? | **NÃO** |
| 6 | Sessão Bootstrap deve ter privilégios limitados? | **SIM** |
| 7 | Conclusão do Bootstrap deve ser transacional? | **SIM** |
| 8 | Bootstrap pode ser executado duas vezes? | **NÃO** |
| 9 | Development pode ter shortcut/bypass específico? | **NÃO** |
| 10 | Recovery pós-Bootstrap deve ser separado do Bootstrap? | **SIM** |

### 20.22 Bootstrap Security Design Gate

**BOOTSTRAP SECURITY DESIGN GATE: APROVADO COM PENDÊNCIAS.**

Nenhuma das pendências listadas na seção 20.20 é bloqueante para o **início** do detalhamento técnico/Work Order da O1.4.3 — todas são decisões de detalhamento de implementação (nomes, schema exato, constraint física, runbook operacional) já orientadas por esta revisão, não lacunas arquiteturais abertas. A implementação de código, entretanto, só pode começar após: (a) ratificação desta arquitetura pelo Product Owner/CTO na Work Order técnica, em particular a decisão de sessão Bootstrap distinta (seção 20.7) e a regra de "nunca ficar sem Administrador Sênior" (seção 20.8); e (b) confirmação de que o secret manager/User Secrets para armazenar o Bootstrap Secret e a lista de e-mails pré-autorizados está disponível no ambiente de implementação (Development: User Secrets, já disponível).

### 20.23 Recomendação para implementação da O1.4.3

A O1.4.3 pode avançar para detalhamento técnico e Work Order de implementação com base nesta revisão. Nenhuma decisão deste documento deve ser tratada como arquitetura definitiva além do que ADR-0020 já aprovou — onde esta revisão recomenda algo não decidido nos documentos de produto/dados (nome de rota, schema de sessão Bootstrap, constraint física de linha única), a decisão final permanece com o Product Owner/CTO na Work Order técnica, não com esta revisão. Reforça-se a regra já vigente (ADR-0020, item 13; `domain-principles.md` §Segurança): a implementação de código do Bootstrap exige nova validação de segurança dedicada depois de implementada, antes de qualquer avanço para Homologação — mesmo padrão já seguido por O1.4.1→O1.4.2→O1.4.2.1→O1.4.2.2.
