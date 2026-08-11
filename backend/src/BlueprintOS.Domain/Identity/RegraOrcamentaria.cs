namespace BlueprintOS.Domain.Identity;

/// <summary>O1.12 — Fundação de Administração de Controle Orçamentário (ADR-0020, revisão R1.1). Registra
/// APENAS o cadastro da regra: Centro de Custo, valor limite e periodicidade, por Unidade de Negócio.
/// ESCOPO MÍNIMO DE FUNDAÇÃO, deliberadamente NÃO implementado aqui: reserva contábil, consumo real de
/// orçamento, integração com ERP financeiro ou qualquer bloqueio operacional de solicitação de compra —
/// tudo isso é escopo de Work Orders futuras (Onda 3/4), quando a fonte de verdade do saldo orçamentário
/// for definida (PENDÊNCIA de produto registrada em `ComprasDataModel.md`/`ComprasFuncional.md`).</summary>
public sealed class RegraOrcamentaria
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public Guid CentroCustoMetadadoId { get; private set; }
    public decimal ValorLimite { get; private set; }
    public PeriodoOrcamentario Periodo { get; private set; }
    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private RegraOrcamentaria() { Nome = string.Empty; }

    public RegraOrcamentaria(
        string nome, Guid unidadeNegocioId, Guid centroCustoMetadadoId, decimal valorLimite,
        PeriodoOrcamentario periodo, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        CriadoEm = agora;
        AtualizadoEm = agora;
        Ativo = true;
        AplicarValores(nome, centroCustoMetadadoId, valorLimite, periodo);
    }

    public void Editar(string nome, Guid centroCustoMetadadoId, decimal valorLimite, PeriodoOrcamentario periodo, DateTimeOffset agora)
    {
        AplicarValores(nome, centroCustoMetadadoId, valorLimite, periodo);
        AtualizadoEm = agora;
    }

    public void Ativar(DateTimeOffset agora)
    {
        if (Ativo) return;
        Ativo = true;
        AtualizadoEm = agora;
    }

    public void Inativar(DateTimeOffset agora)
    {
        if (!Ativo) return;
        Ativo = false;
        AtualizadoEm = agora;
    }

    private void AplicarValores(string nome, Guid centroCustoMetadadoId, decimal valorLimite, PeriodoOrcamentario periodo)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (centroCustoMetadadoId == Guid.Empty) throw new ArgumentException("Centro de Custo é obrigatório.", nameof(centroCustoMetadadoId));
        if (valorLimite <= 0) throw new ArgumentException("Valor limite deve ser maior que zero.", nameof(valorLimite));

        Nome = nome.Trim();
        CentroCustoMetadadoId = centroCustoMetadadoId;
        ValorLimite = valorLimite;
        Periodo = periodo;
    }
}
