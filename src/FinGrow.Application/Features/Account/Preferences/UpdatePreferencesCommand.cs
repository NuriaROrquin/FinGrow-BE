namespace FinGrow.Application.Features.Account.Preferences;

using FinGrow.Application.Common;
using FinGrow.Domain.Enums;
using MediatR;

public sealed record UpdatePreferencesCommand(
    Theme Theme,
    Language Language,
    Currency Currency,
    DateFormat DateFormat) : IRequest<Result>;
