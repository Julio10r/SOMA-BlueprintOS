# Caderno — Encerramento do Projeto

Itens que só fazem sentido resolver (ou só podem ser validados plenamente) ao final do projeto, tipicamente
porque dependem da existência de uma segunda Unidade de Negócio/ERP real para deixar de ser teórico.

---

### FactoryBU.New / onboarding Multi-BU e Multi-ERP

- **Origem:** Onda 2 / B3 — rodada arquitetural Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Processo governado de implantação de uma nova Unidade de Negócio (e, quando aplicável, de um
  novo ERP), transformando Grupo Soma na primeira implementação de referência do +Compras — não em molde
  rígido copiado cegamente para as próximas.
- **Tipo:** Arquitetura
- **Tratar em:** Encerramento do Projeto (framework completo); o contrato arquitetural mínimo já é
  registrado nesta rodada em `applications/mais-compras/docs/architecture/FactoryBU-New.md`.
- **Status:** Em análise (contrato conceitual publicado; nenhuma implementação de código desta Factory
  nesta rodada, por instrução explícita do Product Owner).
- **Resumo:** Nome conceitual provisório `FactoryBU.New("Reserva")`. Fluxo conceitual: BU de referência
  compatível → baseline → discovery da nova BU → diff → adapters/configuração → testes → homologação do
  Product Owner → ativação. Se o ERP da nova BU for o mesmo (ex.: Reserva + Linx), a BU de referência pode
  servir de baseline de conhecimento, mas discovery + diff + homologação são sempre obrigatórios — nunca
  copiar cegamente. Se o ERP for diferente (ex.: Hering + SAP), o processo parte dos contratos canônicos do
  +Compras e descobre, do zero, como aquele ERP fornece cada conceito — nunca reaproveitar o modelo Linx
  como se fosse universal.
- **Decisão:** Escopo desta rodada confirmado pelo Product Owner: contrato arquitetural claro + estrutura
  mínima extensível + documentação, sem implementar um framework completo agora, e sem tomar nenhuma
  decisão atual que bloqueie essa evolução futura.

---

### Guia de implantação de nova Unidade de Negócio / ERP

- **Origem:** Onda 2 / B3 — rodada arquitetural Multi-BU/Multi-ERP (03/09/2026).
- **Assunto:** Documento operacional completo (não apenas conceitual) para times futuros implantarem uma
  nova BU/ERP.
- **Tipo:** Documentação
- **Tratar em:** Encerramento do Projeto.
- **Status:** Pendente (índice de conteúdo obrigatório já registrado abaixo; texto completo depende de pelo
  menos um onboarding real de segunda BU/ERP para ser escrito com evidência, não hipótese).
- **Resumo:** Deve documentar, quando escrito: contratos canônicos exigidos pelo +Compras; processo de
  discovery de ERP; datasets; mapeamentos; autenticação/conexão; segurança; isolamento entre BUs;
  RAW/REFINED/domain; testes; homologação do Product Owner; ativação. Ver
  `applications/mais-compras/docs/architecture/FactoryBU-New.md` para o contrato arquitetural que este guia
  operacionalizará.
