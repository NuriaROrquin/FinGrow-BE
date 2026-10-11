namespace FinGrow.Application.Features.Departments.UpdateDepartment;

using FluentValidation;

public sealed class UpdateDepartmentCommandValidator : AbstractValidator<UpdateDepartmentCommand>
{
    public UpdateDepartmentCommandValidator() => Include(new DepartmentDetailsValidator());
}
