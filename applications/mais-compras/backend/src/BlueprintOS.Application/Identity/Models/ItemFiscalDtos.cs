namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de um Item Fiscal (B3 — Bloco 3, Discovery homologado). As descrições de
/// Conta Contábil/Unidade de Medida são enriquecidas a partir da leitura combinada ERP+metadados locais
/// (Blocos 1/2) — <c>null</c> quando o código de apoio, por qualquer motivo, deixou de existir/ficou
/// inválido depois da criação/edição (nunca deveria acontecer em uso normal, já que a validação impede
/// seleção inválida, mas a projeção não presume).</summary>
public sealed record ItemFiscalDto(
    Guid Id,
    string Codigo,
    string Descricao,
    string UnidadeMedidaCodigoErp,
    string? UnidadeMedidaDescricao,
    string ContaContabilCodigoErp,
    string? ContaContabilDescricao,
    bool Ativo,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação de Item Fiscal. <c>UnidadeNegocioId</c> é deliberadamente ausente — vem
/// sempre da identidade autenticada (mesmo cuidado de <see cref="UnidadeAlocacaoInput"/>/<see cref="UsuarioInput"/>).
/// Granularidade de <c>Codigo</c>/<c>Descricao</c> é livre — decisão da área de Compras, o +Compras não
/// impõe nível de detalhe (Discovery B3 homologado).</summary>
public sealed record ItemFiscalCriarInput(string Codigo, string Descricao, string UnidadeMedidaCodigoErp, string ContaContabilCodigoErp);

/// <summary>Entrada de edição de Item Fiscal. Sem <c>Codigo</c>: imutável após a criação.</summary>
public sealed record ItemFiscalAtualizarInput(string Descricao, string UnidadeMedidaCodigoErp, string ContaContabilCodigoErp);
