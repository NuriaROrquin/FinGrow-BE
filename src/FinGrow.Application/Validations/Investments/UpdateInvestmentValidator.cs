namespace FinGrow.Application.Validations.Investments;

using FinGrow.Application.Features.Investments.UpdateInvestment;
using Interfaces;
using Domain.Entities;
using Domain.Enums;
using FluentValidation;

public sealed class UpdateInvestmentValidator : AbstractValidator<UpdateInvestmentCommand>
{
    private static readonly string SupportedTypes = string.Join(", ", Enum.GetNames<InvestmentType>());

    public UpdateInvestmentValidator(IDateTimeProvider dateTimeProvider)
    {
        RuleFor(command => command.Id).NotEmpty();

        RuleFor(command => command.AssetName)
            .NotEmpty()
            .WithMessage("El nombre del activo es obligatorio.")
            .MaximumLength(Investment.MaxAssetNameLength)
            .WithMessage($"El nombre del activo no puede superar los {Investment.MaxAssetNameLength} caracteres.");

        RuleFor(command => command.Type)
            .IsInEnum()
            .WithMessage($"El tipo de activo no esta soportado. Los tipos validos son: {SupportedTypes}.");

        RuleFor(command => command.InvestedAmount)
            .GreaterThan(0m)
            .WithMessage("El capital invertido tiene que ser mayor a cero.");

        RuleFor(command => command.Currency)
            .IsInEnum();

        RuleFor(command => command.PurchasedOn)
            .NotEqual(default(DateOnly))
            .WithMessage("La fecha de compra es obligatoria.")
            .Must(purchasedOn => purchasedOn <= dateTimeProvider.Today)
            .WithMessage("La fecha de compra no puede estar en el futuro.");

        Include(new InvestmentTrackingValidator());
    }
}
