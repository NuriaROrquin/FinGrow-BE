namespace FinGrow.Domain.UnitTests.Entities;

using FinGrow.Domain.Entities;
using FinGrow.Domain.Enums;
using FinGrow.Domain.Events;
using FinGrow.Domain.ValueObjects;

/// <summary>Los eventos que despiertan a los logros instantaneos.</summary>
public class AchievementTriggerEventsTests
{
    private static readonly Guid EmployeeId = Guid.CreateVersion7();
    private static readonly DateTimeOffset Now = new(2026, 10, 12, 15, 0, 0, TimeSpan.Zero);
    private static readonly DateOnly Today = new(2026, 10, 12);

    [Fact]
    public void Creating_a_goal_announces_it()
    {
        var goal = Goal.Create(EmployeeId, "Vacaciones", Money.From(100_000m, Currency.ARS), Today.AddMonths(6), Now);

        var created = goal.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<GoalCreated>();
        created.GoalId.ShouldBe(goal.Id);
        created.EmployeeId.ShouldBe(EmployeeId);
    }

    [Fact]
    public void Reaching_the_target_is_announced_once_until_the_goal_falls_back()
    {
        var goal = Goal.Create(EmployeeId, "Vacaciones", Money.From(100_000m, Currency.ARS), Today.AddMonths(6), Now);
        goal.DequeueDomainEvents();

        var contribution = goal.AddContribution(Money.From(100_000m, Currency.ARS), Today, null, Now);
        goal.UpdateDetails("Vacaciones de verano", Money.From(90_000m, Currency.ARS), Today.AddMonths(6), Now.AddMinutes(1));

        goal.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<GoalAchieved>().OccurredAt.ShouldBe(Now);

        goal.RemoveContribution(contribution.Id, Now.AddMinutes(2));
        goal.AddContribution(Money.From(90_000m, Currency.ARS), Today, null, Now.AddMinutes(3));

        goal.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<GoalAchieved>();
    }

    [Fact]
    public void Only_a_new_budget_limit_is_announced()
    {
        var budget = Budget.Create(EmployeeId, BudgetPeriod.Monthly, Today, Now);

        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(50_000m, Currency.ARS), Now);
        budget.SetLimit(ExpenseCategory.Alimentos, Money.From(60_000m, Currency.ARS), Now);

        var limitSet = budget.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<BudgetLimitSet>();
        limitSet.BudgetId.ShouldBe(budget.Id);
        limitSet.EmployeeId.ShouldBe(EmployeeId);
        limitSet.Category.ShouldBe(ExpenseCategory.Alimentos);
    }

    [Fact]
    public void Linking_and_relinking_an_integration_are_announced()
    {
        var integration = EmployeeIntegration.Create(EmployeeId, IntegrationProvider.Telegram, "12345", Now);
        integration.Relink("67890", Now.AddDays(1));

        var events = integration.DequeueDomainEvents();

        events.Count.ShouldBe(2);
        events.ShouldAllBe(domainEvent => domainEvent is IntegrationLinked);
        ((IntegrationLinked)events[0]).Provider.ShouldBe(IntegrationProvider.Telegram);
    }

    [Fact]
    public void A_manual_or_imported_investment_is_announced_with_its_source()
    {
        var manual = Investment.Create(
            EmployeeId, "Plazo fijo", InvestmentType.FixedTermDeposit, Money.From(1_000m, Currency.ARS), Today, Now);
        var imported = Investment.Import(
            EmployeeId,
            InvestmentSource.Iol,
            "bcba:GGAL",
            "GGAL - Grupo Financiero Galicia",
            InvestmentType.Stock,
            "GGAL",
            10m,
            Money.From(50_000m, Currency.ARS),
            Today,
            Money.From(56_000m, Currency.ARS),
            Today,
            Now);

        manual.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<InvestmentAdded>().Source.ShouldBe(InvestmentSource.Manual);
        imported.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<InvestmentAdded>().Source.ShouldBe(InvestmentSource.Iol);
    }

    [Fact]
    public void Completing_a_lesson_is_announced()
    {
        var lessonId = Guid.CreateVersion7();

        var completion = LessonCompletion.Create(EmployeeId, lessonId, Now);

        var completed = completion.DequeueDomainEvents().ShouldHaveSingleItem().ShouldBeOfType<LessonCompleted>();
        completed.EmployeeId.ShouldBe(EmployeeId);
        completed.LessonId.ShouldBe(lessonId);
    }
}
