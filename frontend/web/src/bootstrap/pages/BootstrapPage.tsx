import { useEffect, useRef, useState, type FormEvent } from "react";
import { useNavigate } from "react-router-dom";
import { BootstrapApiError, concluir, iniciar, verificarOtp } from "../services/bootstrapApi";

const RESEND_COOLDOWN_SECONDS = 60;
const REDIRECT_DELAY_MS = 2000;

type Step = "acesso" | "otp" | "unidade" | "administrador" | "confirmacao" | "concluido";

const COMBINING_DIACRITICS = new RegExp(String.fromCharCode(0x5b, 0x5c, 0x75, 0x30, 0x33, 0x30, 0x30, 0x2d, 0x5c, 0x75, 0x30, 0x33, 0x36, 0x66, 0x5d), "g");

function slugify(valor: string): string {
  return valor
    .trim()
    .toLowerCase()
    .normalize("NFD")
    .replace(COMBINING_DIACRITICS, "")
    .replace(/[^a-z0-9]+/g, "-")
    .replace(/(^-+|-+$)/g, "");
}

/**
 * Rótulo textual do passo atual, reaproveitando a classe tipográfica já
 * existente (`.section-title`, `frontend/web/src/styles.css`) em vez de
 * introduzir um componente visual novo (ex.: stepper gráfico) — mantém a
 * decisão da Work Order O1.4.3 (seção 16) de que o "passo 0" de segurança
 * (acesso/OTP) não é numerado como parte do wizard de 3 passos de negócio.
 */
function rotuloPasso(step: Step): string | null {
  switch (step) {
    case "acesso":
    case "otp":
      return "Verificação de acesso";
    case "unidade":
      return "Passo 1 de 3 · Unidade de Negócio";
    case "administrador":
      return "Passo 2 de 3 · Administrador Sênior";
    case "confirmacao":
      return "Passo 3 de 3 · Confirmação";
    default:
      return null;
  }
}

/**
 * Wizard de Bootstrap (O1.4.3.3). Passo 0 (não numerado ao usuário como parte
 * dos 3 passos de negócio, Work Order O1.4.3 seção 16): e-mail autorizado +
 * Bootstrap Secret, seguido de OTP — portão de segurança que precede o
 * wizard de produto já especificado em ComprasUX.md (Unidade de Negócio,
 * Administrador Sênior, confirmação explícita).
 *
 * Nenhum campo de e-mail existe no passo de dados do Administrador Sênior
 * nem na confirmação: o e-mail exibido é somente leitura (o mesmo já
 * validado por OTP no passo 0) e nunca é reenviado ao backend em
 * `/bootstrap/concluir` — apenas `administrador.nome`. Nenhum dado sensível
 * (secret, código OTP) é gravado em localStorage/sessionStorage; todo o
 * estado vive em memória de componente.
 */
