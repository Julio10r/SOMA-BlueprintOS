namespace BlueprintOS.Domain.Knowledge.Linx;

/// <summary>Os dois papéis de Agent especialista Linx fundados pela O1.13.5. Consumidores/produtores da
/// base de conhecimento — nunca executores autônomos de SQL de escrita ou integração operacional real
/// (Onda 3/4).</summary>
public enum LinxEspecialista
{
    /// <summary>Conhecimento funcional/técnico do ERP Visual Linx: regras, fluxos, entidades,
    /// comportamento, integrações, customizações.</summary>
    LinxErpSpecialist = 1,

    /// <summary>Conhecimento estrutural do banco Visual Linx/SQL Server (`SOMA_DESENV`): schema, tabelas,
    /// views, procedures, colunas, relacionamentos.</summary>
    LinxDatabaseSpecialist = 2,
}
