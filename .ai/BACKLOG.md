# BACKLOG.md

> Catálogo canônico das oito fases e 56 sprints. Não aprova execução: somente uma Work Order com status `Approved` pode iniciar uma sprint.

## Reclassificação oficial — MVP 1.0 e MVP 1.1 (Replanejamento Frontend First)

O catálogo abaixo preserva integralmente seu histórico, dependências e evidências — nenhuma Work Order concluída foi alterada. Esta seção apenas reclassifica cada código por Onda do MVP 1.0 (ver `.ai/ROADMAP.md`) ou por versão 1.1, refletindo a estratégia Frontend First aprovada.

| Onda / Versão | Códigos | Observação |
|---|---|---|
| Onda 1 — Fundação Funcional | Administração (Unidade de Negócio, Usuários, Perfis, Permissões, Filiais, Centros de Custo, Unidades de Alocação, IdP, Configuração ERP, Workflow, Aprovação, Controle Orçamentário, Feature Flags) | Escopo novo desta Onda, não catalogado nas 56 Work Orders originais; parte do frontend navegável + blueprint de banco. Filiais e Centros de Custo explicitados nesta linha em 06/08/2026 (fechamento da O1.3.4) para refletir o escopo já implementado pelas sprints O1.3.3/O1.3.4, anteriormente ausente desta descrição por omissão. |
| Onda 2 — Cadastros | B1, B2, B2.1, B2.1.1, B2.1.2, B2.1.3, B2.2 (concluídas), B3 | Fornecedores já concluídos; materiais/serviços (B3), categorias, compradores e centros de custo entram nesta onda com sincronização ERP. |
| Onda 3 — Processo de Compras | B4, B5, B6, B7, C1, C2, C3, C5 | Solicitação, cotação, negociação por IA, workflow, aprovação e pedido — versão simplificada de C1–C3/C5 suficiente para o MVP 1.0; C4/C6/C7 avaliados ao final da onda. |
| Onda 4 — Integrações Operacionais | G1, G2, G3, G4 | ERP, Nota Fiscal; Pagamento é escopo novo, não catalogado. |
| Onda 5 — Go Live | H1, H2, H4, H6, H7 (subconjunto mínimo para produção) | Login/segurança mínima, observabilidade, cloud/CI-CD e operação assistida necessários ao Go Live; aprofundamento completo de H1–H7 permanece em 1.1. |
| **MVP 1.1** | A5, A6, C4, C6, C7, D1–D7, E1–E7, F1–F7, G5, G6, G7, H3, H5 | ESG, Portal de Fornecedores, Marketplace, Analytics avançado, Previsão de Demanda, Previsão de Preços, Jurídico, Compliance e Gestão de Riscos — movidos oficialmente para 1.1. A arquitetura permanece preparada para essas capacidades; apenas o roadmap de entrega muda. |

## Regras de estado

- **Implementado:** comprovado por código, testes e/ou histórico Git.
- **Parcial:** há evidência limitada; não representa a entrega integral.
- **Não comprovado:** a referência existe, mas falta evidência suficiente.
- **Planejado:** especificado, ainda sem aprovação ou execução.

