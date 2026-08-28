namespace BlueprintOS.Domain.Identity;

/// <summary>Estado global e de linha única do Bootstrap Mode (ADR-0020, item 12; security-design-auth-o1.4.md
/// §20.2/§20.3/§20.12; Work Order O1.4.3, seção 12). Usa chave primária fixa e conhecida (<see cref="IdFixo"/>),
/// nunca gerada — elimina estruturalmente, via EF Core, a possibilidade de mais de uma linha. <c>Concluido =
/// false</c> é o estado inicial, criado exclusivamente pela seed migration (nunca por código de aplicação em
/// runtime); a transição para <c>true</c> é permanente, única, e implementada na etapa de conclusão
/// (O1.4.3.2) via UPDATE condicional (compare-and-swap) — esta classe, na fundação (O1.4.3.1), expõe apenas
/// leitura e o construtor de seed, sem método de mutação, para não antecipar escopo da etapa de conclusão.</summary>
public sealed class BootstrapEstado
{
    /// <summary>Identificador fixo da linha singleton — sempre referenciado explicitamente ao consultar
    /// (nunca <c>SingleOrDefaultAsync()</c> sem filtro por este Id), conforme Work Order O1.4.3, seção 12.</summary>
    public static readonly Guid IdFixo = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public Guid Id { get; private set; }
    public bool Concluido { get; private set; }
    public DateTimeOffset? ConcluidoEm { get; private set; }
    public Guid? UsuarioAdministradorSeniorId { get; private set; }

    /// <summary>Token de concorrência otimista (O1.4.3.2; Work Order O1.4.3, seção 12/13) — garante que a
    /// transição <c>Concluido = false → true</c> seja um compare-and-swap atômico: duas conclusões
    /// concorrentes produzem exatamente um sucesso e uma <c>DbUpdateConcurrencyException</c> na perdedora
    /// (mesmo mecanismo já usado por <see cref="CodigoVerificacaoOtp.RowVersion"/>), nunca dois Administradores
    /// Sênior "primeiros" simultâneos. Requer migration nova (não criada nesta etapa — aguardando autorização
    /// do Product Owner, conforme instrução explícita desta etapa de implementação).</summary>
    public byte[] RowVersion { get; private set; } = Array.Empty<byte>();

    private BootstrapEstado() { }

    /// <summary>Linha inicial única — usada exclusivamente pela seed migration (<c>AddBootstrapEstado</c>).
    /// Nunca deve ser invocado por código de aplicação em runtime para "corrigir" uma linha ausente: a
    /// ausência de linha em runtime é tratada como indisponível/fail-closed (Work Order O1.4.3, seção 12),
    /// não recriada silenciosamente.</summary>
    public static BootstrapEstado CriarInicial() => new()
    {
        Id = IdFixo,
        Concluido = false,
        ConcluidoEm = null,
        UsuarioAdministradorSeniorId = null,
    };

    /// <summary>Transição única e permanente (O1.4.3.2; Work Order O1.4.3, seção 13, passo 7) — a última
    /// barreira antes da escrita, mesmo já tendo sido checada pela política <c>BootstrapAuthenticated</c> e
    /// pelo próprio caso de uso antes de chamar este método. A proteção real contra corrida é o
    /// <see cref="RowVersion"/> (compare-and-swap na persistência), não este <c>if</c> — que apenas impede
    /// reentrância trivial dentro do mesmo processo/transação.</summary>
    public void Concluir(Guid usuarioAdministradorSeniorId, DateTimeOffset momento)
    {
        if (Concluido)
        {
            throw new InvalidOperationException("O Bootstrap já foi concluído — não pode ser concluído novamente.");
        }

        Concluido = true;
        ConcluidoEm = momento;
        UsuarioAdministradorSeniorId = usuarioAdministradorSeniorId;
    }
}
