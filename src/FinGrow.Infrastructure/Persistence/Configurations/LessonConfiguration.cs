namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class LessonConfiguration : IEntityTypeConfiguration<Lesson>
{
    public void Configure(EntityTypeBuilder<Lesson> builder)
    {
        builder.ToTable("lessons", table =>
        {
            table.HasCheckConstraint("ck_lessons_position_positive", "position > 0");
            table.HasCheckConstraint("ck_lessons_duration_positive", "duration_minutes > 0");
        });

        builder.HasKey(lesson => lesson.Id);

        builder.Property(lesson => lesson.Id).ValueGeneratedNever();

        builder.Property(lesson => lesson.Position).IsRequired();

        builder.Property(lesson => lesson.Title)
            .HasMaxLength(Lesson.MaxTitleLength)
            .IsRequired();

        builder.Property(lesson => lesson.DurationMinutes).IsRequired();

        builder.Property(lesson => lesson.VideoUrl)
            .HasMaxLength(Lesson.MaxVideoUrlLength)
            .IsRequired();

        builder.HasIndex(lesson => new { lesson.CourseId, lesson.Position }).IsUnique();
    }
}
