namespace FinGrow.Application.Features.Receipts.GetReceiptFile;

using FinGrow.Application.Common;
using MediatR;

public sealed record GetReceiptFileQuery(Guid Id) : IRequest<Result<ReceiptFile>>;

/// <summary>Quien lo recibe es responsable de cerrar <see cref="Content"/>.</summary>
public sealed record ReceiptFile(Stream Content, string ContentType, string? FileName);
