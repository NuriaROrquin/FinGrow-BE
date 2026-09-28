namespace FinGrow.Application.Features.Transactions.UpdateTransaction;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FluentValidation;

public sealed class UpdateTransactionValidator : AbstractValidator<UpdateTransactionCommand>
{
    public UpdateTransactionValidator()
    {
        RuleFor(command => command.Id).NotEmpty();
        RuleFor(command => command.Type).IsInEnum();
        RuleFor(command => command.Amount)
            .GreaterThan(0m)
            .WithMessage("El importe tiene que ser mayor a cero.");
        RuleFor(command => command.Currency).IsInEnum();
        RuleFor(command => command.Description)
            .NotEmpty()
            .WithMessage("La descripcion es obligatoria.")
            .MaximumLength(Transaction.MaxDescriptionLength);
        RuleFor(command => command.OccurredOn)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha del movimiento es obligatoria.");
        RuleFor(command => command.PaymentMethod).IsInEnum();
        RuleFor(command => command.Status).IsInEnum();

        When(command => command.Type == TransactionType.Expense, () =>
        {
            RuleFor(command => command.ExpenseCategory)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("La categoria de gasto es obligatoria.")
                .IsInEnum()
                .WithMessage("La categoria de gasto no es valida.");
            RuleFor(command => command.IncomeCategory)
                .Null()
                .WithMessage("Un gasto no puede tener categoria de ingreso.");
        });

        When(command => command.Type == TransactionType.Income, () =>
        {
            RuleFor(command => command.IncomeCategory)
                .Cascade(CascadeMode.Stop)
                .NotNull()
                .WithMessage("La categoria de ingreso es obligatoria.")
                .IsInEnum()
                .WithMessage("La categoria de ingreso no es valida.");
            RuleFor(command => command.ExpenseCategory)
                .Null()
                .WithMessage("Un ingreso no puede tener categoria de gasto.");
        });
    }
}