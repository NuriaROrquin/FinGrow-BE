namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Receipts.AttachReceipt;
using FinGrow.Application.Features.Receipts.GetReceiptFile;
using FinGrow.Application.Features.Receipts.UploadReceipt;
using FinGrow.Domain.Entities;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/receipts")]
[Authorize]
public sealed class ReceiptsController(ISender sender) : ControllerBase
{
    // Margen para los encabezados del multipart: el límite real del archivo lo valida Application.
    private const long MaxRequestBytes = TransactionReceipt.MaxSizeBytes + (64 * 1024);

    [HttpPost]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    [ProducesResponseType<TransactionReceiptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status413PayloadTooLarge)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Upload(IFormFile file, CancellationToken cancellationToken)
    {
        await using var content = file.OpenReadStream();

        var command = new UploadReceiptCommand(content, file.ContentType, file.Length, file.FileName);

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }

    [HttpGet("{id:guid}/file")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> GetFile(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetReceiptFileQuery(id), cancellationToken);

        if (result.IsFailure)
        {
            return result.ToActionResult();
        }

        Response.Headers.XContentTypeOptions = "nosniff";

        // FileStreamResult cierra el stream cuando termina de mandarlo.
        return File(result.Value.Content, result.Value.ContentType);
    }

    [HttpPost("{id:guid}/attach")]
    [ProducesResponseType<TransactionReceiptResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status403Forbidden)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Attach(Guid id, AttachReceiptCommand command, CancellationToken cancellationToken)
    {
        command = command with { ReceiptId = id };

        return (await sender.Send(command, cancellationToken)).ToActionResult();
    }
}
