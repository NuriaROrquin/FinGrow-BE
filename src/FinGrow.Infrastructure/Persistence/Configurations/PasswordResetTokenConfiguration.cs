namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class PasswordResetTokenConfiguration : IEntityTypeConfiguration<PasswordResetToken>
{
    public void Configure(EntityTypeBuilder<PasswordResetToken> builder)
    {
        builder.ToTable("password_reset_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Role)
            .HasMaxLength(PasswordResetToken.MaxRoleLength)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(PasswordResetToken.HashLength)
            .IsRequired();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        // UserId es polimorfico como en refresh_tokens (Employee o Company segun Role): la FK real va
        // contra la empresa, que es lo que se lleva los enlaces en cascada.
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(token => token.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique();

        // Cubre el freno a pedidos repetidos y la invalidacion de los enlaces pendientes al canjear uno.
        builder.HasIndex(token => new { token.UserId, token.CreatedAt });
    }
}
