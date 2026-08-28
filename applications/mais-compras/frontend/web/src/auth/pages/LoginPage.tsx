import { useEffect, useRef, useState, type FormEvent } from "react";
import { useLocation, useNavigate } from "react-router-dom";
import { useAuth } from "../hooks/useAuth";
import { requestOtp, verifyOtp, AuthApiError } from "../services/authApi";
import { fetchDevelopmentOtp } from "../services/developmentOtpInspector";

const RESEND_COOLDOWN_SECONDS = 60;

type Step = "email" | "otp";

/**
 * Login Passwordless por OTP (O1.4.2). Nenhum código/token é armazenado em
 * localStorage/sessionStorage — apenas estado de componente em memória.
 */
export function LoginPage() {
  const { refresh } = useAuth();
  const navigate = useNavigate();
  const location = useLocation();

  const [step, setStep] = useState<Step>("email");
  const [email, setEmail] = useState("");
  const [codigo, setCodigo] = useState("");
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [cooldown, setCooldown] = useState(0);
  const codigoInputRef = useRef<HTMLInputElement>(null);

  useEffect(() => {
    if (cooldown <= 0) return;
    const timer = setInterval(() => setCooldown((atual) => Math.max(0, atual - 1)), 1000);
    return () => clearInterval(timer);
  }, [cooldown]);

  useEffect(() => {
    if (step === "otp") codigoInputRef.current?.focus();
  }, [step]);

  async function handleSolicitarOtp(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await requestOtp(email);
      setStep("otp");
      setCooldown(RESEND_COOLDOWN_SECONDS);
    } catch (err) {
      setErro(err instanceof AuthApiError ? err.message : "Não foi possível solicitar o código.");
    } finally {
      setCarregando(false);
    }
  }

  async function handleReenviar() {
    if (cooldown > 0) return;
    setErro(null);
    setCarregando(true);
    try {
      await requestOtp(email);
      setCooldown(RESEND_COOLDOWN_SECONDS);
    } catch (err) {
      setErro(err instanceof AuthApiError ? err.message : "Não foi possível reenviar o código.");
    } finally {
      setCarregando(false);
    }
  }

  async function handleValidarOtp(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await verifyOtp(email, codigo);
      await refresh();
      const destino = (location.state as { from?: Location })?.from?.pathname ?? "/";
      navigate(destino, { replace: true });
    } catch (err) {
      setErro(err instanceof AuthApiError ? err.message : "Código inválido ou expirado.");
    } finally {
      setCarregando(false);
    }
  }

  async function handlePreencherOtpDev() {
    const codigoDev = await fetchDevelopmentOtp(email);
    if (codigoDev) setCodigo(codigoDev);
  }

  return (
    <div className="auth-page">
      <div className="card auth-card">
        <div>
          <h1>+Compras</h1>
          <p>Login com código de verificação enviado ao seu e-mail corporativo.</p>
        </div>

        {erro && (
          <div className="notice notice-crit" role="alert">
            {erro}
          </div>
        )}

        {step === "email" && (
          <form className="auth-actions" onSubmit={handleSolicitarOtp}>
            <div className="auth-field">
              <label htmlFor="login-email">E-mail corporativo</label>
              <input
                id="login-email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
                aria-describedby="login-email-helper"
              />
              <span id="login-email-helper" className="auth-helper">
                Utilize o e-mail corporativo cadastrado na sua Unidade de Negócio.
              </span>
            </div>
            <button type="submit" className="btn btn-primary" disabled={carregando} aria-busy={carregando}>
              {carregando ? "Enviando…" : "Continuar"}
            </button>
          </form>
        )}

        {step === "otp" && (
          <form className="auth-actions" onSubmit={handleValidarOtp}>
            <div className="auth-field">
              <label htmlFor="login-otp">Código de verificação</label>
              <input
                id="login-otp"
                ref={codigoInputRef}
                className="auth-otp-input"
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                pattern="[0-9]{6}"
                maxLength={6}
                required
                value={codigo}
                onChange={(event) => setCodigo(event.target.value.replace(/\D/g, "").slice(0, 6))}
                aria-describedby="login-otp-helper"
              />
              <span id="login-otp-helper" className="auth-helper">
                Enviamos um código de 6 dígitos para {email}. Ele expira em 10 minutos.
              </span>
            </div>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={carregando || codigo.length !== 6}
              aria-busy={carregando}
            >
              {carregando ? "Validando…" : "Entrar"}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleReenviar}
              disabled={carregando || cooldown > 0}
              aria-describedby="login-cooldown-status"
            >
              Reenviar código
            </button>
            <span id="login-cooldown-status" className="auth-helper" role="status" aria-live="polite">
              {cooldown > 0 ? `Você poderá reenviar em ${cooldown} segundos.` : "Você já pode reenviar o código."}
            </span>
            <button type="button" className="btn btn-secondary" onClick={() => setStep("email")}>
              Usar outro e-mail
            </button>
            {import.meta.env.DEV && (
              <button type="button" className="btn btn-secondary" onClick={handlePreencherOtpDev}>
                Preencher código (Development)
              </button>
            )}
          </form>
        )}
      </div>
    </div>
  );
}
