export type FeatureFlagStatusUnidade = {
  unidadeNegocioId: string;
  unidadeNegocioNome: string;
  ativa: boolean;
};

export type FeatureFlag = {
  id: string;
  nome: string;
  descricao: string;
  status: FeatureFlagStatusUnidade[];
};

export type FeatureFlagCriarInput = {
  nome: string;
  descricao: string;
};
