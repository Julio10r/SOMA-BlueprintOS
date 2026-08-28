using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorCnpjConsultaHistoricoConfiguration : IEntityTypeConfiguration<FornecedorCnpjConsultaHistorico>
{
    public void Configure(EntityTypeBuilder<FornecedorCnpjConsultaHistorico> builder)
    {
        builder.ToTable("FornecedoresCnpjConsultas");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Cnpj_Cpf).HasColumnType("varchar(14)").HasMaxLength(14).IsRequired();
        builder.Property(x => x.FonteConsulta).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Usuario).IsRequired();
        builder.Property(x => x.Status).HasMaxLength(30).IsRequired();
        builder.Property(x => x.Resultado).HasMaxLength(1000).IsRequired();
        builder.Property(x => x.MensagemErro).HasMaxLength(500);
        builder.Property(x => x.CorrelationId).HasMaxLength(100).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80).IsRequired();
        builder.Property(x => x.ErpSistema).HasMaxLength(80);

        // Proveniência híbrida (B2.7/ADR-0023). TipoErro segue a mesma convenção já usada por
        // Status/Resultado nesta tabela: enum persistido como string (nunca int bruto), nulo em
        // consultas bem-sucedidas. PayloadBrutoJson é o snapshot bruto sanitizado (nunca contrato de
        // domínio), opaco/provider-agnostic, nullable, com o limite de tamanho já validado antes de
        // chegar à entidade (FornecedorCnpjConsultaHistorico.LimitePayloadBrutoCaracteres). Nenhum
        // índice foi adicionado sobre estas colunas: o expurgo por retenção varre por DataConsulta,
        // que já é o primeiro campo do índice composto abaixo, e não há leitura operacional que
        // filtre por presença/conteúdo do payload.
        builder.Property(x => x.TipoErro).HasConversion<string?>(v => v.HasValue ? v.Value.ToString() : null,
            v => v == null ? null : Enum.Parse<TipoErroConsultaCnpjHistorico>(v)).HasMaxLength(40);
        builder.Property(x => x.PayloadBrutoJson).HasColumnType("nvarchar(max)");
        builder.Property(x => x.PayloadBrutoDescartadoPorTamanho).IsRequired().HasDefaultValue(false);

        builder.HasIndex(x => new { x.BusinessUnit, x.Cnpj_Cpf, x.DataConsulta });
        builder.HasIndex(x => x.CorrelationId);
        // Índice de suporte ao expurgo por retenção (varredura por DataConsulta com filtro de payload
        // não nulo) — justificado porque a rotina de expurgo roda periodicamente sobre a tabela inteira
        // e precisa localizar eficientemente apenas os registros elegíveis, sem full scan.
        builder.HasIndex(x => x.DataConsulta).HasFilter(null);
    }
}
