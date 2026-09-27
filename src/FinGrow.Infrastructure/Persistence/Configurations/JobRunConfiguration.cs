namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class JobRunConfiguration : IEntityTypeConfiguration<JobRun>
{
    public void Configure(EntityTypeBuilder<JobRun> builder)
    {
        builder.ToTable("job_runs", table =>
            table.HasCheckConstraint("ck_job_runs_finished_after_started", "finished_at IS NULL OR finished_at >= started_at"));

        builder.HasKey(run => run.Id);

        builder.Property(run => run.JobName)
            .HasMaxLength(JobRun.MaxJobNameLength)
            .IsRequired();

        builder.Property(run => run.Status)
            .HasConversion<string>()
            .HasMaxLength(20)
            .IsRequired();

        builder.Property(run => run.StartedAt).IsRequired();

        builder.Property(run => run.Summary)
            .HasMaxLength(JobRun.MaxSummaryLength);

        builder.Property(run => run.Error)
            .HasMaxLength(JobRun.MaxErrorLength);

        builder.Ignore(run => run.IsFinished);
        builder.Ignore(run => run.Duration);

        builder.HasIndex(run => new { run.JobName, run.StartedAt });
    }
}
