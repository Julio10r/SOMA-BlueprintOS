# ProductionAuthoritativeInvestigationPolicyV1 — Producao Como Fonte De Verdade + SOMA_DESENV Como Laboratorio

Status: accepted
Data: 2026-08-27
Escopo: `agents/DATABASE_CONNECTION_POLICY.md`, `agents/AGENT_CONTRACT.md`, `agents/linx-database-specialist-agent/agent.yaml`, `agents/linx-erp-specialist-agent/agent.yaml`, `docs/audits/AgentLearningV1-LinxProgOpPed.md`, `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`.

> **Correção (2026-08-27, etapa posterior):** referências a `192.168.0.200` como servidor de produção
> neste documento estavam incorretas — o endpoint SQL real de produção é `192.168.9.200:1433`. Ver
> `docs/audits/LinxProductionEndpointCorrectionV1.md`. Nenhum conteúdo abaixo foi apagado.

## 1. Motivacao

Durante a investigacao do caso PROG/OP/PED (`docs/audits/AgentLearningV1-LinxProgOpPed.md`), o Agent investigou corretamente `SOMA_DESENV` e encontrou um Knowledge Gap real (secao 7.6.3): a grade `36-44` cadastrada para os produtos da planilha nao inclui o tamanho `34`, apesar da planilha ter uma coluna `Q_34` com quantidades reais.

O Product Owner/especialista Linx esclareceu que `SOMA_DESENV` **nao e um espelho 100% atualizado de producao**: a estrutura costuma ser semelhante, mas objetos e principalmente dados podem estar desatualizados; procedures alteradas em Development nao refletem automaticamente em Production (passam por validacao antes); objetos em Development podem ter sido esquecidos por desenvolvedores. Isso significa que o achado do tamanho 34 nao pode ser tratado, por si so, como um problema funcional confirmado do Linx — pode ser um drift de cadastro entre ambientes, nao a realidade de producao.

## 2. Gap Detectado

A politica anterior (`agents/DATABASE_CONNECTION_POLICY.md` v1, secoes 1-16) ja distinguia corretamente `linx-development`/`SOMA_DESENV` de `linx-production`/`SOMA` para fins de credencial, mismatch de conexao e governanca de escrita, mas **nao declarava explicitamente qual ambiente e a fonte de verdade para investigacao do estado atual** do ERP, nem exigia proveniencia de evidencia por ambiente, nem tratava drift Development/Production como uma categoria de conhecimento distinta de "problema confirmado".

## 3. Politica Anterior (v1)

- DEV (`SOMA_DESENV`) e laboratorio permissivo para desenvolvimento/teste/experimentacao (secao 11).
- Producao (`SOMA`) e conservadora, escrita governada (secao 12).
- Selecao de ambiente por intencao ("analisar" vs "atualizar"), sem regra explicita de qual ambiente responde "como e hoje" (secao 5).
- Nenhuma proveniencia obrigatoria de evidencia por ambiente.
- Nenhuma nocao formal de drift Development/Production.
- Nenhuma politica formal de reproducao controlada PROD->DEV.

## 4. Politica Nova (v1.1)

Adicionada em `agents/DATABASE_CONNECTION_POLICY.md` secoes 17-23:

- **Authoritative source**: Producao responde "como e hoje?"; Development responde "como ficaria?" (secao 17).
- **Laboratorio DEV**: `SOMA_DESENV` continua permissivo para desenvolvimento/teste (secao 11, inalterada), mas nunca e tratado como fonte automatica de verdade de producao (secao 17).
- **Investigacao read-only em Producao**: por padrao, apenas `SELECT`/metadata/definicao de objeto; nao executar procedure mutavel so para observar comportamento (secao 18).
- **Evidence provenance**: toda evidencia de banco carrega o ambiente de origem (`CONFIRMED_IN_PRODUCTION`/`CONFIRMED_IN_DEVELOPMENT` ou equivalentes); conhecimento so-DEV nunca vira automaticamente conhecimento atual de producao (secao 19).
- **Producao indisponivel**: aplica o retry unico ja existente (secao 7); se continuar indisponivel, `PRODUCTION_VALIDATION_PENDING`, nunca fallback silencioso para DEV (secao 20).
- **Objetos para desenvolvimento**: ler versao atual em Producao antes de assumir que `SOMA_DESENV` esta atualizado (secao 21).
- **Reproducao PROD->DEV controlada**: fluxo conceitual com minimizacao de dados, classificacao LGPD, aprovacao/governanca e rastreabilidade; explicitamente **on-demand controlled reproduction**, nunca replicacao automatica (secao 22). Marcado **DOCUMENTED, nao ENFORCED** — nenhum codigo implementa isto ainda.
- **Agent Factory / novos agents**: novos Agents que usem banco devem conhecer estas 4 nocoes; herdado hoje via `AGENT_CONTRACT.md` + `knowledge.update_rules` de cada Agent (secao 23).

## 5. Authoritative Source, Laboratorio DEV, Evidence Provenance, Drift, PROD->DEV, LGPD, Minimizacao

Ver `agents/DATABASE_CONNECTION_POLICY.md` secoes 17-22 (texto integral, nao duplicado aqui para evitar divergencia entre dois documentos).

## 6. Governanca

