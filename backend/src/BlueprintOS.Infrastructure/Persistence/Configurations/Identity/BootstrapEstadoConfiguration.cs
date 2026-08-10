using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

/// <summary>Reforço estrutural de linha única (Work Order O1.4.3, seção 12): a chave primária fixa
/// (<see cref="BootstrapEstado.IdFixo"/>) já elimina, por construção via EF Core, a possibilidade de mais de
/// uma linha — nenhuma lógica de aplicação adicional é necessária além de sempre filtrar por esse Id
/// explicitamente. A linha inicial é criada exclusivamente pela seed migration (<c>AddBootstrapEstado</c>).</summary>
public sealed class BootstrapEstadoConfiguration : IEntityTypeConfiguration<BootstrapEstado>
{
    public void Configure(EntityTypeBuilder<BootstrapEstado> builder)
    {
        builder.ToTable("BootstrapEstado");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Id).ValueGeneratedNever();

        // O1.4.3.2 — compare-and-swap real da transição Concluido=false→true (Work Order O1.4.3, seção 13,
        // passo 7). Requer migration nova (coluna ROWVERSION) — não criada nesta etapa; ver relatório de
        // conclusão da implementação, aguardando autorização explícita do Product Owner antes de
        // `dotnet ef migrations add`/`database update`.
        builder.Property(x => x.RowVersion).IsRowVersion();

        builder.HasData(new
        {
            Id = BootstrapEstado.IdFixo,
            Concluido = false,
            ConcluidoEm = (DateTimeOffset?)null,
            UsuarioAdministradorSeniorId = (Guid?)null,
        });
    }
}
