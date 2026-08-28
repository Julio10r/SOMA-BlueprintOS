import { FormEvent, useState } from "react";
import type { ConfiguracaoNotificacao, ConfiguracaoNotificacaoInput } from "../types/configuracaoNotificacaoTypes";

/**
 * Formulario 1:1 de Configuracao de Notificacoes. ESCOPO MINIMO DE FUNDACAO (O1.11, item #24): apenas
 * ativar/inativar o canal e-mail, e-mail remetente e nome do remetente. Sem catalogo de eventos nesta
 * sprint — indicacao textual discreta de que sera configuravel no futuro, sem checkboxes ficticios.
 */
export function ConfiguracaoNotificacaoForm({ configuracao, error, loading, onSubmit }: {
  configuracao: ConfiguracaoNotificacao | null;
  error: string | null;
  loading: boolean;
  onSubmit: (input: ConfiguracaoNotificacaoInput) => void;
}) {
  const [emailAtivado, setEmailAtivado] = useState(configuracao?.emailAtivado ?? false);
  const [emailRemetente, setEmailRemetente] = useState(configuracao?.emailRemetente ?? "");
  const [nomeRemetente, setNomeRemetente] = useState(configuracao?.nomeRemetente ?? "");

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({
      emailAtivado,
      emailRemetente: emailRemetente || undefined,
      nomeRemetente: nomeRemetente || undefined
    });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>Configuração de Notificações</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        <input type="checkbox" checked={emailAtivado} onChange={(event) => setEmailAtivado(event.target.checked)} disabled={loading} />
        {" "}Notificações por e-mail ativadas
      </label>

      <label>
        E-mail remetente
        <input
          type="email"
          value={emailRemetente}
          onChange={(event) => setEmailRemetente(event.target.value)}
          placeholder="notificacoes@suaempresa.com.br"
          disabled={loading}
          required={emailAtivado}
        />
      </label>

      <label>
        Nome do remetente
        <input value={nomeRemetente} onChange={(event) => setNomeRemetente(event.target.value)} disabled={loading} />
      </label>

      <p className="notice notice-info">
        O catálogo de eventos configuráveis por notificação será disponibilizado nesta tela quando os
        workflows operacionais correspondentes existirem.
      </p>

      <div className="actions">
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
