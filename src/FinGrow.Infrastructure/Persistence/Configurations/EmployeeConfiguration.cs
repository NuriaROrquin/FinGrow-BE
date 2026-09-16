namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

internal sealed class EmployeeConfiguration : IEntityTypeConfiguration<Employee>
{
    public void Configure(EntityTypeBuilder<Employee> builder)
    {
        builder.ToTable("employees");

        builder.HasKey(employee => employee.Id);

        builder.Property(employee => employee.FullName)
            .HasMaxLength(Employee.MaxFullNameLength)
            .IsRequired();

        builder.Property(employee => employee.Email)
            .HasColumnName("email")
            .HasConversion(email => email.Value, value => Email.From(value))
            .HasMaxLength(Email.MaxLength)
            .IsRequired();

        builder.Property(employee => employee.PhoneNumber)
            .HasMaxLength(Employee.MaxPhoneNumberLength);

        builder.Property(employee => employee.PasswordHash)
            .HasMaxLength(200)
            .IsRequired();

        builder.Property(employee => employee.PreferredCurrency)
            .HasConversion<string>()
            .HasMaxLength(3)
            .IsRequired();

        builder.Property(employee => employee.HiredOn).IsRequired();
        builder.Property(employee => employee.IsActive).IsRequired();
        builder.Property(employee => employee.CreatedAt).IsRequired();
        builder.Property(employee => employee.UpdatedAt).IsRequired();

        // Restrict: borrar una empresa con gente adentro tiene que fallar y obligar a decidir
        // que pasa con el historial financiero de esas personas.
        builder.HasOne(employee => employee.Company)
            .WithMany()
            .HasForeignKey(employee => employee.CompanyId)
            .OnDelete(DeleteBehavior.Restrict);

        // Regla R4 del frontend, ahora sostenida por la base: un departamento con empleados
        // asignados no se puede borrar.
        builder.HasOne(employee => employee.Department)
            .WithMany()
            .HasForeignKey(employee => employee.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(employee => employee.Email).IsUnique();
        builder.HasIndex(employee => employee.CompanyId);
        builder.HasIndex(employee => employee.DepartmentId);
    }
}
