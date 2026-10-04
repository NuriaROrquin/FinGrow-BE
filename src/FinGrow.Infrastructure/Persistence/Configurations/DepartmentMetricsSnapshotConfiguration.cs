namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class DepartmentMetricsSnapshotConfiguration : IEntityTypeConfiguration<DepartmentMetricsSnapshot>
{
    public void Configure(EntityTypeBuilder<DepartmentMetricsSnapshot> builder)
    {
        builder.ToTable("department_metrics_snapshots", table =>
            table.HasCheckConstraint("ck_department_metrics_snapshots_period_starts_on_day_one", "extract(day from period_start) = 1"));

        builder.HasKey(snapshot => snapshot.Id);

        builder.Property(snapshot => snapshot.PeriodStart).IsRequired();
        builder.Property(snapshot => snapshot.ComputedAt).IsRequired();

        builder.OwnsPeriodMetrics(snapshot => snapshot.Metrics);

        builder.Ignore(snapshot => snapshot.Period);

        builder.HasOne<Company>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(snapshot => snapshot.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(snapshot => new { snapshot.DepartmentId, snapshot.PeriodStart }).IsUnique();
        builder.HasIndex(snapshot => new { snapshot.CompanyId, snapshot.PeriodStart });
    }
}
