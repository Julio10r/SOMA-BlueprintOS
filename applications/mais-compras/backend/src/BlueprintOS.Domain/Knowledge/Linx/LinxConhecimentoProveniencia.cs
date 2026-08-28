namespace BlueprintOS.Domain.Knowledge.Linx;

/// <summary>Proveniência explícita do conhecimento persistido (Work Order O1.13.5, seção 11). Nunca existe
/// promoção silenciosa: as transições válidas são impostas por <see cref="LinxKnowledgeEntry.Promover"/>.
///
/// <list type="bullet">
/// <item><see cref="Descoberto"/> — observado diretamente em fonte confiável (schema, documentação,
/// código, consulta, adapter existente).</item>
/// <item><see cref="Inferido"/> — conclusão produzida pelo Agent, ainda não validada.</item>
/// <item><see cref="Validado"/> — confirmado por evidência técnica ou humano autorizado.</item>
/// <item><see cref="Aprovado"/> — aceito como padrão/decisão oficial do projeto. Terminal; só alcançável a
/// partir de <see cref="Validado"/>, e apenas por quem possui a permissão dedicada
/// <c>ConhecimentoLinx.Aprovar</c> (nunca pelo próprio Agent que descobriu/inferiu).</item>
/// </list></summary>
public enum LinxConhecimentoProveniencia
{
    Descoberto = 1,
    Inferido = 2,
    Validado = 3,
    Aprovado = 4,
}
