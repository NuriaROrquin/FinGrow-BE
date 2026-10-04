namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Errors;
using FinGrow.Domain.ValueObjects;

public class NotificationTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public void A_new_notification_is_unread_and_keeps_who_it_is_for()
    {
        var notification = Notification.Create(
            NotificationRecipient.Employee(EmployeeId),
            NotificationType.TransactionsToReview,
            "  Movimientos para revisar  ",
            "  Mercado Pago trajo movimientos nuevos.  ",
            "  transactions-to-review:MercadoPago  ",
            Now);

        notification.IsRead.ShouldBeFalse();
        notification.ReadAt.ShouldBeNull();
        notification.RecipientType.ShouldBe(NotificationRecipientType.Employee);
        notification.RecipientId.ShouldBe(EmployeeId);
        notification.Recipient.ShouldBe(NotificationRecipient.Employee(EmployeeId));
        notification.Title.ShouldBe("Movimientos para revisar");
        notification.Body.ShouldBe("Mercado Pago trajo movimientos nuevos.");
        notification.DeduplicationKey.ShouldBe("transactions-to-review:MercadoPago");
        notification.CreatedAt.ShouldBe(Now);
    }

    [Theory]
    [InlineData("", "texto", "clave")]
    [InlineData("titulo", "  ", "clave")]
    [InlineData("titulo", "texto", "")]
    public void A_notification_without_title_text_or_deduplication_criterion_is_rejected(
        string title,
        string body,
        string deduplicationKey)
    {
        Should.Throw<DomainException>(() => Notification.Create(
            NotificationRecipient.Employee(EmployeeId),
            NotificationType.TransactionsToReview,
            title,
            body,
            deduplicationKey,
            Now));
    }

    [Fact]
    public void A_title_longer_than_allowed_is_rejected()
    {
        Should.Throw<DomainException>(() => Notification.Create(
            NotificationRecipient.Employee(EmployeeId),
            NotificationType.TransactionsToReview,
            new string('a', Notification.MaxTitleLength + 1),
            "texto",
            "clave",
            Now));
    }

    [Fact]
    public void Marking_as_read_records_when_it_was_read()
    {
        var notification = NewNotification();

        notification.MarkAsRead(Now.AddMinutes(5));

        notification.IsRead.ShouldBeTrue();
        notification.ReadAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void Reading_it_again_keeps_the_first_read_time()
    {
        var notification = NewNotification();
        notification.MarkAsRead(Now.AddMinutes(5));

        notification.MarkAsRead(Now.AddHours(2));

        notification.ReadAt.ShouldBe(Now.AddMinutes(5));
    }

    [Fact]
    public void It_cannot_be_read_before_it_existed()
    {
        var notification = NewNotification();

        Should.Throw<DomainException>(() => notification.MarkAsRead(Now.AddMinutes(-1)));
    }

    [Fact]
    public void It_is_addressed_only_to_its_recipient_and_not_to_a_company_with_the_same_id()
    {
        var notification = NewNotification();

        notification.IsAddressedTo(NotificationRecipient.Employee(EmployeeId)).ShouldBeTrue();
        notification.IsAddressedTo(NotificationRecipient.Employee(Guid.CreateVersion7())).ShouldBeFalse();
        notification.IsAddressedTo(NotificationRecipient.Company(EmployeeId)).ShouldBeFalse();
    }

    [Fact]
    public void A_recipient_without_id_is_rejected()
    {
        Should.Throw<DomainException>(() => NotificationRecipient.Employee(Guid.Empty));
    }

    private static Notification NewNotification() =>
        Notification.Create(
            NotificationRecipient.Employee(EmployeeId),
            NotificationType.TransactionsToReview,
            "Movimientos para revisar",
            "Mercado Pago trajo movimientos nuevos.",
            "transactions-to-review:MercadoPago",
            Now);
}
