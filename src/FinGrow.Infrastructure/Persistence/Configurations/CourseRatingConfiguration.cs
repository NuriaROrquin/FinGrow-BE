namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class CourseRatingConfiguration : IEntityTypeConfiguration<CourseRating>
{
    public void Configure(EntityTypeBuilder<CourseRating> builder)
    {
        builder.ToTable("course_ratings", table =>
            table.HasCheckConstraint(
                "ck_course_ratings_score_range",
                $"score BETWEEN {CourseRating.MinScore} AND {CourseRating.MaxScore}"));

        builder.HasKey(rating => rating.Id);

        builder.Property(rating => rating.Id).ValueGeneratedNever();

        builder.Property(rating => rating.Score).IsRequired();
        builder.Property(rating => rating.RatedAt).IsRequired();
        builder.Property(rating => rating.UpdatedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(rating => rating.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Course>()
            .WithMany()
            .HasForeignKey(rating => rating.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(rating => new { rating.EmployeeId, rating.CourseId }).IsUnique();
        builder.HasIndex(rating => rating.CourseId);
    }
}
