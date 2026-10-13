namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EmployeeAchievementConfiguration : IEntityTypeConfiguration<EmployeeAchievement>
{
    public void Configure(EntityTypeBuilder<EmployeeAchievement> builder)
    {
        builder.ToTable("employee_achievements", table =>
        {
            table.HasCheckConstraint("ck_employee_achievements_achieved_before_recorded", "achieved_at <= created_at");
            table.HasCheckConstraint(
                "ck_employee_achievements_celebrated_after_recorded",
                "celebrated_at IS NULL OR celebrated_at >= created_at");
        });

        builder.HasKey(achievement => achievement.Id);

        builder.Property(achievement => achievement.Id).ValueGeneratedNever();

        builder.Property(achievement => achievement.Code)
            .HasConversion<string>()
            .HasMaxLength(AchievementConfiguration.MaxCodeLength)
            .IsRequired();

        builder.Property(achievement => achievement.AchievedAt).IsRequired();
        builder.Property(achievement => achievement.CelebratedAt);
        builder.Property(achievement => achievement.CreatedAt).IsRequired();

        builder.Ignore(achievement => achievement.IsCelebrated);

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(achievement => achievement.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Achievement>()
            .WithMany()
            .HasForeignKey(achievement => achievement.Code)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(achievement => new { achievement.EmployeeId, achievement.Code }).IsUnique();
    }
}
