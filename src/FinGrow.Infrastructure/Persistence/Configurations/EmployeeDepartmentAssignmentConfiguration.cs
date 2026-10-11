namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EmployeeDepartmentAssignmentConfiguration : IEntityTypeConfiguration<EmployeeDepartmentAssignment>
{
    public void Configure(EntityTypeBuilder<EmployeeDepartmentAssignment> builder)
    {
        builder.ToTable("employee_department_assignments", table =>
            table.HasCheckConstraint(
                "ck_employee_department_assignments_period",
                "ended_at IS NULL OR ended_at >= started_at"));

        builder.HasKey(assignment => assignment.Id);

        builder.Property(assignment => assignment.Id).ValueGeneratedNever();
        builder.Property(assignment => assignment.StartedAt).IsRequired();
        builder.Property(assignment => assignment.EndedAt);
        builder.Ignore(assignment => assignment.IsCurrent);

        builder.HasOne<Department>()
            .WithMany()
            .HasForeignKey(assignment => assignment.DepartmentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(assignment => assignment.EmployeeId)
            .IsUnique()
            .HasFilter("ended_at IS NULL")
            .HasDatabaseName("ix_employee_department_assignments_employee_id_current");

        builder.HasIndex(assignment => new { assignment.EmployeeId, assignment.StartedAt });
        builder.HasIndex(assignment => new { assignment.DepartmentId, assignment.StartedAt });
    }
}