Uma copia PROD->DEV envolve leitura (Producao) e escrita (Development) — a permissividade de DEV nao elimina a governanca sobre a origem. Segredo (senha/token/secret/credential) nunca e copiado para DEV (aplicacao da secao 4 ja existente, nao excecao a ela). Nenhuma copia foi executada nesta tarefa.

## 7. Enforcement Atual Vs Documental

| Regra | Status |
|---|---|
| Environment mismatch bloqueado antes de qualquer I/O (`EnvironmentMismatch`) | **ENFORCED** — `B1ConnectivityValidator`/`LinxConnectionStringResolver`, testes existentes em `B1ConnectivityValidatorTests.cs` |
| Producao indisponivel nao cai silenciosamente para DEV (retry unico, sem loop) | **ENFORCED** — mesmo validador, ja cobre o mecanismo de retry/`ConnectivityUnavailable`; esta tarefa apenas proibe reinterpretar esse status como licenca para trocar de ambiente (documental) |
| Investigacao de estado atual seleciona Producao por padrao | **DOCUMENTED** — decisao de qual profile usar continua sendo do Agent/orquestrador/humano, nao ha selecao automatica de profile por "tipo de intencao" no codigo |
| Evidencia DEV nao e promovida automaticamente a conhecimento de producao | **DOCUMENTED** — reforcado nos `agent.yaml` dos dois Agents Linx (`knowledge.update_rules`) e no aprendizado reclassificado do caso PROG/OP/PED; nenhum enforcement de codigo existia ou foi criado para isto |
| Copia PROD->DEV exige origem/destino explicitos, segredo nunca copiado, classificacao protegida exige governanca | **PLANNED** — nenhum codigo de copia existe neste repositorio; secao 22 e puramente uma politica para uma capacidade futura |

Nenhum teste artificial foi criado para as regras puramente documentais desta tarefa, conforme instrucao explicita de nao inventar testes quando a regra e apenas documental nesta camada.

## 8. Reclassificacao Do Aprendizado PROG/OP/PED

`docs/audits/AgentLearningV1-LinxProgOpPed.md` e `-Results.json` foram atualizados (nao apagados):

- Nota de reclassificacao adicionada no topo do `.md` e como campo `reclassification_note` no `.json`.
- Secao 7.6.3 (tamanho 34 ausente da grade `36-44`) reclassificada de "Knowledge Gap bloqueante" (implicitamente tratado como problema funcional) para **`DEVELOPMENT_PRODUCTION_DRIFT_SUSPECTED` + `PENDING_PRODUCTION_VALIDATION`** — o gap continua bloqueante para gerar solucao/SQL, mas a interpretacao muda: e uma divergencia suspeita entre ambientes, nao um fato confirmado do Linx.
- Tabela de gaps (secao 7.9) e `knowledge_gaps[3]` no JSON atualizados com o novo status e o proximo passo correto (validar em Producao, read-only, antes de necessariamente perguntar ao Product Owner).
- Grades 36-44, ausencia do tamanho 34, procedures, schema e dados encontrados em `SOMA_DESENV` (secoes 7.6-7.6.5) permanecem no documento como `CONFIRMED_IN_DEVELOPMENT` — validos como leitura de Development, apenas nao mais equiparados a verdade de producao.
- O zero-delta / nao-encontrados (77/77 sem correspondencia em `SOMA_DESENV`, secao 7.6.5) ja usava `PENDING_PRODUCTION_READ_ONLY_VALIDATION`, alinhado com o espirito da politica nova; mantido.

## 9. Gaps Futuros

- Nenhum mecanismo de codigo hoje seleciona automaticamente Producao como padrao de investigacao — depende do Agent/orquestrador seguir a politica documentada. Um Capability/Tool Gateway futuro que padronize "investigar estado atual" poderia enforced isso automaticamente, mas nao existe ainda.
- Nenhum mecanismo de codigo para reproducao PROD->DEV existe; a politica (secao 22) e apenas o desenho para quando essa capacidade for proposta e aprovada.
- A validacao real do gap 7.6.3 em Producao continua pendente e fora do escopo desta tarefa (proxima rodada, ver secao 22 da tarefa original que gerou este documento).

## 10. Secret Scan

Varredura manual sobre todos os arquivos criados/alterados por esta tarefa (`agents/DATABASE_CONNECTION_POLICY.md`, `agents/AGENT_CONTRACT.md`, `agents/linx-database-specialist-agent/agent.yaml`, `agents/linx-erp-specialist-agent/agent.yaml`, `docs/audits/AgentLearningV1-LinxProgOpPed.md`, `docs/audits/AgentLearningV1-LinxProgOpPed-Results.json`, este arquivo). Nenhum segredo/credencial real encontrado. IPs (`192.168.0.200`, `192.168.9.98`) e nomes de banco (`SOMA`, `SOMA_DESENV`) permanecem conforme politica existente (nao sao segredo).

## 11. Confirmacoes

- Nenhuma escrita em banco foi executada.
- Nenhuma copia PROD->DEV foi executada.
- Nenhum objeto de banco foi alterado.
- Nenhum DML/DDL foi executado.
- `node tools/agents/validate-agent-manifests.js` reexecutado apos as mudancas de `agent.yaml`: **PASS** (8 manifests validos, sem bypass/privilege escalation/segredo detectado).
- Nenhum push realizado.
