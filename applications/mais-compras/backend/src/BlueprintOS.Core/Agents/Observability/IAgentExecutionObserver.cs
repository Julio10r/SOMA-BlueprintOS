using System.Diagnostics;

namespace BlueprintOS.Core.Agents.Observability;

/// <summary>Resultado seguro e de baixa cardinalidade de uma etapa observada do Agent.</summary>
public enum AgentExecutionOutcome
{
    /// <summary>A execucao foi iniciada.</summary>
    Started,

    /// <summary>A execucao foi concluida.</summary>
    Succeeded,

    /// <summary>A execucao falhou.</summary>
    Failed,
}

/// <summary>
/// Evento minimo de observabilidade. Nao possui campos para prompt, input, output, PII, secrets ou mensagem de excecao.
/// </summary>
/// <param name="AgentId">ID canonico do Agent.</param>
/// <param name="EventName">Nome estavel do evento.</param>
/// <param name="Outcome">Resultado da etapa.</param>
/// <param name="FailureCategory">Categoria fixa de falha, sem detalhes do payload ou da excecao.</param>
public sealed record AgentExecutionObservation(
    string AgentId,
    string EventName,
    AgentExecutionOutcome Outcome,
    string? FailureCategory = null);

/// <summary>Recebe eventos minimos e redigidos de execucao de Agents.</summary>
public interface IAgentExecutionObserver
{
    /// <summary>Registra um evento que nao contem payload operacional ou dado sensivel.</summary>
    void Record(AgentExecutionObservation observation);
}

/// <summary>
/// Observer padrao baseado em <see cref="DiagnosticListener"/>. Permite coleta por tooling .NET sem
/// introduzir backend de logs e publica apenas <see cref="AgentExecutionObservation"/> redigida.
/// </summary>
public sealed class DiagnosticAgentExecutionObserver : IAgentExecutionObserver
{
    private DiagnosticAgentExecutionObserver()
    {
    }

    /// <summary>Instancia compartilhada usada pelos construtores compativeis dos Agents.</summary>
    public static DiagnosticAgentExecutionObserver Instance { get; } = new();

    /// <summary>Fonte diagnostica estavel para subscribers do processo.</summary>
    public static DiagnosticListener Listener { get; } = new("BlueprintOS.Agents");

    /// <inheritdoc />
    public void Record(AgentExecutionObservation observation)
    {
        if (Listener.IsEnabled(observation.EventName))
        {
            Listener.Write(observation.EventName, observation);
        }
    }
}

/// <summary>Observer sem efeitos para testes ou desativacao explicita.</summary>
public sealed class NullAgentExecutionObserver : IAgentExecutionObserver
{
    private NullAgentExecutionObserver()
    {
    }

    /// <summary>Instancia compartilhada do observer sem efeitos.</summary>
    public static NullAgentExecutionObserver Instance { get; } = new();

    /// <inheritdoc />
    public void Record(AgentExecutionObservation observation)
    {
    }
}

/// <summary>Protege o fluxo do Agent contra falhas do mecanismo auxiliar de observabilidade.</summary>
public static class AgentExecutionObservationRecorder
{
    /// <summary>Tenta registrar o evento sem permitir que falha do observer altere o resultado operacional.</summary>
    public static void RecordSafely(IAgentExecutionObserver observer, AgentExecutionObservation observation)
    {
        try
        {
            observer.Record(observation);
        }
        catch
        {
            // Observability is best-effort and must not become an execution bypass or failure source.
        }
    }
}
