# Canonical AI Execution Policy

Versao: 1.1
Status: accepted
Escopo: qualquer IA, modelo, executor, Agent ou ferramenta operando no SOMA BlueprintOS.

## Regra Global

Toda tarefa pertencente ao dominio ou capability de um Agent registrado deve ser delegada, conduzida ou validada pelo Agent responsavel declarado em `agents/<agent-id>/agent.yaml`.

A IA principal atua como orquestradora. Conhecimento tecnico da IA, disponibilidade de uma ferramenta ou permissao do sistema operacional nao concedem autorizacao para contornar Agents, governanca ou permissoes da identidade efetiva.

## Precedencia

1. `agents/EXECUTION_POLICY.md`: politica global de execucao.
2. `agents/AGENT_CONTRACT.md`: semantica e estrutura dos Agents.
3. `agents/agent.schema.json`: validacao machine-readable.
4. `agents/<agent-id>/agent.yaml`: ownership, limites e configuracao do Agent.
5. Knowledge, prompts, runbooks, scripts e codigo referenciados pelo manifesto.

Uma fonte especifica nao pode remover silenciosamente um guardrail global. Excecoes precisam ser explicitas, versionadas, justificadas, aprovadas por humano autorizado e tecnicamente verificaveis. Nenhuma excecao esta ativa no Contract v1.1.

## Orquestracao E Delegacao Obrigatoria

O orquestrador deve:

1. identificar o dominio e a capability machine-readable;
2. localizar o Agent com ownership `primary`;
3. delegar ou obter validacao desse Agent quando `delegation_required` for `true`;
4. incluir Agents `cross_cutting` quando os criterios declarados forem satisfeitos;
5. produzir `ActionProposal` quando a operacao exigir;
6. submeter a proposta ao `AIGovernancePolicyEngine` e ao `ApprovalPolicy` quando aplicavel;
7. verificar a identidade e sua permissao efetiva;
8. executar apenas por Tool/Adapter governado disponivel;
9. validar o resultado e registrar auditoria.

Sem Runtime Registry ou Tool Gateway universal, parte desse fluxo permanece documental. Isso nao transforma ausencia de enforcement tecnico em permissao de bypass.

## No Direct Bypass

Quando existe Agent responsavel, o orquestrador e outros Agents nao podem contorna-lo por SQL, MCP, shell, script, Python, `pyodbc`, HTTP, API, browser automation ou qualquer ferramenta externa.

`direct_execution_by_others_allowed: false` e `bypass_allowed: false` sao obrigatorios para os Agents atuais. Habilitar bypass e mudanca arquitetural material e exige excecao explicita, revisao de seguranca e autorizacao humana. O validator v1.1 rejeita bypass nos manifests atuais.

## Agents Transversais

Um Agent `cross_cutting` nao assume ownership primario dos dominios revisados. Ele participa quando seus criterios forem satisfeitos.

O `security-lgpd-agent` e transversal e consultivo. Ele interpreta riscos de seguranca, privacidade e LGPD; nao substitui a decisao deterministica do `AIGovernancePolicyEngine`, a verificacao do `ApprovalPolicy` nem um futuro Tool Gateway. Quando a policy exigir sua participacao, ele nao pode ser ignorado.

## Capability Gap

Ausencia de conhecimento, evidencia, tool governada ou permissao nao autoriza bypass. O Agent deve parar e registrar `CAPABILITY GAP` contendo:

- Agent responsavel;
- capability necessaria;
- conhecimento e evidencia disponiveis;
- o que nao esta conhecido ou comprovado;
- motivo pelo qual nao e seguro continuar;
- alternativas permitidas.

A ordem de tratamento e:

1. avaliar evolucao do Agent existente;
2. avaliar ownership natural por outro Agent existente;
3. propor novo Agent somente quando as duas opcoes anteriores forem inadequadas.

## Evolucao E Criacao De Agents

Uma proposta para ensinar ou evoluir Agent existente deve explicar o gap, conhecimento necessario, fonte e proveniencia; atualizar fontes canonicas e manifesto quando a capability mudar; e passar pelo validator. Mudanca material de capability, seguranca, escrita ou acesso exige autorizacao humana explicita.

