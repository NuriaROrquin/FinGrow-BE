namespace FinGrow.Api.Contracts;

using FinGrow.Domain.Enums;

public sealed record ChangeNotificationTypeRequest(bool IsEnabled, NotificationCadence? Cadence, decimal? Threshold);
