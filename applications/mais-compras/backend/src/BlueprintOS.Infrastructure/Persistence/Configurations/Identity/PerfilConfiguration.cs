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

        // O1.5 — RBAC Real. `Descricao` recebe defaultValue "" na migration para que a linha do Perfil
        // "Administrador Sênior" eventualmente já criada pelo Bootstrap (O1.4.3.2) continue válida sem
        // nenhuma alteração destrutiva de dados.
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(400);
        builder.Property(x => x.CriadoEm).IsRequired();
        builder.Property(x => x.AtualizadoEm).IsRequired();

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
        builder.Property(x => x.Descricao).IsRequired().HasMaxLength(400);
        builder.HasIndex(x => x.Codigo).IsUnique();

        // O1.5 — o catálogo global de permissões é dado de referência, não dado de usuário: nasce com o
        // schema, via seed determinístico com Ids estáveis vindos de PermissaoCatalogo (fonte central
        // única). Não existe tela de criação de Permissão — a ADR-0020 (item 8) trata o catálogo como
        // atômico e definido pelo produto, não editável em runtime.
        builder.HasData(PermissaoCatalogo.Todas.Select(definicao => new
        {
            definicao.Id,
            definicao.Codigo,
            definicao.Descricao,
        }));
    }
}

public sealed class PerfilPermissaoConfiguration : IEntityTypeConfiguration<PerfilPermissao>
{
    public void Configure(EntityTypeBuilder<PerfilPermissao> builder)
    {
        builder.ToTable("PerfisPermissoes");
        builder.HasKey(x => new { x.PerfilId, x.PermissaoId });

        // O1.5 — integridade referencial explícita. Sem estas FKs, um `PerfilId`/`PermissaoId` inválido
        // (manipulação de Id, corrida de exclusão) poderia gerar vínculo órfão e, na leitura por JOIN,
        // simplesmente desaparecer — falhando de forma silenciosa em um caminho de autorização.
        // `Restrict` deliberado: nenhuma exclusão em cascata em dado de RBAC.
        builder.HasOne<Perfil>().WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Permissao>().WithMany().HasForeignKey(x => x.PermissaoId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioPerfilConfiguration : IEntityTypeConfiguration<UsuarioPerfil>
{
    public void Configure(EntityTypeBuilder<UsuarioPerfil> builder)
    {
        builder.ToTable("UsuariosPerfis");
        builder.HasKey(x => new { x.UsuarioId, x.PerfilId });

        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<Perfil>().WithMany().HasForeignKey(x => x.PerfilId).OnDelete(DeleteBehavior.Restrict);
    }
}

public sealed class UsuarioCentroCustoConfiguration : IEntityTypeConfiguration<UsuarioCentroCusto>
{
    public void Configure(EntityTypeBuilder<UsuarioCentroCusto> builder)
    {
        builder.ToTable("UsuariosCentrosCusto");
        builder.HasKey(x => new { x.UsuarioId, x.CentroCustoCodigoErp });
        builder.Property(x => x.CentroCustoCodigoErp).IsRequired().HasMaxLength(50);

        // O1.6 — fecha o FK para Usuarios que faltava desde a criação da tabela (O1.4.2): sem ele, um
        // UsuarioId inválido poderia gerar vínculo órfão. Não há FK para "Centro de Custo": não existe
        // tabela local de Centro de Custo nesta sprint (a integração ERP, D3/ADR-0021, é escopo futuro) —
        // o vínculo é pelo código ERP como texto.
        builder.HasOne<Usuario>().WithMany().HasForeignKey(x => x.UsuarioId).OnDelete(DeleteBehavior.Restrict);
    }
}
