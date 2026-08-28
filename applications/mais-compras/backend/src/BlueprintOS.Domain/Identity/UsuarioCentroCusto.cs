namespace BlueprintOS.Domain.Identity;

/// <summary>Escopo operacional do usuário (distinto de Perfil, que é autorização funcional — ADR-0020, item 7/9).
/// Modelo preparado nesta sprint para evolução futura; não é populado nem consultado pelo fluxo de Login OTP.
/// Acesso a "todos os Centros de Custo ativos" é representado por <see cref="Usuario.Id"/> sem nenhum registro
/// nesta tabela combinado com uma flag equivalente a ser adicionada quando a tela de autorização for implementada
/// (fora do escopo de O1.4.2) — aqui apenas o vínculo explícito por Centro de Custo é modelado.</summary>
public sealed class UsuarioCentroCusto
{
    public Guid UsuarioId { get; private set; }
    public string CentroCustoCodigoErp { get; private set; }

    private UsuarioCentroCusto() { CentroCustoCodigoErp = string.Empty; }

    public UsuarioCentroCusto(Guid usuarioId, string centroCustoCodigoErp)
    {
        if (string.IsNullOrWhiteSpace(centroCustoCodigoErp)) throw new ArgumentException("Código ERP do Centro de Custo é obrigatório.", nameof(centroCustoCodigoErp));

        UsuarioId = usuarioId;
        CentroCustoCodigoErp = centroCustoCodigoErp;
    }
}
