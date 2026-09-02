import { FormEvent, KeyboardEvent, useEffect, useMemo, useState } from "react";
import type { CategoriaFornecedorOption, ManualFornecedorDraft, ManualFornecedorValidationResult } from "../types/linxSupplierContract";
import {
  aplicarMascaraCep,
  aplicarMascaraCnpjCpf,
  aplicarMascaraDdi,
  aplicarMascaraTelefone,
  DDI_PADRAO_BRASIL,
  isValidCnpjChecksum,
  isValidCpfChecksum,
  PAISES,
  UNIDADES_FEDERACAO,
  validateManualFornecedor
} from "../types/linxSupplierContract";
import { consultCep, consultCnpj, listarCategoriasFornecedor, listarMunicipiosPorUf } from "../services/supplierEnrichmentApi";

const businessUnit = "SOMA";
const erpSistema = "SOMA_DESENV";

/**
 * Formulário completo de Fornecedor, organizado em seções lógicas (Identificação, Endereço, Contato,
 * Atividade econômica). Reaproveitado tanto pelo cadastro manual ("+ Novo fornecedor") quanto pelo
 * modo de edição da tela de detalhe — mesmo layout de campos nos dois casos, só o rótulo do botão
 * principal e a edição do CNPJ mudam. Layout fiel ao Design Review de Fornecedores (2026-09-01):
 * Identificação em 3 colunas (CNPJ/CPF, Nome Fantasia, Categoria) + Razão Social em linha própria;
 * Endereço em 3 linhas (CEP/Logradouro/Número — Complemento/Bairro — UF/Cidade/País).
 */
