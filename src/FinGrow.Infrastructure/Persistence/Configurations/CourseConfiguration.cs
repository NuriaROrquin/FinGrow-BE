namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Common;
using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CourseConfiguration : IEntityTypeConfiguration<Course>
{
    public void Configure(EntityTypeBuilder<Course> builder)
    {
        builder.ToTable("courses");

        builder.HasKey(course => course.Id);

        builder.Property(course => course.Slug)
            .HasMaxLength(Slug.MaxLength)
            .IsRequired();

        builder.Property(course => course.Title)
            .HasMaxLength(Course.MaxTitleLength)
            .IsRequired();

        builder.Property(course => course.Description)
            .HasMaxLength(Course.MaxDescriptionLength)
            .IsRequired();

        builder.Property(course => course.Level)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(course => course.Category)
            .HasConversion<string>()
            .HasMaxLength(32)
            .IsRequired();

        builder.Property(course => course.RelatedInvestmentType)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(course => course.CreatedAt).IsRequired();
        builder.Property(course => course.UpdatedAt).IsRequired();

        builder.Ignore(course => course.IsPublished);
        builder.Ignore(course => course.DurationMinutes);

        builder.HasMany(course => course.Lessons)
            .WithOne()
            .HasForeignKey(lesson => lesson.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.Navigation(course => course.Lessons)
            .UsePropertyAccessMode(PropertyAccessMode.Field)
            .AutoInclude();

        builder.HasIndex(course => course.Slug).IsUnique();
        builder.HasIndex(course => new { course.Category, course.Level });
        builder.HasIndex(course => course.RelatedInvestmentType);
    }
}
