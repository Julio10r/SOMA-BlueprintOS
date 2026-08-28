using BlueprintOS.Domain.Knowledge.Linx;

namespace BlueprintOS.Application.Knowledge.Linx.Models;

/// <summary>Projeção de leitura de uma entrada de conhecimento Linx, incluindo a proveniência e a fonte —
/// nunca omitidas, pois são a diferença entre um fato e uma hipótese (Work Order O1.13.5, seção 17).</summary>
public sealed record LinxKnowledgeDto(
    Guid Id,
    Guid VersaoRaizId,
    Guid? EntradaAnteriorId,
    int Versao,
    LinxEspecialista Especialista,
    LinxConhecimentoCategoria Categoria,
    string Assunto,
    string Conteudo,
    LinxConhecimentoProveniencia Proveniencia,
    string Fonte,
    string Ator,
    Guid? UnidadeNegocioId,
    IReadOnlyList<string> Tags,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada para registrar uma descoberta/inferência nova (Versão 1) ou uma nova versão de uma
/// entrada já existente (quando <c>VersaoRaizId</c> é informado).</summary>
public sealed record RegistrarConhecimentoInput(
    LinxEspecialista Especialista,
    LinxConhecimentoCategoria Categoria,
    string Assunto,
    string Conteudo,
    LinxConhecimentoProveniencia Proveniencia,
    string Fonte,
    Guid? UnidadeNegocioId,
    IReadOnlyList<string>? Tags,
    Guid? VersaoRaizId = null);

/// <summary>Filtro de busca do MVP de recuperação (Work Order, seção 13) — textual/estruturado. Ponto de
/// extensão futuro para embeddings/RAG sem redesenho: qualquer implementação alternativa de
/// <c>IBuscarConhecimentoUseCase</c> mantém a mesma assinatura de entrada/saída.</summary>
public sealed record LinxKnowledgeFiltro(
    string? Texto = null,
    LinxEspecialista? Especialista = null,
    LinxConhecimentoCategoria? Categoria = null,
    LinxConhecimentoProveniencia? ProvenienciaMinima = null,
    Guid? UnidadeNegocioId = null,
    IReadOnlyList<string>? Tags = null,
    int MaxResultados = 20);
