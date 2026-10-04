namespace FinGrow.Domain.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public interface INotificationRepository
{
    void Add(Notification notification);

    Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<bool> ExistsSinceAsync(
        NotificationRecipient recipient,
        NotificationType type,
        string deduplicationKey,
        DateTimeOffset since,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListAsync(
        NotificationRecipient recipient,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default);

    Task<int> CountAsync(NotificationRecipient recipient, bool unreadOnly, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Notification>> ListUnreadAsync(NotificationRecipient recipient, CancellationToken cancellationToken = default);
}
