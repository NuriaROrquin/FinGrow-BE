namespace FinGrow.Application.Features.Employees.CreateEmployee;

using FluentValidation;

public sealed class CreateEmployeeCommandValidator : AbstractValidator<CreateEmployeeCommand>
{
    public CreateEmployeeCommandValidator() => Include(new EmployeeDetailsValidator());
}
