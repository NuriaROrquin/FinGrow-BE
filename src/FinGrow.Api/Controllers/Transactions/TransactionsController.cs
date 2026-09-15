using FinGrow.Application.DTOs.Transactions;

namespace FinGrow.Api.Controllers.Transactions;

using Extensions;
using Application.Interfaces;
using FinGrow.Application.Features.Transactions.CreateTransaction;
using FinGrow.Application.Features.Transactions.GetTransactionById;
using FinGrow.Application.Features.Transactions.ListTransactions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize]
[ApiController]
[Route("api/transactions")]
public class TransactionsController(ISender sender, ICurrentUser currentUser) : ControllerBase
{
    [HttpPost]
    public async Task<IActionResult> Create(CreateTransactionRequestDto requestDto, CancellationToken cancellationToken)
    {
        var request = new CreateTransactionRequest(currentUser.UserId!.Value, requestDto);
        var result = await sender.Send(request, cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet]
    public async Task<IActionResult> List(CancellationToken cancellationToken)
    {
        var result = await sender.Send(new ListTransactionsRequest(currentUser.UserId!.Value), cancellationToken);
        return result.ToActionResult();
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id, CancellationToken cancellationToken)
    {
        var result = await sender.Send(new GetTransactionByIdRequest(id), cancellationToken);
        return result.ToActionResult();
    }
}
