namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class DepartmentConfiguration : IEntityTypeConfiguration<Department>
{
    public void Configure(EntityTypeBuilder<Department> builder)
    {
        builder.ToTable("departments");

        builder.HasKey(department => department.Id);

        builder.Property(department => department.Name)
            .HasMaxLength(Department.MaxNameLength)
            .IsRequired();

        builder.Property(department => department.Description)
            .HasMaxLength(Department.MaxDescriptionLength);

        builder.Property(department => department.ManagerName)
            .HasMaxLength(Department.MaxManagerNameLength);

        builder.Property(department => department.IsActive).IsRequired();
        builder.Property(department => department.CreatedAt).IsRequired();
        builder.Property(department => department.UpdatedAt).IsRequired();

        // Un departamento no sobrevive a su empresa: forma parte del agregado Company.
        builder.HasOne<Company>()
            .WithMany(company => company.Departments)
            .HasForeignKey(department => department.CompanyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasIndex(department => new { department.CompanyId, department.Name }).IsUnique();
    }
}
