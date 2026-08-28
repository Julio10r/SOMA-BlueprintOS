namespace BlueprintOS.Domain.Identity;

/// <summary>Parâmetro geral de configuração (O1.11). <see cref="UnidadeNegocioId"/> nulo significa
/// parâmetro global (aplicável a todas as Unidades de Negócio); quando informado, é específico daquela
/// Unidade. Único por (<see cref="Chave"/>, <see cref="UnidadeNegocioId"/>).
///
/// Ao contrário de Centro de Custo/Filial/Unidade de Alocação, Parâmetro não é dado mestre integrado de
/// ERP nem possui histórico externo a preservar — exclusão física é aceitável aqui (decisão registrada na
/// Work Order O1.11).</summary>
public sealed class Parametro
{
    public Guid Id { get; private set; }
    public string Chave { get; private set; }
    public string Valor { get; private set; }
    public string Descricao { get; private set; }
    public Guid? UnidadeNegocioId { get; private set; }
    public DateTimeOffset CriadoEm { get; private set; }
    public DateTimeOffset AtualizadoEm { get; private set; }

    private Parametro() { Chave = string.Empty; Valor = string.Empty; Descricao = string.Empty; }

    public Parametro(string chave, string valor, string descricao, Guid? unidadeNegocioId, DateTimeOffset agora)
    {
        if (string.IsNullOrWhiteSpace(chave)) throw new ArgumentException("Chave do Parâmetro é obrigatória.", nameof(chave));

        Id = Guid.NewGuid();
        Chave = chave.Trim();
        Valor = (valor ?? string.Empty).Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        UnidadeNegocioId = unidadeNegocioId;
        CriadoEm = agora;
        AtualizadoEm = agora;
    }

    public void AtualizarValor(string valor, string descricao, DateTimeOffset agora)
    {
        Valor = (valor ?? string.Empty).Trim();
        Descricao = (descricao ?? string.Empty).Trim();
        AtualizadoEm = agora;
    }
}
