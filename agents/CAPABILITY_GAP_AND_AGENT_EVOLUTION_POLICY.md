# Capability Gap And Agent Evolution Policy

Versao: 1.0
Status: accepted
Escopo: qualquer IA, modelo, executor ou Agent operando no SOMA BlueprintOS, independente de provider.
Precedencia: referenciada por `agents/AGENT_CONTRACT.md` e `agents/EXECUTION_POLICY.md`. Formaliza e detalha, sem contradizer, a secao "Capability Gap" ja existente em `agents/EXECUTION_POLICY.md`.

## Principio Central

Quando uma solicitacao nao e coberta pelo conhecimento ou pelas capabilities dos Agents existentes, a IA **nao improvisa por fora do ecossistema de Agents**. Ela para, classifica o gap e segue o fluxo abaixo.

## Fluxo

```text
REQUEST -> REGISTRY -> AGENT OWNER? -> CAPABILITY COBERTA? -> KNOWLEDGE SUFICIENTE?
```

1. **REQUEST**: a solicitacao e recebida pela IA orquestradora.
2. **REGISTRY**: identifica-se o dominio/capability machine-readable envolvida (`capability_ownership` dos manifestos).
3. **AGENT OWNER?**: existe um Agent com `ownership: primary` para essa capability?
4. **CAPABILITY COBERTA?**: o `agent.yaml` do owner declara essa capability em `capabilities`/`capability_ownership`?
5. **KNOWLEDGE SUFICIENTE?**: o Agent owner possui conhecimento validado (schema, runbook, codigo, esclarecimento previo) para agir com seguranca?

## Knowledge Gap

Quando a capability existe mas o conhecimento e insuficiente:

1. declarar explicitamente o que falta;
2. investigar fontes autorizadas (schema real, metadata, documentacao, codigo do repositorio);
3. consultar essas fontes quando o acesso for permitido e somente leitura;
4. perguntar ao usuario/Product Owner quando a fonte autorizada nao resolver;
5. aprender e validar (ver `USER_ARTIFACT_LEARNING_POLICY.md`);
6. persistir apenas conhecimento reutilizavel, com proveniencia, sem segredo;
7. so continuar com evidencia suficiente. **Nunca inventar** dado, schema, grade, regra de negocio ou resultado.

Knowledge Gap interrompe o fluxo de execucao ate ser resolvido ou até o usuário decidir explicitamente aguardar (`WAITING_FOR_EVIDENCE`).

## Capability Gap

Quando nenhum Agent existente declara a capability necessaria:

1. declarar o gap explicitamente (capability ausente, dominio, por que os Agents existentes nao servem);
2. verificar se o Agent mais proximo em responsabilidade e um owner natural para evoluir;
3. propor evolucao do Agent existente (novo conhecimento, capability adicional coerente com sua responsabilidade atual);
4. mostrar o impacto da mudanca (capability, seguranca, escrita, acesso, tests);
5. exigir autorizacao humana explicita para qualquer mudanca material;
6. so entao usar Agent Factory `UPDATE` (nunca editar o manifesto a mao para contornar a Factory);
7. reauditar apos a mudanca (`AUDIT` da Agent Factory v2).

Capability Gap tambem interrompe o fluxo ate resolucao. Nenhum bypass e permitido em nenhuma hipotese — `direct_bypass_allowed` permanece `false` para todos os Agents.

## Ausencia De Owner Natural

Quando nenhum Agent existente e owner adequado, mesmo apos avaliar evolucao:

1. declarar formalmente a ausencia de owner;
2. explicar, um a um, por que os Agents existentes nao servem (responsabilidade, dominio, dados, risco);
3. propor um novo Agent, cobrindo: responsabilidade, capabilities, dados/sistemas, tools, riscos, relacao com Security/LGPD, e necessidade de escrita;
4. pedir autorizacao humana explicita;
5. **somente depois** da autorizacao, usar Agent Factory `CREATE`.

Nenhum novo Agent pode nascer de forma silenciosa ou automatica.

## Ordem De Preferencia

```text
APRENDER (Knowledge Gap resolvido)
  > EVOLUIR Agent existente (Capability Gap resolvido com owner coerente)
    > CRIAR novo Agent (somente quando nenhum owner existente e adequado)
```

Um Agent nao deve se transformar em "faz tudo": evoluir um Agent existente so e apropriado quando a nova capability continua coerente com a responsabilidade declarada em `responsibility.objective`/`responsibilities` do seu manifesto. Quando a nova capability pertence claramente a outro dominio, a resposta correta e delegar/propor um Agent novo, nao inflar o Agent mais conveniente.

## Autoexpansao Proibida

Nenhum Agent, incluindo `agent-factory`, pode:

- autoexpandir suas proprias capabilities sensiveis, de escrita, de destruicao ou de acesso;
- habilitar bypass ou reduzir approval/participacao transversal por conta propria;
- promover `enforcement_status` para `ENFORCED` sem controle tecnico real;
- criar ou registrar um novo Agent sem autorizacao humana explicita registrada (`approved`, `approved_by`, `approved_at`).

`gap_policy.material_capability_change_requires_human_approval` e `gap_policy.explicit_human_approval_required_for_new_agent` permanecem `true` (fixados pelo schema) para todos os Agents atuais e futuros.

## Heranca Por Agents Futuros

Todo Agent novo nasce aderente a esta politica e a `USER_ARTIFACT_LEARNING_POLICY.md`, porque:

1. `agents/AGENT_CONTRACT.md` e `agents/EXECUTION_POLICY.md` referenciam ambas as politicas como fontes canonicas obrigatorias (ver secao "Politicas Canonicas Relacionadas" em cada um);
2. `agent.schema.json` ja fixa `gap_policy.direct_bypass_allowed = false`, `delegation.bypass_allowed = false`, `gap_policy.explicit_human_approval_required_for_new_agent = true` e `gap_policy.material_capability_change_requires_human_approval = true` para todo manifesto valido — nao ha necessidade de alterar o schema para herdar estas politicas, pois o schema ja e estruturalmente compativel com elas;
3. a Agent Factory v2 (`tools/agents/agent-factory-v2.js`) verifica, em `AUDIT`, que estes dois documentos existem e estao referenciados pelo `AGENT_CONTRACT.md` (checagem `AFV2-POLICY-001`), sinalizando WARNING caso a referencia canonica seja removida.

Nenhuma mudanca estrutural em `agent.schema.json` ou `AGENT_CONTRACT.md` foi necessaria para herdar estas politicas nos Agents atuais ou futuros — a heranca ocorre por referencia documental obrigatoria (precedencia 1-2) mais checagem automatizada da Factory, sem alterar semantica de campo existente.
