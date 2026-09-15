namespace FinGrow.Application.UnitTests.Validators.Transactions;

using FinGrow.Application.DTOs.Transactions;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Validators.Transactions;
using FinGrow.Domain.Enums;

public class CreateTransactionRequestValidatorTests
{
    private readonly CreateTransactionRequestValidator _validator = new();

    private static CreateTransactionRequestDto ValidExpenseDto() => new(
        TransactionType.Expense,
        Amount: 100m,
        Currency.ARS,
        ExpenseCategory.Alimentos,
        IncomeCategory: null,
        Description: "Supermercado",
        OccurredOn: new DateOnly(2026, 9, 1),
        PaymentMethod.Cash);

    private static CreateTransactionRequest ValidExpenseRequest() =>
        new(Guid.CreateVersion7(), ValidExpenseDto());

    private static string PropertyPath(string dtoPropertyName) =>
        $"{nameof(CreateTransactionRequest.RequestDto)}.{dtoPropertyName}";

    [Fact]
    public void A_valid_expense_request_passes()
    {
        var result = _validator.Validate(ValidExpenseRequest());

        result.IsValid.ShouldBeTrue();
    }

    [Fact]
    public void A_zero_amount_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { Amount = 0m } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.Amount)));
    }

    [Fact]
    public void A_negative_amount_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { Amount = -50m } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
    }

    [Fact]
    public void An_empty_description_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { Description = "   " } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.Description)));
    }

    [Fact]
    public void An_expense_without_expense_category_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { ExpenseCategory = null } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.ExpenseCategory)));
    }

    [Fact]
    public void An_expense_with_an_income_category_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { IncomeCategory = IncomeCategory.Salario } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.IncomeCategory)));
    }

    [Fact]
    public void An_income_without_income_category_is_rejected()
    {
        var request = ValidExpenseRequest() with
        {
            RequestDto = ValidExpenseDto() with
            {
                Type = TransactionType.Income,
                ExpenseCategory = null,
                IncomeCategory = null,
            },
        };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.IncomeCategory)));
    }

    [Fact]
    public void A_missing_occurred_on_is_rejected()
    {
        var request = ValidExpenseRequest() with { RequestDto = ValidExpenseDto() with { OccurredOn = default } };

        var result = _validator.Validate(request);

        result.IsValid.ShouldBeFalse();
        result.Errors.ShouldContain(error => error.PropertyName == PropertyPath(nameof(CreateTransactionRequestDto.OccurredOn)));
    }
}
