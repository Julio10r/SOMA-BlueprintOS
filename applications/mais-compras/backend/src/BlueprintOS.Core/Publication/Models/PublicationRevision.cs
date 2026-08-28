namespace BlueprintOS.Core.Publication.Models;

/// <summary>
/// Uma entrada do histórico de revisões de um documento. Hoje <see cref="PublicationMetadata.RevisionHistory"/>
/// tipicamente contém no máximo a revisão atual (o Publication Engine não durabiliza revisões
/// anteriores nesta sprint); o tipo já existe como ponto de extensão para o futuro controle de
/// revisão/histórico de versões, sem exigir mudança de modelo quando for implementado.
/// </summary>
/// <param name="Version">Versão do documento nesta revisão.</param>
/// <param name="Date">Data da revisão.</param>
/// <param name="Author">Responsável pela revisão.</param>
/// <param name="Summary">Resumo do que mudou nesta revisão.</param>
public sealed record PublicationRevision(string Version, DateTimeOffset Date, string Author, string Summary);
