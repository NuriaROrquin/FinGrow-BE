namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Common;
using MediatR;

public sealed record UpdateProfileCommand(string FullName, string? PhoneNumber) : IRequest<Result>;