export function BootstrapPage() {
  const navigate = useNavigate();

  const [step, setStep] = useState<Step>("acesso");
  const [email, setEmail] = useState("");
  const [secret, setSecret] = useState("");
  const [codigo, setCodigo] = useState("");
  const [unidadeNome, setUnidadeNome] = useState("");
  const [unidadeSlug, setUnidadeSlug] = useState("");
  const [administradorNome, setAdministradorNome] = useState("");
  const [confirmado, setConfirmado] = useState(false);
  const [carregando, setCarregando] = useState(false);
  const [erro, setErro] = useState<string | null>(null);
  const [indisponivel, setIndisponivel] = useState(false);
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

  const redirectTimeoutRef = useRef<number | undefined>(undefined);

  useEffect(() => {
    return () => {
      if (redirectTimeoutRef.current !== undefined) {
        window.clearTimeout(redirectTimeoutRef.current);
      }
    };
  }, []);

  function mensagemGenerica(err: unknown, fallback: string): string {
    return err instanceof BootstrapApiError ? err.message : fallback;
  }

  async function handleIniciar(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await iniciar(email, secret);
      setStep("otp");
      setCooldown(RESEND_COOLDOWN_SECONDS);
    } catch (err) {
      if (err instanceof BootstrapApiError && err.status === 404) {
        setIndisponivel(true);
      } else {
        setErro(mensagemGenerica(err, "Não foi possível iniciar a configuração inicial."));
      }
    } finally {
      setCarregando(false);
    }
  }

  async function handleReenviar() {
    if (cooldown > 0) return;
    setErro(null);
    setCarregando(true);
    try {
      await iniciar(email, secret);
      setCooldown(RESEND_COOLDOWN_SECONDS);
    } catch (err) {
      if (err instanceof BootstrapApiError && err.status === 404) {
        setIndisponivel(true);
      } else {
        setErro(mensagemGenerica(err, "Não foi possível reenviar o código."));
      }
    } finally {
      setCarregando(false);
    }
  }

  async function handleVerificarOtp(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setCarregando(true);
    try {
      await verificarOtp(email, codigo);
      setStep("unidade");
    } catch (err) {
      setErro(mensagemGenerica(err, "Código inválido ou expirado."));
    } finally {
      setCarregando(false);
    }
  }

  function handleAvancarUnidade(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setStep("administrador");
  }

  function handleAvancarAdministrador(event: FormEvent) {
    event.preventDefault();
    setErro(null);
    setStep("confirmacao");
  }

  async function handleConcluir() {
    setErro(null);
    setCarregando(true);
    try {
      // Nenhum e-mail é incluído neste payload — o backend obtém o e-mail
      // validado por OTP a partir da própria BootstrapSessao (cookie).
      await concluir(
        { nome: unidadeNome.trim(), slug: unidadeSlug.trim() },
        { nome: administradorNome.trim() }
      );
      setStep("concluido");
      redirectTimeoutRef.current = window.setTimeout(() => navigate("/login", { replace: true }), REDIRECT_DELAY_MS);
    } catch (err) {
      if (err instanceof BootstrapApiError && err.status === 401) {
        // 401 = falha de autenticação da BootstrapSessao (inválida, expirada, já usada ou
        // revogada — BootstrapSessionAuthenticationHandler.HandleAuthenticateAsync). Aqui, sim,
        // "reiniciar o processo" é a orientação correta: a sessão em si não é mais válida.
        setErro("Sua sessão de configuração inicial expirou. Reinicie o processo.");
        setStep("acesso");
        setCodigo("");
        setSecret("");
      } else if (err instanceof BootstrapApiError && (err.status === 403 || err.status === 404)) {
        // 403 = BootstrapSessao autenticada com sucesso, mas BootstrapNaoConcluidoRequirement
        // negou porque BootstrapEstado.Concluido == true (ou estado ausente, fail-closed) —
        // BootstrapNaoConcluidoAuthorizationHandler. Bootstrap nunca reabre, então "reiniciar o
        // processo" seria uma orientação incorreta aqui; tratado com a mesma tela de
        // indisponibilidade já usada para 404 ("já concluído").
        setIndisponivel(true);
      } else {
        setErro(mensagemGenerica(err, "Não foi possível concluir a configuração inicial."));
      }
    } finally {
      setCarregando(false);
    }
  }

  if (indisponivel) {
    return (
      <div className="auth-page">
        <div className="card auth-card">
          <h1>+Compras</h1>
          <div className="notice notice-warn" role="status">
            A configuração inicial não está mais disponível. Utilize o login normal.
          </div>
          <button type="button" className="btn btn-primary" onClick={() => navigate("/login", { replace: true })}>
            Ir para o login
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className="auth-page">
      <div className="card auth-card">
        <div>
          <h1>+Compras</h1>
          <p>Configuração inicial do ambiente — criação da Unidade de Negócio e do Administrador Sênior.</p>
        </div>

        {rotuloPasso(step) && <span className="section-title">{rotuloPasso(step)}</span>}

        {erro && (
          <div className="notice notice-crit" role="alert">
            {erro}
          </div>
        )}

        {step === "acesso" && (
          <form className="auth-actions" onSubmit={handleIniciar}>
            <div className="auth-field">
              <label htmlFor="bootstrap-email">E-mail autorizado</label>
              <input
                id="bootstrap-email"
                type="email"
                autoComplete="email"
                required
                value={email}
                onChange={(event) => setEmail(event.target.value)}
              />
            </div>
            <div className="auth-field">
              <label htmlFor="bootstrap-secret">Chave de configuração inicial</label>
              <input
                id="bootstrap-secret"
                type="password"
                autoComplete="off"
                required
                value={secret}
                onChange={(event) => setSecret(event.target.value)}
              />
              <span className="auth-helper">Chave fornecida pela equipe responsável pela implantação.</span>
            </div>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={carregando || !email.trim() || !secret.trim()}
              aria-busy={carregando}
            >
              {carregando ? "Enviando…" : "Continuar"}
            </button>
          </form>
        )}

        {step === "otp" && (
          <form className="auth-actions" onSubmit={handleVerificarOtp}>
            <div className="auth-field">
              <label htmlFor="bootstrap-otp">Código de verificação</label>
              <input
                id="bootstrap-otp"
                ref={codigoInputRef}
                type="text"
                inputMode="numeric"
                autoComplete="one-time-code"
                pattern="[0-9]{6}"
                maxLength={6}
                required
                value={codigo}
                onChange={(event) => setCodigo(event.target.value.replace(/\D/g, "").slice(0, 6))}
              />
              <span className="auth-helper">Enviamos um código de 6 dígitos para {email}.</span>
            </div>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={carregando || codigo.length !== 6}
              aria-busy={carregando}
            >
              {carregando ? "Validando…" : "Continuar"}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={handleReenviar}
              disabled={carregando || cooldown > 0}
            >
              Reenviar código
            </button>
            <span className="auth-helper" role="status" aria-live="polite">
              {cooldown > 0 ? `Você poderá reenviar em ${cooldown} segundos.` : "Você já pode reenviar o código."}
            </span>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => {
                setErro(null);
                setCodigo("");
                setSecret("");
                setStep("acesso");
              }}
              disabled={carregando}
            >
              Usar outro e-mail
            </button>
          </form>
        )}

        {step === "unidade" && (
          <form className="auth-actions" onSubmit={handleAvancarUnidade}>
            <div className="auth-field">
              <label htmlFor="bootstrap-unidade-nome">Nome da Unidade de Negócio</label>
              <input
                id="bootstrap-unidade-nome"
                type="text"
                required
                value={unidadeNome}
                onChange={(event) => {
                  const valor = event.target.value;
                  const slugAnterior = slugify(unidadeNome);
                  setUnidadeNome(valor);
                  if (unidadeSlug === "" || unidadeSlug === slugAnterior) {
                    setUnidadeSlug(slugify(valor));
                  }
                }}
              />
            </div>
            <div className="auth-field">
              <label htmlFor="bootstrap-unidade-slug">Identificador (slug)</label>
              <input
                id="bootstrap-unidade-slug"
                type="text"
                required
                value={unidadeSlug}
                onChange={(event) => setUnidadeSlug(slugify(event.target.value))}
              />
              <span className="auth-helper">Usado como identificador único da Unidade de Negócio.</span>
            </div>
            <button type="submit" className="btn btn-primary" disabled={!unidadeNome.trim() || !unidadeSlug.trim()}>
              Continuar
            </button>
          </form>
        )}

        {step === "administrador" && (
          <form className="auth-actions" onSubmit={handleAvancarAdministrador}>
            <div className="auth-field">
              <label htmlFor="bootstrap-admin-nome">Nome do Administrador Sênior</label>
              <input
                id="bootstrap-admin-nome"
                type="text"
                required
                value={administradorNome}
                onChange={(event) => setAdministradorNome(event.target.value)}
              />
              <span className="auth-helper">
                O e-mail já verificado ({email}) será utilizado como identidade do Administrador Sênior — não é
                possível alterá-lo aqui.
              </span>
            </div>
            <button type="submit" className="btn btn-primary" disabled={!administradorNome.trim()}>
              Continuar
            </button>
            <button type="button" className="btn btn-secondary" onClick={() => setStep("unidade")}>
              Voltar
            </button>
          </form>
        )}

        {step === "confirmacao" && (
          <div className="auth-actions">
            <div className="notice notice-warn" role="status">
              Confirme os dados abaixo. Esta ação cria a Unidade de Negócio e o primeiro Administrador Sênior e não
              poderá ser repetida.
            </div>
            <dl>
              <dt>Unidade de Negócio</dt>
              <dd>
                {unidadeNome} ({unidadeSlug})
              </dd>
              <dt>Administrador Sênior</dt>
              <dd>
                {administradorNome} — {email}
              </dd>
            </dl>
            <div className="auth-field">
              <label htmlFor="bootstrap-confirmar">
                <input
                  id="bootstrap-confirmar"
                  type="checkbox"
                  checked={confirmado}
                  onChange={(event) => setConfirmado(event.target.checked)}
                />{" "}
                Confirmo os dados acima e desejo concluir a configuração inicial.
              </label>
            </div>
            <button
              type="button"
              className="btn btn-primary"
              onClick={handleConcluir}
              disabled={!confirmado || carregando}
              aria-busy={carregando}
            >
              {carregando ? "Concluindo…" : "Concluir configuração inicial"}
            </button>
            <button
              type="button"
              className="btn btn-secondary"
              onClick={() => setStep("administrador")}
              disabled={carregando}
            >
              Voltar
            </button>
          </div>
        )}

        {step === "concluido" && (
          <div
            className="notice"
            role="status"
            style={{ background: "var(--aprovado-bg)", borderColor: "var(--aprovado-bg)", color: "var(--aprovado)" }}
          >
            Configuração inicial concluída. Redirecionando para o login…
          </div>
        )}
      </div>
    </div>
  );
}
