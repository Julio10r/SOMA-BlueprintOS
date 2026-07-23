namespace BlueprintOS.Core.Documentation.Models;

/// <summary>
/// Classifica o tipo de conteúdo representado por um <see cref="DocumentationEntry"/>.
/// </summary>
public enum DocumentationType
{
    /// <summary>
    /// Documentação técnica de um módulo (contratos, classes, dependências).
    /// </summary>
    Technical,

    /// <summary>
    /// Documentação funcional (casos de uso, regras de negócio).
    /// </summary>
    Functional,

    /// <summary>
    /// Documentação destinada a agentes de IA, no formato utilizado em <c>.ai/context</c>.
    /// </summary>
    AI,

    /// <summary>
    /// Documentação voltada a desenvolvedores (guias, README).
    /// </summary>
    Developer,

    /// <summary>
    /// Registro de decisão arquitetural (Architecture Decision Record).
    /// </summary>
    Adr,
}
