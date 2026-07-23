using System.Text;

namespace BlueprintOS.Infrastructure.Documentation.Publishing;

/// <summary>
/// Constrói o envelope de cabeçalho Markdown comum a todos os documentos gerados pelo
/// portal de documentação viva (título, data de geração, versão e última atualização),
/// evitando duplicar essa lógica em cada um dos geradores.
/// </summary>
public static class MarkdownDocumentTemplate
{
    /// <summary>
    /// Envolve o corpo Markdown informado com o cabeçalho padrão do portal de documentação.
    /// </summary>
    /// <param name="title">Título do documento.</param>
    /// <param name="version">Versão do projeto exibida no cabeçalho.</param>
    /// <param name="generatedAt">Data e hora de geração do documento.</param>
    /// <param name="body">Corpo Markdown do documento, sem cabeçalho.</param>
    public static string Render(string title, string version, DateTimeOffset generatedAt, string body)
    {
        var builder = new StringBuilder();
        builder.AppendLine($"# {title}");
        builder.AppendLine();
        builder.AppendLine($"> Documento gerado automaticamente pelo Portal de Documentação Viva do BlueprintOS. Não editar manualmente.");
        builder.AppendLine();
        builder.AppendLine($"- **Versão:** {version}");
        builder.AppendLine($"- **Gerado em:** {generatedAt:yyyy-MM-dd HH:mm:ss} UTC");
        builder.AppendLine($"- **Última atualização:** {generatedAt:yyyy-MM-dd}");
        builder.AppendLine();
        builder.AppendLine("---");
        builder.AppendLine();
        builder.Append(body.TrimEnd());
        builder.AppendLine();

        return builder.ToString();
    }
}
