using System.Text.Json;
using System.Text.Json.Nodes;
using BlueprintOS.Domain.Procurement.Suppliers;

namespace BlueprintOS.Infrastructure.Integrations.CnpjConsulta;

/// <summary>Sanitizador de snapshot bruto específico do contrato BrasilAPI (B2.7/ADR-0023). Não é um
/// mecanismo genérico de DLP — conhece explicitamente o vocabulário do payload da BrasilAPI e remove,
/// antes de qualquer persistência, os campos que nunca podem chegar ao snapshot de auditoria. Um
/// futuro Provider diferente forneceria seu próprio sanitizador (via
/// <see cref="BlueprintOS.Application.Procurement.Suppliers.Contracts.ICnpjConsultaProviderComSnapshot"/>)
/// sem exigir alteração de domínio nem deste sanitizador.</summary>
public static class BrasilApiSnapshotSanitizer
{
    /// <summary>Regra absoluta (ADR-0023, seção I): QSA (sócios/administradores) nunca é persistido,
    /// mesmo dentro do snapshot bruto de auditoria. A chave retornada pela BrasilAPI é <c>"qsa"</c>.</summary>
    private static readonly string[] ChavesQsa = ["qsa"];

    /// <summary>Defesa adicional, não específica de QSA: nenhuma credencial/segredo/cabeçalho de
    /// autenticação deve sobreviver no snapshot, mesmo que a BrasilAPI não os retorne hoje — protege
    /// contra mudanças futuras de contrato do Provider sem exigir nova revisão de segurança.</summary>
    private static readonly string[] ChavesSegredo =
        ["token", "authorization", "auth", "senha", "password", "secret", "apikey", "api_key", "chave", "key"];

    /// <summary>Sanitiza o corpo bruto (texto) retornado pela BrasilAPI, removendo QSA e qualquer
    /// campo sensível, e aplica o limite de tamanho do snapshot
    /// (<see cref="FornecedorCnpjConsultaHistorico.LimitePayloadBrutoCaracteres"/>). Nunca lança
    /// exceção: corpo vazio, nulo ou impossível de sanitizar de forma segura resulta em snapshot
    /// nulo, nunca em snapshot bruto não sanitizado.</summary>
    /// <returns>Snapshot sanitizado (ou <c>null</c> se não houver corpo útil/seguro) e uma flag
    /// indicando se havia um corpo útil que foi descartado por exceder o limite de tamanho.</returns>
    public static (string? SnapshotSanitizado, bool DescartadoPorTamanho) Sanitizar(string? rawBody)
    {
        if (string.IsNullOrWhiteSpace(rawBody)) return (null, false);

        JsonNode? node;
        try
        {
            node = JsonNode.Parse(rawBody);
        }
        catch (JsonException)
        {
            // Corpo não é JSON válido (ex.: resposta corrompida/HTML de erro de infraestrutura).
            // Sem estrutura conhecida, não há como garantir remoção de QSA/segredos por chave —
            // por segurança, não persistimos texto livre não estruturado como snapshot.
            return (null, false);
        }

        if (node is not JsonObject obj)
        {
            // Corpo é JSON válido, mas não é um objeto (ex.: array, escalar) — fora do contrato
            // conhecido da BrasilAPI; nada de estruturado a sanitizar com segurança.
            return (null, false);
        }

        RemoverChaves(obj, ChavesQsa);
        RemoverChaves(obj, ChavesSegredo);

        var sanitizado = obj.ToJsonString();
        return sanitizado.Length > FornecedorCnpjConsultaHistorico.LimitePayloadBrutoCaracteres
            ? (null, true)
            : (sanitizado, false);
    }

    private static void RemoverChaves(JsonObject objeto, string[] chaves)
    {
        foreach (var propriedade in objeto.Select(kv => kv.Key).ToList())
        {
            if (chaves.Any(chave => string.Equals(chave, propriedade, StringComparison.OrdinalIgnoreCase)))
            {
                objeto.Remove(propriedade);
            }
        }
    }
}
