# Work Order - Portal +Compras Frontend

## Identificacao

- Projeto: SOMA BlueprintOS / +Compras
- Frente: Portal +Compras Frontend
- Executor: Claude Code
- Base funcional disponivel: B2.2 - Enriquecimento Inteligente de Fornecedor
- Branch alvo: `feature/a13-procurement-vertical-slice`

## Objetivo

Construir o portal visual +Compras utilizando:

- React;
- TypeScript;
- GDT Design System AZZAS 2154.

O portal deve transformar a primeira tela funcional de fornecedor em uma experiencia navegavel e demonstravel, preservando a integracao real ja existente para fornecedores.

## Contexto funcional disponivel

Fornecedor esta funcional.

Disponivel:

- cadastro;
- consulta CNPJ;
- enriquecimento;
- aprovacao;
- rejeicao;
- integracao API;
- auditoria de consulta e decisao;
- protecao de `NomeFantasia` pela regra Linx.

## Escopo funcional

### Fornecedores

Fornecedores deve ser o unico modulo conectado ao backend nesta etapa.

Fluxos esperados:

- abrir cadastro de fornecedor;
- informar `Cnpj_Cpf`;
- consultar CNPJ;
- visualizar dados retornados;
- visualizar divergencias quando existir fornecedor cadastrado;
- aceitar ou rejeitar campos;
- exibir fonte, data/hora, usuario e `CorrelationId`;
- alertar situacao cadastral `Baixada` e exigir confirmacao;
- manter `NomeFantasia` bloqueado para alteracao automatica.

### Modulos demonstrativos

Criar estrutura visual preparada para evolucao:

- Dashboard;
- Fornecedores;
- Pedidos;
- Negociacoes;
- Indicadores;
- Agentes IA;
- Configuracoes.

Esses modulos podem ter telas demonstrativas, estados vazios, cards de status e placeholders honestos de roadmap.

## Regra obrigatoria

Nao criar funcionalidades falsas.

Somente Fornecedores deve estar conectado ao backend.

Demais modulos podem ser telas demonstrativas preparadas para evolucao, mas nao devem simular persistencia, aprovacoes, execucoes de agentes, pedidos reais ou negociacoes reais.

## Design System

Usar o AZZAS 2154 - GDT Design System:

- tokens em `docs/design-system/colors_and_type.css`;
- referencias em `docs/design-system/README.md`;
- UI kit em `docs/design-system/ui_kits/portal-gdt/`;
- padrao visual sobrio, corporativo, denso e orientado a operacao;
- sem CSS paralelo ao design system quando houver token ou padrao existente.

## Contratos frontend existentes

Arquivos principais:

- `frontend/web/src/procurement/suppliers/linxSupplierContract.ts`;
- `frontend/web/src/procurement/suppliers/supplierEnrichmentApi.ts`;
- `frontend/web/src/procurement/suppliers/CadastroFornecedor.tsx`.

Tipos disponiveis:

- `Fornecedor`;
- `ConsultaCnpjResultado`;
- `FornecedorCampoDivergencia`;
- `FornecedorEnriquecimentoAnalise`;
- `SituacaoCadastralCnpj`;
- `StatusConsultaCnpj`;
- `FornecedorCampoDecisao`;
- `LinxSupplierFormModel`.

## APIs disponiveis

Cadastro e consulta de fornecedores:

- `GET /fornecedores`;
- `GET /fornecedores?q={termo}`;
- `POST /fornecedores`;
- `GET /fornecedores/{id}`;
- `PUT /fornecedores/{id}`;
- `DELETE /fornecedores/{id}`;

Enriquecimento CNPJ:

- `POST /fornecedores/consulta-cnpj`;
- `POST /fornecedores/{id}/enriquecimento-cnpj`;
- `POST /fornecedores/{id}/enriquecimento-cnpj/aprovar`;
- `POST /fornecedores/{id}/enriquecimento-cnpj/rejeitar`.

Sincronizacao ERP:

- `POST /api/fornecedores/sincronizar`;
- `POST /api/fornecedores/sincronizar/lote`;
- `GET /api/fornecedores/{id}/sincronizacoes`.

Descoberta de fornecedores:

- `GET /api/fornecedores/descobertas`;
- `POST /api/fornecedores/descobertas`.

## Criterios de aceite

- Portal visual +Compras criado com navegacao principal.
- Modulo Fornecedores funcional e conectado aos contratos existentes.
- Modulos demonstrativos criados sem funcionalidades falsas.
- Design System AZZAS 2154 aplicado.
- Responsividade minima para desktop e mobile.
- Testes frontend atualizados ou criados para a navegacao e o fluxo de fornecedor.
- Build frontend aprovado.
- Documentacao atualizada com decisoes relevantes.

## Fora de escopo

- Implementar pedidos reais.
- Implementar negociacoes reais.
- Implementar agentes IA reais no frontend.
- Criar novas regras de dominio.
- Alterar contratos backend sem necessidade explicita.
- Substituir BrasilAPI ou introduzir provider pago.
