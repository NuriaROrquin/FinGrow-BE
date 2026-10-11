namespace FinGrow.Application.Features.Departments;

using FinGrow.Domain.Entities;
using FluentValidation;

internal interface IDepartmentDetails
{
    string Name { get; }

    string? Description { get; }

    string? ManagerName { get; }
}

internal sealed class DepartmentDetailsValidator : AbstractValidator<IDepartmentDetails>
{
    public DepartmentDetailsValidator()
    {
        RuleFor(details => details.Name)
            .Cascade(CascadeMode.Stop)
            .NotEmpty()
            .WithMessage("El nombre del departamento es obligatorio.")
            .Must(name => name.Trim().Length <= Department.MaxNameLength)
            .WithMessage($"El nombre del departamento no puede superar los {Department.MaxNameLength} caracteres.");

        RuleFor(details => details.Description)
            .Must(description => description!.Trim().Length <= Department.MaxDescriptionLength)
            .When(details => details.Description is not null)
            .WithMessage($"La descripción no puede superar los {Department.MaxDescriptionLength} caracteres.");

        RuleFor(details => details.ManagerName)
            .Must(managerName => managerName!.Trim().Length <= Department.MaxManagerNameLength)
            .When(details => details.ManagerName is not null)
            .WithMessage($"El nombre del responsable no puede superar los {Department.MaxManagerNameLength} caracteres.");
    }
}
