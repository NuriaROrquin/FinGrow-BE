namespace FinGrow.Api.Controllers;

using FinGrow.Api.Extensions;
using FinGrow.Application.Features.Transactions.GetHistory;
using MediatR;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/transactions")]
public sealed class TransactionsController : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> GetHistory(
        [FromServices] ISender sender,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 20,
        [FromQuery] string? search = null,
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var result = await sender.Send(
            new GetTransactionHistoryQuery(pageNumber, pageSize, search, type),
            cancellationToken);

        return result.ToActionResult();
    }
}