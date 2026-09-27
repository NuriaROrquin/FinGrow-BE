namespace FinGrow.Application.Features.Investments.DeleteInvestment;

using Common;
using MediatR;

public sealed record DeleteInvestmentCommand(Guid Id, Guid EmployeeId) : IRequest<Result>;
