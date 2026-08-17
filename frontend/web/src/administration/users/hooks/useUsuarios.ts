import { useCallback, useEffect, useState } from "react";
import { alterarStatusUsuario, listUsuarios, UsuarioAcessoNegadoError } from "../services/usuariosApi";
import type { Usuario } from "../types/userTypes";

function mensagemDe(err: unknown, fallback: string): string {
  return err instanceof Error ? err.message : fallback;
}

/**
 * Estados reais tratados pela tela (O1.6 — Gestao de Usuarios, backend real): carregando,
 * sucesso, vazio, erro e acesso negado — mesmo padrao de `usePerfis` (O1.5). `acessoNegado`
 * e distinto de `error` porque um 403 e a resposta correta do backend para uma sessao
 * autenticada sem a permissao `Usuario.Gerenciar`, nao uma falha a ser reportada como erro.
 */
export function useUsuarios() {
  const [usuarios, setUsuarios] = useState<Usuario[]>([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [acessoNegado, setAcessoNegado] = useState(false);

  const reload = useCallback(async () => {
    setLoading(true);
    setError(null);
    setAcessoNegado(false);
    try {
      setUsuarios(await listUsuarios());
    } catch (err) {
      if (err instanceof UsuarioAcessoNegadoError) {
        setAcessoNegado(true);
        setUsuarios([]);
      } else {
        setError(mensagemDe(err, "Falha ao carregar usuários."));
      }
    } finally {
      setLoading(false);
    }
  }, []);

  useEffect(() => {
    reload();
  }, [reload]);

  /**
   * Usuarios nunca sao excluidos: apenas ativados/inativados, permanecendo
   * auditaveis (mesmo padrao de Filiais, Centros de Custo e Unidades de
   * Alocacao).
   */
  async function toggleAtivo(usuario: Usuario): Promise<void> {
    await alterarStatusUsuario(usuario.id, !usuario.ativo);
    await reload();
  }

  return { usuarios, loading, error, acessoNegado, reload, toggleAtivo };
}
