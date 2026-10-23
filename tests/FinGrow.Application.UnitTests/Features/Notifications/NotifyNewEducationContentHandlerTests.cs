namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Notifications;
using FinGrow.Application.Features.Notifications.Alerts.EducationContent;
using FinGrow.Application.Features.Notifications.Delivery;
using FinGrow.Application.Interfaces;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.Repositories;
using FinGrow.Domain.ValueObjects;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

public class NotifyNewEducationContentHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 10, 18, 12, 0, 0, TimeSpan.Zero);
    private static readonly Guid FirstEmployee = Guid.CreateVersion7();
    private static readonly Guid SecondEmployee = Guid.CreateVersion7();

    private readonly FakeNotificationAudienceRepository _audience = new();
    private readonly FakeNotificationRepository _notifications = new();
    private readonly FakeNotificationTypeSettingRepository _typeSettings = new();
    private readonly ServiceProvider _provider;

    public NotifyNewEducationContentHandlerTests()
    {
        var services = new ServiceCollection();

        services.AddSingleton(typeof(ILogger<>), typeof(NullLogger<>));
        services.AddSingleton<INotificationRepository>(_notifications);
        services.AddSingleton<INotificationChannelSettingRepository>(new FakeNotificationChannelSettingRepository());
        services.AddSingleton<INotificationTypeSettingRepository>(_typeSettings);
        services.AddSingleton<IUnitOfWork>(new FakeUnitOfWork());
        services.AddSingleton<IDateTimeProvider>(new FakeDateTimeProvider(Now));
        services.AddSingleton<IEnumerable<INotificationSender>>(Array.Empty<INotificationSender>());
        services.AddScoped<Notifier>();

        _provider = services.BuildServiceProvider();
        _audience.ActiveEmployees.Add(FirstEmployee);
        _audience.ActiveEmployees.Add(SecondEmployee);
    }

    [Fact]
    public async Task A_new_course_is_announced_once_to_every_active_employee()
    {
        await Handle(EducationContentKind.Course, "Invertir desde cero");

        _notifications.Notifications.Count.ShouldBe(2);
        _notifications.Notifications.Select(notification => notification.Recipient).ShouldBe(
            new[] { NotificationRecipient.Employee(FirstEmployee), NotificationRecipient.Employee(SecondEmployee) },
            ignoreOrder: true);
        var notification = _notifications.Notifications.First();
        notification.Type.ShouldBe(NotificationType.NewEducationContent);
        notification.Title.ShouldBe(NotifyNewEducationContentHandler.Title);
        notification.Body.ShouldBe("Ya podés ver el curso \"Invertir desde cero\" en Educación.");
    }

    [Fact]
    public async Task A_new_article_says_so()
    {
        await Handle(EducationContentKind.Article, "Qué es un CEDEAR");

        _notifications.Notifications.First().Body.ShouldBe("Ya podés ver el artículo \"Qué es un CEDEAR\" en Educación.");
    }

    [Fact]
    public async Task The_same_content_is_never_announced_twice()
    {
        var published = Published(EducationContentKind.Course, "Invertir desde cero");

        await Handle(published);
        await Handle(published);

        _notifications.Notifications.Count.ShouldBe(2);
    }

    [Fact]
    public async Task Two_different_contents_are_two_alerts()
    {
        await Handle(EducationContentKind.Course, "Invertir desde cero");
        await Handle(EducationContentKind.Course, "Presupuesto básico");

        _notifications.Notifications.Count.ShouldBe(4);
    }

    [Fact]
    public async Task An_employee_who_turned_the_type_off_receives_nothing()
    {
        _typeSettings.Add(NotificationTypeSetting.Create(FirstEmployee, NotificationType.NewEducationContent, false, Now));

        await Handle(EducationContentKind.Course, "Invertir desde cero");

        _notifications.Notifications.ShouldHaveSingleItem().Recipient.ShouldBe(NotificationRecipient.Employee(SecondEmployee));
    }

    [Fact]
    public async Task Without_active_employees_nothing_is_sent()
    {
        _audience.ActiveEmployees.Clear();

        await Handle(EducationContentKind.Course, "Invertir desde cero");

        _notifications.Notifications.ShouldBeEmpty();
    }

    private static EducationContentPublished Published(EducationContentKind kind, string title) =>
        new(Guid.CreateVersion7(), kind, title, Now);

    private Task Handle(EducationContentKind kind, string title) => Handle(Published(kind, title));

    private Task Handle(EducationContentPublished published) =>
        new NotifyNewEducationContentHandler(
            _audience,
            _provider.GetRequiredService<IServiceScopeFactory>(),
            NullLogger<NotifyNewEducationContentHandler>.Instance).Handle(
            new DomainEventEnvelope<EducationContentPublished>(published),
            CancellationToken.None);
}
