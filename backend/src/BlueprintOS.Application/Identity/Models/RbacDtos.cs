namespace BlueprintOS.Application.Identity.Models;

/// <summary>Uma permissão do catálogo global, como devolvida à interface.</summary>
public sealed record PermissaoCatalogoDto(string Codigo, string Recurso, string Acao, string Descricao);

/// <summary>Projeção de leitura de um Perfil. <c>Permissoes</c> traz os códigos canônicos do catálogo;
/// <c>UsuariosVinculados</c> é contado no banco (nunca mantido como contador denormalizado).</summary>
public sealed record PerfilDto(
    Guid Id,
    string Nome,
    string Descricao,
    Guid UnidadeNegocioId,
    bool Ativo,
    IReadOnlyList<string> Permissoes,
    int UsuariosVinculados,
    DateTimeOffset CriadoEm,
    DateTimeOffset AtualizadoEm);

/// <summary>Entrada de criação/edição de Perfil. Note a ausência deliberada de <c>UnidadeNegocioId</c>:
/// a Unidade de Negócio é sempre a da identidade autenticada, nunca um valor escolhido pelo cliente
/// (evita atribuição cruzada de Perfis entre Unidades de Negócio).</summary>
public sealed record PerfilInput(string Nome, string Descricao, IReadOnlyList<string> Permissoes);

public enum RbacFalha
{
    Nenhuma = 0,
    NomeObrigatorio,
    NomeDuplicado,
    PermissaoDesconhecida,
    PerfilNaoEncontrado,
    UltimoPerfilAdministrativo,
    EscalonamentoDePrivilegio,
}

/// <summary>Resultado de operação de escrita de Perfil. Nunca lança exceção para falha de regra de
/// negócio esperada — a camada de API traduz <see cref="Falha"/> em código HTTP.</summary>
public sealed record RbacResultado<T>(bool Sucesso, RbacFalha Falha, string? Mensagem, T? Valor)
{
    public static RbacResultado<T> Ok(T valor) => new(true, RbacFalha.Nenhuma, null, valor);
    public static RbacResultado<T> Erro(RbacFalha falha, string mensagem) => new(false, falha, mensagem, default);
}
