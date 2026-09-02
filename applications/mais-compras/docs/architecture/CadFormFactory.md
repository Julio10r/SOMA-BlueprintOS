# CadFormFactory

Guia operacional para criar ou evoluir qualquer tela de cadastro integrada ao ERP no +Compras (Materiais, Serviços, Categorias, Compradores, Centros de Custo, e futuros).

**Implementação de referência:** Fornecedores (Gate aprovado em 01/09/2026 — ver [Gate-PreB29-AdapterLinxFornecedor.md](./Gate-PreB29-AdapterLinxFornecedor.md) e `.ai/work-orders/completed/B2.9-AdapterLinxFornecedorCnpj.md`). Este documento extrai o **processo**, não as regras específicas de Fornecedor — nenhuma regra funcional de Fornecedor (obrigatoriedade de campo, direção de sincronização, etc.) deve ser tratada como universal. Cada novo cadastro repete o discovery do zero.

Quando o Product Owner pedir "crie o cadastro de X seguindo o CadFormFactory", execute o fluxo abaixo, adaptando cada etapa à entidade real — não pule etapas por similaridade aparente com Fornecedor.

## Fluxo da Factory

```
NOVO CADASTRO
  → Discovery funcional (Linx)
  → Discovery de banco (Linx)
  → Evidências registradas
  → Matriz de autoridade
  → Contrato funcional
  → UX padrão (Design System)
  → Implementação
  → Testes (matriz mínima)
  → Gate técnico
  → Homologação (Product Owner)
  → Workflow integrado futuro (validação end-to-end com o caso de uso completo)
```

Nenhuma etapa substitui a anterior. Implementação sem discovery documentado é retrabalho garantido.

## 1. Discovery funcional e de banco

Todo cadastro relacionado ao ERP começa por discovery, nunca por leitura de tela isolada:

```
Orchestrator → Linx ERP Specialist Agent → Linx Database Specialist Agent → evidências → contrato funcional → implementação
```

**Linx ERP Specialist Agent** investiga, quando aplicável ao cadastro em questão: tela Visual Linx, PRG, SCX/SCT, objetos de entrada/customização, procedures, regras funcionais, obrigatoriedades reais (não assumidas), validações, estados, dependências entre campos, e comportamento de inclusão/edição/inativação. Princípio: **Linx é referência funcional, não modelo de UX** — não copiar a tela antiga, extrair a regra de negócio por trás dela.

**Linx Database Specialist Agent** comprova (nunca infere só pela interface): tabelas, chaves, relacionamentos, campos, tipos, constraints, triggers, procedures, tabelas auxiliares, regras de ativo/inativo, efeitos reais de INSERT/UPDATE, campos protegidos, efeitos colaterais.

Toda descoberta vira evidência registrada em `docs/audits/Discovery-<Cadastro>-*.md` e, quando aplicável, unidade de conhecimento estruturada em `agents/knowledge/<dominio>/*.source.json` (proveniência `Descoberto`/`Inferido`, nunca `Validado`/`Aprovado` sem decisão humana).

## 2. Matriz de autoridade

Antes de implementar, defina explicitamente para cada campo/estado relevante do novo cadastro:

| Pergunta | Resposta obrigatória |
|---|---|
| Quem cria? | +Compras, ERP, ou ambos |
| Quem edita? | +Compras, ERP, ou ambos |
| Quem tem autoridade final? | por campo, não por entidade inteira |
| Quem pode inativar? | +Compras, ERP, ou ambos |
| Direção de sincronização | +Compras→ERP, ERP→+Compras, ou bidirecional |
| Resolução de conflito | timestamp, prioridade de origem, ou regra explícita |

**Nunca assuma sincronização simétrica por padrão.** Fornecedores é o exemplo didático de que a regra pode ser assimétrica (ex.: inativação tem autoridade apenas do ERP para o +Compras) — isso não significa que todo cadastro terá essa mesma assimetria; cada cadastro tem sua própria matriz, levantada no discovery.

## 3. Contrato funcional

Antes de codar, escreva (ou atualize uma Work Order/ADR com) o contrato funcional resultante do discovery: campos, obrigatoriedades reais confirmadas em código Linx, regras de duplicidade/identidade, e a matriz de autoridade acima. Esse contrato é o que a implementação segue — não o discovery bruto.

## 4. Duplicidade e identidade

Antes de implementar, definir: chave funcional da entidade, duplicidade local (+Compras), duplicidade no ERP, comportamento sob concorrência (duas criações simultâneas), e o que fazer quando o registro já existe no ERP sem existir localmente (ou vice-versa). **Nunca assumir "não existe localmente" = "não existe no ERP".**

## 5. UX padrão

Estude a implementação **final aprovada** de Fornecedores como referência de padrão visual (estrutura de página, grid, espaçamento, cards, labels, required, máscaras, campos dependentes, disabled/read-only, loading, empty state, badges, filtros, erros inline, modal de confirmação, footer, botões primário/secundário, ação de inativação, responsividade, acessibilidade) — mas **não copie CSS ou componentes locais**. Primeiro identifique o que já existe em `shared/design-system/` e nos componentes compartilhados do frontend (`shared/components/*`); reutilize. Se Fornecedores tiver um padrão útil ainda implementado localmente que deveria virar componente compartilhado, registre a oportunidade (não refatore sem autorização do Product Owner).

