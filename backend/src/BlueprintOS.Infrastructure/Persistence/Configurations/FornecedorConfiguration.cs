using BlueprintOS.Domain.Procurement.Suppliers;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BlueprintOS.Infrastructure.Persistence.Configurations;

public sealed class FornecedorConfiguration : IEntityTypeConfiguration<Fornecedor>
{
    public void Configure(EntityTypeBuilder<Fornecedor> builder)
    {
        builder.ToTable("Fornecedores");
        builder.HasKey(x => x.Id);
        builder.Property(x => x.RazaoSocial).HasMaxLength(200).IsRequired();
        builder.Ignore(x => x.Nome);
        builder.Property(x => x.Cnpj_Cpf).HasColumnName("Cnpj_Cpf").HasColumnType("varchar(14)").HasMaxLength(14).IsRequired();
        builder.Ignore(x => x.Cnpj);
        builder.Property(x => x.Categoria).HasMaxLength(100);
        builder.Property(x => x.Email).HasMaxLength(254);
        builder.Property(x => x.Telefone).HasMaxLength(30);
        builder.Property(x => x.Website).HasMaxLength(500);
        builder.Property(x => x.Cidade).HasMaxLength(100);
        builder.Property(x => x.Estado).HasMaxLength(100);
        builder.Property(x => x.Pais).HasMaxLength(100);
        builder.Property(x => x.Status).HasMaxLength(40).IsRequired();
        builder.Property(x => x.ScoreIA).HasPrecision(5, 2);
        builder.Property(x => x.TemporaryUserId).IsRequired();
        builder.Property(x => x.CreatedAt).IsRequired();
        builder.Property(x => x.UpdatedAt).IsRequired();
        builder.Property(x => x.BusinessUnit).HasMaxLength(80);
        builder.Property(x => x.ErpSistema).HasMaxLength(80);
        builder.Property(x => x.ErpFornecedorId).HasMaxLength(120);
        builder.Property(x => x.OrigemInformacao).HasMaxLength(30).IsRequired();
        builder.Property(x => x.StatusSincronizacao).HasMaxLength(30).IsRequired();
        builder.Property(x => x.MensagemErroSincronizacao).HasMaxLength(500);
        builder.Property(x => x.NomeFantasia).HasMaxLength(200);
        builder.Property(x => x.TipoPessoa).HasMaxLength(20);
        builder.Property(x => x.InscricaoEstadual).HasMaxLength(40);
        builder.Property(x => x.InscricaoMunicipal).HasMaxLength(40);
        builder.Property(x => x.Cep).HasMaxLength(12);
        builder.Property(x => x.Logradouro).HasMaxLength(200);
        builder.Property(x => x.Numero).HasMaxLength(30);
        builder.Property(x => x.Complemento).HasMaxLength(100);
        builder.Property(x => x.Bairro).HasMaxLength(100);
        builder.Property(x => x.CodigoMunicipio).HasMaxLength(30);
        builder.Property(x => x.Ddd).HasMaxLength(5);
        builder.Property(x => x.EmailFiscal).HasMaxLength(254);
        builder.Property(x => x.Banco).HasMaxLength(20); builder.Property(x => x.Agencia).HasMaxLength(20);
        builder.Property(x => x.Conta).HasMaxLength(30); builder.Property(x => x.DigitosConta).HasMaxLength(5);
        builder.Property(x => x.CondicaoPagamento).HasMaxLength(80); builder.Property(x => x.TipoFornecedor).HasMaxLength(80);
        builder.Property(x => x.SubtipoFornecedor).HasMaxLength(80); builder.Property(x => x.ContaContabil).HasMaxLength(80);
        builder.Property(x => x.RegimeFiscal).HasMaxLength(80); builder.Property(x => x.CategoriasFornecimento).HasMaxLength(500);
        builder.Property(x => x.HashDadosSincronizaveis).HasMaxLength(128); builder.Property(x => x.OrigemUltimaAlteracao).HasMaxLength(30);
        builder.Property(x => x.Beneficiador).IsRequired();
        builder.Property(x => x.Licenciado).IsRequired();
        builder.Property(x => x.Versao).IsRequired();
        builder.HasOne<FornecedorDominioErp>().WithMany().HasForeignKey(x => x.CondicaoPagamentoDominioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FornecedorDominioErp>().WithMany().HasForeignKey(x => x.TipoFornecedorDominioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne<FornecedorDominioErp>().WithMany().HasForeignKey(x => x.SubtipoFornecedorDominioId).OnDelete(DeleteBehavior.Restrict);
        builder.HasIndex(x => x.Cnpj_Cpf).IsUnique();
        builder.HasIndex(x => new { x.BusinessUnit, x.ErpSistema, x.ErpFornecedorId }).IsUnique()
            .HasFilter("[BusinessUnit] IS NOT NULL AND [ErpSistema] IS NOT NULL AND [ErpFornecedorId] IS NOT NULL");
        builder.HasIndex(x => x.RazaoSocial);
        builder.HasIndex(x => x.TemporaryUserId);
    }
}
