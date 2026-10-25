namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using FinGrow.Infrastructure.Persistence.Protection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        // Una baja pedida por el empleado (HU-74) es definitiva: la cuenta no puede volver a estar activa.
        builder.ToTable("users", table =>
            table.HasCheckConstraint("ck_users_withdrawn_is_inactive", "withdrawn_at IS NULL OR NOT is_active"));

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Email)
            .HasColumnName("email")
            .HasConversion(email => email.Value, value => Email.From(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(user => user.PasswordHash)
            .HasMaxLength(User.MaxPasswordHashLength)
            .IsRequired();

        builder.Property(user => user.Role)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(user => user.IsActive).IsRequired();
        builder.Property(user => user.LastLoginAt);

        builder.Property(user => user.TwoFactorSecret)
            .HasAnnotation(EncryptedStringConverter.Annotation, true);

        builder.Property(user => user.TwoFactorEnabledAt);
        builder.Ignore(user => user.IsTwoFactorEnabled);

        builder.Property(user => user.CreatedAt).IsRequired();
        builder.Property(user => user.UpdatedAt).IsRequired();

        builder.HasIndex(user => user.Email).IsUnique();
    }
}