| Código | Fase | Nome | Objetivo | Dependências | Status | Work Order | Evidência de conclusão | Observações |
|---|---|---|---|---|---|---|---|---|
| A1 | Foundation | Arquitetura Base | Estabelecer solution .NET, projetos, camadas, contratos fundamentais, convenções, estrutura inicial e health check. | Nenhuma além da inicialização. | Implementado | [A1](work-orders/completed/A1-arquitetura-base.md) | Código, testes e histórico Git comprovam a fundação. | Ver Work Order. |
| A2 | Foundation | AI Runtime | Implementar contratos de modelos de IA, providers, mensagens, configuração, execução e tratamento básico de respostas. | A1. | Implementado | [A2](work-orders/completed/A2-ai-runtime.md) | Código, testes e histórico Git comprovam o runtime. | Ver Work Order. |
| A3 | Foundation | Agent Framework | Implementar agentes, agente-base, contexto, resultados, fábrica, registro e execução padronizada. | A2. | Implementado | [A3](work-orders/completed/A3-agent-framework.md) | Código, testes e histórico Git comprovam o framework. | Ver Work Order. |
| A4 | Foundation | Workflow e Observabilidade Fundamental | Implementar workflow sequencial básico, logging estruturado, correlation ID, métricas fundamentais e diagnóstico. | A3. | Implementado | [A4](work-orders/completed/A4-workflow-e-observabilidade-fundamental.md) | Código, testes e histórico Git comprovam o workflow básico. | Ver Work Order. |
| A5 | Foundation | Configuração Multiempresa | Definir empresas, unidades de negócio, configurações isoladas, feature flags e preparação para multi-tenancy. | A1; Identity e persistência futuras. | Não comprovado | [A5](work-orders/backlog/fase-a/A5-configuracao-multiempresa.md) | Não há evidência suficiente. | Ver Work Order. |
| A6 | Foundation | Agente Comprador Sênior | Implementar estratégias de negociação, análise de contexto, memória de negociação e recomendações de compra. | A2 e A3. | Parcial | [A6](work-orders/backlog/fase-a/A6-agente-comprador-senior.md) | Parcial: estratégia e memória existem; agente concreto não. | Ver Work Order. |
| A7 | Foundation | Sistema de Documentação | Implementar geração e publicação de documentação executiva, cliente e engenharia nos formatos definidos. | A1. | Implementado | [A7](work-orders/completed/A7-sistema-de-documentacao.md) | Código Documentation e histórico Git comprovam a entrega. | Ver Work Order. |
| B1 | Sourcing Intelligence | Cadastro e Perfil de Fornecedores | Criar domínio, persistência e APIs para fornecedores, contatos, categorias, unidades atendidas e situação cadastral. | A1; H1/H2 propostos. | Concluída | [B1](work-orders/completed/B1-cadastro-e-perfil-de-fornecedores.md) | Código, migration e validação de conectividade concluídos; aplicação da migration pendente de autorização. | Nenhuma. |
| B2 | Sourcing Intelligence | Descoberta Inicial de Fornecedores | Consultar o ERP SOMA_DESENV somente para leitura, aplicar score explicável e persistir descobertas no +Compras. | B1. | Concluída | [B2](work-orders/completed/B2-catalogo-de-materiais-e-servicos.md) | Código, testes e commit `a19e496`; validação operacional ERP pendente de ambiente. | Score é estrutura inicial; não desclassifica a entrega. |
| B2.1 | Sourcing Intelligence | Validação Operacional e Sincronização de Fornecedores com ERP | Sincronizar fornecedores entre +Compras e ERP com contrato canônico, adaptadores por BU, regra temporal, inativação, idempotência e auditoria imutável. | B1; B2; acesso ao ERP SOMA_DESENV. | Concluída | [B2.1](work-orders/completed/B2.1-ValidacaoOperacionalESincronizacaoDeFornecedoresComERP.md) | Importação/exportação, atualização, inativação, auditoria e concorrência validadas; commits `b08769f` e `3b6d54b`. | CLIFORs 315501, 315502, 315503 e 315505 confirmados no Linx. |
| B2.1.1 | Sourcing Intelligence | Completar Mapeamento Canônico ERP → +Compras | Preencher o contrato canônico com dados de identificação, endereço, contato, banco, comercial, fiscal e fornecimento. | B2.1. | Concluída | [B2.1.1](work-orders/completed/B2.1.1-CompletarMapeamentoCanonicoErpMaisCompras.md) | Mapeamento e importação idempotente comprovados; commit `0240c35`. | B2.1.2 concluída posteriormente. |
| B2.1.2 | Sourcing Intelligence | Validação Operacional e Sincronização de Fornecedores com ERP | Consultar fornecedores no ERP `SOMA_DESENV` e sincronizar para o banco `MaisCompras` por uma camada de integração desacoplada. | B2.1; B2.1.1; modelo Linx alinhado; VPN e SQL Server corporativo. | Concluída | [B2.1.2](work-orders/completed/B2.1.2-AlinhamentoEstruturalErpLinxMaisCompras.md) | `IFornecedorErpReader`, `SomaFornecedorReader`, `SincronizarFornecedoresErpUseCase`, endpoint `GET /api/fornecedores/sincronizar-erp`, testes unitários e teste de integração condicionado à VPN/configuração. | O alinhamento estrutural Linx foi concluído no commit `77861eb`; o fluxo real SOMA → +Compras foi endurecido posteriormente na B2.1.3. |
| B2.1.3 | Sourcing Intelligence | Endurecimento da Integração ERP de Fornecedores | Tornar a sincronização ERP de fornecedores uma rotina operacional com lotes, histórico, erros parciais, logs e métricas. | B2.1.2; VPN e SQL Server corporativo para validação real. | Concluída | Sprint direta solicitada em 02/08/2026 | Leitura paginada, entidades `SincronizacaoFornecedor` e `ErroSincronizacaoFornecedor`, migration `202608020001_B213FornecedorErpSyncHardening`, retorno detalhado do endpoint, logs estruturados, 8 testes unitários em `SincronizarFornecedoresErpUseCaseTests`, e validação real executada em 02/08/2026 contra API em Docker, VPN corporativa e banco `MaisCompras` (endpoint `limite=50` retornou `Parcial`, 50 consultados, confirmado por `sqlcmd` em `SincronizacoesFornecedores`/`ErrosSincronizacoesFornecedores`). | `dotnet build` e `dotnet test backend/BlueprintOS.sln` aprovados: 282 testes (277 unitários + 5 integração), 0 falhas. Correções pós-entrega inicial: dois bugs de paginação no teste do use case (parada prematura em lote parcial, commit `21f1a67`; cálculo incorreto do offset, commit `ca48dc3`); e, na validação real de 02/08/2026, mais três problemas encontrados e corrigidos — dependência Docker obrigatória `api → sqlserver` que impedia a API de subir, parâmetro `limite` tratado como tamanho de página em vez de teto total de fornecedores processados, e erro parcial de persistência que virava HTTP 500 por poluição do `ChangeTracker` do EF Core (corrigido com `ChangeTracker.Clear()` no catch). Nenhuma regra de negócio foi alterada em nenhuma correção. |
| B2.2 | Sourcing Intelligence | Consulta CNPJ e Enriquecimento de Fornecedor | Consultar dados externos por `Cnpj_Cpf` como sugestão revisável para o cadastro +Compras, com auditoria e sem atualização automática de ERP. | B2.1, B2.1.1 e B2.1.2 concluídas; provedor externo gratuito BrasilAPI para B2.2.2. | Concluída | [B2.2](work-orders/completed/B2.2-EnriquecimentoCadastralDeFornecedoresPorCnpj.md) | B2.2.1 concluída: contrato `ICnpjConsultaProvider`, resultado tipado, histórico persistido e testes aprovados. B2.2.2 concluída: `BrasilApiCnpjProvider`, configuração externa, timeout, cancelamento, normalização e auditoria via caso de uso. B2.2.3 concluída: comparação campo a campo, aprovação/rejeição, atualização seletiva, proteção `NomeFantasia`/Linx e auditoria por decisão. B2.2.4 concluída: tela React `CadastroFornecedor`, contratos frontend de consulta/divergência/decisão e endpoint complementar `POST /fornecedores/consulta-cnpj`. Commits: `5a6aab8`, `234906c`, `32c9971`. | Próximas evoluções preservadas: Frontend Portal +Compras (concluído tecnicamente no frontend em `8ee8f4e`, backend pendente de revalidação local — ver entrega fora do catálogo "Portal +Compras Frontend"), B2.2.5 se mantida no roadmap e B3. Consulta segue como sugestão revisável; não há atualização automática de ERP. |
| B3 | Sourcing Intelligence | Cadastro e Integração de Itens | Criar consulta ERP, cadastro próprio, famílias, categorias, seleção manual e relacionamentos com fornecedores. | B1; B2.1 e B2.2. | Planejado | [B3](work-orders/backlog/fase-b/B3-historico-de-compras.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| B4 | Sourcing Intelligence | Compras e Pedidos Operacionais | Criar solicitação, rascunho, itens, aprovação humana, persistência +Compras e status de integração. | B3. | Planejado | [B4](work-orders/backlog/fase-b/B4-inteligencia-de-precos.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| B5 | Sourcing Intelligence | Portal Operacional Integrado | Evoluir o portal como interface dos módulos de fornecedor, item e pedido, com seleção e cadastro manuais. | B1 a B4. | Planejado | [B5](work-orders/backlog/fase-b/B5-descoberta-e-qualificacao-de-fornecedores.md) | Não executada; sem evidência de conclusão. | ADR-0017 define a navegação completa e a evolução incremental; não é produto separado. |
| B6 | Sourcing Intelligence | Integrações ERP por BU | Consolidar adaptadores desacoplados, criação confirmada de pedido, identificador externo e reprocessamento. | B3 e B4. | Planejado | [B6](work-orders/backlog/fase-b/B6-recomendacao-de-sourcing.md) | Não executada; sem evidência de conclusão. | Escrita ERP exige confirmação humana. |
| B7 | Sourcing Intelligence | Fluxo Operacional Ponta a Ponta | Validar o ciclo fornecedor, item, pedido, integração e auditoria técnica básica. | B1 a B6. | Planejado | [B7](work-orders/backlog/fase-b/B7-cockpit-de-sourcing.md) | Não executada; sem evidência de conclusão. | Base para inteligência posterior. |
| C1 | Negotiation Automation | Dossiê de Negociação | Consolidar histórico, fornecedor, preços, riscos, demanda, metas e argumentos antes da negociação. | B1, B3, B4 e B6. | Planejado | [C1](work-orders/backlog/fase-c/C1-dossie-de-negociacao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C2 | Negotiation Automation | Planejador de Negociação | Gerar estratégia, objetivo, faixa-alvo, concessões, alternativas, limites e sequência de negociação. | C1. | Planejado | [C2](work-orders/backlog/fase-c/C2-planejador-de-negociacao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C3 | Negotiation Automation | Agente de Negociação | Executar negociações assistidas ou automatizadas por canais controlados, mantendo contexto e regras de autonomia. | C2 e C5. | Planejado | [C3](work-orders/backlog/fase-c/C3-agente-de-negociacao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C4 | Negotiation Automation | Memória Persistente de Negociação | Persistir interações, propostas, contrapropostas, decisões, aprendizados e resultados. | C3; persistência proposta. | Planejado | [C4](work-orders/backlog/fase-c/C4-memoria-persistente-de-negociacao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C5 | Negotiation Automation | Aprovações e Alçadas | Implementar limites de autonomia, aprovação humana, segregação de funções e trilha de decisão. | H1 e H2 propostos. | Planejado | [C5](work-orders/backlog/fase-c/C5-aprovacoes-e-alcadas.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C6 | Negotiation Automation | Avaliação de Resultado | Comparar resultado negociado com baseline, meta, orçamento, mercado e histórico. | C1, C3 e C4. | Planejado | [C6](work-orders/backlog/fase-c/C6-avaliacao-de-resultado.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| C7 | Negotiation Automation | Central de Negociações | Disponibilizar fila, status, intervenções humanas, resultados, alertas e indicadores de negociação. | C1 a C6; H1/H2 propostos. | Planejado | [C7](work-orders/backlog/fase-c/C7-central-de-negociacoes.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D1 | Contract & Compliance | Integração com Plataforma Jurídica | Criar contratos de integração para consulta de contratos, vigência, partes, status e metadados jurídicos. | G1; plataforma jurídica a aprovar. | Planejado | [D1](work-orders/backlog/fase-d/D1-integracao-com-plataforma-juridica.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D2 | Contract & Compliance | Obrigações e Marcos Contratuais | Controlar entregas, renovações, reajustes, vencimentos, garantias e obrigações associadas à compra. | D1. | Planejado | [D2](work-orders/backlog/fase-d/D2-obrigacoes-e-marcos-contratuais.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D3 | Contract & Compliance | Compliance de Compras | Validar políticas internas, documentação obrigatória, concorrência, alçadas e impedimentos. | B3, C5 e H2. | Planejado | [D3](work-orders/backlog/fase-d/D3-compliance-de-compras.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D4 | Contract & Compliance | Agente de Compliance | Avaliar processos de compra, explicar inconsistências e recomendar correções antes da aprovação. | D3, Knowledge e AI Runtime. | Planejado | [D4](work-orders/backlog/fase-d/D4-agente-de-compliance.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D5 | Contract & Compliance | Gestão de Exceções | Registrar desvios, justificativas, aprovações extraordinárias, responsáveis e prazo de regularização. | D3 e C5. | Planejado | [D5](work-orders/backlog/fase-d/D5-gestao-de-excecoes.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D6 | Contract & Compliance | Auditoria e Evidências | Gerar trilha imutável de ações, decisões, documentos, agentes, usuários e integrações. | D1 a D5; H4/H5 propostos. | Planejado | [D6](work-orders/backlog/fase-d/D6-auditoria-e-evidencias.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| D7 | Contract & Compliance | Painel Contratual e de Compliance | Consolidar vencimentos, obrigações, riscos, exceções e conformidade operacional. | D1 a D6; H1/H2 propostos. | Planejado | [D7](work-orders/backlog/fase-d/D7-painel-contratual-e-de-compliance.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E1 | Supplier Risk & ESG | Modelo de Risco de Fornecedor | Definir dimensões, indicadores, pesos, níveis, histórico e metodologia explicável de risco. | B1. | Planejado | [E1](work-orders/backlog/fase-e/E1-modelo-de-risco-de-fornecedor.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E2 | Supplier Risk & ESG | Integração de Dados de Risco | Consumir fontes internas e externas autorizadas sobre situação financeira, fiscal, operacional e reputacional. | E1 e G1. | Planejado | [E2](work-orders/backlog/fase-e/E2-integracao-de-dados-de-risco.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E3 | Supplier Risk & ESG | Monitoramento Contínuo | Executar reavaliações periódicas, detectar alterações e gerar alertas relevantes. | E1 e E2. | Planejado | [E3](work-orders/backlog/fase-e/E3-monitoramento-continuo.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E4 | Supplier Risk & ESG | Agente de Risco | Interpretar sinais, produzir análise explicável e recomendar mitigação ou bloqueio. | E1 a E3; AI Runtime. | Planejado | [E4](work-orders/backlog/fase-e/E4-agente-de-risco.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E5 | Supplier Risk & ESG | Avaliação ESG | Registrar critérios ambientais, sociais e de governança por fornecedor e categoria. | B1 e B2. | Planejado | [E5](work-orders/backlog/fase-e/E5-avaliacao-esg.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E6 | Supplier Risk & ESG | Planos de Mitigação | Criar ações, responsáveis, prazos, evidências e acompanhamento para riscos e desvios ESG. | E1, E4 e E5. | Planejado | [E6](work-orders/backlog/fase-e/E6-planos-de-mitigacao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| E7 | Supplier Risk & ESG | Cockpit de Risco e ESG | Exibir mapa de risco, evolução, criticidade, alertas, mitigação e exposição da cadeia. | E1 a E6; H1/H2 propostos. | Planejado | [E7](work-orders/backlog/fase-e/E7-cockpit-de-risco-e-esg.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F1 | Predictive Analytics | Camada Analítica de Compras | Criar modelos de dados analíticos, indicadores, dimensões, fatos e pipelines de atualização. | B3; persistência e integração propostas. | Planejado | [F1](work-orders/backlog/fase-f/F1-camada-analitica-de-compras.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F2 | Predictive Analytics | Previsão de Demanda | Projetar demanda por item, categoria, empresa, unidade e período usando histórico disponível. | F1. | Planejado | [F2](work-orders/backlog/fase-f/F2-previsao-de-demanda.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F3 | Predictive Analytics | Previsão de Preços | Estimar tendências e intervalos de preço, deixando explícitos nível de confiança e limitações. | F1 e B4. | Planejado | [F3](work-orders/backlog/fase-f/F3-previsao-de-precos.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F4 | Predictive Analytics | Previsão de Lead Time | Estimar prazo de entrega e probabilidade de atraso por fornecedor, item e contexto. | F1, B1 e B3. | Planejado | [F4](work-orders/backlog/fase-f/F4-previsao-de-lead-time.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F5 | Predictive Analytics | Detecção de Anomalias | Identificar desvios em preço, volume, frequência, fornecedor, pedido e comportamento operacional. | F1. | Planejado | [F5](work-orders/backlog/fase-f/F5-deteccao-de-anomalias.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F6 | Predictive Analytics | Simulação de Cenários | Comparar fornecedores, lotes, prazos, condições, concentração, câmbio e estratégias de compra. | F1 a F5. | Planejado | [F6](work-orders/backlog/fase-f/F6-simulacao-de-cenarios.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| F7 | Predictive Analytics | Analytics Executivo | Consolidar savings, riscos, previsões, eficiência, compliance e oportunidades para gestão. | F1 a F6; D/E propostos. | Planejado | [F7](work-orders/backlog/fase-f/F7-analytics-executivo.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G1 | Marketplace & Integrations | Integration Framework | Criar contratos, adapters, filas, retries, idempotência, telemetria e governança de integrações. | A1; H4/H5 propostos. | Planejado | [G1](work-orders/backlog/fase-g/G1-integration-framework.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G2 | Marketplace & Integrations | Integração ERP de Requisições | Receber requisições e demandas dos diferentes ERPs das unidades de negócio. | G1; ERP a identificar. | Planejado | [G2](work-orders/backlog/fase-g/G2-integracao-erp-de-requisicoes.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G3 | Marketplace & Integrations | Integração ERP de Pedidos | Criar, atualizar e consultar pedidos nos ERPs responsáveis por cada unidade de negócio. | G1 e B3; ERP a identificar. | Planejado | [G3](work-orders/backlog/fase-g/G3-integracao-erp-de-pedidos.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G4 | Marketplace & Integrations | Integração de Notas Fiscais | Consultar e registrar informações de notas fiscais e seu vínculo com pedidos e fornecedores. | G1 e G3. | Planejado | [G4](work-orders/backlog/fase-g/G4-integracao-de-notas-fiscais.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G5 | Marketplace & Integrations | Integração n8n e Workflows Externos | Permitir automações externas governadas, autenticadas, auditáveis e idempotentes. | G1, H1, H2 e H4. | Planejado | [G5](work-orders/backlog/fase-g/G5-integracao-n8n-e-workflows-externos.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G6 | Marketplace & Integrations | Portal e Marketplace de Fornecedores | Permitir interação controlada de fornecedores para cadastro, documentos, propostas e acompanhamento. | B1, G1 e H1/H2. | Planejado | [G6](work-orders/backlog/fase-g/G6-portal-e-marketplace-de-fornecedores.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| G7 | Marketplace & Integrations | Central de Integrações | Gerenciar conexões, credenciais, status, falhas, filas, reprocessamentos e indicadores. | G1 a G6; H4/H5. | Planejado | [G7](work-orders/backlog/fase-g/G7-central-de-integracoes.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H1 | Enterprise Scale & Governance | Identidade Corporativa com Entra ID | Implementar autenticação, claims, grupos, usuários, service principals e integração com Microsoft Entra ID. | A1; tenant Entra a aprovar. | Planejado | [H1](work-orders/backlog/fase-h/H1-identidade-corporativa-com-entra-id.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H2 | Enterprise Scale & Governance | Autorização e Segregação de Funções | Implementar papéis, permissões, políticas, alçadas e segregação por aplicativo e empresa. | H1. | Planejado | [H2](work-orders/backlog/fase-h/H2-autorizacao-e-segregacao-de-funcoes.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H3 | Enterprise Scale & Governance | Multi-Tenancy em Produção | Garantir isolamento lógico, configuração, segurança, dados e operação por empresa e unidade de negócio. | H1 e H2. | Planejado | [H3](work-orders/backlog/fase-h/H3-multi-tenancy-em-producao.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H4 | Enterprise Scale & Governance | Observabilidade Corporativa | Implementar logs, métricas, tracing, alertas, dashboards, SLOs, auditoria operacional e custos de IA. | H1/H2 e infraestrutura proposta. | Planejado | [H4](work-orders/backlog/fase-h/H4-observabilidade-corporativa.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H5 | Enterprise Scale & Governance | Segurança, LGPD e Governança de IA | Implementar classificação de dados, retenção, consentimento, anonimização, controles e governança dos agentes. | H1 a H4. | Planejado | [H5](work-orders/backlog/fase-h/H5-seguranca-lgpd-e-governanca-de-ia.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H6 | Enterprise Scale & Governance | Plataforma Cloud e CI/CD | Preparar Google Cloud, pipelines, ambientes, secrets, infraestrutura como código, backup e recuperação. | H1 a H5; G1. | Planejado | [H6](work-orders/backlog/fase-h/H6-plataforma-cloud-e-ci-cd.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |
| H7 | Enterprise Scale & Governance | Produção, Escala e Operação Assistida | Executar readiness review, testes de carga, runbooks, suporte, continuidade, rollout e acompanhamento produtivo. | Demais capacidades necessárias para produção. | Planejado | [H7](work-orders/backlog/fase-h/H7-producao-escala-e-operacao-assistida.md) | Não executada; sem evidência de conclusão. | Requer aprovação explícita. |

## Entregas históricas fora do catálogo de 56

| Código | Nome | Status | Evidência |
|---|---|---|---|
| A8 | Audience-Specific Publishers | Não comprovado | Referência histórica; confirmar por código/Git antes de considerar concluída. |
| A9 | Publication Engine | Implementado | Código de publicação e histórico Git. |
| A10 | Governance and Work Order Foundation | Implementado | Documentação e histórico Git. |
| A11 | Engineering Blueprint | Implementado | Documento e histórico Git. |
| A12 | Especificação Oficial das 56 Work Orders | Implementado | Catálogo, Work Orders e validações documentais desta sprint. |
| Portal +Compras Frontend | Portal +Compras Frontend | Concluído tecnicamente no frontend (parcial) | Commit `8ee8f4e`, branch `feature/a13-procurement-vertical-slice`; shell/navegação, módulo Fornecedores conectado à API real, demais módulos demonstrativos, Design System AZZAS 2154/GDT aplicado; build frontend aprovado (`tsc`+`vite`, 4/4 testes). Backend `dotnet build`/`dotnet test` não executados neste ciclo por falta de SDK .NET no ambiente de revisão — pendente validação local. Ver `.ai/work-orders/superseded/PortalMaisComprasFrontend.md` (SUPERSEDED pela ADR-0021, decisão D8 — caminho original era `active/`) e `docs/demo/PortalMaisComprasDemo.md`. |
| O1.1 | Consolidação Funcional do +Compras | Concluída | Especificação funcional, UX e modelo de dados completos das telas da Onda 1. Ver [O1.1](work-orders/completed/O1.1-ConsolidacaoFuncionalMaisCompras.md). |
| O1.2.1 | Fundação Física do Frontend Vertical Slice | Concluída (06/08/2026) | Estrutura física Vertical Slice criada (`core/`, `procurement/suppliers/`, `shared/components/`); módulo Fornecedores migrado como referência; build e testes frontend aprovados; comportamento funcional preservado. Ver [O1.2.1](work-orders/completed/O1.2.1-FundacaoFisicaFrontendVerticalSlice.md). Próxima etapa: **O1.2.2**. |
| O1.2.2 | Continuidade da Estrutura Vertical Slice (Onda 1) | Planejado | Aplicar o padrão consolidado em O1.2.1 aos demais módulos/domínios ainda na pasta horizontal `pages/` e às telas de Administração da Onda 1. Depende de O1.2.1. |
| O1.3.1 | Fundação funcional do módulo Gestão de Perfis | Concluída | Vertical Slice `administration/profiles` mockada; RBAC exclusivo por perfil refletido na UI. Work Order retroativa (10/08/2026). Ver [O1.3.1](work-orders/completed/O1.3.1-GestaoDePerfis.md). |
| O1.3.2 | Fundação funcional do módulo Gestão de Usuários | Concluída | Vertical Slice `administration/users` mockada; Work Order aberta retroativamente em 06/08/2026 (Housekeeping Administrativo) a partir de evidência já existente (3 testes pré-existentes de `UsuariosPage.test.tsx`); nesta mesma sprint, `deleteUsuario` (exclusão física) substituído por ativação/inativação, alinhando o módulo ao padrão dos demais módulos administrativos. Ver [O1.3.2](work-orders/completed/O1.3.2-GestaoDeUsuarios.md). |
| O1.3.3 | Fundação funcional do módulo Gestão de Filiais | Concluída (06/08/2026) | Vertical Slice `administration/branches` mockada; regra de cadastro integrado do ERP (ADR-0020, item 3). Ver [O1.3.3](work-orders/completed/O1.3.3-GestaoDeFiliais.md). Próxima etapa: **O1.3.4**. |
| O1.3.4 | Fundação funcional do módulo Gestão de Centros de Custo | Concluída (06/08/2026) | Vertical Slice `administration/cost-centers` mockada; mesma regra de cadastro integrado do ERP (ADR-0020, item 3) e preparação visual (mockada) do relacionamento com Unidade de Alocação (ADR-0020, item 5). Build e 25/25 testes aprovados. Ver [O1.3.4](work-orders/completed/O1.3.4-GestaoDeCentrosDeCusto.md). Próxima etapa: **O1.3.5** (Unidades de Alocação). |
| O1.3.5 | Fundação funcional do módulo Gestão de Unidades de Alocação | Concluída (06/08/2026) | Vertical Slice `administration/allocation-units` mockada; ao contrário de Filiais/Centros de Custo, cadastro completo pelo +Compras (criação, edição, visualização, ativação/inativação, sem exclusão física), sem integração ERP (ADR-0020, item 4). Build e 31/31 testes aprovados; smoke test real em navegador headless aprovado. Aprovada pelo Product Owner em 06/08/2026 na revisão consolidada do Gate Administrativo. Ver [O1.3.5](work-orders/completed/O1.3.5-GestaoDeUnidadesDeAlocacao.md). Pendência remanescente não bloqueante: relacionamento N:N com Centro de Custo (ADR-0020, item 5) ainda não implementado. Próxima etapa: **Autenticação da Onda 1**. |
| O1.4.1 | Security Design Review — Autenticação | Concluída (06/08/2026) | Revisão arquitetural de segurança e threat modeling exigida por ADR-0020 (item 13), sem nenhuma implementação de código. Security Design Gate: aprovado com pendências (catálogo de Perfis/Permissões incl. "Administrador Sênior"; provedor de e-mail para OTP; escopo de `Perfil`/`Permissao`). Ver [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md). Work Order retroativa (10/08/2026): [O1.4.1](work-orders/completed/O1.4.1-SecurityDesignReviewAutenticacao.md). |
| O1.4.1.1 | Formalização da Estratégia de Autenticação em Development | Concluída (07/08/2026) | Complemento da O1.4.1: Development Auth Strategy aprovada com ajustes pelo Product Owner, formalizando `IOtpEmailSender`, `DevelopmentOtpEmailSender` exclusivo de Development com fail-closed fora dele, proibição absoluta de OTP em log/API/UI inclusive em Development, e Authentication Infra Readiness Gate obrigatório antes de Homologação. Sem implementação de código. Ver [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md), seção 17. Bloqueador de provedor de e-mail deixa de impedir início de O1.4.2, passando a Homologação. Work Order retroativa (10/08/2026): [O1.4.1.1](work-orders/completed/O1.4.1.1-FormalizacaoEstrategiaAutenticacaoDevelopment.md). |
| O1.4.2 | Login Passwordless OTP e Sessão Segura | Concluída (07/08/2026) | Vertical Slice de autenticação (backend `Domain/Application/Infrastructure/Api.Identity` + frontend `auth/`), com hardening O1.4.2.1/O1.4.2.2; OTP hash+salt/uso único/rate limiting, sessão server-side, cookie seguro, CSRF/CORS/headers, secure-by-default, `DevelopmentOtpEmailSender` fail-closed fora de Development. Security Implementation Gate III: aprovado com pendências não bloqueantes para Development (carregadas ao Authentication Infra Readiness Gate). Work Order retroativa (10/08/2026): [O1.4.2](work-orders/completed/O1.4.2-LoginPasswordlessOtpESessaoSegura.md). |
| O1.4.3 | Security Design do Bootstrap e Administrador Sênior | Concluída (07/08/2026) | Revisão de segurança do Bootstrap Mode (ADR-0020, item 12), sem implementação de código. Bootstrap Security Design Gate: aprovado com pendências (não bloqueantes). Ver [security-design-auth-o1.4.md](../docs/architecture/security-design-auth-o1.4.md), seção 20. Próxima etapa: Work Order técnica de implementação, condicionada à ratificação do Product Owner/CTO. |
| O1.4.3 (WO técnica) | Work Order Técnica — Bootstrap Mode e Administrador Sênior | **Concluída (10/08/2026)** | Plano executável derivado do Security Design, implementado integralmente em 4 etapas (O1.4.3.1–O1.4.3.4). **Security Validation independente: APROVADA COM RESSALVAS** (0 CRITICAL, 0 HIGH; 4 MEDIUM/6 LOW/5 INFORMATIONAL registrados abaixo). Divergências entre Self-Review e Validação independente reconciliadas sem subir severidade. **Ressalvas aceitas formalmente pelo Product Owner em 10/08/2026** — MEDIUM 1 (`no-store`) e MEDIUM 2 (entropia do Bootstrap Secret) bloqueiam explicitamente a promoção para Homologação. Ver [O1.4.3](work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md). |
| O1.4.3.1 | Fundação Backend do Bootstrap (BootstrapEstado + BootstrapSession) | Concluída (07/08/2026) | `BootstrapEstado`/`BootstrapSessao`, `BootstrapSecretOptions`/`BootstrapAllowedCandidatesOptions` fail-closed, `BootstrapSessionAuthenticationHandler`+política `BootstrapAuthenticated`, endpoints `estado`/`iniciar`/`otp/verificar` (sem `concluir`), índice único de `Perfil`. Build limpo, 369/369 testes aprovados no Mac (.NET SDK 9.0.316). Cadeia de migrations reconciliada (`BaselineFornecedorSnapshot` NO-OP + 4 migrations regeneradas via `dotnet ef`), eliminando duplicação de `CREATE TABLE [Fornecedores]`. Migrations validadas mas **não aplicadas** ao banco compartilhado (aplicação é decisão operacional separada, pendente). Ver `.ai/CURRENT_SPRINT.md`. |
| O1.4.3.2 | Conclusão Transacional e Administrador Sênior | Concluída (10/08/2026) | `ConcluirBootstrapUseCase`: conclusão transacional (um único `SaveChangesAsync`) cria/reaproveita `UnidadeNegocio`, cria o Administrador Sênior (e-mail só da `BootstrapSessao` validada por OTP), cria/reaproveita o `Perfil` "Administrador Sênior", vincula `UsuarioPerfil`, invoca a invariante do último Administrador Sênior ativo, conclui `BootstrapEstado` via compare-and-swap otimista (`RowVersion`). Endpoint `POST /bootstrap/concluir` sob `BootstrapAuthenticated`+CSRF+rate limiting. 388/388 testes aprovados (incl. concorrência real InMemory). Migration `20260810120746_AddBootstrapConclusaoConcurrency` (só `RowVersion`) gerada via `dotnet ef` real, auditada, **validada mas não aplicada** ao banco compartilhado; nenhuma migration histórica/reconciliada alterada. Ver `.ai/CURRENT_SPRINT.md`. Próxima etapa: **O1.4.3.3** (Frontend Bootstrap, não iniciada). |
| O1.4.3.3 | Frontend Bootstrap | Concluída (10/08/2026) | Vertical Slice `frontend/web/src/bootstrap/` (wizard completo: e-mail + Bootstrap Secret + OTP → Unidade de Negócio → Administrador Sênior sem e-mail → confirmação → `POST /bootstrap/concluir`). Suíte de frontend 53/53 aprovada; `tsc -b`/`vite build` limpos. **Encerrada formalmente nesta sessão** após smoke test real completo em Chrome (Chrome DevTools MCP), aprovado pelo Product Owner/CTO: fluxo ponta a ponta até `POST /bootstrap/concluir` → 200 OK, Unidade de Negócio "Grupo Soma" e Administrador Sênior "Julio Cesar" criados, `GET /bootstrap/estado` final → `disponivel:false`. Um 401 de investigação anterior não foi reproduzido; nenhuma causa definitiva foi comprovada; nenhuma correção de segurança foi criada para problema não reproduzido. A Work Order mãe O1.4.3 permanece ATIVA — resta **O1.4.3.4** (Security Self-Review dedicada + Security Validation independente, não iniciada). Ver `.ai/CURRENT_SPRINT.md`. |

## Reconciliação dos 41 entregáveis oficiais da Onda 1 e plano executável (10/08/2026)

Sessão de consolidação e planejamento (10/08/2026), exclusivamente documental — nenhum código, migration, frontend ou backend foi alterado; nenhuma nova sprint foi iniciada. Decisões formais do Product Owner registradas na ADR-0021 (`.ai/DECISIONS.md`), decisões D1–D8. Reconciliação completa dos 41 entregáveis oficiais, matriz de classificação (A: concluído de verdade; B: parcial; C: planejado e necessário) e plano executável completo em [`docs/audits/Onda1-Reconciliacao-e-Plano-Execucao.md`](../docs/audits/Onda1-Reconciliacao-e-Plano-Execucao.md). Nenhum dos 41 entregáveis foi retirado, absorvido ou substituído — a auditoria não encontrou evidência canônica suficiente para isso; a métrica oficial permanece 41 entregáveis / 7 Concluído / 11 Em desenvolvimento / 23 Planejado / 17% de Progresso Técnico (`.ai/dashboard/DASHBOARD_STATE.md`, inalterada nesta sessão).

Novas Work Orders propostas para conclusão da Onda 1, todas em status **Draft (Planejada)** — nenhuma `Approved`/`Active`, nenhuma implementação iniciada:

| Código | Título | Tipo | Entregáveis cobertos | Dependências | Work Order |
|---|---|---|---|---|---|
| O1.5 | RBAC Real (Perfis, Permissões, Policies, Enforcement) | ESTRUTURA | #9, #17 | ✅ **CONCLUÍDA (11/08/2026)** — implementação concluída; Security Validation independente APROVADA COM RESSALVAS (0 CRITICAL/0 HIGH após correções); **ressalvas aceitas formalmente pelo Product Owner em 11/08/2026** (as pendências seguem abertas e rastreadas na tabela abaixo). 477 testes backend + 61 frontend aprovados; mock de Perfis removido; migration aplicada ao banco de desenvolvimento; enforcement real (401/403/200) comprovado por teste de pipeline HTTP e smoke test em Chrome. Ver [rbac-o1.5.md](../docs/architecture/rbac-o1.5.md) | [O1.5](work-orders/completed/O1.5-RbacReal.md) |
| O1.6 | Usuários (Backend Real) | ESTRUTURA | #15, #16 | ✅ **CONCLUÍDA (11/08/2026)** — mock de `administration/users` substituído por backend/persistência reais; vínculo real com Perfis (O1.5) e Centros de Custo; flag "Acesso a todos"; regra do Administrador Sênior reaproveitada do Bootstrap (409 comprovado por teste e por smoke test real); 493 testes backend + 67 frontend aprovados; migration `AddUsuarioGestaoO16` aplicada ao banco de desenvolvimento; sem Security Validation independente dedicada (dívida O1.6-M1, ver abaixo) | [O1.6](work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md) |
| O1.7 | Filiais e Centros de Custo Integrados ao ERP | ESTRUTURA | #14, #18 | ✅ **CONCLUÍDA (11/08/2026)** — mocks de `administration/branches`/`cost-centers` substituídos por integração ERP real (`IFilialErpReader`/`ICentroCustoErpReader`, mesmo padrão de `SomaFornecedorReader`) + metadados locais reais; dívida O1.6-L2 resolvida (vínculo Usuário×Centro de Custo validado contra o ERP e ancorado por Unidade de Negócio); 500 testes backend + 68 frontend aprovados; migration `AddFilialCentroCustoMetadadosO17` gerada (não aplicada — sem VPN nesta sessão); smoke test real contra `SOMA_DESENV` pendente de ambiente com VPN | [O1.7](work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md) |
| O1.8 | Unidades de Alocação (Persistência Real) | ESTRUTURA | #19 | Nenhuma bloqueante | [O1.8](work-orders/backlog/O1.8-UnidadesDeAlocacaoPersistenciaReal.md) |
| O1.9 | Centro de Custo × Unidade de Alocação (N:N) | ESTRUTURA | (condição de fechamento de #18/#19) | O1.7, O1.8 | [O1.9](work-orders/backlog/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md) |
| O1.10 | Conclusão do Vertical Slice (O1.2.2) | ESTRUTURA | #4, #5, #6, #7, #8 | Parcial; integração final depende de O1.5–O1.9 | [O1.10](work-orders/backlog/O1.10-ConclusaoVerticalSlice.md) |
| O1.11 | Fundação Multi-Unidade de Negócio e Configuração | ESTRUTURA + DESIGN | #3, #13, #20, #21, #22, #23, #24 | O1.6 | ✅ **CONCLUÍDA (11/08/2026)** — 7/7 entregáveis implementados e validados; #24 (Notificações) entregue em escopo mínimo de fundação, por decisão formal do Product Owner (sem catálogo de eventos, sem motor de envio) | [O1.11](work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md) |
| O1.12 | Workflow, Aprovação, Alçadas e Controle Orçamentário | ESTRUTURA + DESIGN | #25, #26, #27, #28 | O1.5, O1.9 | [O1.12](work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md) — ✅ Concluída |
| O1.13 | Administração Operacional e Monitoramento | ESTRUTURA + DESIGN | #29, #30, #31, #32 | Nenhuma bloqueante | [O1.13](work-orders/completed/O1.13-AdministracaoOperacionalEMonitoramento.md) — ✅ Concluída |
| O1.14 | Blueprint de Banco e Validação Funcional Final | ESTRUTURA | #36, #37, #38, #39, #40, #41 (+ evolução de #33–#35) | O1.5, O1.6, O1.7, O1.8, O1.9 | [O1.14](work-orders/backlog/O1.14-BlueprintDeBancoEValidacaoFuncionalFinal.md) |

### Ressalvas remanescentes da Security Validation independente da O1.5 (aceitas pelo Product Owner em 11/08/2026)

Registradas como pendências rastreáveis, no mesmo modelo dos findings aceitos da O1.4.3. Nenhuma é bloqueante para Development; nenhuma foi corrigida silenciosamente. **O aceite formal do Product Owner (11/08/2026) encerrou a Work Order O1.5, não estas pendências: todas permanecem ABERTAS e explicitamente rastreadas aqui.** Nenhuma foi removida, ocultada ou reclassificada de severidade pelo aceite.

| # | Severidade | Descrição | Situação |
|---|---|---|---|
| O1.5-M1 | MEDIUM | Checagem da invariante anti-auto-bloqueio não é serializada com a escrita: duas requisições concorrentes inativando os dois últimos Perfis administrativos podem, em teoria, passar ambas. Correção adequada: transação serializável ou `RowVersion` em `Perfil` (padrão já usado em `BootstrapEstado`). | Aberta — exige migration; fora do previsto na Work Order |
| O1.5-L1 | LOW | Backfill do catálogo na migration concede permissões por **nome** de Perfil ("Administrador Sênior") e em todas as Unidades de Negócio, em vez de usar o Id registrado em `BootstrapEstado`. | Aberta — não corrigida deliberadamente: a migration já foi aplicada, e editar migration aplicada recriaria o drift reconciliado na O1.4.3.1 |
| O1.5-L2 | LOW | Nenhuma auditoria append-only de alterações de Perfil/Permissão, embora `ComprasFuncional.md` a exija. | Aberta |
| O1.5-L3 | LOW | Grupo administrativo `/api/administracao` sem rate limiting (as rotas de `/auth` têm). | Aberta |
| O1.5-I1 | INFORMATIONAL | `ClaimTypes.Role` fixo em `"Buyer"` e, em Development, vindo de header. Nenhuma decisão de autorização usa role (nenhum `RequireRole`/`IsInRole` no backend) — resíduo, não vetor. | Aberta |
| O1.5-I2 | INFORMATIONAL | `RequestIdentity.Permissoes` documentado como defesa em profundidade, mas nenhum caso de uso o lê: a policy é a única checagem. | Aberta |
| O1.5-I3 | INFORMATIONAL | Testes de enforcement usam endpoints `/probe-*` sintéticos com a mesma composição de `Program.cs`; não detectam a remoção de `.RequireAuthorization(...)` do controller real. | Aberta |
| O1.5-I4 | INFORMATIONAL | `Fornecedor.*` e `Pedido.*` existem no catálogo, mas nenhum endpoint os exige — os endpoints de Fornecedores/Negociações seguem protegidos apenas por autenticação. **D2 (ADR-0021) não está satisfeita para essas superfícies**, fora do escopo declarado da O1.5. | Aberta |

### Fechamento formal da O1.5 e pendências mantidas em rastreamento (11/08/2026)

A **O1.5 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.5-RbacReal.md)) após o aceite formal das ressalvas pelo Product Owner (Julio Cesar) em 11/08/2026. Fechamento exclusivamente documental — nenhum código funcional, migration, banco ou dado de banco alterado. As pendências abaixo permanecem **abertas e rastreadas**; o aceite do PO **não** as fecha.

| Pendência mantida | Situação | Tratamento previsto |
|---|---|---|
| **Enforcement de `Fornecedor.*` e `Pedido.*`** — códigos existem no catálogo, mas nenhum endpoint os exige; endpoints de Fornecedores/Negociações seguem protegidos apenas por autenticação. **D2 (ADR-0021) satisfeita apenas parcialmente.** | Aberta — **deliberadamente fora do escopo da O1.5** por decisão do Product Owner (11/08/2026); escopo não expandido | Work Order futura dedicada; rastreada também como **O1.5-I4** na tabela acima |
| **Ressalvas de segurança remanescentes**: O1.5-M1 (MEDIUM), O1.5-L1, O1.5-L2, O1.5-L3 (LOW), O1.5-I1..I4 (INFORMATIONAL) | Abertas — aceitas pelo Product Owner como não bloqueantes para Development, **não** removidas | Sprint(s) futura(s); detalhe na tabela "Ressalvas remanescentes" acima |
| **Catálogo definitivo de Perfis de negócio** (quais Perfis existem e o que cada um contém) | Aberta — pendência de **conteúdo do Product Owner**, herdada de O1.4.1 §12. A O1.5 entregou a mecânica e o catálogo de **Permissões** derivado apenas dos códigos já documentados; nenhum Perfil foi inventado. Único Perfil de negócio existente: "Administrador Sênior" (Bootstrap) | Decisão de produto; **não** decidida no fechamento da O1.5 |
| **Nomenclatura da permissão de acesso a Centros de Custo** (`CentroCusto.Acessar`?) | Aberta — deliberadamente **não** incluída no catálogo para não inventar nomenclatura; registrada em `docs/product/ComprasFuncional.md` | Decisão de produto; **não** decidida no fechamento da O1.5 |
| **Quatro Perfis inativos de smoke test no banco de desenvolvimento** (`Analista (O1.5 smoke)`, `Aprovador (smoke UI)`, `Pos-hardening`, `Verificacao pos-hardening`) | **Mantidos deliberadamente** por decisão do Product Owner (11/08/2026): são dados técnicos reutilizáveis em testes futuros. Não removidos; nenhuma exclusão física no modelo; nenhuma migration de limpeza criada. Apenas registrados como dados de teste existentes no ambiente de desenvolvimento | **Atividade futura de saneamento, anterior à promoção para HOMOLOGAÇÃO/REVIEW** — **não** pertence à O1.5 e **não** deve ser executada agora |

**Métrica oficial da Onda 1 recalculada neste fechamento** (regra-fonte: `.ai/dashboard/DASHBOARD_STATE.md`, "Política dos percentuais" — só conta entregável "Concluído"; "Em desenvolvimento" sem percentual individual contribui 0): **41 entregáveis / 8 Concluído / 11 Em desenvolvimento / 22 Planejado / 20% de Progresso Técnico** (19,51% exato). Antes: 7 / 11 / 23 / 17%. Nenhum entregável foi criado, retirado, absorvido ou substituído. Mudanças: **#17 "Perfis, papéis e permissões"** Em desenvolvimento → **Concluído** (a única condição registrada que o mantinha em "Em desenvolvimento" era o aceite formal das ressalvas, agora satisfeito); **#9 "Perfis de usuário simulados"** Planejado → **Em desenvolvimento** (aplicando a reclassificação já recomendada por escrito no `DASHBOARD_STATE.md`; contribui 0, sem percentual individual). **#11 "Módulo de Administração"** permanece Em desenvolvimento (apenas `profiles` deixou de ser mockada).

**O1.6 não foi iniciada:** permanece em [`backlog/O1.6-GestaoDeUsuariosBackendReal.md`](work-orders/backlog/O1.6-GestaoDeUsuariosBackendReal.md), Draft/Planejada — não movida, não aberta, não implementada. É apenas a próxima candidata do caminho crítico. **Nenhuma Work Order está ativa** após este fechamento.

Caminho crítico: O1.5 → O1.6 → (O1.7 ‖ O1.8) → O1.9 → O1.12 → O1.14. Paralelizáveis desde o início: O1.10, O1.13 (e O1.11 após O1.6 iniciar). A ativação da primeira Work Order (O1.5) depende de autorização explícita do Product Owner — nenhuma foi autorizada por esta sessão.

`PortalMaisComprasFrontend.md` (decisão D8): movida de `active/` para o novo diretório canônico `.ai/work-orders/superseded/`, com nota de cabeçalho registrando a ADR-0021 como origem da decisão.

## Dívida técnica registrada — Security Validation independente O1.4.3 (aceita pelo Product Owner em 10/08/2026)

Findings remanescentes do Bootstrap Mode/Administrador Sênior (`.ai/work-orders/completed/O1.4.3-BootstrapEAdministradorSenior.md`, seção 23.1), sem risco CRITICAL/HIGH, registrados para tratamento em sprint(s) futura(s). Nenhum item bloqueia a O1.4.3; os dois primeiros MEDIUM bloqueiam explicitamente a promoção para Homologação.

| # | Severidade | Achado | Gate Homologação |
|---|---|---|---|
| 1 | MEDIUM | `Cache-Control: no-store` ausente em `/bootstrap/*` | **BLOQUEIA** |
| 2 | MEDIUM | Bootstrap Secret sem validação de entropia (valor real é frase memorável) | **BLOQUEIA** |
| 3 | MEDIUM | DoS: throttle por e-mail consumido antes da validação do Bootstrap Secret | Não bloqueia |
| 4 | MEDIUM | Fallback CORS não condicionado a `IsDevelopment()` + `AllowCredentials` | Não bloqueia |
| 5 | LOW | Detecção de índice único por substring de mensagem, sem `SqlException.Number` | Não bloqueia |
| 6 | LOW | Gap de teste permanente do ramo `DuplicateRecordException`/índice único de `Perfil` | Não bloqueia |
| 7 | LOW | `BootstrapSessao` não revogada em falha definitiva de `/bootstrap/concluir` | Não bloqueia |
| 8 | LOW | Allowlist não revalidada no momento da conclusão | Não bloqueia |
| 9 | LOW | Rate limit de `/bootstrap/concluir` por IP, não por `BootstrapSessao` | Não bloqueia |
| 10 | LOW | `/bootstrap/estado` sem rate limiting | Não bloqueia |

Observações informacionais (não convertidas em tarefas): invariante do último Administrador Sênior sem enforcement por contagem real (revisitar ao implementar Gestão de Usuários/Perfis); `DevelopmentRequestIdentity` depende de barreira externa de loopback; ausência de `UseForwardedHeaders()` a considerar no desenho de deploy; estado pós-Bootstrap não verificado empiricamente no banco pelo revisor independente; índice único filtrado de `CodigosVerificacaoOtp` não validado em provider relacional real (mesma pendência já herdada de O1.4.2 para o Authentication Infra Readiness Gate).

## Bug técnico separado — drift de schema em Fornecedores (identificado em 10/08/2026, fora de escopo)

Identificado durante o smoke real da correção de identidade de negócio em Development (não é bug de autenticação/identidade — a identidade já chega corretamente ao caso de uso). `GET /fornecedores?q=...` retorna 500: `SqlException: Invalid column name 'Cnpj'`/`'Nome'`. Drift entre o mapeamento EF (tipos owned) e o schema real do banco local de desenvolvimento. Não corrigido nesta sessão; nenhuma migration ou `database update` executada. Requer investigação dedicada (comparar `FornecedorConfiguration` real contra o schema aplicado) antes de correção.

| # | Severidade | Achado | Bloqueia O1.5? |
|---|---|---|---|
| 1 | MEDIUM | `Invalid column name 'Cnpj'`/`'Nome'` em `/fornecedores?q=...` (drift EF × banco) | Não bloqueia (endpoint pré-existente, fora do escopo de O1.5) |

## Pendências de limpeza — Publication Engine (pós Etapa 3, ADR-0019)

Itens identificados na auditoria da Etapa 3.1, sem risco funcional bloqueante, sem sprint ou Work Order associada — registrados apenas para rastreabilidade futura:

1. **Remover `.ai/content/{executive,client,engineering}/`** — não possui mais consumidores vivos (código, testes, configuração ou documentação); confirmado por busca completa no repositório.
2. **Reclassificar e mover para `resources/`**: `docs/Executive Report.md`, `docs/Product Blueprint.md`, `docs/executive/BlueprintOS_Executive_Blueprint.md` — são conteúdo institucional/executivo/apresentação, não documentação técnica, e atualmente contradizem o escopo de `docs/` definido pela ADR-0019.
3. **Auditar e remover ou justificar** as 13 interfaces órfãs do módulo Documentation (`ITechnicalDocumentationGenerator`, `IFunctionalDocumentationGenerator`, `IAiDocumentationGenerator`, `IDeveloperDocumentationGenerator`, `IChangeLogService`, `IDocumentVersioningService`, `IStaleDocumentationDetector`, `IGitLogReader`, `IDocumentationMemoryNotifier`, `IDocumentationRepository`, `IMermaidDiagramGenerator`, `IAdrService`, `IDocumentationSyncService`) — registradas em DI, sem chamador vivo fora de sua própria implementação/teste.
4. **Documentar `Publication.ExcludedTopLevelDirectories`** em configuração de exemplo (`appsettings.json`) — a propriedade já é configurável via `IOptions`/variável de ambiente sem recompilação, mas não há exemplo de configuração no repositório.

### Fechamento formal da O1.6 e dívidas não bloqueantes registradas (11/08/2026)

A **O1.6 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.6-GestaoDeUsuariosBackendReal.md)). Nenhum achado CRITICAL/HIGH identificado na revisão de segurança própria (não houve Security Validation independente dedicada). Dívidas abaixo registradas como **não bloqueantes**, para revisão consolidada ao final da Onda 1 (após O1.14), conforme a política de dívida técnica desta execução:

| # | Severidade | Descrição | Situação |
|---|---|---|---|
| O1.6-M1 | MEDIUM | Sem Security Validation independente dedicada (revisor logicamente isolado) para a O1.6 — apenas auto-revisão do implementador, como o padrão adotado em O1.4.3/O1.5. O padrão de enforcement (policy por permissão, escopo por Unidade de Negócio, CSRF no grupo) é idêntico ao já validado independentemente na O1.5. | Aberta — recomenda-se cobertura por Security Validation independente consolidada ao final da Onda 1 |
| O1.6-L1 | LOW | `docs/database/Database.md` não recebeu fatia própria de Usuários/Identity nesta sprint; a fatia de banco desta sprint está descrita apenas na própria Work Order (seção "Banco"). | Aberta — tratar na revisão de documentação consolidada da Onda 1 |
| O1.6-L2 | LOW | `IUsuarioRepository.SubstituirCentrosCustoAsync`/vínculo de Centro de Custo continuam por código ERP em texto, sem tabela local — mesmo modelo em uso desde O1.4.2, não introduzido nesta sprint; será substituído pela integração ERP real da O1.7 (D3, ADR-0021). | **Resolvida na O1.7** — `ICentroCustoVinculoValidator` (`CentroCustoVinculoValidator`, Infrastructure) valida cada código ERP contra `ICentroCustoErpReader` e ancora um `CentroCustoMetadado` (novo, tabela `CentrosCustoMetadados`) à Unidade de Negócio do ator antes de qualquer persistência em `UsuariosCentrosCusto`; um código já ancorado a outra Unidade de Negócio é rejeitado (`RbacFalha.CentroCustoInvalido`). Decisão: validação em tempo de execução no caso de uso em vez de FK física — evita exigir que o metadado já exista antes do primeiro vínculo. Ver relatório final da O1.7. |
| O1.7-L1 | LOW | `CentroCusto.unidadeAlocacaoPadraoNome`/`quantidadeUnidadesAlocacaoVinculadas` (tipos de frontend `administration/cost-centers`) ficam sempre indefinidos/zero: representam o relacionamento N:N Centro de Custo × Unidade de Alocação (ADR-0020, item 5), que é escopo explícito da O1.9 — o backend da O1.7 não devolve esse dado, e o teste correspondente (`CentrosCustoPage.test.tsx`) foi removido. | Aberta — a resolver na O1.9 |
| O1.7-L2 | LOW | `administration/users` (formulário de vínculo Usuário×Centro de Custo) consumia `services/costCenterCatalog.ts` (catálogo mockado local) em vez do endpoint real. | **Resolvida na própria O1.7** (revisão pós-implementação) — regressão real identificada: os códigos mockados (`cc-001`…`cc-005`) seriam rejeitados pelo novo `ICentroCustoVinculoValidator`, quebrando a criação/edição de usuário com Centro de Custo específico. `costCenterCatalog.ts` removido; `UsuarioFormPage`/`UsuarioForm`/`UsuarioDetalhesPage` passaram a consumir `GET /api/administracao/centros-custo` real. |
| O1.7-M1 | MEDIUM | Corrida entre requisições concorrentes ancorando o mesmo Centro de Custo (primeiro vínculo Usuário×Centro de Custo, ou primeira edição de metadado por duas Unidades de Negócio diferentes) inicialmente resultaria em `DbUpdateException` não tratada (500) — o índice único global de `CentrosCustoMetadados.CodigoErp` protegia a integridade dos dados, mas não havia tradução para um erro de negócio limpo. | **Resolvida na própria O1.7** (revisão pós-implementação) — `CentroCustoMetadadoRepository.SalvarAlteracoesAsync` agora traduz a violação de índice único para `DuplicateRecordException`; `CentroCustoVinculoValidator` e `AtualizarMetadadoCentroCustoUseCase` capturam essa exceção e retornam `RbacFalha.CentroCustoInvalido`/`ErpMetadadoFalha.AncoradoPorOutraUnidadeDeNegocio` (novo, mapeado para HTTP 409). Teste de regressão adicionado (`AtualizarMetadadoCentroCusto_Should_Reject_When_Anchored_By_Another_Business_Unit`). |
| O1.7-M2 | MEDIUM | O vínculo Usuário×Centro de Custo (`CentroCustoVinculoValidator.ValidarEAncorarAsync`) persiste a âncora do `CentroCustoMetadado` em um `SaveChangesAsync` próprio, antes do `Usuario`/`UsuariosCentrosCusto` serem salvos pelo caso de uso chamador (mesmo `DbContext` por escopo de requisição, mas sem transação explícita compartilhada). Uma falha posterior na criação/atualização do usuário (ex.: corrida de e-mail duplicado) deixaria a âncora do Centro de Custo permanentemente criada sem um usuário correspondente. | Aberta — não bloqueante (não permite corrupção cross-BU nem escalonamento; apenas um registro órfão de metadado local, corrigível por edição manual); considerar envolver ambos os `SaveChangesAsync` em uma transação explícita na revisão consolidada pós-O1.14 |
| O1.7-L3 | LOW | `ListarFiliaisUseCase`/`ListarCentrosCustoUseCase` leem no máximo 5000 linhas do ERP por página (`LimiteLeitura`), sem paginação/continuação e sem sinalizar ao cliente se o limite foi atingido — mesmo padrão de limite fixo já usado em `SincronizarFornecedoresErpUseCase`, mas sem o mecanismo de páginas em lote daquele caso de uso. | Aberta — sem risco funcional enquanto o cadastro real do ERP `SOMA_DESENV` permanecer abaixo de 5000 Filiais/Centros de Custo ativos; revisar na consolidação pós-O1.14 |
| O1.7-L4 | LOW | Duplicação mecânica de código entre os pares Filial/CentroCusto introduzidos nesta sprint (entidades de domínio, repositórios EF, use cases de listar/atualizar metadado, introspecção de schema SQL nos readers `SomaFilialReader`/`SomaCentroCustoReader` — terceira cópia do padrão já duplicado de `SomaFornecedorReader`, e quarta cópia do helper `TryResolverUnidadeNegocio`/tratamento de erro HTTP 401/403 no frontend) — identificada por revisão de código pós-implementação (reuse/simplificação/eficiência). Nenhum bug funcional encontrado. | Aberta — candidata a extração de base comum (`SomaErpReaderBase`, `ErpMetadadoLocal`, `EfErpMetadadoRepository<T>`) na consolidação de qualidade pós-O1.14, quando o terceiro/quarto caso (Unidades de Alocação, O1.8) tornar o padrão definitivo |
| O1.7-I1 | INFORMATIONAL | Listagem de Filiais/Centros de Custo (`GET /api/administracao/filiais`/`/centros-custo`) devolve o catálogo ERP completo para qualquer Unidade de Negócio autenticada (apenas os metadados locais — Ativo/Descrição +Compras — são escopados por Unidade de Negócio); nenhuma escrita cross-BU é possível (Centro de Custo tem âncora global única por código; Filial tem metadado único por par Unidade de Negócio+código). Mesmo padrão de visibilidade já em uso para o catálogo de Fornecedores. Não há decisão de produto registrada que exija filtrar a leitura do catálogo ERP por Unidade de Negócio. | Aberta — decisão de produto pendente sobre se o catálogo ERP de Filiais/Centros de Custo deve ser visível a todas as Unidades de Negócio (padrão atual, replicando Fornecedores) ou filtrado por Unidade de Negócio de origem; não bloqueante, pois não há escrita indevida possível |
| O1.6-I1 | INFORMATIONAL | Usuária de teste "Maria Teste O1.6" (Ativa) criada pelo smoke test real permanece no banco de desenvolvimento — mesmo precedente de dados técnicos aceito na O1.5. Não removida; nenhuma exclusão física criada. | Aberta — saneamento pertence a atividade futura anterior à promoção para HOMOLOGAÇÃO/REVIEW |

**Métrica oficial da Onda 1 recalculada neste fechamento** (mesma regra-fonte): **41 entregáveis / 10 Concluído / 10 Em desenvolvimento / 21 Planejado / 24% de Progresso Técnico** (24,3902% exato = 10 ÷ 41). Antes: 8 / 11 / 22 / 20%. Nenhum entregável foi criado, retirado, absorvido ou substituído. Mudanças: **#15 "Usuários"** Em desenvolvimento → **Concluído**; **#16 "Usuário por Unidade de Negócio"** Planejado → **Concluído**. **#11 "Módulo de Administração"** permanece Em desenvolvimento (`branches`, `cost-centers` e `allocation-units` seguem mockados). **Por instrução expressa do Product Owner nesta sessão, `.ai/dashboard/DASHBOARD_STATE.md` não foi editado** — o Dashboard oficial permanece com os valores do fechamento da O1.5 até a próxima execução de `[atualizar dashboard]` pelo Product Owner.

### Fechamento formal da O1.7 e dívidas não bloqueantes registradas (11/08/2026)

A **O1.7 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.7-FiliaisECentrosDeCustoIntegradosAoErp.md)). Integração ERP real de Filiais e Centros de Custo (`IFilialErpReader`/`SomaFilialReader`, `ICentroCustoErpReader`/`SomaCentroCustoReader`, mesmo padrão de `SomaFornecedorReader`, B2.1/B2.1.2), com metadados locais reais (`FilialMetadado`/`CentroCustoMetadado`, migration `20260811173904_AddFilialCentroCustoMetadadosO17`) e frontend (`administration/branches`/`cost-centers`) consumindo a API real (mocks removidos). A dívida **O1.6-L2** foi resolvida (ver linha acima). Revisão de código pós-implementação (multi-dimensional: correção, segurança, simplificação, reuso, eficiência) encontrou e corrigiu, ainda dentro desta sprint: a regressão de `administration/users` (O1.7-L2, acima) e a corrida de concorrência no vínculo/edição de Centro de Custo (O1.7-M1, acima). Nenhum achado CRITICAL/HIGH remanescente. Dívidas abaixo (e acima, O1.7-M2/L3/L4/I1) registradas como **não bloqueantes**, para revisão consolidada ao final da Onda 1 (após O1.14):

**Backend:** `dotnet build backend/BlueprintOS.sln` — 0 erros/0 avisos. `dotnet test backend/BlueprintOS.sln` — **500 aprovados** (499 unitários + 1 novo teste de regressão cross-BU; baseline O1.6 493 → +7 novos em `FilialCentroCustoUseCasesTests.cs`) + **7 integração** (early-return sem VPN/`ErpConnection`, mesmo padrão de `SomaFornecedorSynchronizationIntegrationTests`), 0 falhas.

**Frontend:** `npm run build` (`tsc -b`/`vite build`) aprovado. `npm test` — **68 aprovados**, 0 falhas (inclui o ajuste do teste de `administration/users` para consumir o endpoint real de Centros de Custo em vez do catálogo mockado removido).

**Chrome/MCP:** dispensado nesta sprint — a cobertura automatizada (testes unitários dos use cases/validador, testes de integração HTTP real do frontend simulando 401/403/200, builds limpos de backend e frontend) foi considerada suficiente para os critérios de aceite da Work Order; nenhum comportamento visual/interativo introduzido exigia validação manual. Smoke test real contra o ERP `SOMA_DESENV` (listar Filiais/Centros de Custo reais) não foi executado — ambiente sem VPN corporativa disponível nesta sessão; os nomes de tabela/coluna do ERP (`CADASTRO_CLI_FOR`, `CENTRO_CUSTO`) são suposições configuráveis com introspecção dinâmica de schema (mesmo padrão de `SomaFornecedorReader`), não confirmadas contra o schema real — risco herdado da mesma dependência ambiental já registrada em B2.1.3/O1.7 (ver Work Order, seção "Riscos").

**Métrica oficial da Onda 1 recalculada neste fechamento** (mesma regra-fonte): **41 entregáveis / 12 Concluído / 8 Em desenvolvimento / 21 Planejado / 29% de Progresso Técnico** (29,2683% exato = 12 ÷ 41). Antes: 10 / 10 / 21 / 24%. Nenhum entregável foi criado, retirado, absorvido ou substituído. Mudanças: **#14 "Empresas e filiais"** Em desenvolvimento → **Concluído**; **#18 "Centros de Custo"** Em desenvolvimento → **Concluído**. **#11 "Módulo de Administração"** permanece Em desenvolvimento (apenas `allocation-units` segue mockado, escopo da O1.8). **Por instrução expressa do Product Owner nesta sessão, `.ai/dashboard/DASHBOARD_STATE.md` não foi editado** — o Dashboard oficial permanece com os valores do fechamento da O1.6 até a próxima execução de `[atualizar dashboard]` pelo Product Owner.

**O1.8 não foi iniciada:** permanece em [`backlog/O1.8-UnidadesDeAlocacaoPersistenciaReal.md`](work-orders/backlog/O1.8-UnidadesDeAlocacaoPersistenciaReal.md), Draft/Planejada — não movida, não aberta, não implementada. **Nenhuma Work Order está ativa** após este fechamento.

### Fechamento formal da O1.8 (11/08/2026)

A **O1.8 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.8-UnidadesDeAlocacaoPersistenciaReal.md)). Backend/persistência real de Unidade de Alocação (`Domain/Application/Infrastructure/Api.Identity`, mesmo padrão físico de `Usuario`/O1.6, migration `AddUnidadeAlocacaoO18`) e frontend (`administration/allocation-units`) consumindo a API real (mock removido). Sem vínculo com Centro de Custo (ADR-0020, item 5 — escopo da O1.9) e sem integração ERP (ADR-0020, item 4). Nenhum achado CRITICAL/HIGH.

**Backend:** `dotnet build backend/BlueprintOS.sln` — 0 erros/0 avisos. `dotnet test` — **512 aprovados** (unitários, +13 novos em `UnidadeAlocacaoUseCasesTests.cs`) + **7 integração**, 0 falhas. Migration aplicada ao banco de desenvolvimento (`dotnet ef database update`).

**Frontend:** `npm run build` (`tsc -b`/`vite build`) aprovado. `npm test` — **72 aprovados** (9 arquivos), 0 falhas. Campo "Unidade de Negócio" removido do formulário (correção necessária: a Unidade de Negócio é sempre resolvida pelo backend a partir da sessão, nunca pelo cliente — mesmo cuidado de Usuário/Perfil; não é redesign).

**Chrome/MCP:** dispensado — cobertura automatizada de backend e frontend (incluindo 401/403, isolamento cross-BU, unicidade por Unidade de Negócio e ausência de exclusão física) suficiente para os critérios de aceite; nenhuma interação visual complexa ou vínculo com Centro de Custo está em escopo nesta sprint.

**Métrica oficial da Onda 1 recalculada neste fechamento** (mesma regra-fonte): **41 entregáveis / 13 Concluído / 7 Em desenvolvimento / 21 Planejado / 31,7073% de Progresso Técnico** (exibido **32%**; 13 ÷ 41). Antes: 12 / 8 / 21 / 29%. Mudança: **#19 "Unidades de Alocação"** Em desenvolvimento → **Concluído**. Contribuição da Onda 1 ao MVP: 5,85 → **6,34 pontos** (20% × 31,7073%). Percentual Global do MVP 1.0: 32,85% exato → **33,34%** exato (Foundation 20,0 + Onda 1 6,34 + Onda 2 7,0), exibido **33%**. **Por instrução expressa do Product Owner, `.ai/dashboard/DASHBOARD_STATE.md` não foi editado** — o Dashboard oficial permanece D-1, atualizado pelo Product Owner via `[atualizar dashboard]`.

**O1.9 não foi iniciada:** permanece em [`backlog/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md`](work-orders/backlog/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md), Draft/Planejada — não movida, não aberta, não implementada. **Nenhuma Work Order está ativa** após este fechamento.

### Fechamento formal da O1.9 (11/08/2026)

A **O1.9 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.9-CentroDeCustoXUnidadeDeAlocacaoNN.md)). Relacionamento N:N real entre Centro de Custo e Unidade de Alocação (`CentroCustoUnidadeAlocacao`, migration `AddCentroCustoUnidadeAlocacaoO19`), satisfazendo D4/ADR-0021 e preservando a regra de Unidade de Alocação padrão (ADR-0020, item 6: no máximo uma por Centro de Custo, índice único filtrado). Referencia `CentroCustoMetadado` (identidade canônica local já estabelecida na O1.7) e `UnidadeAlocacao` (O1.8) — nenhuma segunda fonte canônica criada. Nenhum achado CRITICAL/HIGH remanescente.

**Backend:** `dotnet build backend/BlueprintOS.sln` — 0 erros/0 avisos. `dotnet test` — **524 aprovados** (unitários, +12 novos em `CentroCustoUnidadeAlocacaoUseCasesTests.cs`) + **7 integração**, 0 falhas. Migration aplicada ao banco de desenvolvimento (`dotnet ef database update`). Corrigido durante a sprint: `CentroCustoUnidadeAlocacaoRepository.SalvarAlteracoesAsync` não traduzia violação de índice único (corrida de ancoragem concorrente) para erro de negócio — corrigido para o mesmo padrão de `CentroCustoMetadadoRepository` (O1.7).

**Frontend:** `npm run build` (`tsc -b`/`vite build`) aprovado. `npm test` — **74 aprovados** (9 arquivos), 0 falhas. `administration/cost-centers` (formulário e detalhes) passa a exibir/editar o vínculo real com Unidades de Alocação (catálogo real de O1.8, sem catálogo mockado), incluindo seleção de padrão. `CentroCustoDto.UnidadeAlocacaoPadraoNome`/`QuantidadeUnidadesAlocacaoVinculadas` deixam de ser sempre indefinido/zero — dívida da O1.7 **resolvida**.

**Chrome/MCP:** dispensado — cobertura automatizada de backend e frontend (incluindo isolamento cross-BU, unicidade de padrão, padrão fora do vínculo, corrida de ancoragem concorrente, e o fluxo real de checkbox/rádio no frontend) suficiente para os critérios de aceite.

**Impacto em Usuários (O1.6):** nenhum. O vínculo Usuário×Centro de Custo (autorização de acesso) permanece propositalmente independente do vínculo Centro de Custo×Unidade de Alocação (classificação gerencial) — ADR-0020, itens 6/9.

**Métrica oficial da Onda 1 — inalterada neste fechamento**: **41 entregáveis / 13 Concluído / 7 Em desenvolvimento / 21 Planejado / 32% de Progresso Técnico**. Nenhum entregável isolado é atribuído à O1.9 — #18 e #19 já estavam Concluídos (O1.7/O1.8); a O1.9 satisfaz D4 (ADR-0021) como condição de fechamento pleno desses dois entregáveis, não como entregável isolado da lista de 41 (conforme a própria Work Order registra). **Por instrução expressa do Product Owner, `.ai/dashboard/DASHBOARD_STATE.md` não foi editado.**

**Observação para planejamento (Agents Linx):** decisão do Product Owner de incluir na Onda 1 uma fundação para agentes especialistas Linx (ERP/Database Specialist, memória persistente, RAG, sem SQL livre de IA em produção) — preservada para O1.10–O1.14. Nenhum conflito ou dependência evidente identificado entre essa necessidade e o que já foi implementado em O1.5–O1.9; não implementada nesta sprint (fora de escopo, conforme instrução explícita).

**O1.10 não foi iniciada:** permanece em `backlog/`, Draft/Planejada — não movida, não aberta, não implementada. **Nenhuma Work Order está ativa** após este fechamento.

### Planejamento dos Agents Especialistas Linx (11/08/2026)

Antes de abrir a O1.10, análise obrigatória das cinco Work Orders restantes da Onda 1 (O1.10–O1.14), para
decidir onde encaixar a nova decisão do Product Owner de incluir, ainda na Onda 1, a fundação para os Agents
especialistas Linx ERP Specialist e Linx Database Specialist (memória persistente, proveniência do
conhecimento, sem SQL livre de IA em produção — ver prompt da sessão).

**Mapa das Work Orders analisadas:**

- **O1.10** — Conclusão do Vertical Slice (O1.2.2): migração estrutural pura de pastas horizontais restantes
  (Negociações, Configurações, Pedidos) para Vertical Slice + Shell/Menu refletindo Administração real.
  Nenhuma dependência de conhecimento ERP.
- **O1.11** — Fundação Multi-Unidade de Negócio e Configuração: Seleção/Cadastro de BU, Identity Providers,
  Configuração de ERP **por BU** (qual ERP usar, não o schema dele), Parâmetros gerais, Feature Flags,
  Notificações. Depende de O1.6.
- **O1.12** — Workflow, Alçadas, Aprovação e Controle Orçamentário (estrutura configurável, não motor
  operacional — esse é Onda 3). Depende de O1.5 e O1.9.
- **O1.13** — Administração Operacional e Monitoramento: telas de monitor/auditoria consumindo dados já
  reais de sincronização de Fornecedores (B2.1.3). Sem dependência bloqueante.
- **O1.14** — Blueprint de Banco e Validação Funcional Final: consolidação documental do que O1.5–O1.9 já
  implementaram (Matriz tela×campo×entidade, Matriz +Compras×ERP, mapeamento de APIs/integrações). Depende
  de O1.5–O1.9; **não deve ser redigida isoladamente antes das implementações reais** (D7).

**Onde começam integrações ERP mais profundas:** nenhuma das cinco. A única integração ERP real hoje é
Fornecedor (`SomaFornecedorReader`, B2.1/B2.1.2) e Filial/Centro de Custo (`SomaFilialReader`/
`SomaCentroCustoReader`, O1.7) — ambas por introspecção dinâmica de schema, já em produção nesta base de
código. Item e Pedido integrados ao Linx **não aparecem em nenhuma das O1.10–O1.14**: O1.12 exclui
explicitamente "regras de negócio de orçamento por processo de compra" para a Onda 3; O1.13 exclui
explicitamente integrações "que ainda não existem (ex.: Nota Fiscal, Pagamento)" para a Onda 4. Ou seja: as
integrações que mais precisariam do conhecimento profundo dos Agents Linx (Item, Pedido) estão fora da
Onda 1 por decisão já registrada — a fundação dos Agents antecipa a base de conhecimento, sem antecipar as
próprias integrações.

**Decisão de encaixe:** nenhuma das cinco Work Orders tem objetivo naturalmente compatível com esta fundação
(A = não), e incorporá-la a qualquer uma delas distorceria seu escopo declarado (C = não recomendado). Não
há dependência dura que exija a fundação **antes** de O1.10–O1.13 (B = não bloqueante) — é paralelizável.
Recomenda-se uma **nova Work Order própria** (D), posicionada entre O1.13 e O1.14 na sequência (E), seguindo
a mesma convenção de numeração hierárquica já usada no projeto para inserir etapas sem renumerar Work Orders
existentes (ex.: O1.4.1–O1.4.3.4): **O1.13.5 — Fundação dos Agents Especialistas Linx (Conhecimento
ERP/Banco)**, criada em `.ai/work-orders/backlog/O1.13.5-FundacaoAgentsEspecialistasLinx.md`, status
Draft/Planejada, **não iniciada, não aprovada**. Dependências arquiteturais (F): nenhuma bloqueante;
reaproveita o padrão de introspecção dinâmica de schema já validado em B2.1/O1.7; deve concluir antes do
fechamento formal da O1.14 (para que o blueprint final possa opcionalmente referenciá-la), mas não é
dependência dura de nenhuma das O1.10–O1.13.

**MVP proposto para a fundação (G):** base de conhecimento persistente e versionada (`LinxKnowledgeEntry` ou
equivalente) com proveniência explícita (Descoberto/Inferido/Validado/Aprovado), mecanismo de recuperação
por busca (RAG mais sofisticado fica para evolução futura, sem exigir redesenho), acesso READ-ONLY
controlado ao `SOMA_DESENV`, e RBAC real protegendo a promoção de conhecimento a "Aprovado". Os dois papéis
de Agent (ERP Specialist, Database Specialist) ficam definidos como consumidores/produtores dessa base —
nenhuma execução autônoma de SQL de escrita, nesta ou em qualquer Work Order futura sem governança própria.

**Numeração:** proposta como O1.13.5 por analogia às sub-etapas já existentes no projeto (O1.4.1–O1.4.3.4);
se o Product Owner preferir outra posição/numeração na sequência restante da Onda 1, a Work Order criada
pode ser renomeada/reposicionada antes de qualquer aprovação — nada foi iniciado, aberto ou implementado.

**Impacto na sequência restante da Onda 1:** nenhum bloqueio identificado. A O1.13.5 pode ser executada em
paralelo a O1.10–O1.13, a qualquer momento a partir de agora, sem impedir a execução desta sessão (O1.10).

### Fechamento formal da O1.10 (11/08/2026)

A **O1.10 está formalmente CONCLUÍDA** ([Work Order](work-orders/completed/O1.10-ConclusaoVerticalSlice.md)). Migração estrutural pura das duas últimas pastas horizontais do frontend (`pages/Negociacoes` → `negotiations/pages`, `pages/Configuracoes` → `settings/pages`, via `git mv`) — a pasta `pages/` deixa de existir. `procurement/orders/pages/PedidosPage.tsx` já estava em Vertical Slice, sem ação estrutural pendente. `core/AppShell.tsx` atualizado para filtrar os itens de menu de Usuários/Filiais/Centros de Custo/Unidades de Alocação pela permissão RBAC real correspondente, mesmo padrão já aplicado a Perfis desde a O1.5 (reaproveitadas as constantes existentes de `PERMISSOES`, nenhuma permissão nova). Nenhum achado CRITICAL/HIGH — superfície alterada é puramente frontend/estrutural.

**Frontend:** `npm run build` (`tsc -b`/`vite build`) aprovado. `npm test` — **74 aprovados** (9 arquivos), sem regressão. Investigados `CnpjSearch`/`ApprovalPanel`/`CadastroFornecedor` (classificados "Parcial" na auditoria original) — já possuem tratamento adequado de loading/erro; a classificação estava desatualizada, nenhuma dívida real encontrada.

**Backend:** nenhuma alteração — escopo da O1.10 é puramente frontend/estrutural, conforme a Work Order.

**Chrome/MCP:** dispensado — build e suíte automatizada suficientes para uma migração estrutural pura e uma checagem de visibilidade de menu já comprovada pelo mesmo padrão em Perfis.

**Métrica oficial da Onda 1 — inalterada neste fechamento**: **41 entregáveis / 13 Concluído / 7 Em desenvolvimento / 21 Planejado / 32% de Progresso Técnico**. Nenhum entregável passa a "Concluído": #4 "Shell principal", #5 "Menu e navegação completa", #6 "Dashboard inicial", #7 "Frontend mockado navegável" e #8 "Estados de loading/vazio/sucesso/erro" avançam mas permanecem "Em desenvolvimento" — Negociações/Pedidos/Indicadores/Agentes IA/Configurações continuam demonstrativos (mock), como já documentado; a implementação de domínio real desses módulos pertence a Ondas futuras. **Por instrução expressa do Product Owner, `.ai/dashboard/DASHBOARD_STATE.md` não foi editado.**

**O1.11 não foi iniciada:** permanece em `backlog/`, Draft/Planejada — não movida, não aberta, não implementada. **A eventual Work Order O1.13.5 (Fundação dos Agents Especialistas Linx) permanece em `backlog/`, Draft/Planejada — não iniciada, não aprovada.** Nenhuma Work Order está ativa após este fechamento.

## O1.11 — Fundação Multi-Unidade de Negócio e Configuração — ABERTA E PARCIALMENTE CONCLUÍDA (11/08/2026)

Movida de `backlog/` para `active/`. Implementados e validados (backend + frontend, build/testes verdes)
os entregáveis **#3** Seleção de Unidade de Negócio, **#13** Cadastro de Unidades de Negócio, **#20**
Identity Providers por UN, **#21** Configuração de ERP por UN, **#22** Parâmetros gerais, **#23** Feature
Flags — todos protegidos pelas permissões RBAC corporativas já existentes (`UnidadeNegocio.Gerenciar`,
`ConfiguracaoErp.Gerenciar`, `Sistema.Gerenciar`), sem confiar em `UnidadeNegocioId` vindo do corpo da
requisição para autorização, com segredos (Identity Provider/ConfiguracaoErp) cifrados via
`IDataProtector` e nunca devolvidos pela API. **Não implementado:** **#24** Configuração de Notificações
— sem especificação funcional/UX aprovada (`ComprasFuncional.md`/`ComprasUX.md` marcam explicitamente
como pendência), condição exigida pelos próprios critérios de aceite da Work Order antes de qualquer
implementação. **O1.11 permanece em `active/` — não foi fechada.** Revisão de segurança sem achados
CRITICAL/HIGH. Backend: 555 testes aprovados (548 unitários + 7 integração). Frontend: 88 testes
aprovados (14 arquivos), typecheck e build aprovados. Progresso da Onda 1/MVP global mantido inalterado
nesta sessão (41 entregáveis / 13 Concluído / 7 Em desenvolvimento / 21 Planejado / 32% exibido; MVP 33%
exibido) — a reclassificação de #3/#13/#20/#21/#22/#23 para "Concluído" e o recálculo consequente ficam
para o fechamento formal da O1.11. **O1.12 permanece em `backlog/`, Draft/Planejada — não iniciada.**
**O1.13.5 permanece em `backlog/`, Draft/Planejada — não iniciada, não aprovada.**

### Fechamento formal da O1.11 (11/08/2026) — decisão do Product Owner sobre #24 e conclusão 7/7

**Decisão formal do Product Owner sobre o item #24 (Configuração de Notificações):** não retirar da O1.11;
implementar em **escopo mínimo de fundação** — configuração administrativa persistente por Unidade de
Negócio (ativado/inativado do canal e-mail, e-mail remetente com validação de formato, nome do remetente),
sem motor operacional de notificações (sem SMTP/envio real/filas/workers/templates/histórico) e sem
catálogo de eventos configuráveis (verificado: não existe documentação formal aprovada com o conjunto de
eventos em `docs/product/`, `.ai/work-orders/` ou `DECISIONS.md` — será endereçado quando os workflows
operacionais correspondentes existirem; o modelo de dados não fecha essa evolução futura).

Implementado nesta sessão: `ConfiguracaoNotificacao` (Domain, relação 1:1 com `UnidadeNegocio`, mesmo
padrão físico de `ConfiguracaoErp`), migration `AddConfiguracaoNotificacaoO111` (aditiva), endpoints
`GET`/`PUT /api/administracao/unidades-negocio/{id}/configuracao-notificacao` protegidos por
`Sistema.Gerenciar` (reaproveitada, nenhuma permissão nova) + CSRF, e módulo frontend
`administration/notification-configuration`. `unidadeNegocioId` sempre do path (nunca da sessão/corpo da
requisição), mesma barreira de isolamento cross-BU dos demais 6 módulos da O1.11.

**Testes re-executados nesta sessão:** Backend — `dotnet build` limpo; `dotnet test` **557 testes
unitários + 7 de integração**, 0 falhas (9 testes novos de `ConfiguracaoNotificacaoUseCases`, sem
regressão). Frontend — `npm run build` (`tsc -b`/`vite build`) limpo; `npm run test` **98 testes** em 17
arquivos, 0 falhas (4 testes novos de `ConfiguracaoNotificacaoPage`, sem regressão).

**Segurança:** revisão focada na nova superfície (#24). Nenhum achado CRITICAL/HIGH. Mesma barreira de
autorização (`RequireAuthorization` por policy, nunca por nome de Perfil), mesmo padrão de path-based BU
scoping, CSRF nas mutações, sem mass assignment (`Id` sempre gerado no servidor), validação de e-mail no
backend reaproveitando `EmailUsuarioValidator` já auditado na O1.6. A dívida LOW já conhecida da O1.11
(propósito de criptografia compartilhado no `DataProtection`) não é afetada — esta entidade não usa
`ISegredoProtector` — e permanece deferida ao Gate Final pós-O1.14.

**Chrome/MCP:** dispensado — funcionalidade administrativa simples, evidência suficiente pelos testes
automatizados de backend e frontend (mesmo raciocínio já aplicado às demais 6 sub-telas desta Work Order).

**A O1.11 está formalmente CONCLUÍDA (7/7 entregáveis).** Work Order movida de `active/` para
`completed/` ([O1.11](work-orders/completed/O1.11-FundacaoMultiUnidadeDeNegocioEConfiguracao.md)).

**Métrica oficial da Onda 1 recalculada neste fechamento** (mesma regra-fonte,
`.ai/dashboard/DASHBOARD_STATE.md`, "Política dos percentuais" — só conta entregável "Concluído"):
**41 entregáveis / 20 Concluído / 7 Em desenvolvimento / 14 Planejado / 48,7805% de Progresso Técnico**
(exibido **49%**; 20 ÷ 41). Antes: 13 / 7 / 21 / 32%. Mudança: **#3** Seleção de Unidade de Negócio,
**#13** Cadastro de Unidades de Negócio, **#20** Identity Providers por UN, **#21** Configuração de ERP
por UN, **#22** Parâmetros gerais, **#23** Feature Flags e **#24** Configuração de Notificações — todos
Planejado → **Concluído** (nenhum destes 7 estava contado no balde "Em desenvolvimento" antes deste
fechamento — a métrica interina da O1.11 permaneceu deliberadamente congelada em 13/7/21 durante toda a
execução parcial, por decisão de recalcular apenas no fechamento formal; o balde "Em desenvolvimento"
(7 itens, ex.: #4/#5/#6/#7/#8/#9/#11) é inalterado por esta Work Order). Contribuição da Onda 1 ao MVP:
6,34 → **9,7561 pontos**
(20% × 48,7805%). Percentual Global do MVP 1.0: 33,34% exato → **36,7561%** exato (Foundation 20,0 +
Onda 1 9,7561 + Onda 2 7,0), exibido **37%**. **Por instrução expressa do Product Owner,
`.ai/dashboard/DASHBOARD_STATE.md` não foi editado** — o Dashboard oficial permanece D-1, atualizado pelo
Product Owner via `[atualizar dashboard]`.

**Dívidas não bloqueantes registradas nesta sessão:** catálogo de eventos configuráveis por notificação
permanece pendente de documentação formal de produto — a ser endereçado em Work Order futura quando os
workflows operacionais correspondentes existirem. Nenhuma dívida anterior da O1.11 foi resolvida ou
alterada nesta etapa (dívida LOW de `DataProtection` permanece deferida ao Gate Final pós-O1.14).

**O1.12 NÃO foi iniciada** (permanece em `backlog/`, Draft/Planejada, dependente de O1.11 ter fechado —
condição agora satisfeita, mas abertura formal requer nova autorização explícita). **O1.13.5 NÃO foi
iniciada** (permanece em `backlog/`, Draft/Planejada, não aprovada). **O Gate Final da Onda 1** (auditoria
consolidada dos 41 entregáveis, GAPs, dívidas, hardening, validação integrada e revisão de Design
consolidada, após O1.11→O1.12→O1.13→O1.13.5→O1.14) **não foi antecipado** — permanece planejado para
depois da conclusão de todas as Work Orders remanescentes da Onda 1, conforme já registrado neste
documento.

## O1.12 — Workflow, Alçadas, Aprovação e Controle Orçamentário — CONCLUÍDA (11/08/2026)

**A O1.12 está formalmente CONCLUÍDA.** Escopo entregue: fundação configurável real (sem mock) de
`RegraWorkflow`, `AlcadaAprovacao` e `RegraOrcamentaria` — persistência EF Core/SQL Server (migration
`20260811215629_AddAdministracaoWorkflowAlcadaOrcamentoO112`, aplicada ao banco de Desenvolvimento
corporativo), RBAC real (3 novas permissões: `Workflow.Gerenciar`, `Alcada.Gerenciar`,
`Orcamento.Gerenciar`), isolamento Multi-BU (O1.11) e referência real a Centro de Custo (O1.9) — nenhum
motor de aprovação operacional ou integração ERP implementado, conforme "Fora de escopo" da própria Work
Order. Gap de integração identificado e corrigido durante a execução: o frontend usava `codigoErp` (ERP)
em vez do Guid interno de `CentroCustoMetadado` nos seletores de Alçadas/Orçamento — corrigido expondo
`CentroCustoMetadadoId` (campo aditivo) no `CentroCustoDto` já existente. Duas revisões de segurança
independentes, focadas exclusivamente nas mudanças desta sprint, não encontraram nenhum achado
CRITICAL/HIGH/MEDIUM. Backend: 564 → 621 testes (+57), todos verdes; frontend: 98 → 110 testes (+12),
todos verdes; builds limpos. Work Order movida de `active/` para `completed/`
([O1.12](work-orders/completed/O1.12-WorkflowAprovacaoAlcadasOrcamento.md)).

**Métrica oficial da Onda 1 recalculada neste fechamento** (mesma regra-fonte,
`.ai/dashboard/DASHBOARD_STATE.md`, "Política dos percentuais" — só conta entregável "Concluído"):
**41 entregáveis / 24 Concluído / 7 Em desenvolvimento / 10 Planejado / 58,5366% de Progresso Técnico**
(exibido **59%**; 24 ÷ 41). Antes: 20 / 7 / 14 / 49%. Mudança: **#25** "Estrutura de Workflow", **#26**
"Configuração de alçadas", **#27** "Estrutura de aprovação" e **#28** "Estrutura de controle
orçamentário" — todos Planejado → **Concluído** (evidência de entrega 100% do escopo de ESTRUTURA, não do
motor operacional, que pertence à Onda 3 e não foi antecipado). Contribuição da Onda 1 ao MVP: 9,7561 →
**11,70732 pontos** (20% × 58,5366%). Percentual Global do MVP 1.0: 36,7561% exato → **38,70732%** exato
(Foundation 20,0 + Onda 1 11,70732 + Onda 2 7,0), exibido **39%**. `[atualizar dashboard]` **não** foi
executado nesta sessão, conforme instrução permanente da Work Order — o Dashboard oficial permanece D-1,
atualizado pelo Product Owner.

**Dívidas novas registradas (não bloqueantes, para o Gate Final da Onda 1):** catálogo de `TipoProcesso`
(RegraWorkflow) e de critério de negócio de Alçada permanecem pendência de produto; fonte de verdade do
saldo orçamentário (RegraOrcamentaria) não definida — ambos fora de escopo desta sprint, pertencem à Onda
3/4. Nenhuma dívida anterior estava diretamente na superfície modificada por esta sprint.

**O1.13 NÃO foi iniciada** (permanece em `backlog/`, Draft/Planejada). **O1.13.5 NÃO foi iniciada**
(permanece em `backlog/`, Draft/Planejada, não aprovada). **O Gate Final da Onda 1 não foi antecipado** —
permanece planejado para depois de O1.13→O1.13.5→O1.14, conforme já registrado neste documento.

## O1.13.5 — Fundação dos Agents Especialistas Linx — ABERTA E CONCLUÍDA (11/08/2026)

Movida de `backlog/` para `active/` e, na mesma sessão, para `completed/`, com aprovação explícita do
Product Owner. Implementados os critérios de aceite integrais da Work Order: base de conhecimento
persistente e versionada (`LinxKnowledgeEntry`) com proveniência explícita (Descoberto/Inferido/Validado/
Aprovado — máquina de estados que nunca pula etapa nem permite auto-aprovação); mecanismo de recuperação
funcional (busca MVP por especialista/categoria/BU/tags/texto); RBAC dedicado (`ConhecimentoLinx.Gerenciar`/
`ConhecimentoLinx.Aprovar` — a promoção a "Aprovado" nunca é concedida pela mesma permissão de registro);
leitor read-only de descoberta de schema do `SOMA_DESENV` (`LinxSchemaDiscoveryReader`), comprovadamente
incapaz de escrita por teste de reflexão sobre o contrato (nenhum método fora do vocabulário
Buscar/Listar/Obter pode existir); os dois papéis de Agent (`LinxErpSpecialistAgent`/
`LinxDatabaseSpecialistAgent`) implementados como consumidores dessa base, com defesa testada contra prompt
injection/knowledge poisoning. Migration `AddLinxKnowledgeO1135` (aditiva) aplicada ao banco de
desenvolvimento. Backend: 626 → 682 testes unitários (+56), 7 integração inalterado, 689/689 aprovados;
build limpo. Revisão de segurança sem achados CRITICAL/HIGH.

**Reconciliação da Onda 1:** confirmado — nenhum dos 41 entregáveis oficiais corresponde diretamente a esta
fundação (decisão já registrada acima, "Planejamento dos Agents Especialistas Linx"). Progresso técnico da
Onda 1 permanece inalterado nesta sessão (mesma baseline de entrada). `[atualizar dashboard]` não foi
executado.

**Work Order:** movida para `.ai/work-orders/completed/O1.13.5-FundacaoAgentsEspecialistasLinx.md`.

**A O1.14 NÃO foi iniciada** (permanece em `backlog/`, Draft/Planejada). **O Gate Final da Onda 1 NÃO foi
antecipado.**