## 6. Padrão de modais e mensagens

Defina, para o novo cadastro, as mensagens de: confirmação, sucesso, erro, conflito, duplicidade, indisponibilidade externa (ERP fora do ar), falha de integração, operação bloqueada. Regras fixas, para qualquer cadastro:

- Usuário vê mensagem clara, em PT-BR, sem detalhe técnico (nunca exception, stack trace, SQL ou mensagem interna em inglês).
- Log/auditoria carrega o detalhe técnico completo para diagnóstico.
- Sucesso só é informado depois da confirmação real da operação — nunca antes.

## 7. RBAC by design

Todo cadastro novo nasce com RBAC — não é adicionado depois. Antes de implementar, mapear as ações (visualizar, criar, editar, aprovar, inativar, ações especiais) e decidir quais permissões reais são necessárias (só criar permissão nova se as existentes não cobrirem). Regra fixa:

- **Frontend:** sem permissão → ação não aparece.
- **Backend:** sem permissão → `403`.
- Autenticação sem autorização nunca é suficiente.

## 8. Validações

Separar sempre:

- **Frontend:** validação para UX rápida e amigável.
- **Backend/negócio:** validação de integridade real — a API nunca pode permitir bypass de uma regra obrigatória do cadastro manual só porque a chamada veio de outro lugar.

Por outro lado, **não aplique automaticamente as mesmas validações de cadastro manual** a importação, hidratação, sincronização ERP ou processos internos — cada use case/fronteira tem responsabilidade de validação própria, definida no contrato funcional.

## 9. Integração

Diferencie semanticamente as operações — nunca esconda operações diferentes atrás de um "sucesso" genérico:

`CRIAR` · `ATUALIZAR` · `GARANTIR EXISTÊNCIA` · `ADICIONAR PAPEL/VÍNCULO` · `SINCRONIZAR` · `INATIVAR`

Para escrita crítica no ERP, aplicar o padrão:

```
WRITE → READ BACK → VALIDATE → COMMIT → só então "Sincronizado/Sucesso"
```

Em falha: rollback quando aplicável, estado consistente, auditoria/log, mensagem amigável — **nunca falso sucesso**.

## 10. Falhas e resiliência

Todo cadastro integrado precisa de cenários de falha cobertos: ERP indisponível, timeout, API externa indisponível, conflito, duplicidade, falha parcial, erro de persistência. Verificação central em qualquer revisão: **falha nunca pode virar falso sucesso.**

## 11. Test Factory (matriz mínima)

**Unit:** regras, validações, mapeamentos, estados, duplicidade, erros.

**Backend/API:** 401, 403, autorizado, payload válido, payload inválido, tentativa de bypass de validação de frontend, duplicidade, status/inativação, integração.

**Frontend:** renderização, required, máscaras, campos dependentes, RBAC visual, loading, erro, sucesso, modal, filtros/status.

**ERP (quando aplicável):** +Compras → ERP e ERP → +Compras, sempre validando o dado real no destino. `HTTP 200`, toast ou badge **não são prova suficiente** de integração real.

## 12. Gate técnico (checklist mínimo)

```
[ ] Discovery funcional realizado
[ ] Linx ERP Agent consultado
[ ] Linx Database Agent consultado
[ ] Evidências registradas
[ ] Matriz de autoridade definida
[ ] Contrato funcional definido
[ ] Layout padrão aplicado (Design System, sem CSS duplicado)
[ ] Componentes compartilhados reutilizados
[ ] RBAC frontend
[ ] RBAC backend
[ ] Validação frontend
[ ] Validação backend
[ ] Duplicidade e identidade tratadas
[ ] Ativação/inativação definida (matriz de autoridade)
[ ] +Compras → ERP validado
[ ] ERP → +Compras validado
[ ] Falha de integração testada (nunca falso sucesso)
[ ] Unit tests aprovados
[ ] Integration tests aprovados
[ ] Frontend tests aprovados
[ ] Build limpo (backend e frontend)
[ ] Console sem erros
[ ] Network sem erros
[ ] Gate técnico registrado
[ ] Homologação do Product Owner
```

Ajuste o checklist ao cadastro real — ele nasceu do aprendizado de Fornecedores, mas cada cadastro pode adicionar itens específicos (ex.: catálogo hierárquico para Materiais).

## 13. Workflow integrado futuro

Aprovação do Gate técnico de um cadastro **não é o fim do ciclo**. Quando os casos de uso completos de Compras (requisição → pedido → aprovação) estiverem implementados, cada cadastro deve ser reexercitado dentro do fluxo real de ponta a ponta — essa é uma validação integrada futura, não uma reabertura automática do Gate individual. Hipóteses funcionais que só fazem sentido no contexto do workflow completo (ex.: pré-cadastro → aprovação → liberação para uso) devem ser registradas como **ponto de descoberta funcional futura**, não implementadas antecipadamente nem transformadas em regra definitiva antes que o time de negócio confirme o processo.

## Governança

Este documento é um guia de processo de implementação, complementar a `agents/EXECUTION_POLICY.md` e `agents/AGENT_CONTRACT.md` — não os substitui. Toda tarefa de discovery Linx continua exigindo delegação aos Agents especialistas (`linx-erp-specialist-agent`, `linx-database-specialist-agent`); nenhuma IA deve inferir schema ou regra funcional do ERP sem passar por eles.
