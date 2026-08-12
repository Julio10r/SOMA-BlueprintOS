using System.Security.Claims;
using BlueprintOS.Domain.Identity;
using Microsoft.AspNetCore.Authorization;

namespace BlueprintOS.Api.Authorization;

/// <summary>Tipo da claim que transporta uma permissão efetiva do usuário dentro de
/// <c>HttpContext.User</c>. As claims são emitidas exclusivamente pelos authentication handlers do
/// servidor, a partir das permissões resolvidas no banco — nunca lidas de um cabeçalho, corpo ou cookie
/// controlado pelo cliente.</summary>
public static class RbacClaims
{
    public const string Permissao = "maiscompras_permissao";

    /// <summary>Transporta o <see cref="EscopoAdministrativo"/> do ator (Gate Final da Onda 1) — separado
    /// deliberadamente de <see cref="Permissao"/>: RBAC responde "o quê", esta claim responde "em qual
    /// Unidade de Negócio". Emitida exclusivamente pelos authentication handlers, a partir do Perfil
    /// "Administrador Sênior" ativo resolvido no banco — nunca lida de cabeçalho, corpo ou cookie
    /// controlado pelo cliente.</summary>
    public const string EscopoAdministrativo = "maiscompras_escopo_administrativo";
}

/// <summary>Exige uma permissão específica do catálogo (O1.5 — RBAC Real).</summary>
public sealed class PermissaoRequirement(string codigo) : IAuthorizationRequirement
{
    public string Codigo { get; } = codigo;
}

/// <summary>Handler idiomático do ASP.NET Core Authorization. Deliberadamente não consulta o banco nem
/// executa I/O: as permissões efetivas já foram resolvidas uma única vez por requisição pelo
/// authentication handler, exatamente como <see cref="Identity.SessionCurrentIdentity"/> faz com a
/// identidade (O1.4.2.1). Não autorizar aqui produz <c>403 Forbidden</c>; a ausência de autenticação já
/// produziu <c>401 Unauthorized</c> antes, na <c>FallbackPolicy</c> global.</summary>
public sealed class PermissaoAuthorizationHandler : AuthorizationHandler<PermissaoRequirement>
{
    protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, PermissaoRequirement requirement)
    {
        if (context.User.Identity?.IsAuthenticated != true)
        {
            return Task.CompletedTask;
        }

        var possui = context.User.FindAll(RbacClaims.Permissao)
            .Any(claim => string.Equals(claim.Value, requirement.Codigo, StringComparison.OrdinalIgnoreCase));

        if (possui) context.Succeed(requirement);
        return Task.CompletedTask;
    }
}

/// <summary>Registro central das policies de RBAC. Existe uma policy por permissão do catálogo, gerada
/// por iteração sobre <see cref="PermissaoCatalogo"/> — nenhum nome de policy nem código de permissão é
/// escrito literalmente em <c>Program.cs</c> ou nos endpoints.</summary>
public static class RbacPolicies
{
    private const string Prefixo = "permissao:";

    /// <summary>Nome da policy correspondente a uma permissão. Falha imediatamente para um código fora do
    /// catálogo, transformando um erro de digitação em erro de inicialização em vez de um endpoint
    /// acidentalmente aberto.</summary>
    public static string For(string codigoPermissao)
    {
        var canonico = PermissaoCatalogo.Normalizar(codigoPermissao)
            ?? throw new InvalidOperationException(
                $"'{codigoPermissao}' não pertence ao catálogo de permissões (PermissaoCatalogo).");

        return Prefixo + canonico;
    }

    public static AuthorizationOptions AddRbacPolicies(this AuthorizationOptions options)
    {
        foreach (var definicao in PermissaoCatalogo.Todas)
        {
            options.AddPolicy(Prefixo + definicao.Codigo, policy => policy
                .RequireAuthenticatedUser()
                .AddRequirements(new PermissaoRequirement(definicao.Codigo)));
        }

        return options;
    }

    /// <summary>Transforma as permissões efetivas em claims. Usado pelos authentication handlers.</summary>
    public static IEnumerable<Claim> ToClaims(IEnumerable<string> permissoesEfetivas) =>
        permissoesEfetivas.Select(codigo => new Claim(RbacClaims.Permissao, codigo));
}
