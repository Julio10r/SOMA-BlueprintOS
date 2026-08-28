// SHELL screens: Auth (OTP) + AppShell (header tabs) + Welcome
// Uses globals: Logo, OtpInput, NoticeBox, Button, Avatar, Icon, Eyebrow

// ── Step 1: e-mail ────────────────────────────────────────────────
function AuthEmailStep({ email, setEmail, onContinue, loading, error }) {
  const submit = (e) => { e.preventDefault(); onContinue(); };
  return (
    <form className="auth-step" onSubmit={submit}>
      <Eyebrow style={{ marginBottom: 10 }}>Passo 1 · E-mail</Eyebrow>
      <h1 className="auth-title">Acessar o sistema</h1>
      <p className="auth-subtitle">Informe seu e-mail corporativo. Um código de verificação será enviado para validar o acesso.</p>

      <div className="field-group">
        <label className="field-label">E-mail corporativo</label>
        <input
          className={"field-input" + (error ? " error" : "")}
          type="email"
          value={email}
          onChange={e => setEmail(e.target.value)}
          placeholder="seu@somagrupo.com.br"
          autoComplete="email"
          autoFocus
        />
        {error && <span className="auth-error">{error}</span>}
      </div>

      <Button variant="primary" icon="chevron-right" loading={loading} type="submit">
        Continuar
      </Button>
    </form>
  );
}

// ── Step 2: OTP ───────────────────────────────────────────────────
function AuthOtpStep({ email, otp, setOtp, onVerify, onBack, loading, error, demoHint }) {
  const [secondsLeft, setSecondsLeft] = React.useState(59);

  React.useEffect(() => {
    if (secondsLeft <= 0) return;
    const t = setTimeout(() => setSecondsLeft(s => s - 1), 1000);
    return () => clearTimeout(t);
  }, [secondsLeft]);

  // Auto-submit quando bate 6 dígitos (igual ao real)
  React.useEffect(() => {
    if (otp.length === 6 && !loading) {
      const t = setTimeout(() => onVerify(), 200);
      return () => clearTimeout(t);
    }
  }, [otp, loading]);

  const mm = Math.floor(secondsLeft / 60);
  const ss = String(secondsLeft % 60).padStart(2, "0");

  return (
    <form className="auth-step" onSubmit={(e) => { e.preventDefault(); onVerify(); }}>
      <Eyebrow style={{ marginBottom: 10 }}>Passo 2 · Código</Eyebrow>
      <h1 className="auth-title">Verificar e-mail</h1>
      <p className="auth-subtitle">
        Código de 6 dígitos enviado para <strong>{email}</strong>
      </p>

      <NoticeBox kind="info">
        Válido por 15 minutos. Você pode <strong>colar o código inteiro</strong> de uma vez.
        {demoHint && <span style={{ display: "block", marginTop: 4, opacity: 0.8 }}>
          Demo: use <code style={{ fontFamily: "var(--mono)", fontSize: 11 }}>{demoHint}</code> ou qualquer outro código.
        </span>}
      </NoticeBox>

      <OtpInput value={otp} onChange={setOtp} onSubmit={onVerify} />
      {error && <span className="auth-error">{error}</span>}

      <Button variant="primary" icon={loading ? null : "chevron-right"} loading={loading} type="submit" disabled={otp.length < 6}>
        Verificar e entrar
      </Button>

      <div className="resend-row">
        <button
          type="button"
          className="resend-btn"
          disabled={secondsLeft > 0}
          onClick={() => setSecondsLeft(59)}
        >
          Reenviar código
        </button>
        {secondsLeft > 0 && <span className="resend-timer">{mm}:{ss}</span>}
      </div>

      <button type="button" className="auth-back" onClick={onBack}>← Usar outro e-mail</button>
    </form>
  );
}

// ── Full auth screen orchestrator ─────────────────────────────────
function AuthScreen({ data, onAuthed }) {
  const [step, setStep] = React.useState("email");
  const [email, setEmail] = React.useState("");
  const [otp, setOtp] = React.useState("");
  const [loading, setLoading] = React.useState(false);
  const [error, setError] = React.useState(null);

  const sendCode = () => {
    setError(null);
    const e = email.trim().toLowerCase();
    if (!e || !e.includes("@") || !e.includes(".")) {
      setError("Informe um e-mail corporativo válido."); return;
    }
    if (!e.endsWith(data.authorizedDomain)) {
      setError(`Apenas e-mails ${data.authorizedDomain} são autorizados.`); return;
    }
    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      setEmail(e);
      setStep("otp");
    }, 700);
  };

  const verify = () => {
    setError(null);
    if (otp.length < 6) { setError("Digite o código de 6 dígitos completo."); return; }
    setLoading(true);
    setTimeout(() => {
      setLoading(false);
      // Demo: aceita qualquer código
      onAuthed({ ...data.user, email });
    }, 600);
  };

  return (
    <div className="auth-screen">
      <div className="auth-card">
        <div className="auth-logo">
          <Logo height={22} />
          <div className="auth-logo-divider"></div>
          <span className="auth-logo-sub">Gestão de Demandas<br/>de Tecnologia</span>
        </div>

        {step === "email" && (
          <AuthEmailStep
            email={email} setEmail={setEmail}
            onContinue={sendCode} loading={loading} error={error}
          />
        )}
        {step === "otp" && (
          <AuthOtpStep
            email={email} otp={otp} setOtp={setOtp}
            onVerify={verify}
            onBack={() => { setStep("email"); setOtp(""); setError(null); }}
            loading={loading} error={error}
            demoHint={data.demoOtp}
          />
        )}
      </div>
    </div>
  );
}

