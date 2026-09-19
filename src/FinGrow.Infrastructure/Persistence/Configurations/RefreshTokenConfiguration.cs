namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class RefreshTokenConfiguration : IEntityTypeConfiguration<RefreshToken>
{
    public void Configure(EntityTypeBuilder<RefreshToken> builder)
    {
        builder.ToTable("refresh_tokens");

        builder.HasKey(token => token.Id);

        builder.Property(token => token.Role)
            .HasMaxLength(RefreshToken.MaxRoleLength)
            .IsRequired();

        builder.Property(token => token.TokenHash)
            .HasMaxLength(RefreshToken.HashLength)
            .IsRequired();

        builder.Property(token => token.CreatedAt).IsRequired();
        builder.Property(token => token.ExpiresAt).IsRequired();

        // Sin relacion a Employee: UserId es polimorfico (Employee o Company segun Role),
        // asi que no hay una unica tabla principal para una FK real sobre esa columna.
        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(token => token.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(token => token.TokenHash).IsUnique();

        // Cubre la revocacion masiva (RevokeAllForUserAsync) y la busqueda de sesiones activas.
        builder.HasIndex(token => new { token.UserId, token.RevokedAt });
    }
}
