namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class LessonCompletionConfiguration : IEntityTypeConfiguration<LessonCompletion>
{
    public void Configure(EntityTypeBuilder<LessonCompletion> builder)
    {
        builder.ToTable("lesson_completions");

        builder.HasKey(completion => completion.Id);

        builder.Property(completion => completion.Id).ValueGeneratedNever();

        builder.Property(completion => completion.CompletedAt).IsRequired();

        builder.HasOne<Employee>()
            .WithMany()
            .HasForeignKey(completion => completion.EmployeeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Lesson>()
            .WithMany()
            .HasForeignKey(completion => completion.LessonId)
            .OnDelete(DeleteBehavior.Cascade);

        // Una leccion se termina una sola vez; el indice tambien cubre el progreso por empleado.
        builder.HasIndex(completion => new { completion.EmployeeId, completion.LessonId }).IsUnique();
    }
}
