// Small reusable components — icons, badges, buttons, avatar
// All Lucide-style SVGs, stroke 2, currentColor.

const Icon = ({ name, size = 14, style }) => {
  const props = {
    width: size, height: size, viewBox: "0 0 24 24",
    fill: "none", stroke: "currentColor", strokeWidth: 2,
    strokeLinecap: "round", strokeLinejoin: "round", style,
  };
  switch (name) {
    case "grid": return <svg {...props}><rect x="3" y="3" width="7" height="7" rx="1"/><rect x="14" y="3" width="7" height="7" rx="1"/><rect x="3" y="14" width="7" height="7" rx="1"/><rect x="14" y="14" width="7" height="7" rx="1"/></svg>;
    case "file": return <svg {...props}><path d="M14 2H6a2 2 0 0 0-2 2v16a2 2 0 0 0 2 2h12a2 2 0 0 0 2-2V8z"/><polyline points="14 2 14 8 20 8"/></svg>;
    case "settings": return <svg {...props}><circle cx="12" cy="12" r="3"/><path d="M19.4 15a1.65 1.65 0 0 0 .33 1.82l.06.06a2 2 0 0 1-2.83 2.83l-.06-.06a1.65 1.65 0 0 0-1.82-.33 1.65 1.65 0 0 0-1 1.51V21a2 2 0 0 1-4 0v-.09A1.65 1.65 0 0 0 9 19.4a1.65 1.65 0 0 0-1.82.33l-.06.06a2 2 0 0 1-2.83-2.83l.06-.06A1.65 1.65 0 0 0 4.68 15a1.65 1.65 0 0 0-1.51-1H3a2 2 0 0 1 0-4h.09A1.65 1.65 0 0 0 4.6 9a1.65 1.65 0 0 0-.33-1.82l-.06-.06a2 2 0 0 1 2.83-2.83l.06.06A1.65 1.65 0 0 0 9 4.68a1.65 1.65 0 0 0 1-1.51V3a2 2 0 0 1 4 0v.09a1.65 1.65 0 0 0 1 1.51 1.65 1.65 0 0 0 1.82-.33l.06-.06a2 2 0 0 1 2.83 2.83l-.06.06A1.65 1.65 0 0 0 19.4 9a1.65 1.65 0 0 0 1.51 1H21a2 2 0 0 1 0 4h-.09a1.65 1.65 0 0 0-1.51 1z"/></svg>;
    case "search": return <svg {...props}><circle cx="11" cy="11" r="7"/><path d="m21 21-4.35-4.35"/></svg>;
    case "plus": return <svg {...props}><path d="M12 5v14M5 12h14"/></svg>;
    case "check": return <svg {...props}><polyline points="20 6 9 17 4 12"/></svg>;
    case "x": return <svg {...props}><line x1="18" y1="6" x2="6" y2="18"/><line x1="6" y1="6" x2="18" y2="18"/></svg>;
    case "chevron-left": return <svg {...props}><polyline points="15 18 9 12 15 6"/></svg>;
    case "chevron-right": return <svg {...props}><polyline points="9 18 15 12 9 6"/></svg>;
    case "chevron-down": return <svg {...props}><polyline points="6 9 12 15 18 9"/></svg>;
    case "lock": return <svg {...props}><rect x="3" y="11" width="18" height="11" rx="2"/><path d="M7 11V7a5 5 0 0 1 10 0v4"/></svg>;
    case "trend": return <svg {...props}><polyline points="3 17 9 11 13 15 21 7"/><polyline points="14 7 21 7 21 14"/></svg>;
    case "clipboard": return <svg {...props}><rect x="9" y="2" width="6" height="4" rx="1"/><path d="M9 4H5a2 2 0 0 0-2 2v14a2 2 0 0 0 2 2h14a2 2 0 0 0 2-2V6a2 2 0 0 0-2-2h-4"/></svg>;
    case "play": return <svg {...props}><polygon points="5 3 19 12 5 21 5 3"/></svg>;
    case "archive": return <svg {...props}><polyline points="21 8 21 21 3 21 3 8"/><rect x="1" y="3" width="22" height="5"/><line x1="10" y1="12" x2="14" y2="12"/></svg>;
    case "eye": return <svg {...props}><path d="M2 12s3-7 10-7 10 7 10 7-3 7-10 7-10-7-10-7z"/><circle cx="12" cy="12" r="3"/></svg>;
    case "user": return <svg {...props}><circle cx="12" cy="8" r="4"/><path d="M4 20c0-4 3.6-7 8-7s8 3 8 7"/></svg>;
    case "info": return <svg {...props}><circle cx="12" cy="12" r="9"/><path d="M12 8v4"/><path d="M12 16h.01"/></svg>;
    case "alert": return <svg {...props}><path d="m10.29 3.86L1.82 18a2 2 0 0 0 1.71 3h16.94a2 2 0 0 0 1.71-3L13.71 3.86a2 2 0 0 0-3.42 0z"/><path d="M12 9v4M12 17h.01"/></svg>;
    case "logout": return <svg {...props}><path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4"/><polyline points="16 17 21 12 16 7"/><line x1="21" y1="12" x2="9" y2="12"/></svg>;
    default: return null;
  }
};

