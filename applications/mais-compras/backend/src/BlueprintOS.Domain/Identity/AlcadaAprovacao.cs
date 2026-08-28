namespace BlueprintOS.Domain.Identity;

/// <summary>O1.12 — Fundação de Administração de Alçadas de Aprovação (ADR-0020, revisão R1.1). Registra a
/// CONFIGURAÇÃO de uma alçada por Unidade de Negócio: critério de disparo, nível/ordem hierárquica e o
/// aprovador (um Usuário específico OU um Perfil inteiro — nunca os dois, nunca nenhum). ESCOPO MÍNIMO DE
/// FUNDAÇÃO: nenhum motor de avaliação/execução de aprovação é implementado aqui — apenas o cadastro
/// consumido futuramente (Onda 3) pelo fluxo transacional de aprovação de compra.</summary>
public sealed class AlcadaAprovacao
{
    public Guid Id { get; private set; }
    public string Nome { get; private set; }
    public Guid UnidadeNegocioId { get; private set; }
    public CriterioAlcada Criterio { get; private set; }

    /// <summary>Faixa de valor mínimo/máximo, aplicável apenas quando <see cref="Criterio"/> é
    /// <see cref="CriterioAlcada.Valor"/>. Ambos opcionais (faixa aberta em uma ou ambas as pontas), mas
    /// quando os dois são informados, mínimo deve ser &lt;= máximo.</summary>
    public decimal? ValorMinimo { get; private set; }
    public decimal? ValorMaximo { get; private set; }

    /// <summary>Centro de Custo ao qual a alçada se aplica, quando <see cref="Criterio"/> é
    /// <see cref="CriterioAlcada.CentroCusto"/>. FK para <see cref="CentroCustoMetadado.Id"/>, nunca
    /// código-texto solto.</summary>
    public Guid? CentroCustoMetadadoId { get; private set; }

    /// <summary>Nível/ordem hierárquica da alçada dentro do fluxo de aprovação (1 = primeiro nível).</summary>
    public int Nivel { get; private set; }

    /// <summary>Aprovador como Usuário específico. Exatamente um entre este e <see cref="AprovadorPerfilId"/>
    /// deve estar preenchido — nunca os dois, nunca nenhum (invariante de domínio).</summary>
    public Guid? AprovadorUsuarioId { get; private set; }

    /// <summary>Aprovador como qualquer usuário do Perfil (aprovação por papel, não por pessoa).</summary>
    public Guid? AprovadorPerfilId { get; private set; }

    public bool Ativo { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private AlcadaAprovacao() { Nome = string.Empty; }

    public AlcadaAprovacao(
        string nome, Guid unidadeNegocioId, CriterioAlcada criterio, decimal? valorMinimo, decimal? valorMaximo,
        Guid? centroCustoMetadadoId, int nivel, Guid? aprovadorUsuarioId, Guid? aprovadorPerfilId, DateTimeOffset agora)
    {
        Id = Guid.NewGuid();
        UnidadeNegocioId = unidadeNegocioId;
        CriadoEm = agora;
        AtualizadoEm = agora;
        Ativo = true;
        AplicarValores(nome, criterio, valorMinimo, valorMaximo, centroCustoMetadadoId, nivel, aprovadorUsuarioId, aprovadorPerfilId);
    }

    public void Editar(
        string nome, CriterioAlcada criterio, decimal? valorMinimo, decimal? valorMaximo,
        Guid? centroCustoMetadadoId, int nivel, Guid? aprovadorUsuarioId, Guid? aprovadorPerfilId, DateTimeOffset agora)
    {
        AplicarValores(nome, criterio, valorMinimo, valorMaximo, centroCustoMetadadoId, nivel, aprovadorUsuarioId, aprovadorPerfilId);
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

    private void AplicarValores(
        string nome, CriterioAlcada criterio, decimal? valorMinimo, decimal? valorMaximo,
        Guid? centroCustoMetadadoId, int nivel, Guid? aprovadorUsuarioId, Guid? aprovadorPerfilId)
    {
        if (string.IsNullOrWhiteSpace(nome)) throw new ArgumentException("Nome é obrigatório.", nameof(nome));
        if (nivel < 1) throw new ArgumentException("Nível deve ser maior ou igual a 1.", nameof(nivel));
        if (valorMinimo is not null && valorMaximo is not null && valorMinimo > valorMaximo)
        {
            throw new ArgumentException("Valor mínimo não pode ser maior que o valor máximo.", nameof(valorMinimo));
        }

        var possuiUsuario = aprovadorUsuarioId is not null && aprovadorUsuarioId != Guid.Empty;
        var possuiPerfil = aprovadorPerfilId is not null && aprovadorPerfilId != Guid.Empty;
        if (possuiUsuario == possuiPerfil)
        {
            throw new ArgumentException(
                "Exatamente um aprovador deve ser informado: um Usuário OU um Perfil, nunca os dois nem nenhum.",
                nameof(aprovadorUsuarioId));
        }

        Nome = nome.Trim();
        Criterio = criterio;
        ValorMinimo = criterio == CriterioAlcada.Valor ? valorMinimo : null;
        ValorMaximo = criterio == CriterioAlcada.Valor ? valorMaximo : null;
        CentroCustoMetadadoId = criterio == CriterioAlcada.CentroCusto ? centroCustoMetadadoId : null;
        Nivel = nivel;
        AprovadorUsuarioId = possuiUsuario ? aprovadorUsuarioId : null;
        AprovadorPerfilId = possuiPerfil ? aprovadorPerfilId : null;
    }
}
