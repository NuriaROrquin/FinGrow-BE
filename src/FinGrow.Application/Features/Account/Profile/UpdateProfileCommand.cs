namespace FinGrow.Application.Features.Account.Profile;

using FinGrow.Application.Common;
using MediatR;

public sealed record UpdateProfileCommand(
    string FullName,
    string? PhoneNumber,
    string? NationalId,
    DateOnly? BirthDate,
    string? Address) : IRequest<Result>;
