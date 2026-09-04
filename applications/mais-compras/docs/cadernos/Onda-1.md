# Caderno — Onda 1

A Onda 1 já está formalmente concluída (41/41, ver `.ai/PROJECT_STATE.md`) antes da criação deste Caderno —
este arquivo existe por completude da estrutura (`README.md`) e para registrar, retroativamente, achados
arquiteturais que remontam a decisões da Onda 1 mas só ficaram evidentes numa revisão posterior.

---

### Fundação Multi-BU/RBAC administrativo já cobre o eixo Produto × BU, não ainda o eixo de dados operacionais

- **Origem:** Rodada arquitetural Onda 2 — Multi-BU/Multi-ERP (03/09/2026), revisando entregas da O1.11/O1.13.5/ADR-0022.
- **Assunto:** A Onda 1 (O1.11 + ADR-0022) resolveu isolamento Multi-BU para o eixo administrativo/RBAC
  (`UnidadeNegocio`, `EscopoAdministrativoUnidadeNegocio`, `ConfiguracaoErp` por BU, Perfis por BU). Não
  resolveu (nem tinha esse escopo) o isolamento por BU dos dados operacionais/integrados que chegam depois
  (Fornecedores, Itens Fiscais, RAW/REFINED, datasets ERP) — que são o alvo da Onda 2.
- **Tipo:** Arquitetura
- **Tratar em:** Somente documentação (a fundação em si não muda; o registro é só para deixar claro o que a
  Onda 1 cobriu e o que não cobriu, evitando reabrir a Onda 1).
- **Status:** Decidido
- **Resumo:** Nenhuma dívida nova da Onda 1 é criada por este registro — é apenas a delimitação exata de
  fronteira entre o que já existia (RBAC/administrativo) e o que esta rodada da Onda 2 endereça (dados
  operacionais/integrados).
