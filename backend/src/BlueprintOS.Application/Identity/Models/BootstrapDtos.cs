namespace BlueprintOS.Application.Identity.Models;

/// <summary>Resultado de <c>GET /bootstrap/estado</c> — leitura pública mínima, nunca expõe detalhe de
/// configuração/tentativas/e-mails autorizados (security-design-auth-o1.4.md §20.14/§20.19).</summary>
public sealed record ConsultarBootstrapEstadoResultado(bool Disponivel);

/// <summary>Resultado de <c>POST /bootstrap/iniciar</c>. <see cref="BootstrapDisponivel"/> distingue apenas
/// o caso "Concluido == true" (controller responde 404, indistinguível de rota inexistente) — nunca
/// distingue, para o restante da resposta ao cliente, entre secret inválido/e-mail não autorizado/sucesso:
/// todos os três produzem a mesma resposta genérica 200 (security-design-auth-o1.4.md §20.6).</summary>
public sealed record IniciarBootstrapResultado(bool BootstrapDisponivel);

/// <summary>Resultado de <c>POST /bootstrap/otp/verificar</c> — mesmo padrão de resposta genérica já usado
/// em <c>ValidarOtpResultado</c> (login normal): nunca diferencia "código inválido" de "código expirado" de
/// "tentativas excedidas" para o cliente.</summary>
public sealed record ValidarOtpBootstrapResultado(
    bool Sucesso,
    string? MotivoGenerico,
    string? SessionRawToken,
    string? EmailCandidato);

/// <summary>Payload de Unidade de Negócio de <c>POST /bootstrap/concluir</c> (Work Order O1.4.3, seção 13/15):
/// <paramref name="Id"/> informado reaproveita uma Unidade de Negócio existente sem Administrador Sênior;
/// caso contrário, <paramref name="Nome"/>/<paramref name="Slug"/> criam uma nova.</summary>
public sealed record UnidadeNegocioBootstrapPayload(Guid? Id, string? Nome, string? Slug);

/// <summary>Payload do Administrador Sênior de <c>POST /bootstrap/concluir</c> — o e-mail nunca é reenviado
/// aqui (Work Order O1.4.3, seção 13, passo 3): vem exclusivamente da <c>BootstrapSessao</c> já validada por
/// OTP, nunca do payload da requisição.</summary>
public sealed record AdministradorSeniorBootstrapPayload(string? Nome);

/// <summary>Resultado de <c>POST /bootstrap/concluir</c> — sucesso ponta a ponta ou motivo de negócio
/// genérico de rejeição (nunca detalhe interno de infraestrutura).</summary>
public sealed record ConcluirBootstrapResultado(
    bool Sucesso,
    string? MotivoGenerico,
    Guid? UsuarioId,
    string? Email,
    string? Nome,
    Guid? UnidadeNegocioId);
