namespace FinGrow.Application.Features.Departments.CreateDepartment;

using FluentValidation;

public sealed class CreateDepartmentCommandValidator : AbstractValidator<CreateDepartmentCommand>
{
    public CreateDepartmentCommandValidator() => Include(new DepartmentDetailsValidator());
}