const Logo = ({ height = 22, dark }) => (
  <img
    src="../../assets/logos/azzas-2154-mark-black.png"
    alt="AZZAS 2154"
    style={{
      height, width: "auto", display: "block",
      filter: dark ? "invert(1)" : "none",
      opacity: dark ? 0.95 : 1,
    }}
  />
);

const Badge = ({ status, label }) => (
  <span className={"badge b-" + status}>{label}</span>
);

const Button = ({ variant = "primary", icon, children, onClick, disabled, small, loading, type = "button" }) => (
  <button
    type={type}
    className={"btn btn-" + variant + (small ? " btn-sm" : "") + (loading ? " btn-loading" : "")}
    onClick={onClick}
    disabled={disabled || loading}
  >
    {loading && <span className="btn-spinner" />}
    {!loading && icon && <Icon name={icon} />}
    {children}
  </button>
);

const Avatar = ({ initials, size = 26 }) => (
  <div
    className="avatar"
    style={{
      width: size, height: size, borderRadius: "50%",
      background: "var(--accent)", color: "white",
      fontSize: Math.round(size * 0.38), fontWeight: 600,
      display: "grid", placeItems: "center", flexShrink: 0,
    }}
  >{initials}</div>
);

const Eyebrow = ({ children, style }) => (
  <div
    style={{
      fontFamily: "var(--mono)", fontSize: 10, fontWeight: 500,
      textTransform: "uppercase", letterSpacing: "0.07em",
      color: "var(--text-muted)", ...style,
    }}
  >{children}</div>
);

// ── OTP input ─────────────────────────────────────────────────
// 6 cells, auto-focus next on type, paste support fills all cells
const OtpInput = ({ value, onChange, onSubmit }) => {
  const refs = React.useRef([]);
  const digits = (value || "").padEnd(6, " ").slice(0, 6).split("");

  const setAt = (i, ch) => {
    const next = digits.map((d, idx) => idx === i ? (ch || " ") : d).join("").trimEnd();
    onChange(next);
  };

  const handleInput = (i) => (e) => {
    const v = e.target.value.replace(/\D/g, "").slice(-1);
    setAt(i, v);
    if (v && i < 5) refs.current[i + 1]?.focus();
  };

  const handleKey = (i) => (e) => {
    if (e.key === "Backspace" && !digits[i].trim() && i > 0) {
      refs.current[i - 1]?.focus();
    } else if (e.key === "Enter" && value.length === 6) {
      onSubmit && onSubmit();
    } else if (e.key === "ArrowLeft" && i > 0) {
      refs.current[i - 1]?.focus();
    } else if (e.key === "ArrowRight" && i < 5) {
      refs.current[i + 1]?.focus();
    }
  };

  // 🔑 Paste support: encher todas as cells de uma vez
  const handlePaste = (e) => {
    const text = (e.clipboardData.getData("text") || "").replace(/\D/g, "").slice(0, 6);
    if (!text) return;
    e.preventDefault();
    onChange(text);
    const lastIdx = Math.min(text.length, 5);
    setTimeout(() => refs.current[lastIdx]?.focus(), 0);
  };

  return (
    <div className="otp-inputs">
      {[0, 1, 2, 3, 4, 5].map(i => (
        <input
          key={i}
          ref={el => refs.current[i] = el}
          className="otp-digit"
          type="text"
          inputMode="numeric"
          maxLength={1}
          value={digits[i].trim()}
          onChange={handleInput(i)}
          onKeyDown={handleKey(i)}
          onPaste={handlePaste}
          autoComplete={i === 0 ? "one-time-code" : "off"}
        />
      ))}
    </div>
  );
};

// ── Notice box ────────────────────────────────────────────────
const NoticeBox = ({ kind = "info", icon, children }) => (
  <div className={"notice-box notice-" + kind}>
    <Icon name={icon || (kind === "warn" ? "alert" : kind === "crit" ? "x" : kind === "ok" ? "check" : "info")} size={14} />
    <span>{children}</span>
  </div>
);

const Toast = ({ kind = "dark", children, onDone, action }) => {
  React.useEffect(() => {
    const ms = action ? 6000 : 2800;
    const t = setTimeout(() => onDone && onDone(), ms);
    return () => clearTimeout(t);
  }, []);
  return (
    <div className={"toast " + kind}>
      {kind === "success" && <Icon name="check" />}
      {kind === "error" && <Icon name="x" />}
      <span>{children}</span>
      {action && (
        <button
          onClick={() => { action.onClick(); onDone && onDone(); }}
          style={{
            background: "rgba(255,255,255,0.18)", color: "inherit",
            border: "none", padding: "4px 10px", borderRadius: 100,
            fontFamily: "var(--font)", fontSize: 12, fontWeight: 500,
            cursor: "pointer", marginLeft: 4,
          }}
        >{action.label}</button>
      )}
    </div>
  );
};

Object.assign(window, { Icon, Logo, Badge, Button, Avatar, Eyebrow, OtpInput, NoticeBox, Toast });