Novo Agent nunca pode ser criado silenciosamente. A proposta deve listar problema, Agents avaliados, justificativa, dominio, responsabilidades, non-goals, tools, sistemas, dados, risco, relacao com Security/LGPD e necessidade de escrita. A criacao so pode ocorrer apos autorizacao humana explicita. Agent Factory v2 continua fora deste escopo.

Nenhum Agent pode autoexpandir privilegios, habilitar escrita ou destruicao, reduzir approval, remover participacao de Security/LGPD, habilitar bypass, elevar enforcement sem evidencia ou conceder acesso adicional a si mesmo.

## Credenciais E Conexoes

Segredos nunca pertencem ao Git, manifesto, knowledge, runbook, relatorio, log, prompt, catalogo ou audit trail. Isso inclui senha, token, cookie, API key, client secret, private key, credencial pessoal/corporativa e connection string com segredo.

Um `connection_profile` identifica o recurso logico. Ele pode declarar nome de configuracao ou referencia de secret, driver, protocolo, ambiente, intent de leitura e classificacao sustentada por evidencia. Ele nunca carrega credencial. Host, porta e database so podem ser versionados quando sua classificacao permitir; na duvida, devem permanecer fora do manifesto como `AINDA_NAO_MAPEADO`.

Cada usuario usa sua propria identidade. A execucao deve respeitar a permissao efetiva dessa identidade, com `least_privilege: true` e `privilege_escalation_allowed: false`.

```text
EXECUCAO PERMITIDA
= PERMISSAO EFETIVA DA IDENTIDADE
+ POLICY/GOVERNANCE DO BLUEPRINTOS
```

As duas autorizacoes sao independentes e obrigatorias. Se a policy aprovar e a identidade nao possuir permissao, a execucao deve parar e informar: "A acao foi considerada permitida pela governanca, mas a identidade atual nao possui permissao suficiente."

E proibido tentar outro usuario, procurar credencial mais privilegiada, usar credencial de colega, alterar `GRANT`/role, pedir senha administrativa ou contornar o sistema alvo.

## Secret Storage

Estrategia preferencial:

- macOS: Keychain, por adapter futuro;
- Windows: Credential Manager, por adapter futuro;
- outros ambientes: secret store seguro equivalente;
- .NET Development: User Secrets existente;
- CI/Homologacao/Producao: secret manager da plataforma/corporativo;
- fallback: arquivo local ignorado pelo Git, criado vazio a partir de template sem segredo.

Integracoes completas com Keychain/Credential Manager ainda nao existem. Variaveis de ambiente continuam aceitas onde ja sao usadas, mas nao devem ser impressas, persistidas ou compartilhadas.

Agents nunca devem pedir que o usuario digite senha, token ou cookie no chat. Devem orientar cadastro diretamente no mecanismo local seguro e nunca ecoar o valor.

## Novo Clone

Quando uma conexao for necessaria e a credencial local nao existir, o Agent deve identificar o profile, verificar somente os mecanismos locais suportados, orientar a configuracao da credencial propria sem recebe-la no chat e parar. Depois da configuracao pelo usuario, deve testar a conexao com operacao minima e seguir apenas com as permissoes efetivas encontradas.

O Agent nao procura segredo no Git, nao usa credencial compartilhada e nao preenche automaticamente arquivos locais com segredo.

## Cenarios Normativos

### UPDATE No SOMA De Producao

Identificar capability e owner; obter analise do specialist; registrar Capability Gap se faltar semantica; incluir Security/LGPD; produzir `ActionProposal`; passar por Policy Engine e Approval; verificar profile, identidade e permissao; usar Tool/Adapter governado; validar e auditar. Na ausencia de Tool/Adapter governado, parar. Nunca executar SQL direto.

### Agent Nao Conhece A Tabela

Parar e produzir Capability Gap. Propor evoluir Agent existente, delegar a outro owner ou justificar novo Agent. Novo Agent exige autorizacao humana explicita.

### Identidade Sem Permissao

Mesmo com Policy Engine `APPROVED`, um `permission denied` encerra a tentativa. Nao elevar privilegio nem trocar identidade.

### Novo Clone Sem Credencial

Identificar o profile, orientar configuracao local segura sem pedir segredo no chat e parar ate o usuario concluir. Depois, testar acesso minimo usando a identidade do usuario.
