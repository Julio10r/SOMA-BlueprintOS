namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Perda de corrida de concorrência otimista (RowVersion) — a linha foi modificada por outra
/// requisição entre a leitura e esta escrita (O1.4.2.1, Achado B). Traduzida pelos repositórios a partir
/// da exceção específica do provedor de persistência, para que a camada de Application permaneça
/// agnóstica de EF Core/SQL Server.</summary>
public sealed class ConcurrencyConflictException : Exception
{
    public ConcurrencyConflictException(string message) : base(message) { }
}

/// <summary>Violação de restrição única (ex.: índice único filtrado de código Pendente por usuário,
/// ou de e-mail no throttle) — outra requisição concorrente já criou o registro equivalente.</summary>
public sealed class DuplicateRecordException : Exception
{
    public DuplicateRecordException(string message) : base(message) { }
}
