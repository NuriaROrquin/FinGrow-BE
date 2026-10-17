namespace FinGrow.Application.UnitTests.Features.Notifications;

using FinGrow.Application.Common;
using FinGrow.Application.DTOs;
using FinGrow.Application.Features.Notifications.Types.ChangeNotificationType;
using FinGrow.Application.Features.Notifications.Types.ListNotificationTypes;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;

public class NotificationTypeHandlersTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 4, 12, 0, 0, TimeSpan.Zero);

    private readonly FakeNotificationTypeSettingRepository _typeSettings = new();
    private readonly FakeUnitOfWork _unitOfWork = new();
    private readonly FakeDateTimeProvider _clock = new(Now);

    [Fact]
    public async Task Without_choices_every_alert_type_is_enabled()
    {
        var types = (await List()).Value;

        types.Count.ShouldBe(Enum.GetValues<NotificationType>().Length);
        types.ShouldAllBe(type => type.IsEnabled);
    }

    [Fact]
    public async Task Turning_a_type_off_stores_the_choice_and_the_list_reflects_it()
    {
        var result = await Change(NotificationType.GoalStalled, isEnabled: false);

        result.Value.IsEnabled.ShouldBeFalse();
        var stored = _typeSettings.Settings.ShouldHaveSingleItem();
        stored.EmployeeId.ShouldBe(EmployeeId);
        stored.UpdatedAt.ShouldBe(Now);
        _unitOfWork.SaveCount.ShouldBe(1);
        var types = (await List()).Value;
        types.Single(type => type.Type == NotificationType.GoalStalled).IsEnabled.ShouldBeFalse();
        types.Count(type => type.IsEnabled).ShouldBe(types.Count - 1);
    }

    [Fact]
    public async Task Changing_it_again_updates_the_same_choice()
    {
        await Change(NotificationType.GoalStalled, isEnabled: false);
        _clock.UtcNow = Now.AddDays(1);

        var result = await Change(NotificationType.GoalStalled, isEnabled: true);

        result.Value.IsEnabled.ShouldBeTrue();
        var stored = _typeSettings.Settings.ShouldHaveSingleItem();
        stored.IsEnabled.ShouldBeTrue();
        stored.UpdatedAt.ShouldBe(Now.AddDays(1));
    }

    [Fact]
    public async Task The_choices_of_another_employee_are_not_listed()
    {
        await new ChangeNotificationTypeHandler(_typeSettings, _unitOfWork, _clock)
            .Handle(new ChangeNotificationTypeCommand(Guid.CreateVersion7(), NotificationType.GoalStalled, false), CancellationToken.None);

        (await List()).Value.ShouldAllBe(type => type.IsEnabled);
    }

    [Fact]
    public void An_alert_type_that_does_not_exist_is_invalid()
    {
        var validator = new ChangeNotificationTypeCommandValidator();

        validator.Validate(new ChangeNotificationTypeCommand(EmployeeId, (NotificationType)99, IsEnabled: false))
            .IsValid.ShouldBeFalse();
        validator.Validate(new ChangeNotificationTypeCommand(EmployeeId, NotificationType.BudgetExceeded, IsEnabled: false))
            .IsValid.ShouldBeTrue();
    }

    private Task<Result<IReadOnlyList<NotificationTypeResponse>>> List() =>
        new ListNotificationTypesHandler(_typeSettings)
            .Handle(new ListNotificationTypesQuery(EmployeeId), CancellationToken.None);

    private Task<Result<NotificationTypeResponse>> Change(NotificationType type, bool isEnabled) =>
        new ChangeNotificationTypeHandler(_typeSettings, _unitOfWork, _clock)
            .Handle(new ChangeNotificationTypeCommand(EmployeeId, type, isEnabled), CancellationToken.None);
}
