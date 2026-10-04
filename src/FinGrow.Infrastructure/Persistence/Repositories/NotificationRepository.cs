namespace FinGrow.Infrastructure.Persistence.Repositories;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.EntityFrameworkCore;

internal sealed class NotificationRepository(FinGrowDbContext dbContext) : INotificationRepository
{
    public void Add(Notification notification) => dbContext.Notifications.Add(notification);

    public Task<Notification?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
        dbContext.Notifications.FirstOrDefaultAsync(notification => notification.Id == id, cancellationToken);

    public Task<bool> ExistsSinceAsync(
        NotificationRecipient recipient,
        NotificationType type,
        string deduplicationKey,
        DateTimeOffset since,
        CancellationToken cancellationToken = default) =>
        AddressedTo(recipient).AnyAsync(
            notification => notification.Type == type
                && notification.DeduplicationKey == deduplicationKey
                && notification.CreatedAt >= since,
            cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListAsync(
        NotificationRecipient recipient,
        bool unreadOnly,
        int skip,
        int take,
        CancellationToken cancellationToken = default) =>
        await Filtered(recipient, unreadOnly)
            .AsNoTracking()
            .OrderByDescending(notification => notification.CreatedAt)
            .ThenByDescending(notification => notification.Id)
            .Skip(skip)
            .Take(take)
            .ToListAsync(cancellationToken);

    public Task<int> CountAsync(NotificationRecipient recipient, bool unreadOnly, CancellationToken cancellationToken = default) =>
        Filtered(recipient, unreadOnly).CountAsync(cancellationToken);

    public async Task<IReadOnlyList<Notification>> ListUnreadAsync(
        NotificationRecipient recipient,
        CancellationToken cancellationToken = default) =>
        await Filtered(recipient, unreadOnly: true).ToListAsync(cancellationToken);

    private IQueryable<Notification> Filtered(NotificationRecipient recipient, bool unreadOnly) =>
        unreadOnly
            ? AddressedTo(recipient).Where(notification => notification.ReadAt == null)
            : AddressedTo(recipient);

    private IQueryable<Notification> AddressedTo(NotificationRecipient recipient) =>
        dbContext.Notifications.Where(notification =>
            notification.RecipientType == recipient.Type && notification.RecipientId == recipient.Id);
}
