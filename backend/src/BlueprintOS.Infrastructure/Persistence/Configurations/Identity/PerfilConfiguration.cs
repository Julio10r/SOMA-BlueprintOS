using BlueprintOS.Domain.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations.Identity;

public sealed class PerfilConfiguration : IEntityTypeConfiguration<Perfil>
{
    public void Configure(EntityTypeBuilder<Perfil> builder)
    {
        builder.ToTable("Perfis");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Nome).IsRequired().HasMaxLength(120);

        // Fecha a divergência nº 4 da Work Order O1.4.3 (seção 4/12): sem este índice, uma corrida teórica
        // poderia criar dois Perfis com o mesmo nome na mesma Unidade de Negócio — relevante porque a
        // conclusão do Bootstrap (O1.4.3.2) faz "criar ou reaproveitar" o Perfil "Administrador Sênior" via
        // SingleOrDefaultAsync por (UnidadeNegocioId, Nome).
        builder.HasIndex(x => new { x.UnidadeNegocioId, x.Nome })
            .IsUnique()
            .HasDatabaseName("IX_Perfis_UnidadeNegocioId_Nome");
    }
}

public sealed class PermissaoConfiguration : IEntityTypeConfiguration<Permissao>
{
    public void Configure(EntityTypeBuilder<Permissao> builder)
    {
        builder.ToTable("Permissoes");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Codigo).IsRequired().HasMaxLength(100);
        builder.HasIndex(x => x.Codigo).IsUnique();
    }
}

public sealed class PerfilPermissaoConfiguration : IEntityTypeConfiguration<PerfilPermissao>
{
    public void Configure(EntityTypeBuilder<PerfilPermissao> builder)
    {
        builder.ToTable("PerfisPermissoes");
        builder.HasKey(x => new { x.PerfilId, x.PermissaoId });
    }
}

public sealed class UsuarioPerfilConfiguration : IEntityTypeConfiguration<UsuarioPerfil>
{
    public void Configure(EntityTypeBuilder<UsuarioPerfil> builder)
    {
        builder.ToTable("UsuariosPerfis");
        builder.HasKey(x => new { x.UsuarioId, x.PerfilId });
    }
}

public sealed class UsuarioCentroCustoConfiguration : IEntityTypeConfiguration<UsuarioCentroCusto>
{
    public void Configure(EntityTypeBuilder<UsuarioCentroCusto> builder)
    {
        builder.ToTable("UsuariosCentrosCusto");
        builder.HasKey(x => new { x.UsuarioId, x.CentroCustoCodigoErp });
        builder.Property(x => x.CentroCustoCodigoErp).IsRequired().HasMaxLength(50);
    }
}
