using BlueprintOS.Core.Publication.Models;

namespace BlueprintOS.Infrastructure.Publication.Publishers;

/// <summary>
/// Descreve o que é fixo e específico de um documento publicado (Relatório Executivo, Guia do
/// Cliente, Guia de Engenharia) — tudo que NÃO depende do conteúdo autoral carregado em
/// runtime. Combinado com o conteúdo carregado por um <c>IExecutiveContentLoader</c>/
/// <c>IClientContentLoader</c>/<c>IEngineeringContentLoader</c> (e demais seções dinâmicas), o
/// <see cref="DocumentAssembler"/> monta o <see cref="PublicationDocument"/> final.
/// </summary>
/// <param name="Slug">Nome base do arquivo de saída, sem extensão (ex.: <c>ExecutiveReport</c>).</param>
/// <param name="Category">Categoria de publicação (ex.: <c>executive</c>, <c>client</c>, <c>engineering</c>).</param>
/// <param name="Title">Título do documento, exibido na capa.</param>
/// <param name="Subtitle">Subtítulo/tagline exibido na capa.</param>
/// <param name="Audience">Público-alvo do documento (ex.: "Diretoria", "Clientes", "Equipe de Engenharia").</param>
/// <param name="Tags">Marcadores livres para categorização e busca.</param>
/// <param name="Theme">Identidade visual (paleta de cores) do documento.</param>
public sealed record DocumentTemplate(
    string Slug,
    string Category,
    string Title,
    string Subtitle,
    string Audience,
    IReadOnlyList<string> Tags,
    PublicationTheme Theme);
