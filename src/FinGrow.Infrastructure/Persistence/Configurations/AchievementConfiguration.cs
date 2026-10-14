namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class AchievementConfiguration : IEntityTypeConfiguration<Achievement>
{
    internal const int MaxCodeLength = 40;

    public void Configure(EntityTypeBuilder<Achievement> builder)
    {
        builder.ToTable("achievements");

        builder.HasKey(achievement => achievement.Code);

        builder.Property(achievement => achievement.Code)
            .HasConversion<string>()
            .HasMaxLength(MaxCodeLength)
            .ValueGeneratedNever();

        builder.Property(achievement => achievement.Name)
            .HasMaxLength(Achievement.MaxNameLength)
            .IsRequired();

        builder.Property(achievement => achievement.Description)
            .HasMaxLength(Achievement.MaxDescriptionLength)
            .IsRequired();

        builder.Property(achievement => achievement.Condition)
            .HasMaxLength(Achievement.MaxConditionLength)
            .IsRequired();

        builder.Property(achievement => achievement.Evaluation)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(achievement => achievement.IsMajor).IsRequired();

        builder.HasData(AchievementCatalog.All);
    }
}
