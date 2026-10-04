namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Notifications.CountUnreadNotifications;
using FinGrow.Application.Features.Notifications.ListNotifications;
using FinGrow.Application.Features.Notifications.MarkAllNotificationsAsRead;
using FinGrow.Application.Features.Notifications.MarkNotificationAsRead;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.ValueObjects;

public class NotificationInboxHandlersTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);
    private static readonly NotificationRecipient Employee = NotificationRecipient.Employee(EmployeeId);

    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task The_inbox_shows_only_the_recipient_notifications_newest_first()
    {
        var older = Store(Employee, "primera", Now.AddHours(-2));
        var newer = Store(Employee, "segunda", Now.AddHours(-1));
        Store(NotificationRecipient.Employee(Guid.CreateVersion7()), "de otro empleado", Now);
        Store(NotificationRecipient.Company(EmployeeId), "de la empresa con el mismo id", Now);

        var result = await List(new ListNotificationsQuery(Employee));

        result.Value.TotalCount.ShouldBe(2);
        result.Value.Items.Count.ShouldBe(2);
        result.Value.Items[0].Id.ShouldBe(newer.Id);
        result.Value.Items[1].Id.ShouldBe(older.Id);
    }

    [Fact]
    public async Task The_inbox_pages_and_can_show_only_the_unread()
    {
        for (var hour = 1; hour <= 3; hour++)
        {
            Store(Employee, $"alerta {hour}", Now.AddHours(-hour));
        }

        Store(Employee, "ya leida", Now.AddHours(-4)).MarkAsRead(Now);

        var secondPage = await List(new ListNotificationsQuery(Employee, PageNumber: 2, PageSize: 2));
        var unread = await List(new ListNotificationsQuery(Employee, UnreadOnly: true));

        secondPage.Value.TotalCount.ShouldBe(4);
        secondPage.Value.Items.Count.ShouldBe(2);
        secondPage.Value.Items[0].Title.ShouldBe("alerta 3");
        unread.Value.TotalCount.ShouldBe(3);
        unread.Value.Items.ShouldAllBe(notification => !notification.IsRead);
    }

    [Fact]
    public void A_page_size_out_of_range_is_rejected()
    {
        var validator = new ListNotificationsQueryValidator();

        validator.Validate(new ListNotificationsQuery(Employee, PageSize: 0)).IsValid.ShouldBeFalse();
        validator.Validate(new ListNotificationsQuery(Employee, PageSize: ListNotificationsQuery.MaxPageSize + 1)).IsValid.ShouldBeFalse();
        validator.Validate(new ListNotificationsQuery(Employee, PageNumber: 0)).IsValid.ShouldBeFalse();
        validator.Validate(new ListNotificationsQuery(Employee)).IsValid.ShouldBeTrue();
    }

    [Fact]
    public async Task The_unread_counter_ignores_what_was_already_read()
    {
        Store(Employee, "nueva", Now);
        Store(Employee, "otra nueva", Now);
        Store(Employee, "leida", Now).MarkAsRead(Now);

        var result = await new CountUnreadNotificationsHandler(_notifications)
            .Handle(new CountUnreadNotificationsQuery(Employee), CancellationToken.None);

        result.Value.Unread.ShouldBe(2);
    }

    [Fact]
    public async Task Reading_a_notification_marks_it_with_the_current_time()
    {
        var notification = Store(Employee, "nueva", Now.AddHours(-1));

        var result = await MarkAsRead(Employee, notification.Id);

        result.IsSuccess.ShouldBeTrue();
        result.Value.IsRead.ShouldBeTrue();
        result.Value.ReadAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Someone_else_notification_does_not_exist_for_the_recipient()
    {
        var notification = Store(NotificationRecipient.Employee(Guid.CreateVersion7()), "ajena", Now);

        var result = await MarkAsRead(Employee, notification.Id);

        result.IsFailure.ShouldBeTrue();
        result.Error.Type.ShouldBe(ErrorType.NotFound);
        notification.IsRead.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Reading_it_twice_does_not_save_again()
    {
        var notification = Store(Employee, "nueva", Now.AddHours(-1));
        notification.MarkAsRead(Now.AddMinutes(-30));

        var result = await MarkAsRead(Employee, notification.Id);

        result.Value.ReadAt.ShouldBe(Now.AddMinutes(-30));
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    [Fact]
    public async Task Marking_all_as_read_touches_only_the_recipient_unread_notifications()
    {
        Store(Employee, "una", Now.AddHours(-2));
        Store(Employee, "otra", Now.AddHours(-1));
        var foreign = Store(NotificationRecipient.Company(EmployeeId), "de la empresa", Now);

        var result = await MarkAll(Employee);

        result.Value.Marked.ShouldBe(2);
        _notifications.Notifications.Where(notification => notification.IsAddressedTo(Employee)).ShouldAllBe(notification => notification.IsRead);
        foreign.IsRead.ShouldBeFalse();
        _unitOfWork.SaveCount.ShouldBe(1);
    }

    [Fact]
    public async Task Marking_all_with_nothing_unread_does_not_save()
    {
        var result = await MarkAll(Employee);

        result.Value.Marked.ShouldBe(0);
        _unitOfWork.SaveCount.ShouldBe(0);
    }

    private Notification Store(NotificationRecipient recipient, string title, DateTimeOffset createdAt)
    {
        var notification = Notification.Create(
            recipient, NotificationType.TransactionsToReview, title, "texto", $"clave-{title}", createdAt);
        _notifications.Add(notification);

        return notification;
    }

    private Task<Result<PagedResult<NotificationResponse>>> List(ListNotificationsQuery query) =>
        new ListNotificationsHandler(_notifications).Handle(query, CancellationToken.None);

    private Task<Result<NotificationResponse>> MarkAsRead(NotificationRecipient recipient, Guid notificationId) =>
        new MarkNotificationAsReadHandler(_notifications, _unitOfWork, _clock)
            .Handle(new MarkNotificationAsReadCommand(recipient, notificationId), CancellationToken.None);

    private Task<Result<MarkedNotificationsResponse>> MarkAll(NotificationRecipient recipient) =>
        new MarkAllNotificationsAsReadHandler(_notifications, _unitOfWork, _clock)
            .Handle(new MarkAllNotificationsAsReadCommand(recipient), CancellationToken.None);
}
