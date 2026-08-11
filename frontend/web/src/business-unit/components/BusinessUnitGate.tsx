import { useState, type ReactNode } from "react";
import { useMinhasUnidadesNegocio } from "../hooks/useMinhasUnidadesNegocio";
import { SelecaoUnidadeNegocioPage } from "../pages/SelecaoUnidadeNegocioPage";
import type { UnidadeNegocioSelecionavel } from "../types/unidadeNegocioSelecaoTypes";

/**
 * Integracao da Selecao de Unidade de Negocio (O1.11) no fluxo pos-login. Decisao de UX: em vez de
 * alterar `RequireAuth` (guarda de sessao, O1.4.x) ou `AuthContext`, este gate fica entre `RequireAuth`
 * e `AppShell` em `AppRoutes.tsx` — minimamente invasivo, sem tocar sessao/claims/cookies.
 *
 * Consulta `GET /me/unidades-negocio`: se ha exatamente 1 (unico caso hoje em producao), segue direto
 * para o conteudo (Dashboard) sem exibir nenhuma tela. Se ha mais de uma (nunca ocorre hoje, mas o
 * backend e a interface suportam), exibe a tela de selecao ate o usuario escolher; a escolha e mantida
 * apenas em estado de componente (sem persistencia — nao ha, hoje, nenhum efeito de sessao associado a
 * ela, pois o backend ja resolve `unidadeNegocioId` a partir da sessao).
 */
export function BusinessUnitGate({ children }: { children: ReactNode }) {
  const { unidades, loading } = useMinhasUnidadesNegocio();
  const [selecionada, setSelecionada] = useState<UnidadeNegocioSelecionavel | null>(null);

  if (loading) {
    return (
      <div className="auth-loading" role="status" aria-live="polite">
        Carregando Unidades de Negocio…
      </div>
    );
  }

  if (unidades.length > 1 && !selecionada) {
    return <SelecaoUnidadeNegocioPage unidades={unidades} onSelecionar={setSelecionada} />;
  }

  return <>{children}</>;
}
