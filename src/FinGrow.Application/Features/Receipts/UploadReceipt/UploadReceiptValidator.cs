namespace FinGrow.Application.Features.Receipts.UploadReceipt;

using FinGrow.Domain.Entities;
using FluentValidation;

public sealed class UploadReceiptValidator : AbstractValidator<UploadReceiptCommand>
{
    public UploadReceiptValidator()
    {
        RuleFor(command => command.ContentType)
            .Must(TransactionReceipt.IsSupportedContentType)
            .WithMessage(
                $"El comprobante tiene que ser {string.Join(", ", TransactionReceipt.AllowedContentTypes)}.");

        RuleFor(command => command.Length)
            .GreaterThan(0)
            .WithMessage("El archivo esta vacio.")
            .LessThanOrEqualTo(TransactionReceipt.MaxSizeBytes)
            .WithMessage($"El comprobante no puede superar los {TransactionReceipt.MaxSizeBytes / (1024 * 1024)} MB.");
    }
}
