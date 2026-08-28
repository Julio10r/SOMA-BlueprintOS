using BlueprintOS.Domain.Identity;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Contrato mínimo necessário à conclusão do Bootstrap (O1.4.3.2; Work Order O1.4.3, seção 13, passo
/// 5): vincular o Usuario recém-criado ao Perfil "Administrador Sênior".</summary>
public interface IUsuarioPerfilRepository
{
    /// <summary>Rastreia a criação — escrita real ocorre junto com as demais entidades da transação de
    /// conclusão (mesmo <c>DbContext</c> compartilhado, Work Order O1.4.3, seção 13).</summary>
    Task AdicionarAsync(UsuarioPerfil vinculo, CancellationToken ct);
}
