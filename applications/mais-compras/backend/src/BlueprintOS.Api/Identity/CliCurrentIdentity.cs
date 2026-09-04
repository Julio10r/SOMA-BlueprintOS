using BlueprintOS.Application.Identity.Contracts;
using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Api.Identity;

/// <summary>Identidade fixa usada exclusivamente pela composição isolada de CLI (fora de um host HTTP —
/// B3 Bloco 5A, `sincronizar-item-fiscal-erp` em `Program.cs`). Nunca registrada no host ASP.NET real
/// (<c>SessionCurrentIdentity</c> continua sendo a única identidade da API); a Unidade de Negócio é sempre
/// resolvida a partir de um registro real já existente em MAISCOMPRAS antes da construção desta classe,
/// nunca sintética/inventada — não contorna RBAC (os casos de uso executados via CLI não fazem checagem de
/// permissão, mesma natureza das demais operações administrativas deste arquivo).</summary>
public sealed class CliCurrentIdentity(Guid unidadeNegocioId) : ICurrentIdentity
{
    public RequestIdentity GetRequired() => new(Guid.NewGuid(), "SistemaCli", unidadeNegocioId);
}
