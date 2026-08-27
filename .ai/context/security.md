# context/security.md

> Escopo: segurança de aplicação e dados. Complementa STANDARDS.md §18 (segredos) e §17 (APIs) — não repetidos aqui exceto onde há especificidade de autenticação/LGPD/GCP.

## Autenticação

Autenticação oficial via **Microsoft Entra ID** (PROJECT.md §4), implementada pelo módulo Identity. Todo endpoint autenticado deve validar o token emitido pelo Entra ID antes de processar a requisição; nenhum módulo deve implementar seu próprio mecanismo paralelo de autenticação.

## Autorização

Autorização baseada em roles/claims, resolvida a partir do token validado. Como o BlueprintOS é multi-tenant (PROJECT.md §1), toda regra de autorização deve considerar o tenant do usuário autenticado — nenhuma consulta ou operação deve retornar ou afetar dados de um tenant diferente do usuário autenticado, mesmo que a role permita a operação em geral.

## LGPD

Princípios gerais (não um documento jurídico):

- **Classificação de dados** — dados pessoais devem ser identificados como tais nos modelos de domínio, distinguindo-os de dados operacionais/técnicos.
- **Minimização** — armazenar apenas o dado pessoal estritamente necessário ao processo de negócio.
- **Retenção** — dados pessoais devem ter política de retenção definida por domínio; não reter indefinidamente sem justificativa de negócio.
- **Acesso e auditoria** — acesso a dados pessoais deve ser rastreável (ver [observability.md](./observability.md)).

## Proteção de segredos

STANDARDS.md §18 já proíbe armazenar segredos em código, exigindo variáveis de ambiente ou Secret Manager. Como o BlueprintOS é hospedado em GCP (PROJECT.md §4), o mecanismo oficial de Secret Manager é o **Google Cloud Secret Manager**: segredos (connection strings, chaves de API, credenciais) devem ser lidos em runtime a partir dele, nunca versionados em `appsettings.json`, `.env` versionado, ou variáveis hardcoded.

## APIs

Regras estruturais de API (DTOs, versionamento, REST) já estão definidas em STANDARDS.md §17. Do ponto de vista de segurança, toda API deve adicionalmente:

- validar o token de autenticação em todo endpoint que não seja explicitamente público;
- aplicar rate limiting para prevenir abuso, especialmente em endpoints que acionam agentes de IA (custo computacional maior);
- nunca expor mensagens de erro internas (stack trace, detalhes de infraestrutura) diretamente ao cliente.

Ver também [observability.md](./observability.md) para auditoria e rastreamento de acesso.

## AI Governance

A Onda 1 de governanca de Agents esta documentada em `docs/architecture/AIGovernance.md` e implementada no nucleo `BlueprintOS.Core.AI.Governance`.

Separacao obrigatoria:

- **Policy Engine**: regra deterministica e bloqueante quando conectada a um fluxo.
- **Security/LGPD Agent**: especialista consultivo para interpretar contexto, LGPD, minimizacao, finalidade, exposicao em logs/prompts/planilhas e riscos residuais.

Nenhum Agent de dominio deve autoaprovar operacao potencialmente sensivel. Agents podem propor, explicar e estimar impacto; a aprovacao deve estar vinculada ao `ActionProposal.ProposalHash`.

Quando a classificacao de dados for desconhecida, nao inventar. Usar `Unknown` e tratar com cautela.

Segredos (`senha`, `token`, `cookie`, `connection string`, `API key`, `client secret`) devem ser tratados como risco maximo. Tentativas de persistir, logar, exportar, colocar em prompt, relatorio ou Git devem ser modeladas com `ContainsSecrets = true` ou `DataClassification = SecretCredential`, resultando em `Red`.

Status atual: enforcement tecnico existe no `AIGovernancePolicyEngine`, `ApprovalPolicy` e `GovernedActionDemoFlow`; Tool Gateway universal, persistencia de auditoria/aprovacoes e interceptacao de scripts externos ainda sao planejados.
