import { useEffect, useRef, useState } from "react";
import type { UsuarioAutenticado } from "../../auth/types/authTypes";

/**
 * Identidade do usuario no header do portal (Design Review Pos-Onda 1, lote DR.2).
 * Substitui o antigo `<div className="user-chip">{usuario.nome}</div>` + botao
 * "Sair" isolado por um unico chip com avatar (iniciais) + nome + chevron, que
 * abre um dropdown com e-mail, escopo administrativo e a acao de sair.
 *
 * Inspirado em `resources/design-system/ui_kits/portal-gdt/shell.jsx`
 * (`UserChip`), adaptado aos dados reais disponiveis em `UsuarioAutenticado`
 * (sem inventar "perfis" ou nome de Unidade de Negocio que nao existem no tipo).
 */

const ESCOPO_LABEL: Record<UsuarioAutenticado["escopoAdministrativo"], string> = {
  Produto: "Administrador Sênior (cross-BU)",
  Negocio: "Administrador de Unidade de Negócio"
};

function iniciaisDoNome(nome: string): string {
  const partes = nome.trim().split(/\s+/).filter(Boolean);
  if (partes.length === 0) return "?";
  if (partes.length === 1) return partes[0].slice(0, 2).toUpperCase();
  return (partes[0][0] + partes[partes.length - 1][0]).toUpperCase();
}

export function UserMenu({ usuario, onLogout }: { usuario: UsuarioAutenticado; onLogout: () => void }) {
  const [aberto, setAberto] = useState(false);
  const wrapRef = useRef<HTMLDivElement>(null);

  useEffect(() => {
    if (!aberto) return;
    function aoClicarFora(evento: MouseEvent) {
      if (wrapRef.current && !wrapRef.current.contains(evento.target as Node)) {
        setAberto(false);
      }
    }
    document.addEventListener("click", aoClicarFora);
    return () => document.removeEventListener("click", aoClicarFora);
  }, [aberto]);

  return (
    <div className="user-chip-wrap" ref={wrapRef}>
      <button
        type="button"
        className="user-chip"
        onClick={() => setAberto((atual) => !atual)}
        aria-expanded={aberto}
        aria-haspopup="menu"
      >
        <span className="user-avatar" aria-hidden="true">{iniciaisDoNome(usuario.nome)}</span>
        <span className="user-name-text">{usuario.nome}</span>
        <svg viewBox="0 0 24 24" aria-hidden="true"><path d="m6 9 6 6 6-6" /></svg>
      </button>
      {aberto && (
        <div className="user-dropdown" role="menu">
          <div className="dd-info">
            <div className="dd-email">{usuario.email}</div>
            <div className="dd-perfis">
              <span className="dd-badge">{ESCOPO_LABEL[usuario.escopoAdministrativo]}</span>
            </div>
          </div>
          <button type="button" className="dd-item dd-item-danger" role="menuitem" onClick={onLogout}>
            Sair
          </button>
        </div>
      )}
    </div>
  );
}
