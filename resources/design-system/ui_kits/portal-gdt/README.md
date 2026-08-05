# Portal GDT — UI Kit v2.0

Recreação clicável da **SHELL completa do Portal GDT** (Gestão de Demandas de Tecnologia · AZZAS 2154), refeita na Onda 3 para acompanhar o portal real `uploads/portal-GDT/`.

## Como usar

Abra `index.html`. O kit é um click-thru completo:

1. **Login OTP em 2 passos** — informe um e-mail `@somagrupo.com.br` → digite (ou cole) o código de 6 dígitos (demo: `842156`, mas qualquer 6 dígitos funcionam)
2. **Welcome screen** — atalhos para os 4 módulos principais
3. **Navegação por tabs no header** — Nova demanda, Acompanhamento, Curadoria, Decisão, Esteira, Administração (tab admin tem cor especial roxa)
4. **Logout** pelo dropdown do user-chip (canto superior direito)

### Tabs implementadas

| Tab | Status | O que está funcionando |
|---|---|---|
| **Curadoria**      | ✅ Funcional | Filas SLA color-coded (A/B/C/D), cur-cards expandíveis com compare-row, ações de curar/descartar + toast |
| **Decisão**        | ✅ Funcional | KPI hero com composição (FTE + ganhos = total), bar chart por área, ranking sortable com perf-badges, delta-tags e detail row expandida |
| **Acompanhamento** | ✅ Funcional | Lookup binário (ID / e-mail) com validação, status stepper horizontal de 6 etapas (com halo verde no current), avaliação comment colorido, info cards colapsáveis |
| **Nova demanda**, **Esteira**, **Administração** | 🟡 Placeholder | Apenas roteamento — conteúdo entra em ondas futuras |

## Arquivos

| Arquivo | Função |
|---|---|
| `index.html` | Shell — carrega React 18, Babel, e os scripts JSX abaixo |
| `portal.css` | Estilos do portal (importa `colors_and_type.css` da raiz para os tokens) |
| `data.js` | Dataset fake — usuário, módulos, perfis, 7 demandas, áreas, stats, timeline |
| `components.jsx` | Primitivos: `Icon`, `Logo`, `Badge`, `Button`, `Avatar`, `Eyebrow`, `OtpInput`, `NoticeBox`, `Toast` |
| `shell.jsx`     | SHELL: `AuthScreen` (OTP 2 passos), `AppShell` (header + tabs + user chip dropdown), `WelcomeScreen`, `TabPlaceholder` |
| `curadoria.jsx` | Tab Curadoria — `CurCard` + filas SLA + ações |
| `avaliacao.jsx` | Tab Decisão — `KPI hero` + `bar chart` + `ranking sortable` |
| `acompanhamento.jsx` | Tab Acompanhamento — `LookupForm`, `DemandView`, `StatusStepper`, `InfoCard`, `AvaliacaoComment` |
| `app.jsx` | Root — roteamento entre auth ↔ módulos |

## Notas de design

### Arquitetura
- **Single source of truth**: `colors_and_type.css` (raiz do projeto). Nada de tokens hardcoded em arquivos isolados.
- **Header com tabs**, não sidebar — replicando o padrão da SHELL real do portal-GDT/index.html.
- **Tab admin** tem cor especial (`--aguardando` roxo) tanto no nav-tab quanto no profile-badge do dropdown.
- **Header height** é 56px (`--header-h`), seguindo a regra do design system para SHELLs com tab-nav.

### Logo
Sempre o PNG oficial `/assets/logos/azzas-2154-mark-black.png` em 22px de altura. Componente `<Logo>` lida com a variante dark (filter: invert).

### OTP com paste support
O `<OtpInput>` aceita colar o código inteiro de uma vez (handler `onPaste` que filtra dígitos e distribui pelos 6 inputs). Auto-submit quando atinge 6 dígitos.

### Status stepper
6 etapas (registrada → curada → em avaliação → aprovada → em execução → concluída). Estados:
- **done** (preenchido preto + check)
- **current** (preto + halo verde de 2px)
- **next** (branco com borda preta + sombra leve)
- **skipped** (vermelho-pastel — usado quando a demanda é rejeitada)

### Filas SLA
Cores via tokens `--fila-a/b/c/d` (+ `-bg` e `-border`) do `colors_and_type.css`. Não tem cor inventada aqui.

## O que NÃO está implementado (intencional)

- Persistência (recarregar reseta o estado)
- Autenticação real (qualquer 6 dígitos passam no OTP — demo)
- Tabs "Nova demanda", "Esteira" e "Administração" — apenas roteamento, sem conteúdo
- Multi-usuário / RBAC real (perfis são fake mas as tabs já filtram por permissão)
- Integração com Jira, Redmine, n8n etc.

Este é um UI kit, não código de produção. Para implementar de verdade:
1. Pegue `colors_and_type.css` como base de tokens
2. Use os componentes JSX deste kit como referência visual
3. Substitua o `data.js` por chamadas reais aos webhooks do n8n
4. Implemente persistência de sessão via `localStorage` + token expiration
