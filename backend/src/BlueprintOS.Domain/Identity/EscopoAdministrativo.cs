namespace BlueprintOS.Domain.Identity;

/// <summary>Escopo administrativo do ator — responde "EM QUAL Unidade de Negócio" uma permissão pode ser
/// exercida, uma pergunta ortogonal ao RBAC (que responde apenas "O QUE" o ator pode fazer). Decisão
/// formal do Product Owner (Gate Final da Onda 1): existem dois níveis administrativos distintos no
/// +Compras, e nenhuma permissão do catálogo — nem mesmo <see cref="PermissaoCatalogo.SistemaGerenciar"/> —
/// é, por si só, um passe cross-BU.</summary>
public enum EscopoAdministrativo
{
    /// <summary>Administração de Negócio: o ator só pode agir dentro da própria Unidade de Negócio,
    /// mesmo possuindo a permissão RBAC necessária para o recurso.</summary>
    Negocio = 0,

    /// <summary>Administração de Produto: o ator é reconhecido como Administrador Sênior (Perfil
    /// <see cref="Perfil.AdministradorSenior"/> ativo) e pode atravessar Unidades de Negócio
    /// legitimamente, desde que autorizado explicitamente pelo backend — nunca por simplesmente confiar
    /// em um <c>unidadeNegocioId</c> recebido do cliente.</summary>
    Produto = 1,
}
