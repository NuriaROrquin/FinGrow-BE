namespace FinGrow.Domain.Events;

using FinGrow.Domain.Common;
using FinGrow.Domain.Enums;

public sealed record EducationContentPublished(
    Guid ContentId,
    EducationContentKind Kind,
    string Title,
    DateTimeOffset OccurredAt) : IDomainEvent;
