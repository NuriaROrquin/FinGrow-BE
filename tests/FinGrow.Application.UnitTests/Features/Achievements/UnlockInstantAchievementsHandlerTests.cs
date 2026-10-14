namespace FinGrow.Application.UnitTests.Features.Achievements;

using FinGrow.Application.Events;
using FinGrow.Application.Features.Achievements;
using FinGrow.Application.Features.Achievements.Unlock;
using FinGrow.Application.UnitTests.Fakes;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using Microsoft.Extensions.Logging.Abstractions;

public class UnlockInstantAchievementsHandlerTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);

    private readonly FakeAchievementRepository _achievements = new();
    private readonly FakeAchievementProgressReadRepository _progress = new();

    private UnlockInstantAchievementsHandler Handler => new(
        new AchievementGranter(_achievements, _progress, new FakeUnitOfWork(), new FakeDateTimeProvider(Now)),
        NullLogger<UnlockInstantAchievementsHandler>.Instance);

    [Fact]
    public async Task A_confirmed_transaction_checks_the_transaction_achievements()
    {
        _progress.Reach(EmployeeId, AchievementCode.FirstTransaction, Now);
        _progress.Reach(EmployeeId, AchievementCode.ReceiptScanned, Now);

        await Handler.Handle(
            new DomainEventEnvelope<TransactionConfirmed>(new TransactionConfirmed(
                Guid.CreateVersion7(), EmployeeId, 1_500m, Currency.ARS, ExpenseCategory.Alimentos, new DateOnly(2026, 10, 12), Now)),
            CancellationToken.None);

        _achievements.Achievements.Select(achievement => achievement.Code)
            .ShouldBe(new[] { AchievementCode.FirstTransaction, AchievementCode.ReceiptScanned }, ignoreOrder: true);
    }

    [Fact]
    public async Task An_achieved_goal_only_grants_what_the_employee_actually_reached()
    {
        _progress.Reach(EmployeeId, AchievementCode.GoalAchieved, Now);

        await Handler.Handle(
            new DomainEventEnvelope<GoalAchieved>(new GoalAchieved(Guid.CreateVersion7(), EmployeeId, Now)),
            CancellationToken.None);

        _achievements.Achievements.ShouldHaveSingleItem().Code.ShouldBe(AchievementCode.GoalAchieved);
        _progress.Requests.ShouldHaveSingleItem().Codes
            .ShouldBe(new[] { AchievementCode.GoalAchieved, AchievementCode.ThreeGoalsAchieved }, ignoreOrder: true);
    }

    [Theory]
    [InlineData(nameof(GoalCreated), AchievementCode.FirstGoal)]
    [InlineData(nameof(BudgetLimitSet), AchievementCode.FirstBudget)]
    [InlineData(nameof(IntegrationLinked), AchievementCode.ChannelLinked)]
    [InlineData(nameof(InvestmentAdded), AchievementCode.FirstInvestment)]
    [InlineData(nameof(LessonCompleted), AchievementCode.FirstCourse)]
    public async Task Each_event_checks_its_own_achievement(string eventName, AchievementCode code)
    {
        _progress.Reach(EmployeeId, code, Now);

        await Raise(eventName);

        _achievements.Achievements.ShouldHaveSingleItem().Code.ShouldBe(code);
    }

    [Fact]
    public async Task A_failed_grant_does_not_break_the_other_reactions_to_the_event()
    {
        _progress.Failure = new InvalidOperationException("La base no responde.");

        await Should.NotThrowAsync(() => Handler.Handle(
            new DomainEventEnvelope<GoalCreated>(new GoalCreated(Guid.CreateVersion7(), EmployeeId, Now)),
            CancellationToken.None));

        _achievements.Achievements.ShouldBeEmpty();
    }

    private Task Raise(string eventName) =>
        eventName switch
        {
            nameof(GoalCreated) => Handler.Handle(
                new DomainEventEnvelope<GoalCreated>(new GoalCreated(Guid.CreateVersion7(), EmployeeId, Now)),
                CancellationToken.None),
            nameof(BudgetLimitSet) => Handler.Handle(
                new DomainEventEnvelope<BudgetLimitSet>(new BudgetLimitSet(Guid.CreateVersion7(), EmployeeId, ExpenseCategory.Alimentos, Now)),
                CancellationToken.None),
            nameof(IntegrationLinked) => Handler.Handle(
                new DomainEventEnvelope<IntegrationLinked>(new IntegrationLinked(Guid.CreateVersion7(), EmployeeId, IntegrationProvider.Telegram, Now)),
                CancellationToken.None),
            nameof(InvestmentAdded) => Handler.Handle(
                new DomainEventEnvelope<InvestmentAdded>(new InvestmentAdded(Guid.CreateVersion7(), EmployeeId, InvestmentSource.Iol, Now)),
                CancellationToken.None),
            nameof(LessonCompleted) => Handler.Handle(
                new DomainEventEnvelope<LessonCompleted>(new LessonCompleted(EmployeeId, Guid.CreateVersion7(), Now)),
                CancellationToken.None),
            _ => throw new ArgumentOutOfRangeException(nameof(eventName), eventName, "Evento sin caso de prueba."),
        };
}
