import { FormEvent, useState } from "react";
import { useCentrosCusto } from "../../cost-centers/hooks/useCentrosCusto";
import { usePerfis } from "../../profiles/hooks/usePerfis";
import { useUsuarios } from "../../users/hooks/useUsuarios";
import { CRITERIO_ALCADA, CRITERIO_ALCADA_LABELS } from "../types/alcadaAprovacaoTypes";
import type { AlcadaAprovacao, AlcadaAprovacaoInput, CriterioAlcada, TipoAprovador } from "../types/alcadaAprovacaoTypes";

/**
 * O backend (O1.12) identifica Centro de Custo por `CentroCustoMetadadoId` (Guid interno de
 * `CentroCustoMetadado`), agora exposto por `administration/cost-centers` (O1.7/O1.12) como
 * `CentroCusto.centroCustoMetadadoId`. Esse campo e `undefined` para Centros de Custo que ainda nao tem
 * metadado local (`temMetadadoLocal === false`) — nesses casos o seletor abaixo desabilita a opcao, pois o
 * backend rejeitaria a submissao (nao ha Guid para enviar).
 */
export function AlcadaAprovacaoForm({ unidadeNegocioId, alcada, error, loading, onSubmit, onCancel }: {
  unidadeNegocioId: string;
  alcada?: AlcadaAprovacao;
  error: string | null;
  loading: boolean;
  onSubmit: (input: AlcadaAprovacaoInput) => void;
  onCancel: () => void;
}) {
  const { usuarios } = useUsuarios();
  const { perfis } = usePerfis();
  const { centrosCusto } = useCentrosCusto();

  const [nome, setNome] = useState(alcada?.nome ?? "");
  const [criterio, setCriterio] = useState<CriterioAlcada>(alcada?.criterio ?? CRITERIO_ALCADA.Valor);
  const [valorMinimo, setValorMinimo] = useState<string>(alcada?.valorMinimo != null ? String(alcada.valorMinimo) : "");
  const [valorMaximo, setValorMaximo] = useState<string>(alcada?.valorMaximo != null ? String(alcada.valorMaximo) : "");
  const [centroCustoMetadadoId, setCentroCustoMetadadoId] = useState<string>(alcada?.centroCustoMetadadoId ?? "");
  const [nivel, setNivel] = useState(alcada?.nivel ?? 1);
  const [tipoAprovador, setTipoAprovador] = useState<TipoAprovador>(alcada?.aprovadorPerfilId ? "Perfil" : "Usuário");
  const [aprovadorUsuarioId, setAprovadorUsuarioId] = useState(alcada?.aprovadorUsuarioId ?? "");
  const [aprovadorPerfilId, setAprovadorPerfilId] = useState(alcada?.aprovadorPerfilId ?? "");

  const criterioValor = criterio === CRITERIO_ALCADA.Valor;

  void unidadeNegocioId;

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    onSubmit({
      nome,
      criterio,
      valorMinimo: criterioValor && valorMinimo !== "" ? Number(valorMinimo) : undefined,
      valorMaximo: criterioValor && valorMaximo !== "" ? Number(valorMaximo) : undefined,
      centroCustoMetadadoId: centroCustoMetadadoId || undefined,
      nivel: Number(nivel),
      aprovadorUsuarioId: tipoAprovador === "Usuário" ? aprovadorUsuarioId || undefined : undefined,
      aprovadorPerfilId: tipoAprovador === "Perfil" ? aprovadorPerfilId || undefined : undefined
    });
  }

  return (
    <form className="card form-card" onSubmit={handleSubmit}>
      <div className="card-heading">
        <h2>{alcada ? "Editar Alçada de Aprovação" : "Nova Alçada de Aprovação"}</h2>
      </div>

      {error && <div className="notice notice-crit">{error}</div>}

      <label>
        Nome
        <input value={nome} onChange={(event) => setNome(event.target.value)} disabled={loading} required />
      </label>

      <label>
        Criterio
        <select
          value={criterio}
          onChange={(event) => setCriterio(Number(event.target.value) as CriterioAlcada)}
          disabled={loading}
        >
          {(Object.entries(CRITERIO_ALCADA_LABELS) as [string, string][]).map(([valor, label]) => (
            <option key={valor} value={valor}>
              {label}
            </option>
          ))}
        </select>
      </label>

      {criterioValor && (
        <>
          <label>
            Valor minimo
            <input
              type="number"
              step="0.01"
              value={valorMinimo}
              onChange={(event) => setValorMinimo(event.target.value)}
              disabled={loading}
            />
          </label>
          <label>
            Valor maximo
            <input
              type="number"
              step="0.01"
              value={valorMaximo}
              onChange={(event) => setValorMaximo(event.target.value)}
              disabled={loading}
            />
          </label>
        </>
      )}

      <label>
        Nivel
        <input
          type="number"
          min={1}
          value={nivel}
          onChange={(event) => setNivel(Number(event.target.value))}
          disabled={loading}
          required
        />
      </label>

      <label>
        Centro de Custo (opcional)
        <select value={centroCustoMetadadoId} onChange={(event) => setCentroCustoMetadadoId(event.target.value)} disabled={loading}>
          <option value="">Nenhum</option>
          {centrosCusto.map((centroCusto) => (
            <option
              key={centroCusto.id}
              value={centroCusto.centroCustoMetadadoId ?? ""}
              disabled={!centroCusto.centroCustoMetadadoId}
            >
              {centroCusto.codigoErp} — {centroCusto.descricaoErp}
              {!centroCusto.centroCustoMetadadoId
                ? " (disponível apenas após primeira edição em Gestão de Centros de Custo)"
                : ""}
            </option>
          ))}
        </select>
      </label>

      <label>
        Tipo de aprovador
        <select
          value={tipoAprovador}
          onChange={(event) => setTipoAprovador(event.target.value as TipoAprovador)}
          disabled={loading}
        >
          <option value="Usuário">Usuário</option>
          <option value="Perfil">Perfil</option>
        </select>
      </label>

      {tipoAprovador === "Usuário" ? (
        <label>
          Usuário aprovador
          <select value={aprovadorUsuarioId} onChange={(event) => setAprovadorUsuarioId(event.target.value)} disabled={loading} required>
            <option value="" disabled>
              Selecione...
            </option>
            {usuarios.map((usuario) => (
              <option key={usuario.id} value={usuario.id}>
                {usuario.nome}
              </option>
            ))}
          </select>
        </label>
      ) : (
        <label>
          Perfil aprovador
          <select value={aprovadorPerfilId} onChange={(event) => setAprovadorPerfilId(event.target.value)} disabled={loading} required>
            <option value="" disabled>
              Selecione...
            </option>
            {perfis.map((perfil) => (
              <option key={perfil.id} value={perfil.id}>
                {perfil.nome}
              </option>
            ))}
          </select>
        </label>
      )}

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : "Salvar"}
        </button>
      </div>
    </form>
  );
}
