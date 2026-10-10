namespace FinGrow.Application.Features.Receipts.AttachReceipt;

using FluentValidation;

public sealed class AttachReceiptValidator : AbstractValidator<AttachReceiptCommand>
{
    public AttachReceiptValidator()
    {
        RuleFor(command => command.ReceiptId).NotEmpty();
        RuleFor(command => command.TransactionId)
            .NotEmpty()
            .WithMessage("Hay que indicar el movimiento al que se asocia el comprobante.");
    }
}
