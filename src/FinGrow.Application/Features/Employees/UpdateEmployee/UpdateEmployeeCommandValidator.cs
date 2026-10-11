namespace FinGrow.Application.Features.Employees.UpdateEmployee;

using FluentValidation;

public sealed class UpdateEmployeeCommandValidator : AbstractValidator<UpdateEmployeeCommand>
{
    public UpdateEmployeeCommandValidator() => Include(new EmployeeDetailsValidator());
}
