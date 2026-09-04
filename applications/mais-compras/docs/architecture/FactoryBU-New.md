# FactoryBU.New — Processo Governado de Onboarding de Nova Business Unit

Nome conceitual provisório (não precisa ser o nome final de uma classe real). Representa um **processo
governado**, não um framework de código pronto — este documento registra o contrato arquitetural mínimo
para que decisões de hoje não bloqueiem essa evolução; a implementação completa é escopo do Encerramento do
Projeto (ver `applications/mais-compras/docs/cadernos/Encerramento-Projeto.md`).

## Por que isso existe

Grupo Soma é hoje a única Unidade de Negócio operacional do +Compras, e Linx o único ERP integrado. O código
atual — necessariamente — foi escrito e validado só contra essa combinação. O risco arquitetural é que
particularidades da Grupo Soma/Linx (nomes de tabela, triggers, regras de código legado, decisões de UX já
homologadas) sejam silenciosamente tratadas como regra universal do +Compras. `FactoryBU.New` é o contrato
que formaliza o oposto: **Grupo Soma é a primeira implementação de referência, não o molde**.

## Fluxo conceitual

```mermaid
flowchart LR
    A["BU de referência compatível\n(quando ERP igual)"] --> B[Baseline]
    B --> C["Discovery da nova BU"]
    C --> D[Diff]
    D --> E["Adapters / Configuração"]
    E --> F[Testes]
    F --> G["Homologação do PO"]
    G --> H[Ativação]
```

`FactoryBU.New("Reserva")` (exemplo ilustrativo, não uma API real ainda):

1. **Baseline.** Se a nova BU usa o **mesmo ERP** de uma BU já implementada (ex.: Reserva + Linx, mesmo ERP
   da Grupo Soma), essa BU de referência pode servir de ponto de partida de conhecimento — nunca de cópia
   cega. Se o ERP for **diferente** (ex.: Hering + SAP), não há baseline de adapter reaproveitável: o
   processo parte diretamente dos contratos canônicos do +Compras (camada A de
   `MultiBU-MultiErp-Arquitetura.md`) e descobre, do zero, como aquele ERP fornece cada conceito.
2. **Discovery da nova BU.** Levantamento real (nunca hipotético) de tabelas, schemas, chaves, status,
   timestamps, particularidades e volume de dados do ERP daquela BU — mesmo processo já usado nos discoveries
   reais de Fornecedor/CNPJ e Item Fiscal desta aplicação (ver `docs/audits/Discovery-*`), generalizado para
   qualquer BU/ERP.
3. **Diff.** Comparação explícita entre o que a baseline (se existir) assumia e o que o discovery real da
   nova BU encontrou — toda divergência é um achado documentado, nunca uma correção silenciosa por
   suposição.
4. **Adapters / Configuração.** Implementação (ou reaproveitamento, quando o ERP é o mesmo) dos adapters de
   leitura (camada B) e da `ConfiguracaoErp` da nova BU (camada C) — servidor, banco, credenciais seguindo a
   mesma política de segredo já em vigor (`agents/EXECUTION_POLICY.md`, seção "Credenciais e Conexões").
5. **Testes.** Mesma disciplina de testes determinísticos já praticada no projeto (unitários + integração
   real opt-in, nunca em CI, mesmo padrão de `B29_REAL_WRITE_TESTS`) — cobrindo especificamente isolamento
   entre a nova BU e as já existentes (nenhum dado de uma aparece na outra).
6. **Homologação do Product Owner.** Nenhuma BU nova é ativada sem homologação explícita — mesmo padrão já
   em uso nos Gates desta aplicação (Gate Fornecedores, Gate B3 por bloco).
7. **Ativação.** A BU passa a operar; seu `Profile` (camada C) é o único lugar onde particularidades
   daquela BU específica vivem — nunca vazando para as camadas A/B.

## O que este documento NÃO é

- Não é uma API ou classe `FactoryBUNew` implementada — nenhum código foi criado por esta rodada.
- Não é permissão para generalizar prematuramente: nenhuma abstração de "segunda BU" deve ser construída
  sem uma segunda BU real para validar contra ela (mesmo princípio de "não codar para hipótese" já em vigor
  neste projeto).
- Não substitui o Guia de Implantação operacional completo (`Encerramento-Projeto.md`), que só pode ser
  escrito com evidência de pelo menos um onboarding real.

## Decisões atuais que preservam este caminho (não bloqueiam a Factory futura)

- Contratos canônicos (camada A) já são independentes de ERP por construção (Clean Architecture, ADR-0001).
- `ConfiguracaoErp`/`UnidadeNegocio` (O1.11, ADR-0022) já modelam BU e ERP como conceitos configuráveis, não
  hardcoded.
- Gaps abertos registrados em `MultiBU-MultiErp-Arquitetura.md` (Fornecedor/CNPJ por BU, `DatasetLoadState`/
  `IntegrationOccurrence` sem dimensão de BU) são reconhecidos como bloqueadores de uma segunda BU real e
  precisam de decisão do Product Owner antes da primeira execução real de `FactoryBU.New` — não antes disso.

## Ver também

- `MultiBU-MultiErp-Arquitetura.md` — as três camadas conceituais que este processo instancia.
- `applications/mais-compras/docs/cadernos/Encerramento-Projeto.md` — registro formal desta entrada e do
  Guia de Implantação futuro.
- `CadFormFactory.md` — processo análogo já em uso para cadastros individuais (Fornecedores), referência de
  estilo/disciplina para esta Factory em escopo maior (BU/ERP inteiros).
