namespace FinGrow.Application.Validators.Transactions;

using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FluentValidation;

public class CreateTransactionRequestValidator : AbstractValidator<CreateTransactionRequest>
{
    public CreateTransactionRequestValidator()
    {
        RuleFor(request => request.RequestDto.Type)
            .IsInEnum();

        RuleFor(request => request.RequestDto.Amount)
            .GreaterThan(0m)
            .WithMessage("El importe tiene que ser mayor a cero.");

        RuleFor(request => request.RequestDto.Currency)
            .IsInEnum();

        RuleFor(request => request.RequestDto.Description)
            .NotEmpty()
            .WithMessage("La descripcion es obligatoria.")
            .MaximumLength(Transaction.MaxDescriptionLength);

        RuleFor(request => request.RequestDto.OccurredOn)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha del movimiento es obligatoria.");

        RuleFor(request => request.RequestDto.PaymentMethod)
            .IsInEnum();

        When(request => request.RequestDto.Type == TransactionType.Expense, () =>
        {
            RuleFor(request => request.RequestDto.ExpenseCategory)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("La categoria de gasto es obligatoria.")
                .IsInEnum()
                .WithMessage("La categoria de gasto no es valida.");

            RuleFor(request => request.RequestDto.IncomeCategory)
                .Null()
                .WithMessage("Un gasto no puede tener categoria de ingreso.");
        });

        When(request => request.RequestDto.Type == TransactionType.Income, () =>
        {
            RuleFor(request => request.RequestDto.IncomeCategory)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("La categoria de ingreso es obligatoria.")
                .IsInEnum()
                .WithMessage("La categoria de ingreso no es valida.");

            RuleFor(request => request.RequestDto.ExpenseCategory)
                .Null()
                .WithMessage("Un ingreso no puede tener categoria de gasto.");
        });
    }
}
