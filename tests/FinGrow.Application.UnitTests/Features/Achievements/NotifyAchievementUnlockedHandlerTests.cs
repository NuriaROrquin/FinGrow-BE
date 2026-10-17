namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.AchievementUnlocked;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.Logging.Abstractions;
using AchievementUnlocked = FinGrow.Domain.Events.AchievementUnlocked;

public class NotifyAchievementUnlockedHandlerTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationRepository _notifications = new();

    [Fact]
    public async Task An_unlocked_achievement_reaches_the_employee_inbox_with_its_name()
    {
        await Handle(AchievementCode.GoalAchieved);

        var notification = _notifications.Notifications.ShouldHaveSingleItem();
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Type.ShouldBe(NotificationType.AchievementUnlocked);
        notification.Title.ShouldBe("Logro desbloqueado: Meta cumplida");
        notification.Body.ShouldBe("Llegaste al objetivo de una meta de ahorro.");
        notification.DeduplicationKey.ShouldBe("achievement:GoalAchieved");
    }

    [Fact]
    public async Task The_same_achievement_is_announced_only_once()
    {
        await Handle(AchievementCode.FirstGoal);
        await Handle(AchievementCode.FirstGoal);

        _notifications.Notifications.ShouldHaveSingleItem();
    }

    private Task Handle(AchievementCode code)
    {
        var notifier = new Notifier(
            _notifications,
            new FakeNotificationChannelSettingRepository(),
            new FakeNotificationTypeSettingRepository(),
            Array.Empty<INotificationSender>(),
            new FakeUnitOfWork(),
            new FakeDateTimeProvider(Now),
            NullLogger<Notifier>.Instance);

        return new NotifyAchievementUnlockedHandler(notifier).Handle(
            new DomainEventEnvelope<AchievementUnlocked>(new AchievementUnlocked(Guid.CreateVersion7(), EmployeeId, code, Now)),
            CancellationToken.None);
    }
}
