namespace FinGrow.Application.DTOs;

using Domain.Entities;

public sealed record TransactionReceiptResponse(
    Guid Id,
    Guid? TransactionId,
    string ContentType,
    long SizeBytes,
    string? FileName,
    DateTimeOffset UploadedAt)
{
    public static TransactionReceiptResponse FromEntity(TransactionReceipt receipt) => new(
        receipt.Id,
        receipt.TransactionId,
        receipt.ContentType,
        receipt.SizeBytes,
        receipt.FileName,
        receipt.UploadedAt);
}