export function ManualFornecedorForm({
  draft,
  onDraftChange,
  onSubmit,
  onCancel,
  loading,
  error,
  submitLabel = "Cadastrar fornecedor",
  cnpjEditavel = true,
  title,
  subtitle
}: {
  draft: ManualFornecedorDraft;
  onDraftChange: (draft: ManualFornecedorDraft) => void;
  onSubmit: (draft: ManualFornecedorDraft) => void;
  onCancel: () => void;
  loading: boolean;
  error?: string | null;
  submitLabel?: string;
  cnpjEditavel?: boolean;
  /** Quando informado, renderiza um cabeçalho com ícone + título + subtítulo + botão fechar (X)
   * — usado no fluxo de "+ Novo fornecedor" (modal dedicado). Omitido no modo de edição inline
   * da tela de detalhe, que já tem seu próprio cabeçalho de página. */
  title?: string;
  subtitle?: string;
}) {
  const [errors, setErrors] = useState<ManualFornecedorValidationResult[]>([]);
  const [consultandoCep, setConsultandoCep] = useState(false);
  const [avisoCep, setAvisoCep] = useState<string | null>(null);
  const [consultandoCnpj, setConsultandoCnpj] = useState(false);
  const [avisoCnpj, setAvisoCnpj] = useState<string | null>(null);
  // CNPJ aguardando confirmação do usuário no modal próprio da aplicação (nunca window.confirm —
  // diálogo nativo do navegador não é aceitável nesta tela). null = nenhuma confirmação pendente.
  const [cnpjAguardandoConfirmacao, setCnpjAguardandoConfirmacao] = useState<string | null>(null);
  const correlationId = useMemo(() => `manual-fornecedor-${crypto.randomUUID()}`, []);

  // Gate de homologação (2026-09-01): Categoria deixou de ser texto livre — combobox do catálogo
  // pré-cadastrado do +Compras (GET /fornecedores/categorias), nunca lista hardcoded no frontend.
  const [categorias, setCategorias] = useState<CategoriaFornecedorOption[]>([]);
  const [carregandoCategorias, setCarregandoCategorias] = useState(true);
  useEffect(() => {
    let cancelado = false;
    listarCategoriasFornecedor()
      .then((lista) => {
        if (!cancelado) setCategorias(lista);
      })
      .catch(() => {
        if (!cancelado) setCategorias([]);
      })
      .finally(() => {
        if (!cancelado) setCarregandoCategorias(false);
      });
    return () => {
      cancelado = true;
    };
  }, []);

  // Gate de homologação (2026-09-01): Cidade é combo dependente da UF selecionada (municípios
  // reais via IBGE, backend). "EX" (exterior) não tem lista de municípios — cidade permanece
  // digitável nesse caso.
  const [municipios, setMunicipios] = useState<string[]>([]);
  const [carregandoMunicipios, setCarregandoMunicipios] = useState(false);
  useEffect(() => {
    const uf = draft.estado;
    if (!uf || uf === "EX") {
      setMunicipios([]);
      return;
    }
    let cancelado = false;
    setCarregandoMunicipios(true);
    listarMunicipiosPorUf(uf)
      .then((lista) => {
        if (!cancelado) setMunicipios(lista);
      })
      .catch(() => {
        if (!cancelado) setMunicipios([]);
      })
      .finally(() => {
        if (!cancelado) setCarregandoMunicipios(false);
      });
    return () => {
      cancelado = true;
    };
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draft.estado]);
  // A cidade já preenchida (ex: por consulta de CEP/CNPJ) pode não bater com a grafia exata do
  // IBGE — mantém como opção extra em vez de "sumir" silenciosamente do combo.
  const opcoesCidade = draft.cidade && !municipios.includes(draft.cidade) ? [draft.cidade, ...municipios] : municipios;

  // Gate de homologação (2026-09-01): País é pré-validado pela UF e preenchido automaticamente —
  // só fica editável quando UF="EX" (mesmo padrão de dependência de Cidade). Enquanto a UF real
  // não é "EX", País é sempre "Brasil".
  const paisEditavel = draft.estado === "EX";
  useEffect(() => {
    if (draft.estado && draft.estado !== "EX" && draft.pais !== "Brasil") {
      onDraftChange({ ...draft, pais: "Brasil" });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [draft.estado]);

  // Gate de homologação (2026-09-01): telefone precisa de DDI — se UF (não-exterior) ou País é
  // Brasil, o DDI é preenchido sozinho com +55 e fica travado; só é editável quando o fornecedor
  // é do exterior (UF="EX", País diferente de Brasil).
  const isBrasil = draft.pais.trim().toLowerCase() === "brasil";
  useEffect(() => {
    if (isBrasil && draft.telefoneDdi !== DDI_PADRAO_BRASIL) {
      onDraftChange({ ...draft, telefoneDdi: DDI_PADRAO_BRASIL });
    }
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [isBrasil]);

  function update<K extends keyof ManualFornecedorDraft>(field: K, value: ManualFornecedorDraft[K]) {
    onDraftChange({ ...draft, [field]: value });
  }

  /** Tipo de pessoa nunca é escolhido manualmente — é identificado automaticamente pela
   * quantidade de caracteres do CNPJ/CPF (11 = Pessoa Física, 14 = Pessoa Jurídica). CNPJ
   * alfanumérico (Instrução Normativa RFB nº 2.229/2024): nunca usar \D aqui, isso apagaria as
   * letras — CPF continua sempre numérico, então uma letra só é possível num CNPJ. */
  function updateCnpjCpf(value: string) {
    const chars = value.toUpperCase().replace(/[^0-9A-Z]/g, "");
    const tipoPessoa = chars.length > 11 ? "PJ" : chars.length > 0 ? "PF" : draft.tipoPessoa;
    onDraftChange({ ...draft, cnpj_Cpf: aplicarMascaraCnpjCpf(value), tipoPessoa });
  }

  function errorFor(field: keyof ManualFornecedorDraft): string | undefined {
    return errors.find((item) => item.field === field)?.message;
  }

  /**
   * Gate de homologação (2026-09-01): toda vez que o campo CNPJ/CPF perde o foco com um CNPJ válido
   * (Pessoa Jurídica) preenchido, pergunta se o usuário quer consultar os dados online — mesma UX
   * do Linx (achado 1, docs/audits/Discovery-Tela-001016G1.md: "Deseja consultar online os dados
   * cadastrais deste CNPJ ?"). Pergunta sempre, mesmo que já tenha perguntado antes para o mesmo
   * CNPJ (ex: usuário sai do campo e volta sem mudar o valor). Só preenche campos vazios ao
   * confirmar (nunca sobrescreve algo já digitado).
   */
  function handleCnpjBlur() {
    // CNPJ alfanumérico (Instrução Normativa RFB nº 2.229/2024, vigente a partir de julho/2026):
    // nunca usar \D aqui — isso apagaria as letras das 12 primeiras posições do CNPJ. CPF continua
    // sempre numérico (a mudança da Receita Federal não altera CPF).
    const chars = draft.cnpj_Cpf.toUpperCase().replace(/[^0-9A-Z]/g, "");
    // Gate de homologação (2026-09-01): CNPJ/CPF com dígito verificador inválido nunca chama a
    // API — só devolve "CNPJ/CPF inválido." na hora (evita gastar consulta à toa num documento que
    // já se sabe errado sem precisar ir até o servidor/BrasilAPI). Validação de CPF (11 dígitos)
    // não existia antes — um CPF inválido nunca era sinalizado ao usuário ao sair do campo.
    if (chars.length === 11) {
      setErrors((prev) => {
        const semCnpjCpf = prev.filter((item) => item.field !== "cnpj_Cpf");
        return isValidCpfChecksum(chars) ? semCnpjCpf : [...semCnpjCpf, { field: "cnpj_Cpf", message: "CPF inválido." }];
      });
      return;
    }
    if (chars.length !== 14) {
      // Nem 11 (CPF) nem 14 (CNPJ) caracteres — comprimento incompleto/errado, também sinalizado
      // na hora (evita o usuário só descobrir isso ao tentar cadastrar).
      setErrors((prev) => {
        const semCnpjCpf = prev.filter((item) => item.field !== "cnpj_Cpf");
        return chars.length === 0 ? semCnpjCpf : [...semCnpjCpf, { field: "cnpj_Cpf", message: "CNPJ/CPF inválido." }];
      });
      return;
    }
    if (!isValidCnpjChecksum(chars)) {
      setErrors((prev) => [...prev.filter((item) => item.field !== "cnpj_Cpf"), { field: "cnpj_Cpf", message: "CNPJ inválido." }]);
      return;
    }
    if (draft.tipoPessoa !== "PJ") return;
    setErrors((prev) => prev.filter((item) => item.field !== "cnpj_Cpf"));
    setCnpjAguardandoConfirmacao(chars);
  }

  function cancelarConsultaCnpj() {
    setCnpjAguardandoConfirmacao(null);
  }

  async function confirmarConsultaCnpj() {
    const digits = cnpjAguardandoConfirmacao;
    setCnpjAguardandoConfirmacao(null);
    if (!digits) return;

    setConsultandoCnpj(true);
    setAvisoCnpj(null);
    try {
      const resultado = await consultCnpj(digits, { businessUnit, erpSistema, correlationId });
      if (!resultado.sucesso) {
        setAvisoCnpj(resultado.mensagemErro ?? "CNPJ não encontrado.");
        return;
      }
      onDraftChange({
        ...draft,
        razaoSocial: draft.razaoSocial.trim() || resultado.razaoSocial || "",
        nomeFantasia: draft.nomeFantasia.trim() || resultado.nomeFantasia || "",
        email: draft.email.trim() || resultado.email || "",
        telefone: draft.telefone.trim() || resultado.telefone || "",
        cep: draft.cep.trim() || resultado.cep || "",
        logradouro: draft.logradouro.trim() || resultado.logradouro || "",
        numero: draft.numero.trim() || resultado.numero || "",
        complemento: draft.complemento.trim() || resultado.complemento || "",
        bairro: draft.bairro.trim() || resultado.bairro || "",
        cidade: draft.cidade.trim() || resultado.cidade || "",
        estado: draft.estado.trim() || resultado.estado || "",
        pais: resultado.pais || draft.pais,
        cnaePrincipalCodigo: draft.cnaePrincipalCodigo.trim() || resultado.cnaePrincipalCodigo || "",
        cnaePrincipalDescricao: draft.cnaePrincipalDescricao.trim() || resultado.cnaePrincipalDescricao || ""
      });
    } catch (err) {
      setAvisoCnpj(err instanceof Error ? err.message : "Falha ao consultar o CNPJ.");
    } finally {
      setConsultandoCnpj(false);
    }
  }

  async function handleCepBlur() {
    const digits = draft.cep.replace(/\D/g, "");
    if (digits.length !== 8) return;
    setConsultandoCep(true);
    setAvisoCep(null);
    try {
      const resultado = await consultCep(digits);
      if (!resultado.sucesso) {
        setAvisoCep(resultado.mensagemErro ?? "CEP não encontrado.");
        return;
      }
      // Gate de homologação (2026-09-01): Complemento nunca é preenchido pela consulta de CEP —
      // é informação específica do fornecedor (sala, bloco, etc.), não do endereço em si. ViaCEP só
      // cobre endereços brasileiros, então País é sempre "Brasil" quando a consulta tem sucesso.
      // Diferente do CNPJ (que só preenche campo vazio), o CEP é a fonte de verdade do endereço:
      // ao trocar o CEP para outro válido, os campos de endereço são atualizados para o novo CEP,
      // mesmo que já estivessem preenchidos por uma consulta anterior.
      onDraftChange({
        ...draft,
        logradouro: resultado.logradouro || "",
        bairro: resultado.bairro || "",
        cidade: resultado.cidade || "",
        estado: resultado.estado || "",
        pais: "Brasil"
      });
    } catch (err) {
      setAvisoCep(err instanceof Error ? err.message : "Falha ao consultar o CEP.");
    } finally {
      setConsultandoCep(false);
    }
  }

  function handleSubmit(event: FormEvent) {
    event.preventDefault();
    const validation = validateManualFornecedor(draft);
    setErrors(validation);
    if (validation.length > 0) return;
    onSubmit(draft);
  }

  /**
   * Gate de homologação (2026-09-01): Enter dentro de um campo de texto/combo nunca deve submeter
   * o formulário (o único jeito de cadastrar é clicando no botão) — em vez disso, move o foco para
   * o próximo campo, como um "tab" (não se aplica a botões, que mantêm o clique normal do Enter).
   */
  function handleFormKeyDown(event: KeyboardEvent<HTMLFormElement>) {
    if (event.key !== "Enter") return;
    const target = event.target as HTMLElement;
    if (target.tagName !== "INPUT" && target.tagName !== "SELECT") return;
    event.preventDefault();
    const form = event.currentTarget;
    const campos = Array.from(form.querySelectorAll<HTMLElement>("input:not([disabled]), select:not([disabled])"));
    const indiceAtual = campos.indexOf(target);
    if (indiceAtual >= 0 && indiceAtual < campos.length - 1) {
      campos[indiceAtual + 1].focus();
    }
  }

  return (
    <>
    <form className={`card form-compact${title ? " modal-form" : ""}`} onSubmit={handleSubmit} onKeyDown={handleFormKeyDown} noValidate>
      {title && (
        <div className="modal-form-header">
          <div className="modal-form-header-icon" aria-hidden="true">
            <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
              <path d="M4 21V7l8-4 8 4v14" />
              <path d="M9 21v-6h6v6" />
              <path d="M9 11h.01M15 11h.01M9 15h.01M15 15h.01" />
            </svg>
          </div>
          <div style={{ flex: 1 }}>
            <h2>{title}</h2>
            {subtitle && <p>{subtitle}</p>}
          </div>
          <button type="button" className="modal-form-close" onClick={onCancel} aria-label="Fechar">
            ×
          </button>
        </div>
      )}
      {error && <div className="notice notice-crit">{error}</div>}

      <div className="data-block">
        <div className="section-title">Identificação</div>
        {avisoCnpj && <div className="notice notice-warn">{avisoCnpj}</div>}
        <div className="data-grid-3">
          <label className="field-editable" htmlFor="manual-fornecedor-cnpj">
            <span>
              CNPJ/CPF *{consultandoCnpj ? " (consultando...)" : ""}{" "}
              <span className="field-info-icon" title="Tipo de pessoa é identificado automaticamente pelo CNPJ/CPF.">
                ⓘ
              </span>
            </span>
            <input
              id="manual-fornecedor-cnpj"
              name="cnpj_Cpf"
              value={draft.cnpj_Cpf}
              disabled={!cnpjEditavel}
              onChange={(event) => updateCnpjCpf(event.target.value)}
              onBlur={handleCnpjBlur}
              placeholder="00.000.000/0000-00"
              autoComplete="off"
              aria-invalid={!!errorFor("cnpj_Cpf")}
            />
            <span className="field-feedback">{errorFor("cnpj_Cpf")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-nome-fantasia">
            <span>Nome Fantasia *</span>
            <input
              id="manual-fornecedor-nome-fantasia"
              name="nomeFantasia"
              value={draft.nomeFantasia}
              onChange={(event) => update("nomeFantasia", event.target.value)}
              aria-invalid={!!errorFor("nomeFantasia")}
            />
            <span className="field-feedback">{errorFor("nomeFantasia")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-categoria">
            <span>Categoria *</span>
            <select
              id="manual-fornecedor-categoria"
              value={draft.categoria}
              onChange={(event) => update("categoria", event.target.value)}
              disabled={carregandoCategorias}
              aria-invalid={!!errorFor("categoria")}
            >
              <option value="">{carregandoCategorias ? "Carregando..." : "Selecione a categoria"}</option>
              {categorias.map((categoria) => (
                <option key={categoria.codigo} value={categoria.descricao}>
                  {categoria.descricao}
                </option>
              ))}
            </select>
            <span className="field-feedback">{errorFor("categoria")}</span>
          </label>
          <label className="field-editable field-span-all" htmlFor="manual-fornecedor-razao-social">
            <span>Razão Social *</span>
            <input
              id="manual-fornecedor-razao-social"
              name="razaoSocial"
              value={draft.razaoSocial}
              onChange={(event) => update("razaoSocial", event.target.value)}
              aria-invalid={!!errorFor("razaoSocial")}
            />
            <span className="field-feedback">{errorFor("razaoSocial")}</span>
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Endereço</div>
        {avisoCep && <div className="notice notice-warn">{avisoCep}</div>}
        <div className="data-grid-3">
          <label className="field-editable" htmlFor="manual-fornecedor-cep">
            <span>CEP *{consultandoCep ? " (consultando...)" : ""}</span>
            <div className="field-inline-action">
              <input
                id="manual-fornecedor-cep"
                value={draft.cep}
                onChange={(event) => update("cep", aplicarMascaraCep(event.target.value))}
                onBlur={handleCepBlur}
                placeholder="00000-000"
                aria-invalid={!!errorFor("cep")}
              />
              <button type="button" className="btn btn-secondary" onClick={handleCepBlur} disabled={consultandoCep}>
                Buscar CEP
              </button>
            </div>
            <span className="field-feedback">{errorFor("cep")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-logradouro">
            <span>Logradouro *</span>
            <input
              id="manual-fornecedor-logradouro"
              value={draft.logradouro}
              onChange={(event) => update("logradouro", event.target.value)}
              aria-invalid={!!errorFor("logradouro")}
            />
            <span className="field-feedback">{errorFor("logradouro")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-numero">
            <span>Número *</span>
            <input
              id="manual-fornecedor-numero"
              value={draft.numero}
              onChange={(event) => update("numero", event.target.value)}
              aria-invalid={!!errorFor("numero")}
            />
            <span className="field-feedback">{errorFor("numero")}</span>
          </label>
        </div>
        <div className="data-grid-2">
          <label className="field-editable" htmlFor="manual-fornecedor-complemento">
            <span>Complemento</span>
            <input
              id="manual-fornecedor-complemento"
              value={draft.complemento}
              onChange={(event) => update("complemento", event.target.value)}
            />
            <span className="field-feedback" />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-bairro">
            <span>Bairro *</span>
            <input
              id="manual-fornecedor-bairro"
              value={draft.bairro}
              onChange={(event) => update("bairro", event.target.value)}
              aria-invalid={!!errorFor("bairro")}
            />
            <span className="field-feedback">{errorFor("bairro")}</span>
          </label>
        </div>
        <div className="data-grid-3">
          <label className="field-editable" htmlFor="manual-fornecedor-estado">
            <span>UF *</span>
            <select
              id="manual-fornecedor-estado"
              value={draft.estado}
              onChange={(event) => {
                // Trocar a UF invalida a Cidade previamente selecionada (não existe combinação
                // Cidade/UF livre — a lista de cidades é sempre da UF atual). Ao ir para "EX"
                // (exterior), País e DDI ficam em branco para o usuário preencher; ao sair de "EX"
                // para uma UF brasileira, o efeito abaixo já força País="Brasil"/DDI="+55".
                const novaUf = event.target.value;
                const exterior = novaUf === "EX";
                onDraftChange({
                  ...draft,
                  estado: novaUf,
                  cidade: "",
                  pais: exterior ? "" : "Brasil",
                  telefoneDdi: exterior ? "" : DDI_PADRAO_BRASIL
                });
              }}
              aria-invalid={!!errorFor("estado")}
            >
              <option value="">Selecione...</option>
              {UNIDADES_FEDERACAO.map((uf) => (
                <option key={uf.value} value={uf.value}>
                  {uf.label}
                </option>
              ))}
            </select>
            <span className="field-feedback">{errorFor("estado")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-cidade">
            <span>Cidade *{carregandoMunicipios ? " (carregando...)" : ""}</span>
            {draft.estado === "EX" ? (
              // Exterior não tem lista de municípios do IBGE — cidade permanece digitável.
              <input
                id="manual-fornecedor-cidade"
                value={draft.cidade}
                onChange={(event) => update("cidade", event.target.value)}
                placeholder="Cidade no exterior"
                aria-invalid={!!errorFor("cidade")}
              />
            ) : (
              <select
                id="manual-fornecedor-cidade"
                value={draft.cidade}
                onChange={(event) => update("cidade", event.target.value)}
                aria-invalid={!!errorFor("cidade")}
                disabled={!draft.estado || carregandoMunicipios}
              >
                <option value="">
                  {!draft.estado ? "Selecione a UF primeiro" : carregandoMunicipios ? "Carregando..." : "Selecione..."}
                </option>
                {opcoesCidade.map((cidade) => (
                  <option key={cidade} value={cidade}>
                    {cidade}
                  </option>
                ))}
              </select>
            )}
            <span className="field-feedback">{errorFor("cidade")}</span>
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-pais">
            <span>País *</span>
            {/* Gate de homologação (2026-09-01): País é pré-validado pela UF e preenchido
                automaticamente — só editável quando UF="EX" (mesmo padrão de Cidade). */}
            {paisEditavel ? (
              <>
                <input
                  id="manual-fornecedor-pais"
                  className="field-combo"
                  list="manual-fornecedor-paises"
                  value={draft.pais}
                  onChange={(event) => update("pais", event.target.value)}
                  placeholder="Buscar país..."
                  aria-invalid={!!errorFor("pais")}
                />
                <datalist id="manual-fornecedor-paises">
                  {PAISES.map((pais) => (
                    <option key={pais} value={pais} />
                  ))}
                </datalist>
              </>
            ) : (
              <input
                id="manual-fornecedor-pais"
                value={draft.estado ? draft.pais : ""}
                disabled
                placeholder={!draft.estado ? "Selecione a UF primeiro" : ""}
                aria-invalid={!!errorFor("pais")}
              />
            )}
            <span className="field-feedback">{errorFor("pais")}</span>
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Contato</div>
        <div className="data-grid-3">
          <label className="field-editable" htmlFor="manual-fornecedor-email">
            <span>E-mail *</span>
            <input
              id="manual-fornecedor-email"
              type="email"
              value={draft.email}
              onChange={(event) => update("email", event.target.value)}
              placeholder="exemplo@dominio.com.br"
              aria-invalid={!!errorFor("email")}
            />
            <span className="field-feedback">{errorFor("email")}</span>
          </label>
          <div className="field-editable">
            {/* A <label> não envolve os dois inputs (DDI + número) para o "for" seguir associando
                o texto "Telefone *" apenas ao campo de número — do contrário
                getByLabelText/leitores de tela leriam o rótulo para os dois campos. */}
            <label htmlFor="manual-fornecedor-telefone">Telefone *</label>
            <div className="field-phone-group">
              <input
                className="field-ddi"
                id="manual-fornecedor-telefone-ddi"
                value={draft.telefoneDdi}
                disabled={isBrasil}
                onChange={(event) => update("telefoneDdi", aplicarMascaraDdi(event.target.value))}
                placeholder="+55"
                aria-label="DDI"
                aria-invalid={!!errorFor("telefoneDdi")}
              />
              <input
                className="field-numero"
                id="manual-fornecedor-telefone"
                value={draft.telefone}
                onChange={(event) => update("telefone", aplicarMascaraTelefone(event.target.value, draft.telefoneDdi))}
                placeholder="(00) 00000-0000"
                aria-invalid={!!errorFor("telefone")}
              />
            </div>
            <span className="field-feedback">{errorFor("telefone") ?? errorFor("telefoneDdi")}</span>
          </div>
          <label className="field-editable" htmlFor="manual-fornecedor-website">
            <span>Website</span>
            <input
              id="manual-fornecedor-website"
              value={draft.website}
              onChange={(event) => update("website", event.target.value)}
              placeholder="https://www.exemplo.com.br"
            />
            <span className="field-feedback" />
          </label>
        </div>
      </div>

      <div className="data-block">
        <div className="section-title">Atividade econômica</div>
        <div className="data-grid-2">
          <label className="field-editable" htmlFor="manual-fornecedor-cnae-codigo">
            <span>CNAE principal (código)</span>
            <input
              id="manual-fornecedor-cnae-codigo"
              value={draft.cnaePrincipalCodigo}
              onChange={(event) => update("cnaePrincipalCodigo", event.target.value)}
            />
            <span className="field-feedback" />
          </label>
          <label className="field-editable" htmlFor="manual-fornecedor-cnae-descricao">
            <span>CNAE principal (descrição)</span>
            <input
              id="manual-fornecedor-cnae-descricao"
              value={draft.cnaePrincipalDescricao}
              onChange={(event) => update("cnaePrincipalDescricao", event.target.value)}
            />
            <span className="field-feedback" />
          </label>
        </div>
      </div>

      <div className="actions">
        <button type="button" className="btn btn-secondary" onClick={onCancel} disabled={loading}>
          Cancelar
        </button>
        <button type="submit" className="btn btn-primary" disabled={loading}>
          {loading ? "Salvando..." : submitLabel}
        </button>
      </div>
    </form>

    {/* Gate de homologação (2026-09-01): confirmação de consulta online de CNPJ como modal da
        própria aplicação (Design System +Compras), nunca window.confirm nativo do navegador. */}
    {cnpjAguardandoConfirmacao && (
      <div className="modal-overlay" role="dialog" aria-modal="true">
        <div className="modal-card card">
          <h2>Consultar CNPJ</h2>
          <p>Deseja consultar online os dados cadastrais deste CNPJ?</p>
          <div className="actions">
            <button type="button" className="btn btn-secondary" onClick={cancelarConsultaCnpj}>
              Não, cadastrar manualmente
            </button>
            <button type="button" className="btn btn-primary" onClick={confirmarConsultaCnpj}>
              Sim, consultar
            </button>
          </div>
        </div>
      </div>
    )}
    </>
  );
}

export const manualFornecedorDraftInicial: ManualFornecedorDraft = {
  razaoSocial: "",
  nomeFantasia: "",
  cnpj_Cpf: "",
  tipoPessoa: "PJ",
  email: "",
  telefoneDdi: DDI_PADRAO_BRASIL,
  telefone: "",
  website: "",
  cep: "",
  logradouro: "",
  numero: "",
  complemento: "",
  bairro: "",
  cidade: "",
  estado: "",
  pais: "Brasil",
  categoria: "",
  cnaePrincipalCodigo: "",
  cnaePrincipalDescricao: ""
};