// ── User dropdown ─────────────────────────────────────────────────
function UserChip({ user, onLogout, perfilMeta }) {
  const [open, setOpen] = React.useState(false);
  React.useEffect(() => {
    if (!open) return;
    const close = () => setOpen(false);
    document.addEventListener("click", close);
    return () => document.removeEventListener("click", close);
  }, [open]);

  return (
    <div className="user-chip-wrap" onClick={e => e.stopPropagation()}>
      <button className="user-chip" onClick={() => setOpen(!open)}>
        <Avatar initials={user.initials} size={26} />
        <span className="user-name-text">{user.name || user.email}</span>
        <Icon name="chevron-down" size={12} />
      </button>
      {open && (
        <div className="user-dropdown">
          <div className="dd-info">
            <div className="dd-email">{user.email}</div>
            <div className="dd-perfis">
              {(user.perfis || []).map(p => {
                const meta = perfilMeta[p] || { label: p, css: "" };
                return <span key={p} className={"dd-badge dd-badge-" + meta.css}>{meta.label}</span>;
              })}
            </div>
          </div>
          <button className="dd-item dd-item-danger" onClick={onLogout}>
            <Icon name="logout" size={13} /> Sair
          </button>
        </div>
      )}
    </div>
  );
}

// ── App shell: header + tab nav + body ────────────────────────────
function AppShell({ user, modules, perfilMeta, activeModule, onNavigate, onLogout, children }) {
  const allowed = modules.filter(m => m.perfis.some(p => user.perfis.includes(p)));
  return (
    <div className="shell">
      <header className="shell-header">
        <div className="shell-left">
          <Logo height={22} />
          <div className="shell-divider"></div>
          <nav className="shell-nav">
            {allowed.map(m => (
              <button
                key={m.key}
                className={"nav-tab" + (activeModule === m.key ? " active" : "") + (m.isAdmin ? " admin-tab" : "")}
                onClick={() => onNavigate(m.key)}
              >
                <Icon name={m.icon} size={14} />
                <span>{m.label}</span>
              </button>
            ))}
          </nav>
        </div>
        <div className="shell-right">
          <UserChip user={user} onLogout={onLogout} perfilMeta={perfilMeta} />
        </div>
      </header>
      <div className="shell-body">{children}</div>
    </div>
  );
}

// ── Welcome screen ────────────────────────────────────────────────
function WelcomeScreen({ user, modules, onNavigate }) {
  const allowed = modules.filter(m => m.perfis.some(p => user.perfis.includes(p)) && !m.isAdmin);
  return (
    <div className="welcome-screen">
      <div className="welcome-icon">
        <Logo height={28} />
      </div>
      <h2 className="welcome-title">Bem-vindo, {user.name?.split(" ")[0]}</h2>
      <p className="welcome-sub">Selecione um módulo no menu acima para começar — ou escolha um atalho abaixo.</p>
      <div className="welcome-modules">
        {allowed.slice(0, 4).map(m => (
          <button key={m.key} className="welcome-module-btn" onClick={() => onNavigate(m.key)}>
            <Icon name={m.icon} size={14} /> {m.label}
          </button>
        ))}
      </div>
    </div>
  );
}

// ── Placeholder for Batch B/C/D tab contents ──────────────────────
function TabPlaceholder({ moduleKey, moduleLabel }) {
  return (
    <div className="welcome-screen">
      <Eyebrow style={{ marginBottom: 8 }}>{moduleLabel}</Eyebrow>
      <h2 className="welcome-title" style={{ fontSize: 18 }}>Tab em construção</h2>
      <p className="welcome-sub" style={{ maxWidth: 400 }}>
        O conteúdo da aba <code style={{ fontFamily: "var(--mono)", fontSize: 12 }}>{moduleKey}</code> entra na próxima onda do UI Kit.
        A SHELL, login OTP e roteamento entre tabs já estão funcionais.
      </p>
    </div>
  );
}

Object.assign(window, { AuthScreen, AppShell, WelcomeScreen, TabPlaceholder });
