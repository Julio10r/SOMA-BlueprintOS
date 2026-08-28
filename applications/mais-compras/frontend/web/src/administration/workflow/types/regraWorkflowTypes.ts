/** Espelha RegraWorkflowDto do backend (O1.12, BlueprintOS.Application.Identity.Models). */
export type RegraWorkflow = {
  id: string;
  unidadeNegocioId: string;
  nome: string;
  tipoProcesso: string;
  ordem: number;
  status: "Ativo" | "Inativo";
  criadoEm: string;
  atualizadoEm: string;
};

export type RegraWorkflowInput = {
  nome: string;
  tipoProcesso: string;
  ordem: number;
};
