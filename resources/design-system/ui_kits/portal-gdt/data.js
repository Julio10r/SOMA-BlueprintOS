// AZZAS 2154 · Portal GDT — fake data
// Plausible Brazilian Portuguese demand records for tech demand management.

window.GDT_DATA = {
  // ── Auth/session demo ──────────────────────────────────────────
  // O OTP é fake — qualquer 6 dígitos funciona, mas mostramos "8 4 2 1 5 6"
  // como hint pra demo. Em produção o backend (n8n) é a fonte da verdade.
  demoOtp: "842156",
  authorizedDomain: "@somagrupo.com.br",

  user: {
    initials: "BA",
    name: "Bruno Afonso",
    email: "bruno.afonso@somagrupo.com.br",
    perfis: ["admin", "governanca"],
  },

  // ── Módulos disponíveis (tabs do shell) ───────────────────────
  // Cada módulo tem key, label, ícone e os perfis que podem ver.
  // O módulo `admin` recebe o tratamento "isAdmin" (cor especial roxa).
  modules: [
    { key: "entrada",        label: "Nova demanda",  icon: "plus",      perfis: ["admin", "governanca", "solicitante"] },
    { key: "acompanhamento", label: "Acompanhamento", icon: "grid",     perfis: ["admin", "governanca", "solicitante", "visualizacao"] },
    { key: "curadoria",      label: "Curadoria",     icon: "check",     perfis: ["admin", "governanca"] },
    { key: "avaliacao",      label: "Decisão",       icon: "eye",       perfis: ["admin", "governanca"] },
    { key: "esteira",        label: "Esteira",       icon: "play",      perfis: ["admin", "governanca", "visualizacao"] },
    { key: "admin",          label: "Administração", icon: "user",      perfis: ["admin"], isAdmin: true },
  ],

  perfilMeta: {
    admin:         { label: "admin",        css: "admin" },
    governanca:    { label: "governanca",   css: "gov" },
    solicitante:   { label: "solicitante",  css: "sol" },
    visualizacao:  { label: "visualizacao", css: "viz" },
  },

  // ── Pipeline / demandas ───────────────────────────────────────
  demands: [
    {
      id: "GDT-042",
      name: "Automação de relatórios financeiros",
      area: "Financeiro",
      solicitante: "maria.silva@somagrupo.com.br",
      data: "12/04/2026",
      status: "novo",        statusLabel: "Novo",
      complexidade: "intermediario", nivel: "N2a",
      sla: "ok", slaLabel: "SLA OK · 4d",
      saving: "R$ 340K", fte: "2.5", score: 87.4,
      fila: "B",
      descricao:
        "Automatizar a consolidação mensal dos relatórios de fechamento contábil das marcas do grupo. Hoje o time gasta ~40h/mês em consolidação manual.",
    },
    {
      id: "GDT-041",
      name: "Pricing dinâmico — outlet de Anacapri",
      area: "Comercial",
      solicitante: "joao.costa@somagrupo.com.br",
      data: "10/04/2026",
      status: "avaliacao", statusLabel: "Em Avaliação",
      complexidade: "complexo", nivel: "N1",
      sla: "atencao", slaLabel: "SLA Atenção · 1d",
      saving: "R$ 1.2M", fte: "4.0", score: 76.8,
      fila: "A",
      descricao:
        "Implementar mecanismo de pricing dinâmico baseado em estoque, sell-through e elasticidade por SKU para o outlet de Anacapri.",
    },
    {
      id: "GDT-039",
      name: "Integração SAP × plataforma de e-commerce",
      area: "Tecnologia",
      solicitante: "joao.costa@somagrupo.com.br",
      data: "08/04/2026",
      status: "aprovado", statusLabel: "Aprovado",
      complexidade: "complexo", nivel: "N1",
      sla: "ok", slaLabel: "SLA OK · 12d",
      saving: "R$ 2.4M", fte: "6.5", score: 91.2,
      fila: "C",
      descricao:
        "Estabelecer integração nativa entre SAP S/4HANA e a stack VTEX/Shopify das marcas, eliminando 11 jobs ETL noturnos.",
    },
    {
      id: "GDT-038",
      name: "Curadoria de imagens de produto via IA",
      area: "Marketing",
      solicitante: "ana.ribeiro@somagrupo.com.br",
      data: "06/04/2026",
      status: "execucao", statusLabel: "Em Execução",
      complexidade: "intermediario", nivel: "N2a",
      sla: "ok", slaLabel: "SLA OK · 22d",
      saving: "R$ 480K", fte: "3.0", score: 81.0,
      fila: "B",
      descricao:
        "Pipeline de IA para pré-aprovar fotos de produto (background, ângulo, iluminação) antes do upload para os e-commerces.",
    },
    {
      id: "GDT-035",
      name: "Dashboard de KPIs operacionais (CD São Paulo)",
      area: "Operações",
      solicitante: "carlos.menezes@somagrupo.com.br",
      data: "01/04/2026",
      status: "rejeitado", statusLabel: "Rejeitado",
      complexidade: "simples", nivel: "N3",
      sla: "estourado", slaLabel: "SLA Estourado · −2d",
      saving: "R$ 90K", fte: "1.0", score: 45.8,
      fila: "D",
      descricao:
        "Dashboard com 8 KPIs operacionais do CD-SP. Rejeitado: já existe iniciativa equivalente no programa Visão Única (Q3).",
    },
    {
      id: "GDT-033",
      name: "Onboarding digital de fornecedores",
      area: "Supply chain",
      solicitante: "rafaela.lima@somagrupo.com.br",
      data: "28/03/2026",
      status: "aguardando", statusLabel: "Aguardando",
      complexidade: "intermediario", nivel: "N2b",
      sla: "atencao", slaLabel: "SLA Atenção · 3d",
      saving: "R$ 220K", fte: "2.0", score: 68.4,
      fila: "D",
      descricao:
        "Portal de auto-cadastro para fornecedores, com workflow de aprovação por área e validação de docs fiscais.",
    },
    {
      id: "GDT-030",
      name: "Previsão de demanda por SKU — Hering",
      area: "Comercial",
      solicitante: "maria.silva@somagrupo.com.br",
      data: "22/03/2026",
      status: "concluida", statusLabel: "Concluída",
      complexidade: "complexo", nivel: "N2a",
      sla: "ok", slaLabel: "Concluída em prazo",
      saving: "R$ 3.1M", fte: "5.5", score: 94.6,
      fila: "C",
      descricao:
        "Modelo ML de previsão semanal por SKU e loja para Hering. Em produção desde fev/2026.",
    },
  ],

  areas: [
    { name: "Financeiro",   count: 17, fill: 85, color: "#2D6A4F" },
    { name: "Tecnologia",   count: 13, fill: 65, color: "#4A90D9" },
    { name: "Operações",    count:  8, fill: 40, color: "#E09B3D" },
    { name: "Comercial",    count:  6, fill: 30, color: "#9B6CC8" },
    { name: "Supply chain", count:  4, fill: 20, color: "#539193" },
    { name: "Marketing",    count:  2, fill: 10, color: "#1A6FB5" },
  ],

  stats: {
    total: 42, novas: 12, aprovadas: 24, rejeitadas: 6,
    avaliacao: 18, aguardando: 7,
    saving: "R$ 9.7M", fte: "34.2",
  },

  timeline: [
    { who: "BA", whoName: "bruno.afonso", action: "Aprovou a demanda",
      when: "15/04/2026 14:32",
      comment: "Aprovado para sprint de maio. Prioridade alta dentro do programa Atacama." },
    { who: "MS", whoName: "maria.silva", action: "Enviou para avaliação", when: "14/04/2026 09:15" },
    { who: "BA", whoName: "bruno.afonso", action: "Solicitou ajuste de escopo",
      when: "13/04/2026 17:08",
      comment: "Adicionar Animale e Cris Barros à fase 1. Hering fica em fase 2." },
    { who: "MS", whoName: "maria.silva", action: "Criou a demanda", when: "12/04/2026 16:45" },
  ],
};
