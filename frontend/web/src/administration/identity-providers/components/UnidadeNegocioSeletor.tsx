import { useEffect, useState } from "react";
import { listUnidadesNegocio } from "../../business-units/services/unidadesNegocioApi";
import type { UnidadeNegocio } from "../../business-units/types/unidadeNegocioTypes";

/**
 * Seletor de Unidade de Negocio reutilizado pelos modulos operados por UN explicita no path
 * (Identity Providers, Configuracao de ERP): Sistema.Gerenciar/ConfiguracaoErp.Gerenciar sao permissoes
 * corporativas, entao quem administra escolhe qual UN esta operando a partir da listagem completa.
 */
export function UnidadeNegocioSeletor({ value, onChange }: {
  value: string | null;
  onChange: (unidadeNegocioId: string) => void;
}) {
  const [unidades, setUnidades] = useState<UnidadeNegocio[]>([]);
  const [loading, setLoading] = useState(true);

  useEffect(() => {
    listUnidadesNegocio()
      .then(setUnidades)
      .finally(() => setLoading(false));
  }, []);

  return (
    <label>
      Unidade de Negocio
      <select value={value ?? ""} disabled={loading} onChange={(event) => onChange(event.target.value)}>
        <option value="" disabled>
          Selecione...
        </option>
        {unidades.map((unidadeNegocio) => (
          <option key={unidadeNegocio.id} value={unidadeNegocio.id}>
            {unidadeNegocio.nome}
          </option>
        ))}
      </select>
    </label>
  );
}
