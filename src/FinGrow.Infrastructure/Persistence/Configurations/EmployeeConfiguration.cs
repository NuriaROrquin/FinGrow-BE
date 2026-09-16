namespace FinGrow.Infrastructure.Persistence.Configurations;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
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

        // Empleado de prueba para desarrollo local, colgado de la empresa de prueba de
        // CompanyConfiguration. Sirve para tener a quien emitirle un JWT mientras HU-01 no
        // este mergeada. Se borra cuando haya un flujo real de alta de empleado.
        // Password de prueba: "Password123!" (hash bcrypt real, generado con BCrypt.Net-Next
        // igual que PasswordHasher, para que el login real funcione contra este seed).
        var seedTimestamp = new DateTimeOffset(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

        builder.HasData(new
        {
            Id = Guid.Parse("22222222-2222-2222-2222-222222222222"),
            CompanyId = Guid.Parse("11111111-1111-1111-1111-111111111111"),
            DepartmentId = (Guid?)null,
            FullName = "Empleado de Desarrollo",
            Email = Email.From("empleado.dev@fingrowapp.local"),
            PhoneNumber = (string?)null,
            PasswordHash = "$2a$11$kzAiw9odq64SZtAevVr.S.OiU2TzpfH.wxL4kIatQHch0TyIA8RbG",
            PreferredCurrency = Currency.ARS,
            HiredOn = new DateOnly(2026, 1, 1),
            IsActive = true,
            LastLoginAt = (DateTimeOffset?)null,
            CreatedAt = seedTimestamp,
            UpdatedAt = seedTimestamp,
        });
    }
}
