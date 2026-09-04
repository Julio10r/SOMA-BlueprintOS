namespace BlueprintOS.Application.Identity.Models;

/// <summary>Projeção de leitura de um Item Fiscal (B3 — Bloco 3/5A, Discovery homologado). As descrições de
/// Conta Contábil/Unidade de Medida são enriquecidas a partir da leitura combinada ERP+metadados locais
/// (Blocos 1/2) — <c>null</c> quando o código de apoio está ausente (origem Linx incompleta) ou, para um
/// código preenchido, deixou de existir/ficou inválido depois da criação/edição.
///
/// <c>Ativo</c> é situação CADASTRAL (nunca falsificada). <c>AptidaoOperacional</c> é um conceito
/// DIFERENTE, computado em tempo de leitura (Bloco 5A, decisão do Product Owner): só é <c>true</c> quando
/// Conta Contábil E Unidade de Medida estão preenchidas, existem e estão ativas — um Item Fiscal pode ser
/// <c>Ativo=true</c> e <c>AptidaoOperacional=false</c> ao mesmo tempo (ex.: Item real do Linx sem Conta
/// Contábil). <c>MotivosInaptidao</c> lista, em português, cada requisito não satisfeito — vazia quando
/// <c>AptidaoOperacional</c> é <c>true</c>.</summary>
public sealed record ItemFiscalDto(
    Guid Id,
    string Codigo,
    string Descricao,
    string? UnidadeMedidaCodigoErp,
    string? UnidadeMedidaDescricao,
    string? ContaContabilCodigoErp,
    string? ContaContabilDescricao,
    bool Ativo,
    string OrigemInformacao,
    bool AptidaoOperacional,
    IReadOnlyList<string> MotivosInaptidao,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação de Item Fiscal. <c>UnidadeNegocioId</c> é deliberadamente ausente — vem
/// sempre da identidade autenticada (mesmo cuidado de <see cref="UnidadeAlocacaoInput"/>/<see cref="UsuarioInput"/>).
/// Granularidade de <c>Codigo</c>/<c>Descricao</c> é livre — decisão da área de Compras, o +Compras não
/// impõe nível de detalhe (Discovery B3 homologado).</summary>
public sealed record ItemFiscalCriarInput(string Codigo, string Descricao, string UnidadeMedidaCodigoErp, string ContaContabilCodigoErp);

/// <summary>Entrada de edição de Item Fiscal. Sem <c>Codigo</c>: imutável após a criação.</summary>
public sealed record ItemFiscalAtualizarInput(string Descricao, string UnidadeMedidaCodigoErp, string ContaContabilCodigoErp);
