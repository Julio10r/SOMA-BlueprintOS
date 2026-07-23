namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Representa um Architecture Decision Record (ADR) gerenciado pelo sistema de documentação.
/// </summary>
/// <param name="Id">Identificador único da ADR, no formato <c>ADR-NNNN</c>.</param>
/// <param name="Title">Título da decisão arquitetural.</param>
/// <param name="Status">Status atual da decisão.</param>
/// <param name="Context">Contexto ou problema que motivou a decisão.</param>
/// <param name="Decision">Descrição do que foi decidido.</param>
/// <param name="Consequences">Consequências, positivas e negativas, da decisão.</param>
/// <param name="CreatedAt">Data e hora de criação da ADR.</param>
public sealed record AdrRecord(
    string Id,
    string Title,
    AdrStatus Status,
    string Context,
    string Decision,
    string Consequences,
    DateTimeOffset CreatedAt);
