using BlueprintOS.Application.Identity.Models;

namespace BlueprintOS.Application.Identity.Contracts;

/// <summary>Fornece a identidade do solicitante sem acoplar casos de uso ao mecanismo de autenticação.</summary>
public interface ICurrentIdentity
{
    RequestIdentity GetRequired();
}
